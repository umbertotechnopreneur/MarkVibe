/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; deterministic client input fingerprint; 2026-10-16
 * Modified with AI: OpenAI Codex; include bundled distribution notices in stale-output detection; 2026-10-16
 * Modified with AI: OpenAI Codex; prevent Debug client output and source maps from entering Release builds; 2026-10-23
 * Human guidance: reject stale bundles and dependencies before building
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { createHash } from "node:crypto";
import { readFile, readdir } from "node:fs/promises";
import { join, resolve } from "node:path";

export const repositoryRoot = resolve(import.meta.dirname, "..");

/** Reject Debug assets in non-Debug builds; Debug may also consume the normal optimized client. */
export function validateBundleConfiguration(saved, configuration = "Release") {
  if (!["Debug", "Release"].includes(saved.configuration) || !Array.isArray(saved.outputs)) {
    throw new Error("Client bundle configuration evidence is missing. Run npm run bundle again.");
  }
  if (configuration.toLowerCase() !== "debug") {
    if (saved.configuration !== "Release") {
      throw new Error("Debug client assets cannot be used by a " + configuration + " build. Run npm run bundle.");
    }
    if (saved.outputs.some(output => output.endsWith(".map"))) {
      throw new Error("Release client assets must not include source maps. Run npm run bundle.");
    }
  }
}

/** Require the installed npm graph to agree with the committed lock before consuming generated output. */
export async function verifyInstalledPackages() {
  const locked = JSON.parse(await readFile(join(repositoryRoot, "package-lock.json"), "utf8"));
  let installed;

  try {
    installed = JSON.parse(await readFile(join(repositoryRoot, "node_modules/.package-lock.json"), "utf8"));
  } catch {
    throw new Error("Installed dependency evidence is missing. Run npm ci --ignore-scripts --no-audit --no-fund.");
  }

  for (const [path, packageInfo] of Object.entries(installed.packages || {})) {
    const expected = locked.packages[path];
    if (!expected ||
      packageInfo.version !== expected.version ||
      packageInfo.integrity !== expected.integrity) {
      throw new Error("Installed dependencies differ from package-lock.json. Run npm ci --ignore-scripts --no-audit --no-fund.");
    }
  }

  for (const [path, packageInfo] of Object.entries(locked.packages)) {
    if (path && !packageInfo.optional && !installed.packages?.[path]) {
      throw new Error("Required dependency missing from the installed graph: " + path + ". Run npm ci.");
    }
  }
}

/** Collect maintained client files and embedded JSON inputs in deterministic portable path order. */
async function inputPaths(directory, jsonOnly = false) {
  const paths = [];

  for (const item of await readdir(join(repositoryRoot, directory), { withFileTypes: true })) {
    if (item.name === "bin" || item.name === "obj") {
      continue;
    }

    const path = directory + "/" + item.name;
    if (item.isDirectory()) {
      paths.push(...await inputPaths(path, jsonOnly));
    } else if (item.isFile() && (!jsonOnly || item.name.endsWith(".json"))) {
      paths.push(path);
    }
  }

  return paths;
}

/** Hash client contracts and bundled legal notices so changes cannot reuse stale generated distribution files. */
export async function clientFingerprint() {
  const files = [
    ...(await inputPaths("client")),
    ...(await inputPaths("src/MarkVibe.Core", true)),
    "package.json",
    "package-lock.json",
    "THIRD-PARTY-NOTICES.txt",
    "scripts/bundle.mjs",
    "scripts/client-inputs.mjs"
  ].sort();

  const hash = createHash("sha256");
  for (const path of files) {
    hash.update(path + "\0");
    hash.update(await readFile(join(repositoryRoot, path)));
    hash.update("\0");
  }

  return hash.digest("hex");
}
