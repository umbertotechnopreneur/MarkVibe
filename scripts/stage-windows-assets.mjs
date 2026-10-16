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

import { readFile, mkdir, copyFile, access } from "node:fs/promises";
import path from "node:path";

// Materialize the Browser build's static-asset manifest, including WASM runtime
// and OpenSilver resources. The desktop host needs neither a dev server nor HTTP.
const [manifestPath, destination] = process.argv.slice(2);
if (!manifestPath || !destination) {
  throw new Error("Usage: stage-windows-assets.mjs <runtime-manifest> <destination>");
}
const manifest = JSON.parse(await readFile(manifestPath, "utf8"));
const assetsDestination = path.resolve(destination);
let stagedAssetCount = 0;
/** Copy contained static assets from the runtime manifest and visit its child directories. */
async function visit(node, segments = []) {
  if (node.Asset && !segments.at(-1)?.match(/\.(gz|br)$/u)) {
    const target = path.resolve(assetsDestination, ...segments);
    if (!target.startsWith(assetsDestination + path.sep)) {
      throw new Error("Unsafe static asset path");
    }
    const source = path.resolve(manifest.ContentRoots[node.Asset.ContentRootIndex], node.Asset.SubPath);
    await mkdir(path.dirname(target), { recursive: true });
    await copyFile(source, target);
    stagedAssetCount++;
  }
  for (const [name, child] of Object.entries(node.Children ?? {})) {
    await visit(child, [...segments, name]);
  }
}
await visit(manifest.Root);
await access(path.join(assetsDestination, "index.html"));
await access(path.join(assetsDestination, "_framework", "blazor.webassembly.js"));
await access(path.join(assetsDestination, "js", "markvibe.js"));
console.log(`Desktop assets staged: ${stagedAssetCount} files`);
