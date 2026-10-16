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
 * Modified with AI: OpenAI Codex; exclude retired annotation sidecars from workspace resources; 2026-10-16
 * Human guidance: Umberto Giacobbi; simplify the unreleased model without backward compatibility
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

export class WorkspaceError extends Error {
  /** Retain the existing workspace error code alongside its user-facing explanation. */
  constructor(code, message) {
    super(message);
    this.code = code;
  }
}
export const limits = Object.freeze({
  entries: 4096,
  visited: 10000,
  directories: 1024,
  depth: 16,
  documentBytes: 1048576,
  assetBytes: 8 * 1048576,
  assetBudget: 32 * 1048576,
  zipBytes: 32 * 1048576,
  zipEntries: 2048,
  zipExpanded: 64 * 1048576,
  zipDirectoryBytes: 2 * 1048576,
  imagePixels: 16 * 1048576,
  imagePixelBudget: 32 * 1048576
});
/** Stop the current browser action when its cancellation signal is already aborted. */
export function checkCancelled(signal) {
  if (signal?.aborted) {
    throw new WorkspaceError("cancelled", "Action cancelled.");
  }
}
/** Canonicalize a relative workspace path while rejecting unsafe names and traversal beyond the root. */
export function normalizePath(path, imported = false) {
  if (typeof path !== "string" || !path.trim() || path.length > 1024 ||
    path.startsWith("/") || /[\\:\u0000-\u001f\u007f]/u.test(path)) {
    throw new WorkspaceError("invalid-path", "This resource has an unsafe workspace path.");
  }
  const pathSegments = [];
  for (const pathSegment of path.normalize("NFC").split("/")) {
    if (!pathSegment || pathSegment === ".") {
      if (imported) {
        throw new WorkspaceError("invalid-path", "The imported path is not canonical.");
      }
      continue;
    }
    if (pathSegment === "..") {
      if (imported || !pathSegments.length) {
        throw new WorkspaceError("outside-workspace", "This link points outside the opened workspace.");
      }
      pathSegments.pop();
    } else {
      pathSegments.push(pathSegment);
    }
  }
  if (!pathSegments.length || pathSegments.length > 64) {
    throw new WorkspaceError("invalid-path", "This resource has an unsafe workspace path.");
  }
  return pathSegments.join("/");
}
const ignored = new Set([".git", "node_modules", "bin", "obj", ".vs", "__macosx"]);
/** Check whether a path includes a directory excluded from documentation workspaces. */
export const isIgnored = path => path.split("/").some(segment => ignored.has(segment.toLowerCase()));
/** Recognize supported Markdown filename extensions without case sensitivity. */
export const isMarkdown = path => /\.(md|markdown|mdown)$/iu.test(path);
export const mapLayoutFileName = ".markvibe-map.json";
/** Recognize the workspace's reserved persistent map filename. */
export const isMapLayout = path => path === mapLayoutFileName;
/** Recognize the local resource extensions supported by workspace providers. */
export const isAsset = path => /\.(png|jpe?g|gif|webp|svg|pdf|txt)$/iu.test(path);
/** Select supported documentation resources while excluding ignored paths. */
export const isRelevant = path => !isIgnored(path) && (isMarkdown(path) || isMapLayout(path) || isAsset(path));
/** Return an allowed external URL or reject unsafe schemes, credentials and control characters. */
export function safeExternal(value) {
  if (typeof value !== "string" || value.length > 2048 || /[\u0000-\u0020\u007f\\]/u.test(value) || value.startsWith("//")) {
    return null;
  }
  try {
    const url = new URL(value);
    return ["https:", "http:", "mailto:"].includes(url.protocol) && !url.username && !url.password ? url.href : null;
  } catch {
    return null;
  }
}
/** Resolve an application resource against the current browser base URI. */
export const resourceUrl = path => new URL(path, document.baseURI).href;
/** Yield until the next animation frame before work that depends on visible layout. */
export const nextFrame = () => new Promise(resolve => requestAnimationFrame(resolve));
/** Yield to the event loop between bounded processing batches. */
export const pause = () => new Promise(resolve => setTimeout(resolve, 0));
/** Generate an opaque local identity using browser UUID support or the established fallback. */
export const makeId = () => globalThis.crypto?.randomUUID?.() ?? "w-" + Math.random().toString(36).slice(2) + Date.now().toString(36);
/** Translate browser capability and file failures into the existing workspace error contract. */
export function errorFor(error, permissionWasGranted = false) {
  if (error instanceof WorkspaceError) {
    return error;
  }
  if (error?.name === "AbortError") {
    return new WorkspaceError("cancelled", "No files were selected. Your current workspace is still available.");
  }
  if (error?.name === "NotAllowedError") {
    return new WorkspaceError(permissionWasGranted ? "permission-revoked" : "permission-denied",
      permissionWasGranted ? "Folder access is no longer available. Reopen the folder." : "Folder access was not granted. Choose another folder or import files.");
  }
  if (error?.name === "NotFoundError") {
    return new WorkspaceError("missing-file", "This file is no longer available. Refresh or reopen its folder.");
  }
  if (error?.name === "SecurityError") {
    return new WorkspaceError("unavailable", "Open this app through HTTPS or localhost, then choose the folder again.");
  }
  return new WorkspaceError("read-failed", "This file could not be read. Choose it again or import a documentation ZIP.");
}
