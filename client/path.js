export class WorkspaceError extends Error {
  constructor(code, message) { super(message); this.code = code; }
}
export const limits = Object.freeze({
  entries: 4096, visited: 10000, directories: 1024, depth: 16,
  documentBytes: 1048576, assetBytes: 8 * 1048576, assetBudget: 32 * 1048576,
  zipBytes: 32 * 1048576, zipEntries: 2048, zipExpanded: 64 * 1048576,
  zipDirectoryBytes: 2 * 1048576, imagePixels: 16 * 1048576, imagePixelBudget: 32 * 1048576
});
export function checkCancelled(signal) {
  if (signal?.aborted) throw new WorkspaceError("cancelled", "Action cancelled.");
}
export function normalizePath(path, imported = false) {
  if (typeof path !== "string" || !path.trim() || path.length > 1024 ||
      path.startsWith("/") || /[\\:\u0000-\u001f\u007f]/u.test(path))
    throw new WorkspaceError("invalid-path", "This resource has an unsafe workspace path.");
  const result = [];
  for (const part of path.normalize("NFC").split("/")) {
    if (!part || part === ".") {
      if (imported) throw new WorkspaceError("invalid-path", "The imported path is not canonical.");
      continue;
    }
    if (part === "..") {
      if (imported || !result.length) throw new WorkspaceError("outside-workspace", "This link points outside the opened workspace.");
      result.pop();
    } else result.push(part);
  }
  if (!result.length || result.length > 64)
    throw new WorkspaceError("invalid-path", "This resource has an unsafe workspace path.");
  return result.join("/");
}
const ignored = new Set([".git", "node_modules", "bin", "obj", ".vs", "__macosx"]);
export const isIgnored = path => path.split("/").some(segment => ignored.has(segment.toLowerCase()));
export const isMarkdown = path => /\.(md|markdown|mdown)$/iu.test(path);
export const isSidecar = path => /\.(md|markdown|mdown)\.ink\.json$/iu.test(path);
export const isAsset = path => /\.(png|jpe?g|gif|webp|svg|pdf|txt)$/iu.test(path);
export const isRelevant = path => !isIgnored(path) && (isMarkdown(path) || isSidecar(path) || isAsset(path));
export function safeExternal(value) {
  if (typeof value !== "string" || value.length > 2048 || /[\u0000-\u0020\u007f\\]/u.test(value) || value.startsWith("//")) return null;
  try {
    const url = new URL(value);
    return ["https:", "http:", "mailto:"].includes(url.protocol) && !url.username && !url.password ? url.href : null;
  } catch { return null; }
}
export const resourceUrl = path => new URL(path, document.baseURI).href;
export const nextFrame = () => new Promise(resolve => requestAnimationFrame(resolve));
export const pause = () => new Promise(resolve => setTimeout(resolve, 0));
export const makeId = () => globalThis.crypto?.randomUUID?.() ?? "w-" + Math.random().toString(36).slice(2) + Date.now().toString(36);
export function errorFor(error, permissionWasGranted = false) {
  if (error instanceof WorkspaceError) return error;
  if (error?.name === "AbortError") return new WorkspaceError("cancelled", "No files were selected. Your current workspace is still available.");
  if (error?.name === "NotAllowedError") return new WorkspaceError(permissionWasGranted ? "permission-revoked" : "permission-denied",
    permissionWasGranted ? "Folder access is no longer available. Reopen the folder." : "Folder access was not granted. Choose another folder or import files.");
  if (error?.name === "NotFoundError") return new WorkspaceError("missing-file", "This file is no longer available. Refresh or reopen its folder.");
  if (error?.name === "SecurityError") return new WorkspaceError("unavailable", "Open this app through HTTPS or localhost, then choose the folder again.");
  return new WorkspaceError("read-failed", "This file could not be read. Choose it again or import a documentation ZIP.");
}
