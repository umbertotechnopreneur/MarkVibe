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
import { normalizePath, isIgnored, safeExternal, checkCancelled } from "../../client/path.js";

test("provider paths preserve encoded filenames and normalize Unicode without decoding", () => {
  assert.equal(normalizePath("guide/cafe\u0301.md", true), "guide/café.md");
  assert.equal(normalizePath("guide/%2e%2e.md", true), "guide/%2e%2e.md");
  assert.equal(normalizePath("guide/../README.md"), "README.md");
});
test("imported paths cannot collapse traversal, absolute paths, controls or backslashes", () => {
  for (const path of ["../a.md", "a/../b.md", "/a.md", "C:/a.md", "a\\b.md", "a//b.md", "./a.md", "a/\0b.md"]) {
    assert.throws(() => normalizePath(path, true));
  }
  assert.ok(isIgnored("folder/NoDe_MoDuLeS/readme.md"));
  assert.ok(isIgnored(".git/config"));
  assert.throws(() => normalizePath("a/".repeat(65) + "b.md"));
});
test("external navigation rejects execution, credentials and URL ambiguity", () => {
  for (const url of ["javascript:alert(1)", "data:text/html,x", "//example.com", "https://user:pass@example.com", "https:\\example.com", " https://example.com"]) {
    assert.equal(safeExternal(url), null);
  }
  assert.equal(safeExternal("https://example.com/page"), "https://example.com/page");
  assert.equal(safeExternal("mailto:reader@example.com"), "mailto:reader@example.com");
});
test("cancelled jobs produce a typed error without reading data", () => {
  const controller = new AbortController();
  controller.abort();
  assert.throws(() => checkCancelled(controller.signal), error => error.code === "cancelled");
});
