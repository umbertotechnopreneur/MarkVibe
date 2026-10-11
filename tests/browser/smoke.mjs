import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { mkdir, writeFile, mkdtemp, readFile } from "node:fs/promises";
import { resolve, join } from "node:path";
import { tmpdir } from "node:os";
import { zipSync, strToU8 } from "fflate";

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.MARKVIBE_PLAYWRIGHT_PATH || "playwright");
const base = process.env.MARKVIBE_BASE_URL || "http://localhost:5080/";
const artifacts = resolve(process.env.MARKVIBE_ARTIFACTS_DIR || "artifacts/browser");
const browser = await chromium.launch({ channel: process.env.MARKVIBE_BROWSER_CHANNEL || "msedge", headless: true });
const results = [];
await mkdir(artifacts, { recursive: true });

// page: an isolated browser page containing only synthetic documents.
async function ready(page) {
  await page.goto(base, { waitUntil: "networkidle" });
  await page.waitForFunction(() => document.body.innerText.includes("source lines"), {}, { timeout: 30000 });
  await page.getByRole("document", { name: "Rendered Markdown document" }).waitFor({ state: "visible" });
  assert.ok(await page.locator(".reader h1").isVisible(), "The reader must contain visible document content.");
}

// page: the current app page.
// name: an English XAML toolbar label. OpenSilver handles shell focus internally.
async function view(page, name) {
  await page.getByText(name, { exact: true }).first().click();
}

// page: the current app page.
// path: a workspace-relative document identity, not an operating-system path.
async function documentIs(page, path) {
  await page.waitForFunction(expected => {
    const hosts = [...document.querySelectorAll(".mv-dom-host")];
    return hosts.some(host => host.shadowRoot?.querySelector(".document-meta .eyebrow")?.textContent === expected);
  }, path);
  assert.equal(new URLSearchParams(new URL(page.url()).hash.slice(1)).get("doc"), path);
}

async function check(name, run) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const errors = [], failures = [], remote = [];
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
  page.on("response", response => { if (response.status() >= 400) failures.push(response.status() + " " + response.url()); });
  page.on("request", request => {
    if (/^https?:/u.test(request.url()) && new URL(request.url()).origin !== new URL(base).origin) remote.push(request.url());
  });
  try {
    await run(page, context);
    assert.deepEqual(errors, [], "Unexpected browser errors");
    assert.deepEqual(failures, [], "Failed HTTP assets");
    assert.deepEqual(remote, [], "Document content must not initiate remote requests");
    results.push({ name, passed: true });
    console.log("PASS " + name);
  } catch (error) {
    await page.screenshot({ path: join(artifacts, "failure-" + results.length + ".png"), fullPage: true });
    results.push({ name, passed: false, error: error.message, errors, failures, remote });
    console.error("FAIL " + name + ": " + error.message);
  } finally { await context.close(); }
}

const synthetic = "# Synthetic reading fixture\n\n[Second page](second.md#repeat-1)\n\n" +
  "## Repeat\n\nFirst heading.\n\n## Repeat\n\nSecond heading.\n\n" +
  Array.from({ length: 60 }, (_, index) => "## Section " + index + "\n\nA paragraph with stable source locations and enough content to scroll.\n").join("\n");
const second = "# Second page\n\n[Home](README.md)\n\n## Repeat\n\nOne.\n\n## Repeat\n\nTwo.\n\n[Missing](missing.md)\n";
const exportFixture = "# Export fixture — Caffè e qualità\n\nA local, portable document with Unicode text: perché, città, più.\n\n" +
  "[Jump to details](#details) · [Other document](second.md) · [External reference](https://example.com/)\n\n" +
  "## Details\n\n| Format | Result |\n| --- | --- |\n| HTML | Standalone local document |\n| PDF | Document-only print view |\n\n" +
  "```csharp\nvar message = \"Caffè e qualità\";\nConsole.WriteLine(message);\n```\n\n" +
  "```mermaid\nflowchart LR\n Markdown --> HTML\n HTML --> PDF\n```\n\n" +
  Array.from({ length: 7 }, (_, index) => "## Reading section " + (index + 1) + "\n\n" +
    ("This paragraph exercises page breaks, readable margins and stable typography. Local documents stay on this device. ".repeat(5)) + "\n\n").join("") +
  "## Local images below the fold\n\n![Local vector](vector.svg)\n\n![Local raster](pixel.png)\n\n" +
  "![Blocked remote](https://example.com/remote.png)\n\n![Missing local](missing.svg)\n\n<script>window.exportAttack=true</script>\n";
const exportFiles = () => [
  { name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(exportFixture) },
  { name: "second.md", mimeType: "text/markdown", buffer: Buffer.from(second) },
  { name: "vector.svg", mimeType: "image/svg+xml", buffer: Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="500" height="120" viewBox="0 0 500 120"><rect width="500" height="120" rx="12" fill="#eceefe"/><circle cx="65" cy="60" r="30" fill="#525ce8"/><text x="120" y="66" fill="#20283a" font-size="22">Local vector image</text></svg>') },
  { name: "pixel.png", mimeType: "image/png", buffer: Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6vCkAAAAASUVORK5CYII=", "base64") }
];

async function htmlExport(page) {
  await page.locator("[dataid=SettingsButton]").click();
  const download = page.waitForEvent("download");
  await page.locator("[dataid=ExportHtmlButton]").click();
  const file = await download;
  assert.equal(file.suggestedFilename(), "README.html");
  const target = join(artifacts, "export-fixture.html"); await file.saveAs(target);
  await page.waitForFunction(() => document.body.innerText.includes("HTML download started: README.html"));
  return readFile(target, "utf8");
}

// page: current app page.
// files: generated in-memory File payloads for the native picker.
async function openFiles(page, files) {
  const chooser = page.waitForEvent("filechooser");
  await view(page, "Open file");
  await (await chooser).setFiles(files);
  await documentIs(page, "README.md");
  const title = files.find(file => file.name === "README.md").buffer.toString("utf8").match(/^# (.+)$/mu)[1];
  await headingIs(page, title);
}

// title: the expected synthetic document heading after a workspace replacement.
async function headingIs(page, title) {
  await page.waitForFunction(expected => [...document.querySelectorAll(".mv-dom-host")]
    .some(host => host.shadowRoot?.querySelector(".reader h1")?.textContent === expected), title);
}

async function setting(page, control, value) {
  // Click the ComboBox itself; its template correctly intercepts clicks on selected text.
  await page.locator('[dataid="' + control + '"]').click();
  await page.getByText(value, { exact: true }).last().click();
}

async function appCall(page, operation, argumentsValue) {
  return page.evaluate(async ({ operation, argumentsValue }) => {
    const reply = JSON.parse(await window.markvibe.call(operation, JSON.stringify(argumentsValue), "browser-check"));
    if (!reply.ok) throw new Error(reply.message);
    return reply.value;
  }, { operation, argumentsValue });
}

async function readerPosition(page) {
  const hostId = await page.evaluate(() => [...document.querySelectorAll(".mv-dom-host")]
    .find(host => host.shadowRoot?.querySelector(".reader")).closest("[data-mv-host]").dataset.mvHost);
  return appCall(page, "markdown.capture", { hostId });
}

try {
  await check("demo reader, local image and Mermaid render without remote requests", async page => {
    await ready(page);
    assert.equal(await page.locator(".reader h1").innerText(), "Welcome to your reading space");
    await page.locator(".reader img").evaluate(image => {
      if (!image.complete || !image.naturalWidth) throw new Error("The local demo image did not load.");
    });
    await page.locator(".mv-diagram svg").waitFor({ state: "attached" });
    assert.notEqual(await page.locator(".mv-diagram .node rect").first().evaluate(node => getComputedStyle(node).fill), "rgb(0, 0, 0)");
    await page.screenshot({ path: join(artifacts, "desktop-read.png"), fullPage: true });
  });

  await check("real document links and browser Back/Forward preserve navigation", async page => {
    await ready(page);
    await page.getByRole("link", { name: "Prepare a workspace", exact: true }).click();
    await documentIs(page, "guide/setup.md");
    await page.goBack(); await documentIs(page, "README.md");
    await page.goForward(); await documentIs(page, "guide/setup.md");
    await page.goBack(); await documentIs(page, "README.md");
    await page.getByRole("link", { name: "Find your reading rhythm", exact: true }).click();
    await documentIs(page, "guide/reading.md");
  });

  await check("Source, Split and Read survive repeated switches and source stays read-only", async page => {
    await ready(page);
    for (let iteration = 0; iteration < 3; iteration++) {
      await view(page, "Source");
      await page.locator(".monaco-editor textarea").waitFor({ state: "visible" });
      assert.equal(await page.locator(".monaco-editor textarea").getAttribute("readonly"), "true");
      await view(page, "Split");
      await page.locator(".reader h1").waitFor({ state: "visible" });
      await page.locator(".monaco-editor").waitFor({ state: "visible" });
      await view(page, "Read");
      await page.locator(".reader h1").waitFor({ state: "visible" });
    }
    await view(page, "Split");
    await page.locator(".monaco-editor").waitFor({ state: "visible" });
    await page.screenshot({ path: join(artifacts, "desktop-split.png"), fullPage: true });
    assert.ok(await page.locator(".mv-dom-host").count() <= 3, "Repeated switches must not accumulate DOM hosts.");
    await openFiles(page, [{ name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(synthetic) }]);
    await page.locator(".monaco-editor").waitFor({ state: "visible" });
    await page.getByRole("searchbox", { name: "Find in current source", exact: true }).fill("Section 45");
    const sourceId = await page.evaluate(() => [...document.querySelectorAll(".mv-dom-host")]
      .find(host => host.shadowRoot?.querySelector(".source-content")).closest("[data-mv-host]").dataset.mvHost);
    await page.waitForFunction(async hostId => {
      const response = JSON.parse(await window.markvibe.call("source.capture", JSON.stringify({ hostId }), "browser-source-find"));
      return response.ok && response.value.sourceLine > 50;
    }, sourceId);
  });

  await check("Map exposes actual documents, connections, focus and document navigation", async page => {
    await ready(page);
    await view(page, "Map");
    await page.locator(".page-card").first().waitFor({ state: "visible" });
    assert.equal(await page.locator(".page-card").count(), 4);
    assert.match(await page.locator(".map-summary").first().innerText(), /4 of 4 pages.*visible connections/u);
    const before = await page.locator(".map-world").getAttribute("style");
    await page.getByRole("button", { name: "Zoom map in", exact: true }).click();
    assert.notEqual(await page.locator(".map-world").getAttribute("style"), before);
    await page.getByRole("button", { name: "Focus on this page", exact: true }).click();
    await page.getByRole("button", { name: "Show all pages", exact: true }).waitFor();
    await page.screenshot({ path: join(artifacts, "desktop-map.png"), fullPage: true });
    await page.getByRole("button", { name: /Open Prepare a workspace, guide\/setup\.md/u }).click();
    await documentIs(page, "guide/setup.md");
    await page.locator(".reader h1").waitFor({ state: "visible" });
  });

  await check("file import, duplicate anchors, missing page and source-location Split synchronization", async page => {
    await ready(page);
    await openFiles(page, [
      { name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(synthetic) },
      { name: "second.md", mimeType: "text/markdown", buffer: Buffer.from(second) }
    ]);
    assert.deepEqual(await page.locator(".reader h2").evaluateAll(headings => headings.slice(0, 2).map(h => h.id)), ["repeat", "repeat-1"]);
    await page.getByRole("link", { name: "Second page", exact: true }).click();
    await documentIs(page, "second.md");
    assert.equal(new URLSearchParams(new URL(page.url()).hash.slice(1)).get("anchor"), "repeat-1");
    await page.getByRole("link", { name: "Missing", exact: true }).click();
    await page.getByText("This page could not be opened", { exact: true }).waitFor({ state: "visible" });
    await page.getByText("Choose missing file", { exact: true }).waitFor({ state: "visible" });
    await page.goBack(); await documentIs(page, "README.md");
    await view(page, "Split");
    await page.locator(".monaco-editor").waitFor({ state: "visible" });
    await page.locator(".reader").evaluate(element => { element.scrollTop = element.scrollHeight * .6; });
    await page.waitForFunction(async () => {
      const hosts = [...document.querySelectorAll(".mv-dom-host")];
      const source = hosts.find(host => host.shadowRoot?.querySelector(".monaco-editor"));
      const hostId = source?.closest("[data-mv-host]")?.dataset.mvHost;
      if (!hostId) return false;
      const response = JSON.parse(await window.markvibe.call("source.capture", JSON.stringify({ hostId }), "browser-source-position"));
      return response.ok && response.value.sourceLine > 5;
    });
  });

  await check("320, 390, 768, 1024 and 1440px layouts show content; touch Source uses its fallback", async page => {
    await ready(page);
    for (const width of [320, 390, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await page.locator(".reader h1").waitFor({ state: "visible" });
      const bounds = await page.locator(".mv-dom-host").first().boundingBox();
      assert.ok(bounds && bounds.width > 200 && bounds.height > 300, "Readable document area at " + width + "px");
      assert.ok(bounds.x >= 0 && bounds.x + bounds.width <= width + 1, "No document overflow at " + width + "px");
    }
    await page.setViewportSize({ width: 390, height: 844 });
    await view(page, "Source");
    await page.locator(".plain-source[aria-readonly=true]").waitFor({ state: "visible" });
    await page.screenshot({ path: join(artifacts, "mobile-source.png"), fullPage: true });
    await view(page, "Read");
    await page.screenshot({ path: join(artifacts, "mobile-read.png"), fullPage: true });
  });

  await check("imported folder removes the artificial root; bounded ZIP opens nested documents", async page => {
    await ready(page);
    const folder = await mkdtemp(join(tmpdir(), "markvibe-fixture-"));
    await writeFile(join(folder, "README.md"), "# Imported folder\n\n[Child](nested/child.md)\n");
    await mkdir(join(folder, "nested"));
    await writeFile(join(folder, "nested/child.md"), "# Nested child\n\n[Home](../README.md)\n");
    await view(page, "◐");
    const chooseFolder = page.waitForEvent("filechooser");
    await page.getByText("Import folder", { exact: true }).click();
    await (await chooseFolder).setFiles(folder);
    await documentIs(page, "README.md");
    await headingIs(page, "Imported folder");
    assert.equal(await page.locator(".reader h1").innerText(), "Imported folder");
    await page.getByRole("link", { name: "Child", exact: true }).click();
    await documentIs(page, "nested/child.md");
    await page.goto(base, { waitUntil: "networkidle" });
    await page.locator("[dataid=ResumeWorkspaceButton]").waitFor({ state: "visible" });
    const reopenFolder = page.waitForEvent("filechooser");
    await page.locator("[dataid=ResumeWorkspaceButton]").click();
    await (await reopenFolder).setFiles(folder);
    await documentIs(page, "nested/child.md");
    await headingIs(page, "Nested child");
    await view(page, "◐");
    const chooseZip = page.waitForEvent("filechooser");
    await page.getByText("Import documentation ZIP", { exact: true }).click();
    const archive = zipSync({ "README.md": strToU8("# ZIP fixture\n\n[Child](nested/child.md)\n"), "nested/child.md": strToU8("# ZIP child\n") });
    await (await chooseZip).setFiles({ name: "fixture.zip", mimeType: "application/zip", buffer: Buffer.from(archive) });
    await documentIs(page, "README.md");
    await headingIs(page, "ZIP fixture");
    assert.equal(await page.locator(".reader h1").innerText(), "ZIP fixture");
    await page.getByRole("link", { name: "Child", exact: true }).click();
    await documentIs(page, "nested/child.md");
    assert.equal(await page.locator(".reader h1").innerText(), "ZIP child");
    await page.goto(base, { waitUntil: "networkidle" });
    await page.locator("[dataid=ResumeWorkspaceButton]").waitFor({ state: "visible" });
    const reopenZip = page.waitForEvent("filechooser");
    await page.locator("[dataid=ResumeWorkspaceButton]").click();
    await (await reopenZip).setFiles({ name: "fixture.zip", mimeType: "application/zip", buffer: Buffer.from(archive) });
    await documentIs(page, "nested/child.md");
    await headingIs(page, "ZIP child");
  });

  await check("untrusted Markdown blocks scripts, remote images and executable links", async page => {
    await ready(page);
    const hostile = "# Content boundary fixture\n\n<script>window.markvibeInjected=true</script>\n\n" +
      "![Remote](https://example.invalid/image.svg)\n\n[Unsafe](javascript:alert(1))\n\n" +
      "<img src=x onerror=\"window.markvibeInjected=true\">\n\n```mermaid\nflowchart LR\n A --> B\n```\n";
    await openFiles(page, [{ name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(hostile) }]);
    assert.equal(await page.evaluate(() => window.markvibeInjected), undefined);
    assert.equal(await page.locator(".reader script").count(), 0);
    assert.equal(await page.getByRole("link", { name: "Unsafe", exact: true }).count(), 0);
    await page.locator(".missing-resource").first().waitFor({ state: "visible" });
  });

  await check("Modern Theme, Monaco, Mermaid, reading styles and settings follow Light/Dark/System and survive reload", async page => {
    await ready(page);
    await view(page, "Split");
    await page.locator(".monaco-editor").waitFor({ state: "visible" });
    await page.locator("[dataid=SettingsButton]").click();
    await setting(page, "AppearanceCombo", "Dark");
    await page.locator(".monaco-editor.vs-dark").waitFor({ state: "visible" });
    await page.waitForFunction(() => [...document.querySelectorAll('[dataid=AppearanceCombo] *')]
      .some(element => getComputedStyle(element).backgroundColor === "rgb(24, 31, 45)"));
    await page.locator(".mv-diagram svg").waitFor({ state: "attached" });
    assert.equal(await page.locator(".reader").evaluate(element => getComputedStyle(element).color), "rgb(229, 234, 244)");
    await setting(page, "StyleCombo", "Editorial reading");
    await page.waitForFunction(() => document.documentElement.dataset.documentStyle === "editorial");
    await setting(page, "StyleCombo", "Compact reading");
    await page.waitForFunction(() => document.documentElement.dataset.documentStyle === "compact");
    await page.locator("[dataid=CloseSettingsButton]").click();
    await page.locator("[dataid=ZoomInButton]").click();
    await page.locator("[dataid=SettingsButton]").click();
    await page.screenshot({ path: join(artifacts, "modern-dark-settings.png"), fullPage: true });
    await page.reload({ waitUntil: "networkidle" });
    await page.locator(".reader h1").waitFor({ state: "visible" });
    assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "dark");
    assert.equal(await page.evaluate(() => document.documentElement.dataset.documentStyle), "compact");
    assert.equal(await page.locator("[dataid=ZoomLabel]").innerText(), "110%");
    await page.locator("[dataid=SettingsButton]").click();
    await setting(page, "AppearanceCombo", "Light");
    await page.waitForFunction(() => document.documentElement.dataset.theme === "light");
    await page.emulateMedia({ colorScheme: "dark" });
    await setting(page, "AppearanceCombo", "System");
    await page.waitForFunction(() => document.documentElement.dataset.theme === "dark");
    await page.emulateMedia({ colorScheme: "light" });
    await page.waitForFunction(() => document.documentElement.dataset.theme === "light");
    for (const appearance of ["Dark", "Light", "Dark", "Light"])
      await setting(page, "AppearanceCombo", appearance);
    await page.waitForFunction(() => document.documentElement.dataset.theme === "light" &&
      [...document.querySelectorAll('[dataid=AppearanceCombo] *')].some(element => getComputedStyle(element).backgroundColor === "rgb(255, 255, 255)"));
    await page.locator("[dataid=ResetSettingsButton]").click();
    await page.waitForFunction(() => document.documentElement.dataset.documentStyle === "technical");
    const saved = await page.evaluate(() => JSON.parse(localStorage.getItem("markvibe.preferences")));
    assert.deepEqual(saved, { version: 1, values: { appearance: "system", documentStyle: "technical", zoom: 100,
      simpleSource: false, rememberLastDocument: true } });
    await page.setViewportSize({ width: 320, height: 600 });
    await page.waitForFunction(() => {
      const rect = document.querySelector("[dataid=SettingsPanel]")?.getBoundingClientRect();
      return rect && rect.height > 0 && rect.left >= 0 && rect.right <= innerWidth && rect.bottom <= innerHeight;
    }, {}, { timeout: 5000 });
    const bounds = await page.locator("[dataid=SettingsPanel]").boundingBox();
    assert.ok(bounds && bounds.x >= 0 && bounds.x + bounds.width <= 320 && bounds.y + bounds.height <= 600,
      "Settings must fit a small viewport and scroll internally.");
    await page.screenshot({ path: join(artifacts, "mobile-settings.png"), fullPage: true });
  });

  await check("last document, anchor and source position resume after restart and can be forgotten", async page => {
    await ready(page);
    await page.getByRole("link", { name: "Find your reading rhythm", exact: true }).click();
    await documentIs(page, "guide/reading.md");
    await page.goto(base, { waitUntil: "networkidle" });
    await documentIs(page, "guide/reading.md");
    const files = [{ name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(synthetic) },
      { name: "second.md", mimeType: "text/markdown", buffer: Buffer.from(second + "\n" + synthetic.slice(synthetic.indexOf("## Section 0"))) }];
    await openFiles(page, files);
    await page.getByRole("link", { name: "Second page", exact: true }).click();
    await documentIs(page, "second.md");
    await page.locator(".reader").evaluate(element => { element.scrollTop = element.scrollHeight * .6; });
    await page.waitForFunction(() => JSON.parse(localStorage.getItem("markvibe.lastDocument"))?.position?.sourceLine > 50);
    const before = await readerPosition(page);
    await page.goto(base, { waitUntil: "networkidle" });
    await page.getByText("Continue where you left off", { exact: true }).waitFor({ state: "visible" });
    assert.match(await page.locator("[dataid=Breadcrumb]").innerText(), /second.md/u);
    assert.equal(await page.locator(".reader h1").count(), 0, "Imported content must not be persisted silently.");
    const chooser = page.waitForEvent("filechooser");
    await page.locator("[dataid=ResumeWorkspaceButton]").click();
    await (await chooser).setFiles(files);
    await documentIs(page, "second.md");
    await page.waitForFunction(() => [...document.querySelectorAll(".mv-dom-host")]
      .some(host => host.shadowRoot?.querySelector(".reader")?.scrollTop > 1000));
    const after = await readerPosition(page);
    assert.ok(Math.abs(before.sourceLine - after.sourceLine) < 3, "Reading position must resume using source mappings.");
    assert.equal(new URLSearchParams(new URL(page.url()).hash.slice(1)).get("anchor"), "repeat-1");
    await page.locator("[dataid=SettingsButton]").click();
    await page.locator("[dataid=RememberDocumentCheck]").click();
    await page.waitForFunction(() => !localStorage.getItem("markvibe.lastDocument") && !localStorage.getItem("markvibe.lastWorkspace"));
    await page.goto(base, { waitUntil: "networkidle" });
    await documentIs(page, "README.md");
    await headingIs(page, "Welcome to your reading space");
  });

  await check("Monaco runs its locally bundled module worker", async page => {
    let adapterUrl;
    const workers = [];
    page.on("request", request => { if (/\/monaco-adapter-[^/]+\.js(?:\?|$)/u.test(request.url())) adapterUrl = request.url(); });
    page.on("worker", worker => workers.push(worker.url()));
    await ready(page);
    await view(page, "Source");
    await page.locator(".monaco-editor").waitFor({ state: "visible" });
    assert.ok(adapterUrl, "The lazy Monaco adapter must be requested.");
    const changes = await page.evaluate(async adapter => {
      const { monaco } = await import(adapter);
      const node = document.createElement("div"); node.style.cssText = "position:fixed;left:-1000px;width:500px;height:300px;opacity:0";
      document.body.append(node);
      const original = monaco.editor.createModel("one\ntwo"), modified = monaco.editor.createModel("one\nthree");
      const editor = monaco.editor.createDiffEditor(node);
      let subscription;
      try {
        await new Promise((resolve, reject) => {
          const timeout = setTimeout(() => reject(new Error("The editor worker did not calculate a diff.")), 8000);
          subscription = editor.onDidUpdateDiff(() => { if (editor.getLineChanges()?.length) { clearTimeout(timeout); resolve(); } });
          editor.setModel({ original, modified });
        });
        return editor.getLineChanges().length;
      } finally { subscription?.dispose(); editor.dispose(); original.dispose(); modified.dispose(); node.remove(); }
    }, adapterUrl);
    assert.equal(changes, 1);
    assert.ok(workers.includes(new URL("js/editor.worker.js", base).href), "Worker assets must respect the application base path.");
  });

  await check("a failed Monaco adapter leaves a usable read-only source fallback", async page => {
    await page.route("**/monaco-adapter-*.js", route => route.fulfill({ status: 200, contentType: "text/javascript",
      body: 'throw new Error("Synthetic adapter failure");' }));
    await ready(page);
    await view(page, "Source");
    await page.locator(".plain-source[aria-readonly=true]").waitFor({ state: "visible" });
    await page.getByRole("button", { name: "Editor view", exact: true }).waitFor({ state: "visible" });
    await page.getByRole("searchbox", { name: "Find in current source", exact: true }).fill("Future ink annotations");
    await page.locator(".source-match").waitFor({ state: "visible" });
  });

  await check("legacy preferences migrate and corrupt or future settings recover to defaults", async (page, context) => {
    await context.addInitScript(() => {
      if (!localStorage.getItem("markvibe.preferences")) localStorage.setItem("markvibe.preferences", JSON.stringify({ appearance: "dark",
        documentStyle: "editorial", zoom: 120, simpleSource: true, ignoredField: "untrusted" }));
    });
    await ready(page);
    assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "dark");
    await view(page, "Source");
    await page.locator(".plain-source").waitFor({ state: "visible" });
    const migrated = await page.evaluate(() => JSON.parse(localStorage.getItem("markvibe.preferences")));
    assert.equal(migrated.version, 1); assert.equal(migrated.values.ignoredField, undefined);
    await page.evaluate(() => localStorage.setItem("markvibe.preferences", "{broken JSON"));
    await page.reload({ waitUntil: "networkidle" });
    await page.locator(".reader h1").waitFor({ state: "visible" });
    assert.equal(await page.locator("[dataid=ZoomLabel]").innerText(), "100%");
    await page.evaluate(() => localStorage.setItem("markvibe.preferences", JSON.stringify({ version: 999, values: { zoom: 150 } })));
    await page.reload({ waitUntil: "networkidle" });
    await page.locator(".reader h1").waitFor({ state: "visible" });
    assert.equal(await page.locator("[dataid=ZoomLabel]").innerText(), "100%");
  });

  await check("unavailable browser storage keeps themes usable for the session", async (page, context) => {
    await context.addInitScript(() => {
      for (const method of ["getItem", "setItem", "removeItem"]) Storage.prototype[method] = () => { throw new DOMException("Synthetic storage failure", "QuotaExceededError"); };
    });
    await ready(page);
    await page.locator("[dataid=SettingsButton]").click();
    await setting(page, "AppearanceCombo", "Dark");
    await page.waitForFunction(() => document.documentElement.dataset.theme === "dark");
    await page.getByText("Preferences are available for this session. Browser storage is unavailable.", { exact: true }).waitFor({ state: "visible" });
    await page.reload({ waitUntil: "networkidle" });
    await page.locator(".reader h1").waitFor({ state: "visible" });
    assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "light");
  });

  await check("a genuine persisted directory handle resumes without a startup permission prompt", async page => {
    await ready(page);
    await page.evaluate(async () => {
      // Origin-private synthetic files exercise real handle serialization, not a mocked picker.
      const root = await navigator.storage.getDirectory();
      const folder = await root.getDirectoryHandle("markvibe-connected-fixture", { create: true });
      for (const [path, text] of [["README.md", "# Connected workspace\n"], ["child.md", "# Connected child\n\nRestored from a real browser directory handle.\n"]]) {
        const handle = await folder.getFileHandle(path, { create: true });
        const writer = await handle.createWritable(); await writer.write(text); await writer.close();
      }
      const id = "connected-fixture";
      await new Promise((resolve, reject) => {
        const request = indexedDB.open("markvibe-folders", 1);
        request.onerror = () => reject(request.error);
        request.onsuccess = () => {
          const database = request.result, transaction = database.transaction("folder", "readwrite");
          transaction.objectStore("folder").put({ id, handle: folder }, "last");
          transaction.oncomplete = () => { database.close(); resolve(); };
          transaction.onerror = () => { database.close(); reject(transaction.error); };
        };
      });
      localStorage.setItem("markvibe.lastWorkspace", JSON.stringify({ version: 1, id, mode: "connected", name: folder.name }));
      localStorage.setItem("markvibe.lastDocument", JSON.stringify({ id: "connected-entry", workspaceId: id, path: "child.md", anchor: "",
        position: { blockId: "", fraction: 0, sourceLine: 1 } }));
      // The fixture must replace the current history entry too, so pagehide cannot rewrite it.
      history.replaceState({ markvibe: JSON.parse(localStorage.getItem("markvibe.lastDocument")) }, "", location.href);
    });
    await page.goto(base, { waitUntil: "networkidle" });
    await documentIs(page, "child.md");
    await headingIs(page, "Connected child");
    assert.match(await page.locator("[dataid=WorkspaceMode]").innerText(), /Connected folder/u);
  });

  await check("HTML export embeds local assets and diagrams in an offline, script-free document", async page => {
    await ready(page); await openFiles(page, exportFiles());
    await view(page, "Source");
    const html = await htmlExport(page);
    assert.ok(html.startsWith("<!doctype html>"));
    const offline = await browser.newContext({ javaScriptEnabled: false, offline: true });
    const exported = await offline.newPage(), requests = [];
    exported.on("request", request => { if (/^https?:/u.test(request.url())) requests.push(request.url()); });
    try {
      await exported.setContent(html);
      assert.equal(await exported.locator("h1").innerText(), "Export fixture — Caffè e qualità");
      assert.equal(await exported.locator("img[src^='data:']").count(), 2);
      assert.equal(await exported.locator(".mv-diagram svg").count(), 1);
      assert.notEqual(await exported.locator(".mv-diagram .node rect").first().evaluate(node => getComputedStyle(node).fill), "rgb(0, 0, 0)", "Sanitization must retain safe diagram fills.");
      assert.equal(await exported.locator(".mv-diagram .node text").count(), 3);
      assert.equal(await exported.locator("script, iframe, link, object").count(), 0);
      assert.equal(await exported.locator("[data-mv-path], [data-mv-asset]").count(), 0);
      assert.equal(await exported.getByText("Jump to details", { exact: true }).getAttribute("href"), "#details");
      assert.equal(await exported.getByText("Other document", { exact: true }).getAttribute("href"), null);
      assert.equal(await exported.locator(".missing-resource").count(), 2);
      for (const image of await exported.locator("img").all()) assert.ok(await image.evaluate(node => node.complete && node.naturalWidth > 0));
      assert.deepEqual(requests, []);
      await exported.screenshot({ path: join(artifacts, "export-html.png"), fullPage: true });
    } finally { await offline.close(); }
  });

  await check("PDF export prepares only the current document and prints the same portable layout", async (page, context) => {
    // Intercept the native dialog while exercising the actual export button and iframe.
    await context.addInitScript(() => {
      window.print = () => {
        if (window.frameElement?.classList.contains("mv-print-document")) {
          parent.__printedDocumentForCheck = document.documentElement.outerHTML;
          window.dispatchEvent(new Event("afterprint"));
        }
      };
    });
    await ready(page); await openFiles(page, exportFiles());
    await page.locator("[dataid=SettingsButton]").click();
    await setting(page, "AppearanceCombo", "Dark");
    await page.locator("[dataid=CloseSettingsButton]").click();
    await view(page, "Map");
    await page.locator("[dataid=SettingsButton]").click();
    await page.locator("[dataid=ExportPdfButton]").click();
    await page.waitForFunction(() => typeof window.__printedDocumentForCheck === "string");
    await page.waitForFunction(() => document.body.innerText.includes("Print dialog opened. Choose Save as PDF"));
    assert.equal(await page.locator(".mv-print-document").count(), 0, "The print frame must be cleaned up after printing.");
    assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "dark", "Export must retain the app theme.");
    const html = await page.evaluate(() => window.__printedDocumentForCheck);
    const printPage = await context.newPage();
    try {
      await printPage.setContent("<!doctype html>" + html);
      assert.equal(await printPage.locator("article.markdown").count(), 1);
      assert.equal(await printPage.locator("[dataid], .source, .map, .monaco-editor").count(), 0);
      await printPage.emulateMedia({ media: "print" });
      assert.equal(await printPage.locator("body").evaluate(node => getComputedStyle(node).backgroundColor), "rgb(255, 255, 255)");
      assert.notEqual(await printPage.locator(".mv-diagram .node rect").first().evaluate(node => getComputedStyle(node).fill), "rgb(0, 0, 0)");
      await printPage.evaluate(async () => { await document.fonts.ready; await Promise.all([...document.images].map(image => image.decode())); });
      await printPage.pdf({ path: join(artifacts, "export-fixture.pdf"), format: "A4", printBackground: true, preferCSSPageSize: true });
      await printPage.screenshot({ path: join(artifacts, "export-print.png"), fullPage: true });
    } finally { await printPage.close(); }
  });
  await check("browser ink downloads a separate sidecar, reopens it and exports vector notes", async page => {
    await ready(page);
    const markdown = "# Ink fixture\n\nA unique paragraph for a handwritten note.\n";
    const files = [{ name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(markdown) }];
    await openFiles(page, files);
    await page.getByRole("button", { name: "Annotate", exact: true }).click();
    await page.locator(".reader section[data-block-id]").filter({ hasText: "A unique paragraph for a handwritten note." }).getByRole("button", { name: "Add handwritten note for this paragraph", exact: true }).click();
    const box = await page.locator(".ink-band svg").boundingBox();
    await page.mouse.move(box.x + box.width * .1, box.y + box.height * .2); await page.mouse.down();
    await page.mouse.move(box.x + box.width * .7, box.y + box.height * .6, { steps: 10 }); await page.mouse.up();
    await page.waitForFunction(() => [...document.querySelectorAll(".mv-dom-host")].some(host => host.shadowRoot?.querySelector(".ink-status")?.textContent === "Unsaved ink"));
    assert.equal(await page.evaluate(() => window.markvibe.canClose()), false);
    const downloaded = page.waitForEvent("download"); await page.getByRole("button", { name: "Save ink", exact: true }).click();
    const file = await downloaded; assert.equal(file.suggestedFilename(), "README.md.ink.json");
    const path = join(artifacts, "browser-note.ink.json"); await file.saveAs(path); const json = await readFile(path, "utf8");
    const ink = JSON.parse(json); assert.equal(ink.schemaVersion, 1); assert.equal(ink.document.path, "README.md");
    assert.equal(ink.document.contentHash, (await import("node:crypto")).createHash("sha256").update(markdown).digest("hex"));
    assert.equal(ink.annotations.length, 1); assert.ok(ink.annotations[0].strokes[0].points.length >= 2);
    await page.waitForFunction(() => window.markvibe.canClose());
    const html = await htmlExport(page); assert.match(html, /class="ink-annotation"/u); assert.match(html, /stroke="#2458c7"/u);
    await page.locator("[dataid=CloseSettingsButton]").click();
    await openFiles(page, [...files, { name: "README.md.ink.json", mimeType: "application/json", buffer: Buffer.from(json) }]);
    await page.locator(".ink-band path").waitFor(); assert.equal(await page.getByRole("button", { name: "Save ink", exact: true }).isDisabled(), true);
  });

  await check("unsupported ink remains read-only and can be exported without modifying it", async page => {
    await ready(page);
    const original = '{"schemaVersion":2,"document":{},"annotations":[],"unexpected":"<script>window.inkAttack=true</script>"}';
    await openFiles(page, [
      { name: "README.md", mimeType: "text/markdown", buffer: Buffer.from("# Preserved ink\n\nA synthetic page.") },
      { name: "README.md.ink.json", mimeType: "application/json", buffer: Buffer.from(original) }
    ]);
    await page.getByText("This annotation format is unsupported.", { exact: false }).first().waitFor();
    assert.equal(await page.getByRole("button", { name: "Annotate", exact: true }).isDisabled(), true);
    assert.equal(await page.evaluate(() => window.inkAttack === true), false);
    const downloaded = page.waitForEvent("download"); await page.getByRole("button", { name: "Export ink", exact: true }).click();
    const path = join(artifacts, "unsupported-note.ink.json"); await (await downloaded).saveAs(path);
    assert.equal(await readFile(path, "utf8"), original);
  });
} finally {
  await writeFile(join(artifacts, "results.json"), JSON.stringify({ base, browser: browser.version(), results }, null, 2));
  await browser.close();
}

console.log(results.filter(result => result.passed).length + "/" + results.length + " browser checks passed.");
if (results.some(result => !result.passed)) process.exitCode = 1;
