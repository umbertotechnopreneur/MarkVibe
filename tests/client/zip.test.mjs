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

import test from "node:test";
import assert from "node:assert/strict";
import { zipSync, strToU8 } from "fflate";
import { inspectZip, extractEntry, crc32 } from "../../client/zip.js";

/** Generate a synthetic ZIP fixture from independently encoded file contents. */
const archive = (files, options = {}) => zipSync(Object.fromEntries(Object.entries(files).map(([path, text]) => [path, strToU8(text)])), options);
/** Wrap synthetic archive bytes in the file shape consumed by the ZIP importer. */
const file = bytes => new Blob([bytes]);
/** Read central-directory offsets from the synthetic archive bytes. */
function directory(bytes) {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  return { view, end: bytes.length - 22, start: view.getUint32(bytes.length - 6, true) };
}
/** Require the synthetic ZIP fixture to fail with the expected importer error. */
const rejected = async bytes => assert.rejects(() => inspectZip(file(bytes)), error => ["invalid-zip", "invalid-path", "outside-workspace"].includes(error.code));

test("known CRC and streamed extraction agree with independent ZIP generation", async () => {
  assert.equal((crc32(strToU8("123456789")) ^ 0xffffffff) >>> 0, 0xcbf43926);
  for (const level of [0, 6]) {
    const bytes = archive({
      "guide/setup.md": "# Setup\n\nA connected page.\n",
      "assets/info.txt": "asset",
      ".git/config": "ignored"
    }, { level });
    const entries = await inspectZip(file(bytes));
    assert.equal(entries.size, 2);
    assert.ok(!entries.has(".git/config"));
    assert.equal(await (await extractEntry(file(bytes), entries.get("guide/setup.md"), 1048576)).text(), "# Setup\n\nA connected page.\n");
  }
});
test("unsafe paths are rejected even when an entry would otherwise be ignored", async () => {
  for (const path of ["../escape.md", "guide/../../escape.md", "/absolute.md", "guide\\file.md", "C:/file.md", "unused/../secret.exe"]) {
    await rejected(archive({ [path]: "small" }, { level: 0 }));
  }
});
test("malformed, encrypted and ZIP64 directories are rejected before extraction", async () => {
  await rejected(new Uint8Array(30));
  const encrypted = archive({ "README.md": "# Home" }, { level: 0 });
  let details = directory(encrypted);
  details.view.setUint16(details.start + 8, 1, true);
  await rejected(encrypted);
  const zip64 = archive({ "README.md": "# Home" });
  details = directory(zip64);
  details.view.setUint16(details.end + 10, 65535, true);
  await rejected(zip64);
});
test("expansion ratio, actual output and CRC are bounded independently", async () => {
  await rejected(archive({ "README.md": "x".repeat(250000) }, { level: 9 }));
  const bytes = archive({ "README.md": "actual bytes" }, { level: 0 });
  const { view, start } = directory(bytes);
  // Keep both headers consistent while lying about the expected uncompressed size.
  view.setUint32(start + 24, 1, true);
  view.setUint32(22, 1, true);
  await rejected(bytes); // Stored entries must declare equal compressed and uncompressed lengths.
  const compressed = archive({ "README.md": "actual bytes and some repeated bytes repeated bytes" }, { level: 6 });
  const data = directory(compressed);
  data.view.setUint32(data.start + 24, 1, true);
  data.view.setUint32(22, 1, true);
  const entries = await inspectZip(file(compressed));
  await assert.rejects(() => extractEntry(file(compressed), entries.get("README.md"), 1048576), error => error.code === "invalid-zip");
  const corrupt = archive({ "README.md": "actual bytes" }, { level: 0 });
  corrupt[30 + "README.md".length] ^= 1;
  const indexed = await inspectZip(file(corrupt));
  await assert.rejects(() => extractEntry(file(corrupt), indexed.get("README.md"), 1048576), error => error.code === "invalid-zip");
});
test("per-document byte limits and cancellation reject before allocation", async () => {
  const bytes = archive({ "README.md": "small" }, { level: 0 });
  const entries = await inspectZip(file(bytes));
  await assert.rejects(() => extractEntry(file(bytes), entries.get("README.md"), 1), error => error.code === "file-too-large");
  const controller = new AbortController();
  controller.abort();
  await assert.rejects(() => inspectZip(file(bytes), controller.signal), error => error.code === "cancelled");
});
