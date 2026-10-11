import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { spawn } from "node:child_process";
import { mkdir, readFile, writeFile, stat, symlink } from "node:fs/promises";
import { resolve, join } from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.MARKVIBE_PLAYWRIGHT_PATH || "playwright");
const artifacts = resolve(process.env.MARKVIBE_WINDOWS_ARTIFACTS_DIR || "artifacts/windows");
const fixture = join(artifacts, "workspace"), profile = join(artifacts, "profile-" + Date.now()), exports = join(artifacts, "exports");
const executable = resolve("src/MarkVibe.Windows/bin/Debug/net10.0-windows/MarkVibe.Windows.exe");
const port = Number(process.env.MARKVIBE_WINDOWS_DEBUG_PORT || 9321);
const source = "# Windows reading fixture — Caffè\n\nA stable paragraph for a handwritten note.\n\n[Next page](next.md#details)\n\n" +
  "![Local diagram](image.svg)\n\n```mermaid\nflowchart LR\n Markdown --> Ink\n Ink --> PDF\n```\n\n## Details\n\nUnicode text: perché, città, più.\n";
await mkdir(fixture, { recursive: true }); await mkdir(exports, { recursive: true });
await writeFile(join(fixture, "README.md"), source); await writeFile(join(fixture, "next.md"), "# Next page\n\n[Back](README.md)\n\n## Details\n\nA second local document.");
await writeFile(join(fixture, "image.svg"), '<svg xmlns="http://www.w3.org/2000/svg" width="360" height="100"><rect width="360" height="100" fill="#eef0ff"/><text x="18" y="55" font-size="22" fill="#2458c7">Local image in native PDF</text></svg>');
// A junction proves that native file access does not follow an in-root escape.
const outside = join(artifacts, "outside"); await mkdir(outside, { recursive: true }); await writeFile(join(outside, "secret.md"), "Synthetic outside data");
const junction = join(fixture, "escape");
try { await symlink(outside, junction, "junction"); } catch (error) { if (error.code !== "EEXIST") throw error; }
// Every run gets fresh ink without removing other test artifacts.
try { await writeFile(join(fixture, "README.md.ink.json"), "", { flag: "wx" }); } catch { }
const firstInk = { schemaVersion: 1, document: { path: "README.md", hashAlgorithm: "sha256-utf8-exact", contentHash: (await import("node:crypto")).createHash("sha256").update(source).digest("hex") }, annotations: [] };
await writeFile(join(fixture, "README.md.ink.json"), JSON.stringify(firstInk));

let child, browser, page; const results = [], errors = [], failedAssets = [], remote = [];
async function launch(open = false) {
  const args = ["--smoke-test", "--data-dir", profile, "--export-dir", exports, "--debug-port", String(port)];
  if (open) args.push("--open", join(fixture, "README.md"));
  child = spawn(executable, args, { windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
  let output = ""; child.stdout.on("data", bytes => { output += bytes; }); child.stderr.on("data", bytes => { output += bytes; });
  child.on("error", error => { output += error.message; });
  for (let attempt = 0; attempt < 90; attempt++) {
    if (child.exitCode !== null) throw new Error("Native host exited: " + child.exitCode + " " + output);
    try { browser = await chromium.connectOverCDP("http://127.0.0.1:" + port); break; } catch { await delay(500); }
  }
  if (!browser) throw new Error("WebView2 debugging endpoint was not ready: " + output);
  for (let attempt = 0; attempt < 90; attempt++) {
    page = browser.contexts().flatMap(context => context.pages()).find(candidate => candidate.url().startsWith("https://markvibe.local/index.html"));
    if (page) break; await delay(500);
  }
  if (!page) throw new Error("Native app page was not created.");
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
  page.on("response", response => { if (response.status() >= 400) failedAssets.push(response.status() + " " + response.url()); });
  page.on("request", request => { if (/^https?:/u.test(request.url()) && !request.url().startsWith("https://markvibe.local/")) remote.push(request.url()); });
  await page.waitForFunction(() => [...document.querySelectorAll(".mv-dom-host")].some(host => host.shadowRoot?.querySelector(".ink-toolbar")), {}, { timeout: 90000 });
}
async function stop() {
  if (browser) { await browser.close(); browser = null; }
  if (child && child.exitCode === null) { const exited = new Promise(resolve => child.once("exit", resolve)); child.kill(); await exited; }
  child = null; await delay(800);
}
async function check(name, run) {
  try { await run(); results.push({ name, passed: true }); console.log("PASS " + name); }
  catch (error) { results.push({ name, passed: false, error: error.stack }); await page?.screenshot({ path: join(artifacts, "failure-" + results.length + ".png") }).catch(() => {}); throw error; }
}
async function native(operation, args) {
  return page.evaluate(({ operation, args }) => new Promise(resolve => {
    const id = crypto.randomUUID();
    const receive = event => { if (event.data?.id !== id) return; window.chrome.webview.removeEventListener("message", receive); resolve(event.data); };
    window.chrome.webview.addEventListener("message", receive);
    window.chrome.webview.postMessage({ version: 1, capability: window.markvibeDesktop.capability, id, operation, args });
  }), { operation, args });
}
async function currentPath(path) { await page.waitForFunction(path => [...document.querySelectorAll(".mv-dom-host")].some(host => host.shadowRoot?.querySelector(".document-meta .eyebrow")?.textContent === path), path); }
async function saved() { await page.waitForFunction(() => [...document.querySelectorAll(".mv-dom-host")].some(host => host.shadowRoot?.querySelector(".ink-status")?.textContent === "Ink saved")); }

try {
  await launch(true);
  await check("cold launch opens native Markdown and adjacent local resources", async () => {
    await currentPath("README.md"); assert.match(await page.locator(".reader h1").innerText(), /Caffè/u);
    await page.locator(".reader .mv-diagram svg").waitFor(); await page.locator(".reader img[src^=blob]").waitFor();
    assert.equal(await page.evaluate(() => window.markvibeDesktop.version), 1);
    assert.equal(await page.evaluate(() => new URLSearchParams(location.hash.slice(1)).get("doc")), "README.md");
  });
  const session = await native("initialize", {}); const workspaceId = session.value.session?.entry.workspaceId || JSON.parse(await page.evaluate(() => localStorage.getItem("markvibe.lastDocument"))).workspaceId;
  await check("native bridge rejects traversal, junction escapes and unknown operations", async () => {
    for (const path of ["../outside/secret.md", "escape/secret.md", "README.md:stream", "CON.md"]) { const response = await native("workspace.read", { workspaceId, path }); assert.equal(response.ok, false, path); }
    assert.equal((await native("shell.run", { command: "unused" })).code, "unknown-action");
    const entries = (await native("workspace.entries", { workspaceId })).value; assert.ok(entries.every(entry => !entry.path.startsWith("escape/")));
  });
  await check("pointer ink autosaves a separate sidecar, with undo and redo", async () => {
    await page.getByRole("button", { name: "Annotate", exact: true }).click();
    await page.locator(".reader section[data-block-id]").filter({ hasText: "A stable paragraph for a handwritten note." }).getByRole("button", { name: "Add handwritten note for this paragraph", exact: true }).click();
    const band = page.locator(".ink-band").first();
    const bounds = await band.locator("svg").boundingBox();
    await page.mouse.move(bounds.x + bounds.width * .1, bounds.y + bounds.height * .3); await page.mouse.down();
    await page.mouse.move(bounds.x + bounds.width * .6, bounds.y + bounds.height * .6, { steps: 14 }); await page.mouse.up(); await saved();
    let ink = JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8")); assert.equal(ink.annotations.length, 1); assert.ok(ink.annotations[0].strokes[0].points.length >= 2);
    assert.equal(await readFile(join(fixture, "README.md"), "utf8"), source);
    await page.getByRole("button", { name: "Undo", exact: true }).click(); await saved();
    assert.equal(JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8")).annotations.length, 0);
    await page.getByRole("button", { name: "Redo", exact: true }).click(); await saved();
    ink = JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8")); assert.equal(ink.annotations.length, 1);
    await page.getByRole("button", { name: "Annotate", exact: true }).click();
    await page.screenshot({ path: join(artifacts, "native-reader-ink.png") });
  });
  await check("rapid consecutive strokes queue during autosave without losing ink", async () => {
    await page.getByRole("button", { name: "Pen", exact: true }).click();
    const box = await page.locator(".ink-band svg").first().boundingBox();
    for (let stroke = 0; stroke < 3; stroke++) {
      const y = box.y + box.height * (.15 + stroke * .12);
      await page.mouse.move(box.x + box.width * .2, y); await page.mouse.down();
      await page.mouse.move(box.x + box.width * .4, y + 4); await page.mouse.up();
    }
    await page.waitForFunction(() => window.markvibe.canClose()); await saved();
    assert.equal(JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8")).annotations[0].strokes.length, 4);
    for (let undo = 0; undo < 3; undo++) { await page.getByRole("button", { name: "Undo", exact: true }).click(); await saved(); }
    assert.equal(JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8")).annotations[0].strokes.length, 1);
    await page.getByRole("button", { name: "Annotate", exact: true }).click();
  });
  await check("native HTML and PDF save directly with image, diagram and vector ink", async () => {
    await page.locator("[dataid=SettingsButton]").click(); await page.locator("[dataid=ExportHtmlButton]").click();
    await page.waitForFunction(() => document.body.innerText.includes("Saved README.html"));
    const html = await readFile(join(exports, "README.html"), "utf8"); assert.match(html, /ink-annotation/u); assert.match(html, /data:image\/svg/u); assert.ok(!html.includes("<script"));
    await page.locator("[dataid=ExportPdfButton]").click(); await page.waitForFunction(() => document.body.innerText.includes("Saved README.pdf"), {}, { timeout: 60000 });
    const pdf = await readFile(join(exports, "README.pdf")); assert.equal(pdf.subarray(0, 5).toString(), "%PDF-"); assert.ok((await stat(join(exports, "README.pdf"))).size > 10000);
    await page.locator("[dataid=CloseSettingsButton]").click();
  });
  await check("pen samples retain pressure, highlighter strokes and eraser behavior", async () => {
    await page.getByRole("button", { name: "Highlight", exact: true }).click();
    const box = await page.locator(".ink-band svg").first().boundingBox();
    const session = await page.context().newCDPSession(page);
    const x = box.x + box.width * .2, y = box.y + box.height * .7;
    await session.send("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", buttons: 1, pointerType: "pen", force: .4 });
    await session.send("Input.dispatchMouseEvent", { type: "mouseMoved", x: x + 120, y: y + 10, button: "left", buttons: 1, pointerType: "pen", force: .8 });
    await session.send("Input.dispatchMouseEvent", { type: "mouseReleased", x: x + 160, y: y + 15, button: "left", buttons: 0, pointerType: "pen", force: 0 });
    await saved(); await session.detach();
    const ink = JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8"));
    assert.equal(ink.annotations[0].strokes.length, 2); assert.equal(ink.annotations[0].strokes[1].tool, "highlighter");
    assert.ok(ink.annotations[0].strokes[1].points.some(point => Math.abs(point.pressure - .8) < .01));
    await page.getByRole("button", { name: "Erase", exact: true }).click();
    await page.locator('.ink-band path[data-stroke="1"]').click({ force: true }); await saved();
    assert.equal(JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8")).annotations[0].strokes.length, 1);
    await page.getByRole("button", { name: "Annotate", exact: true }).click();
  });
  await check("dark theme keeps ink visible and native navigation remembers the selected document", async () => {
    await page.locator("[dataid=SettingsButton]").click();
    await page.locator("[dataid=AppearanceCombo]").click(); await page.getByText("Dark", { exact: true }).last().click();
    await page.waitForFunction(() => document.documentElement.dataset.theme === "dark"); await page.locator("[dataid=CloseSettingsButton]").click();
    assert.equal(await page.locator(".ink-band path").count() > 0, true); await page.screenshot({ path: join(artifacts, "native-dark-ink.png") });
    await page.locator(".reader a").filter({ hasText: "Next page" }).click(); await currentPath("next.md");
    await page.waitForTimeout(500); const remembered = (await native("initialize", {})).value.session; assert.equal(remembered.entry.path, "next.md");
  });
  await stop(); await launch(false);
  await check("cold restart restores the last native document and ink reopens on return", async () => {
    await currentPath("next.md"); assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "dark");
    await page.locator(".reader a").filter({ hasText: "Back" }).click(); await currentPath("README.md");
    await page.locator(".ink-band path").first().waitFor(); assert.equal(await page.getByRole("button", { name: "Save ink", exact: true }).isDisabled(), true);
  });
  await check("external sidecar changes are preserved and unsaved ink can be exported", async () => {
    const sidecar = join(fixture, "README.md.ink.json"); const original = await readFile(sidecar, "utf8"); await writeFile(sidecar, original + "\n");
    await page.getByRole("button", { name: "Annotate", exact: true }).click();
    const bounds = await page.locator(".ink-band svg").first().boundingBox();
    await page.mouse.move(bounds.x + 10, bounds.y + 10); await page.mouse.down(); await page.mouse.move(bounds.x + 60, bounds.y + 50); await page.mouse.up();
    await page.getByText("The annotation file changed on disk.", { exact: false }).first().waitFor(); assert.equal(await readFile(sidecar, "utf8"), original + "\n");
    assert.equal(await page.evaluate(() => window.markvibe.canClose()), false);
    await page.getByRole("button", { name: "Export ink", exact: true }).click();
    await page.getByText("Annotation backup exported", { exact: false }).first().waitFor();
    assert.ok(JSON.parse(await readFile(join(exports, "README.md.ink.json"), "utf8")).annotations.length >= 1);
    page.once("dialog", dialog => dialog.accept());
    await page.getByRole("button", { name: "Discard edits", exact: true }).click();
    await saved(); assert.equal(await page.evaluate(() => window.markvibe.canClose()), true);
  });
  await check("changed Markdown requires explicit ink review and exports a multipage native PDF", async () => {
    const expanded = source + "\n" + Array.from({ length: 36 }, (_, i) => `## Additional section ${i + 1}\n\nThis synthetic paragraph verifies native PDF pagination, stable text and margins. Local files remain on this device and handwritten notes stay separate from Markdown.\n`).join("\n");
    const originalInk = await readFile(join(fixture, "README.md.ink.json"), "utf8");
    await writeFile(join(fixture, "README.md"), expanded);
    await page.locator("[dataid=SettingsButton]").click();
    await page.locator("[dataid=ExportPdfButton]").hover();
    for (let tick = 0; tick < 18; tick++) { await page.mouse.wheel(0, 200); await page.waitForTimeout(25); }
    await page.locator("[dataid=RefreshButton]").click();
    await page.locator("[dataid=SettingsButton]").click();
    await page.getByRole("button", { name: "Use current document", exact: true }).waitFor();
    assert.equal(await page.getByRole("button", { name: "Annotate", exact: true }).isDisabled(), true);
    assert.equal(await readFile(join(fixture, "README.md.ink.json"), "utf8"), originalInk);
    await page.getByRole("button", { name: "Use current document", exact: true }).click(); await saved();
    const ink = JSON.parse(await readFile(join(fixture, "README.md.ink.json"), "utf8"));
    assert.equal(ink.document.contentHash, (await import("node:crypto")).createHash("sha256").update(expanded).digest("hex"));
    await page.locator("[dataid=SettingsButton]").click();
    const panel = await page.locator("[dataid=SettingsPanel]").boundingBox(); await page.mouse.move(panel.x + panel.width / 2, panel.y + panel.height / 2);
    for (let tick = 0; tick < 18; tick++) { await page.mouse.wheel(0, -200); await page.waitForTimeout(25); }
    await page.locator("[dataid=ExportPdfButton]").click();
    await page.waitForFunction(() => document.body.innerText.includes("Saved README.pdf"), {}, { timeout: 60000 });
  });
  await check("switching to a browser workspace does not restore stale native session metadata", async () => {
    const panel = await page.locator("[dataid=SettingsPanel]").boundingBox(); await page.mouse.move(panel.x + panel.width / 2, panel.y + panel.height / 2);
    for (let tick = 0; tick < 24; tick++) { await page.mouse.wheel(0, 200); await page.waitForTimeout(25); }
    await page.getByText("Explore sample workspace", { exact: true }).click();
    await page.waitForFunction(() => JSON.parse(localStorage.getItem("markvibe.lastWorkspace"))?.mode === "demo");
    assert.equal((await native("initialize", {})).value.session, null);
    await stop(); await launch(false);
    assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem("markvibe.lastWorkspace")).mode), "demo");
    assert.equal((await native("initialize", {})).value.session, null);
  });
  assert.deepEqual(errors, []); assert.deepEqual(failedAssets, []); assert.deepEqual(remote, []);
} catch (error) { console.error(error); process.exitCode = 1; }
finally { await writeFile(join(artifacts, "results.json"), JSON.stringify({ runtime: browser?.version(), results, errors, failedAssets, remote }, null, 2)); await stop(); }
console.log(`${results.filter(result => result.passed).length}/${results.length} Windows checks passed.`);
