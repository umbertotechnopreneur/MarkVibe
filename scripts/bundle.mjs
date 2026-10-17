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

import { build } from "esbuild";
import { mkdir, readFile, writeFile, readdir, copyFile, rm, lstat } from "node:fs/promises";
import { dirname, resolve, join, relative } from "node:path";
import { clientFingerprint, verifyInstalledPackages } from "./client-inputs.mjs";

const repositoryRoot = resolve(import.meta.dirname, "..");
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
const result = await build({
  absWorkingDir: repositoryRoot,
  entryPoints: { markvibe: "client/main.js", "editor.worker": "client/editor.worker.js" },
  outdir: bundleDirectory,
  bundle: true,
  splitting: true,
  format: "esm",
  platform: "browser",
  target: ["es2022"],
  minify: true,
  sourcemap: false,
  metafile: true,
  legalComments: "external",
  assetNames: "assets/[name]-[hash]",
  chunkNames: "chunks/[name]-[hash]",
  loader: { ".ttf": "file", ".woff": "file", ".woff2": "file", ".txt": "text" }
});
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
  outputs: Object.keys(result.metafile.outputs)
}, null, 2));
console.log("Client modules, editor worker, CSS, fonts and third-party notices bundled at " + relative(repositoryRoot, bundleDirectory));
