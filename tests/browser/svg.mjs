/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; SVG presentation and resource-boundary regressions; 2026-10-25
 * Human guidance: Umberto Giacobbi; preserve readable local SVG covers
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import assert from "node:assert/strict";
import { build } from "esbuild";
import { chromium } from "playwright";
import { fileURLToPath } from "node:url";

// Exercise the same sanitizer used by reader/export assets in a blank browser,
// without opening the application or using private workspace documents.
const bundle = await build({
  entryPoints: [fileURLToPath(new URL("../../client/svg.js", import.meta.url))],
  bundle: true,
  write: false,
  format: "iife",
  globalName: "SvgValidation",
  platform: "browser"
});
const browser = await chromium.launch({ channel: "msedge", headless: true });
try {
  const page = await browser.newPage();
  const requests = [];
  await page.route("**/*", route => {
    requests.push(route.request().url());
    return route.abort();
  });
  await page.addScriptTag({ content: bundle.outputFiles[0].text });

  const cover = `<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="620">
    <defs><linearGradient id="progress"><stop stop-color="#f0c94e"/></linearGradient></defs>
    <style>
      .page { fill:#1e1e2e; } .label { fill:#e0ddec; }
      text { font-family:Arial; text-anchor:middle; font-weight:500; }
      @media (prefers-color-scheme:light) {
        .page { fill:#fcfaf6; } .label { fill:#282835; }
      }
    </style>
    <rect class="page" width="1000" height="620"/>
    <text class="label" x="500" y="60" style="text-anchor:start;opacity:.8">Dashboard</text>
    <rect id="bar" width="452" height="32" style="fill:url('#progress')"/>
  </svg>`;
  for (const colorScheme of ["dark", "light"]) {
    await page.emulateMedia({ colorScheme });
    const result = await page.evaluate(async source => {
      const image = await SvgValidation.sanitizeSvg(new Blob([source]));
      const text = await image.blob.text();
      const parsed = new DOMParser().parseFromString(text, "image/svg+xml");
      document.body.replaceChildren(document.importNode(parsed.documentElement, true));
      const label = document.querySelector("text");
      return {
        pixels: image.pixels,
        background: getComputedStyle(document.querySelector("rect")).fill,
        label: getComputedStyle(label).fill,
        anchor: getComputedStyle(label).textAnchor,
        opacity: getComputedStyle(label).opacity,
        bar: document.querySelector("#bar").getAttribute("style")
      };
    }, cover);
    assert.equal(result.pixels, 620000);
    assert.equal(result.background, colorScheme === "dark" ? "rgb(30, 30, 46)" : "rgb(252, 250, 246)");
    assert.equal(result.label, colorScheme === "dark" ? "rgb(224, 221, 236)" : "rgb(40, 40, 53)");
    assert.equal(result.anchor, "start");
    assert.equal(result.opacity, "0.8");
    assert.match(result.bar, /url\(["']?#progress["']?\)/u);
  }

  const hostile = String.raw`<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" onload="alert(1)">
    <script>alert(1)</script><foreignObject><div>HTML</div></foreignObject>
    <image href="https://blocked.invalid/image.png"/>
    <use href="https://blocked.invalid/icons.svg#icon"/><a href="https://blocked.invalid">Link</a>
    <animate attributeName="fill" values="red;blue"/>
    <style>
      @import url('https://blocked.invalid/sheet.css');
      @font-face { font-family:Remote; src:url(https://blocked.invalid/font.woff); }
      @keyframes pulse { from { opacity:0 } to { opacity:1 } }
      .unsafe { fill:u\72l(https://blocked.invalid/paint.svg); stroke:#00ff00; animation:pulse 1s; }
    </style>
    <rect class="unsafe" width="20" height="20"
      style="fill:url(//blocked.invalid/paint.svg);stroke:#ff0000;background:url(https://blocked.invalid);position:fixed"/>
  </svg>`;
  const clean = await page.evaluate(async source => {
    document.body.replaceChildren();
    const image = await SvgValidation.sanitizeSvg(new Blob([source]));
    return image.blob.text();
  }, hostile);
  assert.doesNotMatch(clean, /blocked\.invalid|onload|<script|foreignObject|<image|<use|<a\s|<animate|@import|@font-face|@keyframes|animation:|position:|background:/iu);
  assert.match(clean, /stroke:\s*(?:#f{2}0000|rgb\(255, 0, 0\))/iu);
  assert.match(clean, /stroke:\s*(?:#00f{2}00|rgb\(0, 255, 0\))/iu);
  assert.equal(requests.length, 0);

  const overflow = await page.evaluate(async () => {
    try {
      const source = '<svg xmlns="http://www.w3.org/2000/svg"><style>'
        + ".page{fill:red;}".repeat(513) + "</style></svg>";
      await SvgValidation.sanitizeSvg(new Blob([source]));
      return "accepted";
    } catch (error) {
      return error.code;
    }
  });
  assert.equal(overflow, "image-too-large");
  console.log("SVG color-scheme, inline styles, local gradients, active content and CSS limits passed.");
} finally {
  await browser.close();
}
