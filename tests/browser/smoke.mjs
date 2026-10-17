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
import { mkdir, writeFile, mkdtemp, readFile } from "node:fs/promises";
import { resolve, join } from "node:path";
import { zipSync, strToU8 } from "fflate";

const base = process.env.MARKVIBE_BASE_URL || "http://localhost:5080/";
const artifacts = resolve(process.env.MARKVIBE_ARTIFACTS_DIR || "artifacts/browser");
const browser = await chromium.launch({ channel: process.env.MARKVIBE_BROWSER_CHANNEL || "msedge", headless: true });
const results = [];
await mkdir(artifacts, { recursive: true });

// page: an isolated browser page containing only synthetic documents.
/** Wait for the synthetic browser workspace to finish opening and indexing. */
async function ready(page) {
  await page.goto(base, { waitUntil: "networkidle" });
  await page.locator(".reader h1").waitFor({ timeout: 90000 });
  await page.getByRole("document", { name: "Rendered Markdown document" }).waitFor({ state: "visible" });
  assert.ok(await page.locator(".reader h1").isVisible(), "The reader must contain visible document content.");
}

// page: the current app page.
// name: an English XAML toolbar label. OpenSilver handles shell focus internally.
/** Select a shell view and wait for its browser host to become usable. */
async function view(page, name) {
  await page.getByText(name, { exact: true }).first().click();
}

// page: the current app page.
// path: a workspace-relative document identity, not an operating-system path.
/** Wait until the reader displays the expected synthetic document. */
async function documentIs(page, path) {
  await page.waitForFunction(expected => {
    const hosts = [...document.querySelectorAll(".mv-dom-host")];
    return hosts.some(host => host.shadowRoot?.querySelector(".document-meta .eyebrow")?.textContent === expected);
  }, path);
  assert.equal(new URLSearchParams(new URL(page.url()).hash.slice(1)).get("doc"), path);
}

/** Run a named browser scenario and record its result with relevant failure evidence. */
async function check(name, run) {
  const context = await browser.newContext({ locale: "en-US", viewport: { width: 1440, height: 1000 }, reducedMotion: "reduce" });
  const page = await context.newPage();
  const errors = [];
  const failures = [];
  const remote = [];
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => {
    if (message.type() === "error") {
      errors.push(message.text());
    }
  });
  page.on("response", response => {
    if (response.status() >= 400) {
      failures.push(response.status() + " " + response.url());
    }
  });
  page.on("request", request => {
    if (/^https?:/u.test(request.url()) && new URL(request.url()).origin !== new URL(base).origin) {
      remote.push(request.url());
    }
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
  } finally {
    await context.close();
  }
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
/** Read the exported synthetic fixture files from the isolated artifact directory. */
const exportFiles = () => [
  { name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(exportFixture) },
  { name: "second.md", mimeType: "text/markdown", buffer: Buffer.from(second) },
  {
    name: "vector.svg",
    mimeType: "image/svg+xml",
    buffer: Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="500" height="120" viewBox="0 0 500 120"><rect width="500" height="120" rx="12" fill="#eceefe"/><circle cx="65" cy="60" r="30" fill="#525ce8"/><text x="120" y="66" fill="#20283a" font-size="22">Local vector image</text></svg>')
  },
  {
    name: "pixel.png",
    mimeType: "image/png",
    buffer: Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6vCkAAAAASUVORK5CYII=", "base64")
  }
];

/** Request an HTML export and retain the resulting synthetic download. */
async function htmlExport(page) {
  const download = page.waitForEvent("download");
  await page.locator("[dataid=ExportHtmlButton]").click();
  const file = await download;
  assert.equal(file.suggestedFilename(), "README.html");
  const target = join(artifacts, "export-fixture.html");
  await file.saveAs(target);
  await page.waitForFunction(() => document.body.innerText.includes("HTML download started: README.html"));
  return readFile(target, "utf8");
}

/** Download a map snapshot through the visible toolbar and read only the synthetic artifact. */
async function downloadMap(page, name) {
  const download = page.waitForEvent("download");
  await page.getByRole("button", { name: "Download layout", exact: true }).click();
  const path = join(artifacts, name + ".json");
  await (await download).saveAs(path);
  return JSON.parse(await readFile(path, "utf8"));
}

// page: current app page.
// files: generated in-memory File payloads for the native picker.
/** Select synthetic documentation files through the application's import control. */
async function openFiles(page, files) {
  const chooser = page.waitForEvent("filechooser");
  await view(page, "Open file");
  await (await chooser).setFiles(files);
  await documentIs(page, "README.md");
  const title = files.find(file => file.name === "README.md").buffer.toString("utf8").match(/^# (.+)$/mu)[1];
  await headingIs(page, title);
}

// title: the expected synthetic document heading after a workspace replacement.
/** Wait until the expected synthetic heading is visible in the reader. */
async function headingIs(page, title) {
  await page.waitForFunction(expected => [...document.querySelectorAll(".mv-dom-host")]
    .some(host => host.shadowRoot?.querySelector(".reader h1")?.textContent === expected), title);
}

/** Change the requested settings control using the application's visible UI. */
async function setting(page, control, value) {
  // Click the ComboBox itself; its template correctly intercepts clicks on selected text.
  await page.locator('[dataid="' + control + '"]').click();
  await page.getByText(value, { exact: true }).last().click();
}

/** Invoke the application's browser boundary for an explicit synthetic scenario. */
async function appCall(page, operation, argumentsValue) {
  return page.evaluate(async ({ operation, argumentsValue }) => {
    const reply = JSON.parse(await window.markvibe.call(operation, JSON.stringify(argumentsValue), "browser-check"));
    if (!reply.ok) {
      throw new Error(reply.message);
    }
    return reply.value;
  }, { operation, argumentsValue });
}

/** Capture the synthetic reader's source-backed position for scenario assertions. */
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
      if (!image.complete || !image.naturalWidth) {
        throw new Error("The local demo image did not load.");
      }
    });
    await page.locator(".mv-diagram svg").waitFor({ state: "attached" });
    assert.notEqual(await page.locator(".mv-diagram .node rect").first().evaluate(node => getComputedStyle(node).fill), "rgb(0, 0, 0)");
    await page.screenshot({ path: join(artifacts, "desktop-read.png"), fullPage: true });
  });

  await check("real document links and browser Back/Forward preserve navigation", async page => {
    await ready(page);
    await page.getByRole("link", { name: "Prepare a workspace", exact: true }).click();
    await documentIs(page, "guide/setup.md");
    await page.goBack();
    await documentIs(page, "README.md");
    await page.goForward();
    await documentIs(page, "guide/setup.md");
    await page.goBack();
    await documentIs(page, "README.md");
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
    await page.locator("[dataid=SourceSearch]").locator("input,textarea").fill("Section 45");
    const sourceId = await page.evaluate(() => [...document.querySelectorAll(".mv-dom-host")]
      .find(host => host.shadowRoot?.querySelector(".source-content")).closest("[data-mv-host]").dataset.mvHost);
    await page.waitForFunction(async hostId => {
      const response = JSON.parse(await window.markvibe.call("source.capture", JSON.stringify({ hostId }), "browser-source-find"));
      return response.ok && response.value.sourceLine > 50;
    }, sourceId);
  });

  await check("Map commands work immediately, click selects and double-click opens", async page => {
    await ready(page);
    await view(page, "Map");
    const card = page.getByRole("button", { name: "Select Prepare a workspace, guide/setup.md", exact: true });
    await card.waitFor();
    assert.equal(await page.locator(".page-card").count(), 4);
    await page.getByRole("button", { name: "Arrange in a grid", exact: true }).click();
    await card.click();
    await eventually(async () => assert.equal(await card.getAttribute("aria-pressed"), "true"));
    assert.equal(new URLSearchParams(new URL(page.url()).hash.slice(1)).get("doc"), "README.md");
    const before = await page.locator(".map-world").getAttribute("style");
    await card.hover();
    await page.mouse.wheel(0, -300);
    await eventually(async () => assert.notEqual(await page.locator(".map-world").getAttribute("style"), before));
    await page.getByRole("button", { name: "Advanced view", exact: true }).click();
    await page.locator(".advanced-map .map-statistics").first().waitFor();
    await page.screenshot({ path: join(artifacts, "desktop-map.png"), fullPage: true });
    await card.dblclick();
    await documentIs(page, "guide/setup.md");
    await page.locator(".reader h1").waitFor();
  });

  await check("card resize persists in downloads and resetting requires explicit confirmation", async page => {
    await ready(page);
    await view(page, "Map");
    const grip = page.getByRole("button", { name: "Resize Welcome to your reading space", exact: true });
    await grip.waitFor();
    await grip.focus();
    await page.keyboard.press("ArrowRight");
    const resized = await downloadMap(page, "resized-map");
    assert.equal(resized.schemaVersion, 4);
    assert.equal(resized.nodes["README.md"].width, 270);
    await page.getByRole("button", { name: "Reset all", exact: true }).click();
    let dialog = page.locator(".mv-dialog").last();
    await dialog.getByText("Cancel", { exact: true }).click();
    await dialog.waitFor({ state: "hidden" });
    assert.deepEqual(await downloadMap(page, "cancelled-reset"), resized);
    await page.getByRole("button", { name: "Reset all", exact: true }).click();
    dialog = page.locator(".mv-dialog").last();
    await dialog.getByText("Reset", { exact: true }).click();
    await dialog.waitFor({ state: "hidden" });
    const reset = await downloadMap(page, "confirmed-reset");
    assert.equal(reset.nodes["README.md"].width, undefined);
    await view(page, "Read");
    assert.equal(await page.locator(".reader h1").innerText(), "Welcome to your reading space");
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
    await page.goBack();
    await documentIs(page, "README.md");
    await view(page, "Split");
    await page.locator(".monaco-editor").waitFor({ state: "visible" });
    await page.locator(".reader").evaluate(element => {
      element.scrollTop = element.scrollHeight * .6;
    });
    await page.waitForFunction(async () => {
      const hosts = [...document.querySelectorAll(".mv-dom-host")];
      const source = hosts.find(host => host.shadowRoot?.querySelector(".monaco-editor"));
      const hostId = source?.closest("[data-mv-host]")?.dataset.mvHost;
      if (!hostId) {
        return false;
      }
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

  await check("folder and ZIP providers preserve nested paths and return their actual source", async page => {
    await ready(page);
    const folder = await mkdtemp(join(artifacts, "folder-fixture-"));
    await writeFile(join(folder, "README.md"), "# Imported folder\n\n[Child](nested/child.md)\n");
    await mkdir(join(folder, "nested"));
    await writeFile(join(folder, "nested/child.md"), "# Nested child");
    const folderPicker = page.waitForEvent("filechooser");
    const folderOpening = appCall(page, "workspace.open", { mode: "imported" });
    await (await folderPicker).setFiles(folder);
    const folderInfo = await folderOpening;
    const entries = await appCall(page, "workspace.entries", { workspaceId: folderInfo.id });
    assert.ok(entries.some(entry => entry.path === "nested/child.md"));
    assert.equal(await appCall(page, "workspace.text", { workspaceId: folderInfo.id, path: "nested/child.md" }), "# Nested child");
    const zipPicker = page.waitForEvent("filechooser");
    const zipOpening = appCall(page, "workspace.open", { mode: "zip" });
    await (await zipPicker).setFiles({
      name: "fixture.zip", mimeType: "application/zip",
      buffer: Buffer.from(zipSync({ "README.md": strToU8("# ZIP fixture"), "nested/child.md": strToU8("# ZIP child") }))
    });
    const zipInfo = await zipOpening;
    assert.equal(await appCall(page, "workspace.text", { workspaceId: zipInfo.id, path: "nested/child.md" }), "# ZIP child");
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

  await check("Appearance and reading styles apply immediately and survive reload", async page => {
    await ready(page);
    await view(page, "Split");
    await page.locator(".monaco-editor").waitFor();
    await page.locator("[dataid=SettingsButton]").click();
    await setting(page, "AppearanceCombo", "Dark");
    await page.waitForFunction(() => document.documentElement.dataset.theme === "dark");
    await setting(page, "StyleCombo", "Compact reading");
    await page.waitForFunction(() => document.documentElement.dataset.documentStyle === "compact");
    await page.locator("[dataid=UiFontCombo]").waitFor();
    await closeSettings(page);
    await page.reload();
    await page.locator(".reader h1").waitFor();
    assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "dark");
    assert.equal(await page.evaluate(() => document.documentElement.dataset.documentStyle), "compact");
    await page.locator("[dataid=SettingsButton]").click();
    await page.emulateMedia({ colorScheme: "light" });
    await setting(page, "AppearanceCombo", "System");
    await page.waitForFunction(() => document.documentElement.dataset.theme === "light");
    await page.emulateMedia({ colorScheme: "dark" });
    await page.waitForFunction(() => document.documentElement.dataset.theme === "dark");
    await page.getByText("Reset preferences", { exact: true }).click();
    await page.waitForFunction(() => document.documentElement.dataset.documentStyle === "technical");
    const saved = await page.evaluate(() => JSON.parse(localStorage.getItem("markvibe.preferences")));
    assert.equal(saved.version, 1);
    assert.equal(saved.values.appearance, "system");
    assert.equal(saved.values.documentStyle, "technical");
    await page.screenshot({ path: join(artifacts, "settings.png"), fullPage: true });
    await closeSettings(page);
  });

  await check("last document, anchor and source position resume after restart and can be forgotten", async page => {
    await ready(page);
    await page.getByRole("link", { name: "Find your reading rhythm", exact: true }).click();
    await documentIs(page, "guide/reading.md");
    await page.goto(base, { waitUntil: "networkidle" });
    await documentIs(page, "guide/reading.md");
    const files = [{ name: "README.md", mimeType: "text/markdown", buffer: Buffer.from(synthetic) },
    {
      name: "second.md",
      mimeType: "text/markdown",
      buffer: Buffer.from(second + "\n" + synthetic.slice(synthetic.indexOf("## Section 0")))
    }];
    await openFiles(page, files);
    await page.getByRole("link", { name: "Second page", exact: true }).click();
    await documentIs(page, "second.md");
    await page.locator(".reader").evaluate(element => {
      element.scrollTop = element.scrollHeight * .6;
    });
    await page.waitForFunction(() => JSON.parse(localStorage.getItem("markvibe.lastDocument"))?.position?.sourceLine > 50);
    const before = await readerPosition(page);
    await page.goto(base, { waitUntil: "networkidle" });
    await page.getByText("Continue where you left off", { exact: true }).waitFor({ state: "visible" });
    assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem("markvibe.lastDocument")).path), "second.md");
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
    page.on("request", request => {
      if (/\/monaco-adapter-[^/]+\.js(?:\?|$)/u.test(request.url())) {
        adapterUrl = request.url();
      }
    });
    page.on("worker", worker => workers.push(worker.url()));
    await ready(page);
    await view(page, "Source");
    await page.locator(".monaco-editor").waitFor({ state: "visible" });
    assert.ok(adapterUrl, "The lazy Monaco adapter must be requested.");
    const changes = await page.evaluate(async adapter => {
      const { monaco } = await import(adapter);
      const node = document.createElement("div");
      node.style.cssText = "position:fixed;left:-1000px;width:500px;height:300px;opacity:0";
      document.body.append(node);
      const original = monaco.editor.createModel("one\ntwo");
      const modified = monaco.editor.createModel("one\nthree");
      const editor = monaco.editor.createDiffEditor(node);
      let subscription;
      try {
        await new Promise((resolve, reject) => {
          const timeout = setTimeout(() => reject(new Error("The editor worker did not calculate a diff.")), 8000);
          subscription = editor.onDidUpdateDiff(() => {
            if (editor.getLineChanges()?.length) {
              clearTimeout(timeout);
              resolve();
            }
          });
          editor.setModel({ original, modified });
        });
        return editor.getLineChanges().length;
      } finally {
        subscription?.dispose();
        editor.dispose();
        original.dispose();
        modified.dispose();
        node.remove();
      }
    }, adapterUrl);
    assert.equal(changes, 1);
    assert.ok(workers.includes(new URL("js/editor.worker.js", base).href), "Worker assets must respect the application base path.");
  });

  await check("a failed Monaco adapter leaves a usable read-only source fallback", async page => {
    await page.route("**/monaco-adapter-*.js", route => route.fulfill({
      status: 200,
      contentType: "text/javascript",
      body: 'throw new Error("Synthetic adapter failure");'
    }));
    await ready(page);
    await view(page, "Source");
    await page.locator(".plain-source[aria-readonly=true]").waitFor({ state: "visible" });
    await page.locator("[dataid=SimpleButton]").waitFor({ state: "visible" });
    await page.locator("[dataid=SourceSearch]").locator("input,textarea").fill("Markdown");
    await page.locator(".source-match").waitFor({ state: "visible" });
  });

  await check("current preferences load and corrupt or unsupported settings recover to defaults", async (page, context) => {
    await context.addInitScript(() => {
      if (!sessionStorage.getItem("fixture.preferences")) {
        sessionStorage.setItem("fixture.preferences", "yes");
        localStorage.setItem("markvibe.preferences", JSON.stringify({
          version: 1, values: { appearance: "dark", documentStyle: "editorial", simpleSource: true, language: "en", ignoredField: "untrusted" }
        }));
      }
    });
    await ready(page);
    assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), "dark");
    await view(page, "Source");
    await page.locator(".plain-source").waitFor();
    const saved = await page.evaluate(() => JSON.parse(localStorage.getItem("markvibe.preferences")));
    assert.equal(saved.values.ignoredField, undefined);
    for (const invalid of ["{broken JSON", JSON.stringify({ version: 999, values: { appearance: "dark" } })]) {
      await page.evaluate(value => localStorage.setItem("markvibe.preferences", value), invalid);
      await page.reload();
      await page.locator(".reader h1").waitFor();
      assert.equal(await page.evaluate(() => document.documentElement.dataset.documentStyle), "technical");
    }
  });

  await check("unavailable browser storage keeps themes usable for the session", async (page, context) => {
    await context.addInitScript(() => {
      for (const method of ["getItem", "setItem", "removeItem"]) {
        Storage.prototype[method] = () => {
          throw new DOMException("Synthetic storage failure", "QuotaExceededError");
        };
      }
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
        const writer = await handle.createWritable();
        await writer.write(text);
        await writer.close();
      }
      const id = "connected-fixture";
      await new Promise((resolve, reject) => {
        const request = indexedDB.open("markvibe-folders", 1);
        request.onerror = () => reject(request.error);
        request.onsuccess = () => {
          const database = request.result;
          const transaction = database.transaction("folder", "readwrite");
          transaction.objectStore("folder").put({ id, handle: folder }, "last");
          transaction.oncomplete = () => {
            database.close();
            resolve();
          };
          transaction.onerror = () => {
            database.close();
            reject(transaction.error);
          };
        };
      });
      localStorage.setItem("markvibe.lastWorkspace", JSON.stringify({ version: 1, id, mode: "connected", name: folder.name }));
      localStorage.setItem("markvibe.lastDocument", JSON.stringify({
        id: "connected-entry",
        workspaceId: id,
        path: "child.md",
        anchor: "",
        position: { blockId: "", fraction: 0, sourceLine: 1 }
      }));
      // The fixture must replace the current history entry too, so pagehide cannot rewrite it.
      history.replaceState({ markvibe: JSON.parse(localStorage.getItem("markvibe.lastDocument")) }, "", location.href);
    });
    await page.goto(base, { waitUntil: "networkidle" });
    await documentIs(page, "child.md");
    await headingIs(page, "Connected child");
    assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem("markvibe.lastWorkspace")).mode), "connected");
  });

  await check("HTML export embeds local assets and diagrams in an offline, script-free document", async page => {
    await ready(page);
    await openFiles(page, exportFiles());
    await view(page, "Source");
    const html = await htmlExport(page);
    assert.ok(html.startsWith("<!doctype html>"));
    const offline = await browser.newContext({ javaScriptEnabled: false, offline: true });
    const exported = await offline.newPage();
    const requests = [];
    exported.on("request", request => {
      if (/^https?:/u.test(request.url())) {
        requests.push(request.url());
      }
    });
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
      for (const image of await exported.locator("img").all()) {
        assert.ok(await image.evaluate(node => node.complete && node.naturalWidth > 0));
      }
      assert.deepEqual(requests, []);
      await exported.screenshot({ path: join(artifacts, "export-html.png"), fullPage: true });
    } finally {
      await offline.close();
    }
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
    await ready(page);
    await openFiles(page, exportFiles());
    await page.locator("[dataid=SettingsButton]").click();
    await setting(page, "AppearanceCombo", "Dark");
    await closeSettings(page);
    await view(page, "Map");
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
      await printPage.evaluate(async () => {
        await document.fonts.ready;
        await Promise.all([...document.images].map(image => image.decode()));
      });
      await printPage.pdf({
        path: join(artifacts, "export-fixture.pdf"),
        format: "A4",
        printBackground: true,
        preferCSSPageSize: true
      });
      await printPage.screenshot({ path: join(artifacts, "export-print.png"), fullPage: true });
    } finally {
      await printPage.close();
    }
  });

} finally {
  await writeFile(join(artifacts, "results.json"), JSON.stringify({ base, browser: browser.version(), results }, null, 2));
  await browser.close();
}

console.log(results.filter(result => result.passed).length + "/" + results.length + " browser checks passed.");
if (results.some(result => !result.passed)) {
  process.exitCode = 1;
}
