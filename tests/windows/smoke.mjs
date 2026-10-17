/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; readability revision; 2026-10-11
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import assert from "node:assert/strict";
import { chromium } from "playwright";
import { eventually, closeSettings } from "../ui/waits.mjs";
import { spawn } from "node:child_process";
import { mkdir, readFile, writeFile, stat, symlink, access } from "node:fs/promises";
import { resolve, join } from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const artifacts = resolve(process.env.MARKVIBE_WINDOWS_ARTIFACTS_DIR || "artifacts/windows");
const fixture = join(artifacts, "workspace");
const profile = join(artifacts, "profile-" + Date.now());
const exports = join(artifacts, "exports");
const executable = resolve(process.env.MARKVIBE_WINDOWS_EXECUTABLE || "src/MarkVibe.Windows/bin/Debug/net10.0-windows10.0.19041.0/win-x64/MarkVibe.Windows.exe");
const port = Number(process.env.MARKVIBE_WINDOWS_DEBUG_PORT || 9321);
const source = "# Windows reading fixture — Caffè\n\nA stable paragraph for a handwritten note.\n\n[Next page](next.md#details)\n\n" +
  "![Local diagram](image.svg)\n\n```mermaid\nflowchart LR\n Markdown --> Ink\n Ink --> PDF\n```\n\n## Details\n\nUnicode text: perché, città, più.\n";
await mkdir(fixture, { recursive: true });
await mkdir(exports, { recursive: true });
await writeFile(join(fixture, "README.md"), source);
await writeFile(join(fixture, "next.md"), "# Next page\n\n[Back](README.md)\n\n## Details\n\nA second local document.");
await writeFile(join(fixture, "image.svg"), '<svg xmlns="http://www.w3.org/2000/svg" width="360" height="100"><rect width="360" height="100" fill="#eef0ff"/><text x="18" y="55" font-size="22" fill="#2458c7">Local image in native PDF</text></svg>');
// A junction proves that native file access does not follow an in-root escape.
const outside = join(artifacts, "outside");
await mkdir(outside, { recursive: true });
await writeFile(join(outside, "secret.md"), "Synthetic outside data");
const junction = join(fixture, "escape");
try {
  await symlink(outside, junction, "junction");
} catch (error) {
  if (error.code !== "EEXIST") {
    throw error;
  }
}

let child;
let browser;
let page;
const results = [];
const errors = [];
const failedAssets = [];
const remote = [];
/** Start the synthetic isolated desktop fixture with explicit smoke and export destinations. */
async function launch(open = false) {
  await access(executable);
  const args = ["--smoke-test", "--data-dir", profile, "--export-dir", exports, "--debug-port", String(port)];
  if (open) {
    args.push("--open", join(fixture, "README.md"));
  }
  child = spawn(executable, args, { windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
  let output = "";
  let launchError;
  child.stdout.on("data", bytes => {
    output += bytes;
  });
  child.stderr.on("data", bytes => {
    output += bytes;
  });
  child.on("error", error => {
    launchError = error;
    output += error.message;
  });
  for (let attempt = 0; attempt < 90; attempt++) {
    if (launchError) { throw launchError; }
    if (child.exitCode !== null) {
      throw new Error("Native host exited: " + child.exitCode + " " + output);
    }
    try {
      browser = await chromium.connectOverCDP("http://127.0.0.1:" + port);
      break;
    } catch {
      await delay(500);
    }
  }
  if (!browser) {
    throw new Error("WebView2 debugging endpoint was not ready: " + output);
  }
  for (let attempt = 0; attempt < 90; attempt++) {
    page = browser.contexts().flatMap(context => context.pages()).find(candidate => candidate.url().startsWith("https://markvibe.local/index.html"));
    if (page) {
      break;
    }
    await delay(500);
  }
  if (!page) {
    throw new Error("Native app page was not created.");
  }
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => {
    if (message.type() === "error") {
      errors.push(message.text());
    }
  });
  page.on("response", response => {
    if (response.status() >= 400) {
      failedAssets.push(response.status() + " " + response.url());
    }
  });
  page.on("request", request => {
    if (/^https?:/u.test(request.url()) && !request.url().startsWith("https://markvibe.local/")) {
      remote.push(request.url());
    }
  });
  await page.evaluate(() => {
    const saved = JSON.parse(localStorage.getItem("markvibe.preferences") || "{}");
    localStorage.setItem("markvibe.preferences", JSON.stringify({ version: 1, values: { ...saved.values, language: "en" } }));
  });
  await page.reload();
  await page.locator(".reader h1").waitFor({ timeout: 90000 });
}
/** Close the desktop fixture and release its automation resources. */
async function stop() {
  if (browser) {
    await browser.close();
    browser = null;
  }
  if (child?.pid && child.exitCode === null) {
    child.kill();
    await eventually(async () => assert.notEqual(child.exitCode, null), 10000);
  }
  child = null;
  await delay(800);
}
/** Run a named desktop scenario and retain its observed result and failure details. */
async function check(name, run) {
  try {
    await run();
    results.push({ name, passed: true });
    console.log("PASS " + name);
  }
  catch (error) {
    results.push({ name, passed: false, error: error.stack });
    await page?.screenshot({ path: join(artifacts, "failure-" + results.length + ".png") }).catch(() => {
    });
    throw error;
  }
}
/** Send a fixture request through the real desktop bridge and await its matching response. */
async function native(operation, args) {
  return page.evaluate(({ operation, args }) => new Promise((resolve, reject) => {
    const id = crypto.randomUUID();
    const timeout = setTimeout(() => {
      window.chrome.webview.removeEventListener("message", receive);
      reject(new Error("Native request timed out: " + operation));
    }, 15000);
    /** Resolve the fixture bridge request when its matching native response arrives. */
    const receive = event => {
      if (event.data?.id !== id) {
        return;
      }
      clearTimeout(timeout);
      window.chrome.webview.removeEventListener("message", receive);
      resolve(event.data);
    };
    window.chrome.webview.addEventListener("message", receive);
    window.chrome.webview.postMessage({ version: 1, capability: window.markvibeDesktop.capability, id, operation, args });
  }), { operation, args });
}
/** Read the synthetic document path currently displayed by the application. */
async function currentPath(path) {
  await page.waitForFunction(path => [...document.querySelectorAll(".mv-dom-host")].some(host => host.shadowRoot?.querySelector(".document-meta .eyebrow")?.textContent === path), path);
}
/** Observe the persisted card dimensions rather than relying on transient status messages. */
async function savedMapWidth(width) {
  await eventually(async () => {
    const layout = JSON.parse(await readFile(join(fixture, ".markvibe-map.json"), "utf8"));
    assert.equal(layout.schemaVersion, 4);
    assert.equal(layout.nodes["README.md"].width, width);
  });
}

try {
  await launch(true);
  await check("cold launch opens native Markdown and adjacent local resources", async () => {
    await currentPath("README.md");
    assert.match(await page.locator(".reader h1").innerText(), /Caffè/u);
    await page.locator(".reader .mv-diagram svg").waitFor();
    await page.locator(".reader img[src^=blob]").waitFor();
    assert.equal(await page.evaluate(() => window.markvibeDesktop.version), 1);
    assert.equal(await page.evaluate(() => new URLSearchParams(location.hash.slice(1)).get("doc")), "README.md");
  });
  const session = await native("initialize", {});
  const workspaceId = session.value.session?.entry.workspaceId || JSON.parse(await page.evaluate(() => localStorage.getItem("markvibe.lastDocument"))).workspaceId;
  await check("native bridge rejects traversal, junction escapes and unknown operations", async () => {
    for (const path of ["../outside/secret.md", "escape/secret.md", "README.md:stream", "CON.md"]) {
      const response = await native("workspace.read", { workspaceId, path });
      assert.equal(response.ok, false, path);
    }
    assert.equal((await native("shell.run", { command: "unused" })).code, "unknown-action");
    const entries = (await native("workspace.entries", { workspaceId })).value;
    assert.ok(entries.every(entry => !entry.path.startsWith("escape/")));
  });
  await check("card resize autosaves a layout without modifying the source document", async () => {
    const aiStatus = await native("ai.connection.status", { provider: "api-key" });
    assert.equal(aiStatus.value.configured, false, "Smoke mode must isolate real credentials");
    await page.locator("[dataid=MapButton]").click();
    const grip = page.getByRole("button", { name: "Resize Windows reading fixture — Caffè", exact: true });
    await grip.waitFor();
    await grip.focus();
    await page.keyboard.press("ArrowRight");
    await savedMapWidth(270);
    assert.equal(await readFile(join(fixture, "README.md"), "utf8"), source);
    await page.screenshot({ path: join(artifacts, "native-map.png") });
  });
  await check("single click selects a map card and double click opens its document", async () => {
    const card = page.getByRole("button", { name: "Select Next page, next.md", exact: true });
    await card.click();
    await eventually(async () => assert.equal(await card.getAttribute("aria-pressed"), "true"));
    assert.equal(await page.evaluate(() => new URLSearchParams(location.hash.slice(1)).get("doc")), "README.md");
    await card.dblclick();
    await currentPath("next.md");
  });
  await check("native HTML and PDF save Unicode, local images and diagrams", async () => {
    await page.locator(".reader a").filter({ hasText: "Back" }).click();
    await currentPath("README.md");
    await page.locator("[dataid=ExportHtmlButton]").click();
    await eventually(async () => {
      const html = await readFile(join(exports, "README.html"), "utf8");
      assert.match(html, /Caffè/u);
      assert.match(html, /data:image\/svg/u);
      assert.ok(!html.includes("<script"));
    });
    await page.locator("[dataid=ExportPdfButton]").click();
    await eventually(async () => {
      const pdf = await readFile(join(exports, "README.pdf"));
      assert.equal(pdf.subarray(0, 5).toString(), "%PDF-");
      assert.ok((await stat(join(exports, "README.pdf"))).size > 1000);
    }, 60000);
  });
  await check("appearance and native navigation persist without settings panel assumptions", async () => {
    await page.locator("[dataid=SettingsButton]").click();
    await page.locator("[dataid=AppearanceCombo]").click();
    await page.getByText("Dark", { exact: true }).last().click();
    await page.waitForFunction(() => document.documentElement.dataset.theme === "dark");
    await page.locator("[dataid=UiFontCombo]").waitFor();
    await closeSettings(page);
    await page.locator(".reader a").filter({ hasText: "Next page" }).click();
    await currentPath("next.md");
    await eventually(async () => assert.equal((await native("initialize", {})).value.session.entry.path, "next.md"));
  });
  await stop();
  await launch(false);
  await check("cold restart restores the document, appearance and saved card size", async () => {
    await currentPath("next.md");
    assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "dark");
    await page.locator("[dataid=MapButton]").click();
    await page.getByRole("button", { name: "Resize Windows reading fixture — Caffè", exact: true }).waitFor();
    await savedMapWidth(270);
  });
  await check("external map changes are preserved and save failures remain visible", async () => {
    const mapPath = join(fixture, ".markvibe-map.json");
    const external = (await readFile(mapPath, "utf8")) + "\n";
    await writeFile(mapPath, external);
    await page.getByRole("button", { name: "Resize Windows reading fixture — Caffè", exact: true }).focus();
    await page.keyboard.press("ArrowRight");
    await page.locator("[dataid=MapNoticeText]").filter({ hasText: "changed on disk" }).waitFor();
    assert.equal(await readFile(mapPath, "utf8"), external);
    assert.equal(await page.evaluate(() => window.markvibe.canClose()), false);
  });

  assert.deepEqual(errors, []);
  assert.deepEqual(failedAssets, []);
  assert.deepEqual(remote, []);
} catch (error) {
  console.error(error);
  process.exitCode = 1;
}
finally {
  await writeFile(join(artifacts, "results.json"), JSON.stringify({ runtime: browser?.version(), results, errors, failedAssets, remote }, null, 2));
  await stop();
}
console.log(`${results.filter(result => result.passed).length}/${results.length} Windows checks passed.`);
