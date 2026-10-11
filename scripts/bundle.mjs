import { build } from "esbuild";
import { mkdir, readFile, writeFile, readdir, copyFile, rm, lstat } from "node:fs/promises";
import { dirname, resolve, join, relative } from "node:path";

const root = resolve(import.meta.dirname, "..");
const output = join(root, "src/MarkVibe.Browser/wwwroot/js");
if (!output.startsWith(root + "/") && !output.startsWith(root + "\\")) throw new Error("Unsafe generated output directory");
const previous = await lstat(output).catch(error => { if (error.code !== "ENOENT") throw error; return null; });
if (previous?.isSymbolicLink()) throw new Error("Generated client output must not be a link");
await rm(output, { recursive: true, force: true });
await mkdir(output, { recursive: true });
const result = await build({
  absWorkingDir: root,
  entryPoints: { markvibe: "client/main.js", "editor.worker": "client/editor.worker.js" },
  outdir: output, bundle: true, splitting: true, format: "esm", platform: "browser",
  target: ["es2022"], minify: true, sourcemap: false, metafile: true, legalComments: "external",
  assetNames: "assets/[name]-[hash]", chunkNames: "chunks/[name]-[hash]",
  loader: { ".ttf": "file", ".woff": "file", ".woff2": "file" }
});
await writeFile(join(output, "bundle-manifest.json"), JSON.stringify(result.metafile, null, 2));
const modulesRoot = join(root, "node_modules");
const packages = new Set();
for (const name of Object.keys(result.metafile.inputs)) {
  const absolute = resolve(root, name);
  if (!absolute.startsWith(modulesRoot + "/") && !absolute.startsWith(modulesRoot + "\\")) continue;
  let folder = dirname(absolute);
  while (folder.startsWith(modulesRoot) && folder !== modulesRoot) {
    try {
      const metadata = JSON.parse(await readFile(join(folder, "package.json"), "utf8"));
      if (metadata.name && metadata.version) { packages.add(folder); break; }
      folder = dirname(folder);
    }
    catch { folder = dirname(folder); }
  }
}
await mkdir(join(output, "licenses"), { recursive: true });
const notices = ["MarkVibe — Copyright (c) 2026 Umberto Giacobbi. Application license: MIT.", "Bundled third-party notices; dependency licenses remain applicable.", ""];
for (const folder of [...packages].sort()) {
  const metadata = JSON.parse(await readFile(join(folder, "package.json"), "utf8"));
  const licenses = (await readdir(folder, { withFileTypes: true }))
    .filter(item => item.isFile() && /^(licen[cs]e|copying|notice)(?:[._-].*)?$/iu.test(item.name)).slice(0, 20);
  notices.push(metadata.name + " " + metadata.version + " — " + (typeof metadata.license === "string" ? metadata.license : "See package notices"));
  if (!licenses.length) notices.push("No top-level notice file. Review package licensing before public distribution.");
  for (const item of licenses) {
    const destination = metadata.name.replace(/[@/\\]/gu, "_") + "-" + item.name;
    await copyFile(join(folder, item.name), join(output, "licenses", destination));
    notices.push(await readFile(join(folder, item.name), "utf8"));
  }
  notices.push("");
}
await writeFile(join(output, "THIRD_PARTY_NOTICES.txt"), notices.join("\n"));
console.log("Client modules, editor worker, CSS, fonts and third-party notices bundled at " + relative(root, output));
