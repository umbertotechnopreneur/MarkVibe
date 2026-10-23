/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; read-only stale bundle guard; 2026-10-16
 * Modified with AI: OpenAI Codex; reject Debug source-map bundles from Release builds; 2026-10-23
 * Human guidance: never launch a successful .NET build with old JavaScript
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { access, readFile } from "node:fs/promises";
import { join, resolve, sep } from "node:path";
import { clientFingerprint, repositoryRoot, validateBundleConfiguration, verifyInstalledPackages } from "./client-inputs.mjs";

try {
  await verifyInstalledPackages();
  const directory = join(repositoryRoot, "src/MarkVibe.Browser/wwwroot/js");
  const saved = JSON.parse(await readFile(join(directory, "input-fingerprint.json"), "utf8"));
  const configurationIndex = process.argv.indexOf("--configuration");
  const configuration = configurationIndex < 0 ? "Release" : process.argv[configurationIndex + 1] ?? "Release";
  validateBundleConfiguration(saved, configuration);
  if (saved.fingerprint !== await clientFingerprint()) {
    throw new Error("Client sources or dependency locks changed after the last bundle.");
  }
  for (const output of saved.outputs) {
    const target = resolve(repositoryRoot, output);
    if (!target.startsWith(directory + sep)) { throw new Error("Unexpected generated output path."); }
    await access(target);
  }
} catch (error) {
  console.error("Client bundle is missing, incomplete or stale: " + error.message);
  console.error("Run pwsh -NoProfile -File scripts/prepare-client.ps1 before building.");
  process.exitCode = 1;
}
