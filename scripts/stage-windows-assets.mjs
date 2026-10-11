import { readFile, mkdir, copyFile, access } from "node:fs/promises";
import path from "node:path";

// Materialize the Browser build's static-asset manifest, including WASM runtime
// and OpenSilver resources. The desktop host needs neither a dev server nor HTTP.
const [manifestPath, destination] = process.argv.slice(2);
if (!manifestPath || !destination) throw new Error("Usage: stage-windows-assets.mjs <runtime-manifest> <destination>");
const manifest = JSON.parse(await readFile(manifestPath, "utf8"));
const output = path.resolve(destination);
let count = 0;
async function visit(node, segments = []) {
  if (node.Asset && !segments.at(-1)?.match(/\.(gz|br)$/u)) {
    const target = path.resolve(output, ...segments);
    if (!target.startsWith(output + path.sep)) throw new Error("Unsafe static asset path");
    const source = path.resolve(manifest.ContentRoots[node.Asset.ContentRootIndex], node.Asset.SubPath);
    await mkdir(path.dirname(target), { recursive: true }); await copyFile(source, target); count++;
  }
  for (const [name, child] of Object.entries(node.Children ?? {})) await visit(child, [...segments, name]);
}
await visit(manifest.Root);
await access(path.join(output, "index.html"));
await access(path.join(output, "_framework", "blazor.webassembly.js"));
await access(path.join(output, "js", "markvibe.js"));
console.log(`Desktop assets staged: ${count} files`);
