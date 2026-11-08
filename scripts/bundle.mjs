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
 * Modified with AI: OpenAI Codex; record Debug source-map configuration for Release bundle validation; 2026-10-23
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { build } from "esbuild";
import { createHash } from "node:crypto";
import { mkdir, readFile, writeFile, readdir, copyFile, rm, lstat } from "node:fs/promises";
import { dirname, resolve, join, relative } from "node:path";
import { clientFingerprint, verifyInstalledPackages } from "./client-inputs.mjs";

const repositoryRoot = resolve(import.meta.dirname, "..");
const debug = process.argv.includes("--debug");
const bundleDirectory = join(repositoryRoot, "src/MarkVibe.Browser/wwwroot/js");
await verifyInstalledPackages();
const inputFingerprint = await clientFingerprint();
if (!bundleDirectory.startsWith(repositoryRoot + "/") && !bundleDirectory.startsWith(repositoryRoot + "\\")) {
  throw new Error("Unsafe generated output directory");
}
const previousOutputInfo = await lstat(bundleDirectory).catch(error => {
  if (error.code !== "ENOENT") {
    throw error;
  }
  return null;
});
if (previousOutputInfo?.isSymbolicLink()) {
  throw new Error("Generated client output must not be a link");
}
await rm(bundleDirectory, { recursive: true, force: true });
await mkdir(bundleDirectory, { recursive: true });
// The opaque diagram frame cannot import modules from the application's origin.
// Inline only this source-built runtime and authorize its exact bytes in the frame CSP.
const diagramFrame = await build({
  absWorkingDir: repositoryRoot,
  entryPoints: ["client/diagram-frame.js"],
  outfile: join(bundleDirectory, "diagram-frame.js"),
  bundle: true,
  format: "iife",
  platform: "browser",
  target: ["es2022"],
  supported: { "inline-script": true },
  minify: !debug,
  legalComments: "inline",
  metafile: true,
  write: false
});
const frameOutputs = Object.values(diagramFrame.metafile.outputs);
if (diagramFrame.outputFiles.length !== 1 || frameOutputs.length !== 1 || frameOutputs[0].imports.length !== 0) {
  throw new Error("The diagram runtime must be a single self-contained script.");
}
const frameScript = diagramFrame.outputFiles[0].text.replace(/\r\n?/gu, "\n");
if (/<\/script(?:[\t\n\f\r />]|$)/iu.test(frameScript)) {
  throw new Error("The diagram runtime cannot be embedded safely.");
}
const frameHash = createHash("sha256").update(frameScript).digest("base64");
const framePolicy = "default-src 'none'; script-src 'sha256-" + frameHash +
  "'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'";
const framePath = join(bundleDirectory, "diagram-frame.html");
const frameHtml = '<!doctype html><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="' +
  framePolicy + '"><meta name="referrer" content="no-referrer"><body><script>' + frameScript + '</script></body>';
await writeFile(framePath, frameHtml);

const result = await build({
  absWorkingDir: repositoryRoot,
  entryPoints: { markvibe: "client/main.js", "editor.worker": "client/editor.worker.js" },
  outdir: bundleDirectory,
  bundle: true,
  splitting: true,
  format: "esm",
  platform: "browser",
  target: ["es2022"],
  minify: !debug,
  sourcemap: debug ? "linked" : false,
  metafile: true,
  legalComments: "external",
  assetNames: "assets/[name]-[hash]",
  chunkNames: "chunks/[name]-[hash]",
  loader: { ".ttf": "file", ".woff": "file", ".woff2": "file", ".txt": "text" }
});
Object.assign(result.metafile.inputs, diagramFrame.metafile.inputs);
const frameOutput = relative(repositoryRoot, framePath).split("\\").join("/");
result.metafile.outputs[frameOutput] = {
  ...frameOutputs[0],
  bytes: Buffer.byteLength(frameHtml)
};
await writeFile(join(bundleDirectory, "bundle-manifest.json"), JSON.stringify(result.metafile, null, 2));
const modulesRoot = join(repositoryRoot, "node_modules");
const bundledPackages = new Set();
for (const name of Object.keys(result.metafile.inputs)) {
  const absolute = resolve(repositoryRoot, name);
  if (!absolute.startsWith(modulesRoot + "/") && !absolute.startsWith(modulesRoot + "\\")) {
    continue;
  }
  let folder = dirname(absolute);
  while (folder.startsWith(modulesRoot) && folder !== modulesRoot) {
    try {
      const metadata = JSON.parse(await readFile(join(folder, "package.json"), "utf8"));
      if (metadata.name && metadata.version) {
        bundledPackages.add(folder);
        break;
      }
      folder = dirname(folder);
    }
    catch {
      folder = dirname(folder);
    }
  }
}
await mkdir(join(bundleDirectory, "licenses"), { recursive: true });
const notices = ["MarkVibe — Copyright (c) 2026 Umberto Giacobbi. Application license: MIT.", "Bundled third-party notices; dependency licenses remain applicable.", ""];
notices.push(await readFile(join(repositoryRoot, "THIRD-PARTY-NOTICES.txt"), "utf8"), "");
for (const folder of [...bundledPackages].sort()) {
  const metadata = JSON.parse(await readFile(join(folder, "package.json"), "utf8"));
  const licenses = (await readdir(folder, { withFileTypes: true }))
    .filter(item => item.isFile() && /^(licen[cs]e|copying|notice)(?:[._-].*)?$/iu.test(item.name)).slice(0, 20);
  notices.push(metadata.name + " " + metadata.version + " — " + (typeof metadata.license === "string" ? metadata.license : "See package notices"));
  if (!licenses.length) {
    notices.push("No top-level notice file. Review package licensing before public distribution.");
  }
  for (const item of licenses) {
    const destination = metadata.name.replace(/[@/\\]/gu, "_") + "-" + item.name;
    await copyFile(join(folder, item.name), join(bundleDirectory, "licenses", destination));
    notices.push(await readFile(join(folder, item.name), "utf8"));
  }
  notices.push("");
}
await writeFile(join(bundleDirectory, "THIRD_PARTY_NOTICES.txt"), notices.join("\n"));
// Publish freshness evidence only after every bundle output and notice has been written.
await writeFile(join(bundleDirectory, "input-fingerprint.json"), JSON.stringify({
  fingerprint: inputFingerprint,
  configuration: debug ? "Debug" : "Release",
  outputs: Object.keys(result.metafile.outputs)
}, null, 2));
console.log("Client modules, editor worker, CSS, fonts and third-party notices bundled at " + relative(repositoryRoot, bundleDirectory));
