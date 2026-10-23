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
 * Modified with AI: OpenAI Codex; retain only the current map sidecar in provider metadata; 2026-10-16
 * Human guidance: Umberto Giacobbi; simplify the unreleased model without backward compatibility
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import DOMPurify from "dompurify";
import { isDesktop, nativeCall } from "./native.js";
import {
  WorkspaceError, limits, normalizePath, isRelevant, isMarkdown, isMapLayout, mapLayoutFileName, isIgnored,
  checkCancelled, errorFor, resourceUrl, makeId, pause
} from "./path.js";

const workspaces = new Map();
const leases = new Map();
let remembered = null;
let rememberWorkspace = true;
let assetBytes = 0;
let assetPixels = 0;
let pendingAssetBytes = 0;
const decode = new TextDecoder("utf-8", { fatal: true });
const demoPaths = ["README.md", "guide/setup.md", "guide/reading.md", "reference/browser-notes.md", "assets/workspace.svg"];

/** Report the browser's available local file, directory and storage capabilities. */
export function capabilities() {
  const input = document.createElement("input");
  input.type = "file";
  return {
    connected: isDesktop() || globalThis.isSecureContext && typeof window.showDirectoryPicker === "function",
    imported: "webkitdirectory" in input,
    rememberedFolder: remembered !== null
  };
}

/** Read, write or remove the retained directory handle using a bounded IndexedDB transaction. */
async function handleStorage(value, remove = false) {
  if (!globalThis.indexedDB) {
    throw new WorkspaceError("storage-unavailable", "Folder access will last for this session.");
  }
  const database = await new Promise((resolve, reject) => {
    const request = indexedDB.open("markvibe-folders", 1);
    let timedOut = false;
    const timeout = setTimeout(() => {
      timedOut = true;
      reject(new Error("Storage timed out"));
    }, 2500);
    request.onupgradeneeded = () => request.result.createObjectStore("folder");
    request.onsuccess = () => {
      clearTimeout(timeout);
      if (timedOut) {
        request.result.close();
      } else {
        resolve(request.result);
      }
    };
    request.onerror = request.onblocked = () => {
      clearTimeout(timeout);
      reject(request.error ?? new Error("Storage unavailable"));
    };
  });
  try {
    return await new Promise((resolve, reject) => {
      const transaction = database.transaction("folder", value || remove ? "readwrite" : "readonly");
      const store = transaction.objectStore("folder");
      const request = remove ? store.delete("last") : value ? store.put(value, "last") : store.get("last");
      transaction.oncomplete = () => resolve(remove ? null : value ?? request.result ?? null);
      transaction.onerror = transaction.onabort = () => reject(transaction.error ?? new Error("Storage unavailable"));
    });
  } finally {
    database.close();
  }
}
/** Restore the remembered connected handle when browser storage is available. */
export async function initializeWorkspaceStorage() {
  try {
    remembered = await handleStorage();
  } catch {
    remembered = null;
  }
}

/** Enable or clear retained workspace handle memory according to the selected preference. */
export async function setWorkspaceMemory(enabled) {
  // enabled: the C# preference. Disabling it also removes the remembered directory handle.
  rememberWorkspace = enabled;
  if (!enabled) {
    remembered = null;
    try {
      await handleStorage(null, true);
    } catch {
    }
  }
}

/** Check whether the requested retained provider can resume without prompting for new permission. */
export async function canResumeWorkspace(id) {
  // Query only: startup must never trigger a permission prompt without a user gesture.
  try {
    return remembered?.id === id && await remembered.handle.queryPermission({ mode: "read" }) === "granted";
  }
  catch {
    return false;
  }
}

/** Select local files through a temporary input with explicit cancellation and focus recovery. */
export function pickFiles({ directory = false, multiple = true, accept = "" } = {}, signal) {
  checkCancelled(signal);
  if (directory && !capabilities().imported) {
    throw new WorkspaceError("unavailable", "Choose a documentation ZIP or select related files together.");
  }
  return new Promise((resolve, reject) => {
    const input = document.createElement("input");
    input.type = "file";
    input.multiple = multiple;
    input.accept = accept;
    if (directory) {
      input.webkitdirectory = true;
      input.setAttribute("webkitdirectory", "");
    }
    input.className = "mv-picker";
    input.setAttribute("aria-label", directory ? "Choose documentation folder" : "Choose documentation files");
    document.body.append(input);
    let settled = false;
    let focusTimer = 0;
    /** Settle the file selection and release its listeners, timers and temporary input. */
    const finish = (files, error) => {
      if (settled) {
        return;
      }
      settled = true;
      clearTimeout(focusTimer);
      signal?.removeEventListener("abort", abort);
      window.removeEventListener("focus", focus);
      input.remove();
      error ? reject(error) : resolve(files);
    };
    /** Cancel the pending file selection using the existing workspace error. */
    const abort = () => finish(null, new WorkspaceError("cancelled", "Action cancelled."));
    /** Check for a cancelled picker after focus returns without rejecting a completed selection. */
    const focus = () => {
      focusTimer = setTimeout(() => {
        if (!input.files?.length) {
          abort();
        }
      }, 600);
    };
    input.addEventListener("change", () => {
      if (!input.files?.length) {
        return abort();
      }
      if (input.files.length > limits.visited) {
        return finish(null, new WorkspaceError("too-many-files", "Choose a smaller folder or documentation ZIP."));
      }
      finish(Array.from(input.files), null);
    }, { once: true });
    input.addEventListener("cancel", abort, { once: true });
    signal?.addEventListener("abort", abort, { once: true });
    window.addEventListener("focus", focus, { once: true });
    input.click();
  });
}

class Workspace {
  /** Create a workspace identity, mode and bounded resource entry collection. */
  constructor(name, mode, id = makeId()) {
    this.id = id;
    this.name = name;
    this.mode = mode;
    this.entries = new Map();
  }
  /** Describe the provider's workspace identity and currently available capabilities. */
  info() {
    return {
      id: this.id,
      name: this.name,
      mode: this.mode,
      capabilities: {
        canRefresh: this.mode === "connected",
        canReopen: this.mode === "connected" || this.mode === "imported" || this.mode === "zip",
        canWriteSidecars: false,
        canExport: true,
        canAddFiles: ["files", "imported"].includes(this.mode),
        handleRemembered: !!this.handleRemembered
      }
    };
  }
  /** List retained resource entries using the established wire shape and ordering. */
  list() {
    return [...this.entries.entries()].sort(([a], [b]) => a.localeCompare(b)).map(([path, entry]) => ({
      path,
      bytes: entry.size ?? 0,
      kind: isMarkdown(path) ? "document" : isMapLayout(path) ? "sidecar" : "asset"
    }));
  }
  /** Add a canonical relevant resource while enforcing entry and duplicate-path limits. */
  add(path, entry) {
    path = normalizePath(path, true);
    if (!isRelevant(path)) {
      return;
    }
    if (this.entries.has(path)) {
      throw new WorkspaceError("duplicate-path", "Some selected files share a path. Open their folder to preserve their structure.");
    }
    if (this.entries.size >= limits.entries) {
      throw new WorkspaceError("too-many-files", "Choose a workspace with fewer than 4,096 relevant files.");
    }
    this.entries.set(path, entry);
  }
  /** Read a local resource from its provider while enforcing permission, cancellation and byte bounds. */
  async blob(path, maximum, signal) {
    path = normalizePath(path);
    checkCancelled(signal);
    const entry = this.entries.get(path);
    if (!entry) {
      throw new WorkspaceError("missing-file", "This resource is missing. Choose the file or open its folder.");
    }
    let file;
    try {
      if (this.mode === "connected") {
        await this.checkPermission();
        file = await entry.handle.getFile();
      } else if (this.mode === "zip") {
        const { extractEntry } = await import("./zip.js");
        file = await extractEntry(this.file, entry, maximum, signal);
      } else if (this.mode === "demo") {
        const response = await fetch(resourceUrl("demo/" + path.split("/").map(encodeURIComponent).join("/")), { signal, credentials: "same-origin" });
        if (!response.ok) {
          throw new WorkspaceError("missing-file", "The sample resource is unavailable.");
        }
        const length = Number(response.headers.get("content-length"));
        if (Number.isFinite(length) && length > maximum) {
          throw new WorkspaceError("file-too-large", "This file exceeds the reading limit.");
        }
        const reader = response.body?.getReader();
        if (!reader) {
          throw new WorkspaceError("read-failed", "This browser could not read the sample resource.");
        }
        const chunks = [];
        let count = 0;
        try {
          while (true) {
            const { done, value } = await reader.read();
            if (done) {
              break;
            }
            count += value.length;
            if (count > maximum) {
              await reader.cancel();
              throw new WorkspaceError("file-too-large", "This file exceeds the reading limit.");
            }
            chunks.push(value);
          }
          file = new Blob(chunks);
        } finally {
          reader.releaseLock();
        }
      } else {
        file = entry.file;
      }
      checkCancelled(signal);
      if (file.size > maximum) {
        throw new WorkspaceError("file-too-large", "This file exceeds the reading limit.");
      }
      return file;
    } catch (error) {
      throw errorFor(error, this.mode === "connected");
    }
  }
  /** Decode a bounded Markdown resource as UTF-8 and retain the existing document rules. */
  async text(path, signal) {
    const blob = await this.blob(path, limits.documentBytes, signal);
    let text;
    try {
      text = decode.decode(await blob.arrayBuffer());
    } catch {
      throw new WorkspaceError("invalid-text", "Choose a UTF-8 Markdown file to read this page.");
    }
    checkCancelled(signal);
    if (text.includes("\0")) {
      throw new WorkspaceError("invalid-text", "This file does not appear to be a text document.");
    }
    return text;
  }
  /** Require an existing readable connected-folder grant without implicitly requesting a new one. */
  async checkPermission() {
    if (typeof this.root?.queryPermission === "function" && await this.root.queryPermission({ mode: "read" }) !== "granted") {
      throw new WorkspaceError("permission-revoked", "Folder access is no longer available. Reopen the folder.");
    }
  }
  /** Enumerate a connected folder within depth, directory and visited-entry budgets. */
  async scan(signal) {
    await this.checkPermission();
    const next = new Workspace(this.name, this.mode, this.id);
    let visited = 0;
    let directories = 0;
    /** Visit one connected directory and retain only supported contained resources within the traversal budgets. */
    const walk = async (directory, parent, depth) => {
      if (depth > limits.depth || ++directories > limits.directories) {
        throw new WorkspaceError("folder-too-large", "Choose a smaller documentation folder.");
      }
      for await (const [name, handle] of directory.entries()) {
        checkCancelled(signal);
        if (++visited > limits.visited) {
          throw new WorkspaceError("folder-too-large", "Choose a smaller documentation folder.");
        }
        const path = normalizePath(parent + name, true);
        if (isIgnored(path)) {
          continue;
        }
        if (handle.kind === "directory") {
          await walk(handle, path + "/", depth + 1);
        }
        else if (isRelevant(path)) {
          next.add(path, { handle });
        }
        if (visited % 30 === 0) {
          await pause();
        }
      }
    };
    try {
      await walk(this.root, "", 0);
    } catch (error) {
      throw errorFor(error, true);
    }
    this.entries = next.entries;
  }
}

/** Return a retained workspace provider or require the user to reopen it. */
export function getWorkspace(id) {
  const workspace = workspaces.get(id);
  if (!workspace) {
    throw new WorkspaceError("workspace-unavailable", "Reopen this workspace to continue reading.");
  }
  return workspace;
}

class NativeWorkspace extends Workspace {
  /** Retain the identity and capabilities supplied by the Windows native workspace service. */
  constructor(info) {
    super(info.name, "native", info.id);
    this.nativeInfo = info;
  }
  /** Return the native workspace's existing capability description. */
  info() {
    return this.nativeInfo;
  }
  /** Refresh native workspace entries through the authenticated desktop bridge. */
  async scan(signal) {
    const entries = await nativeCall("workspace.entries", { workspaceId: this.id }, signal);
    this.entries = new Map(entries.map(entry => [entry.path, { size: entry.bytes }]));
  }
  /** Read bounded native bytes through the desktop bridge and wrap them as a browser resource. */
  async blob(path, maximum, signal) {
    path = normalizePath(path, true);
    const base64 = await nativeCall("workspace.read", { workspaceId: this.id, path }, signal);
    if (base64.length > Math.ceil(maximum / 3) * 4 + 4) {
      throw new WorkspaceError("file-too-large", "This file exceeds its reading limit.");
    }
    const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
    if (bytes.length > maximum) {
      throw new WorkspaceError("file-too-large", "This file exceeds its reading limit.");
    }
    const extension = path.split(".").at(-1).toLowerCase();
    const types = {
      svg: "image/svg+xml",
      png: "image/png",
      jpg: "image/jpeg",
      jpeg: "image/jpeg",
      gif: "image/gif",
      webp: "image/webp"
    };
    return new Blob([bytes], { type: types[extension] || "application/octet-stream" });
  }
}

/** Opens the requested provider and derives imported identity independently of map and note sidecars. */
export async function openWorkspace(mode, signal, recentPath = "", recentKind = "") {
  checkCancelled(signal);
  if (isDesktop() && ["files", "folder", "connected", "reopen", "resume", "startup", "recent"].includes(mode)) {
    const info = await nativeCall("workspace.open", {
      mode: mode === "startup" ? "files" : mode, recentPath, recentKind
    }, signal);
    const workspace = new NativeWorkspace(info);
    await workspace.scan(signal);
    const old = workspaces.get(workspace.id);
    if (old) {
      releaseWorkspace(old.id);
    }
    workspaces.set(workspace.id, workspace);
    return workspace.info();
  }
  if (mode === "folder") {
    mode = capabilities().connected ? "connected" : "imported";
  }
  let workspace;
  if (mode === "connected" || mode === "reopen" || mode === "resume") {
    let saved = mode === "reopen" || mode === "resume" ? remembered : null;
    if (mode === "resume" && !await canResumeWorkspace(saved?.id)) {
      throw new WorkspaceError("permission-required", "Reopen the last folder to allow reading its documents.");
    }
    if (saved) {
      if (mode !== "resume" && typeof saved.handle.requestPermission === "function" &&
        await saved.handle.requestPermission({ mode: "read" }) !== "granted") {
        throw new WorkspaceError("permission-denied", "Folder access was not granted. Import the folder or choose another one.");
      }
    } else {
      if (!capabilities().connected) {
        if (mode === "reopen") {
          return openWorkspace("imported", signal);
        }
        throw new WorkspaceError("unavailable", "Choose Import folder or a documentation ZIP in this browser.");
      }
      try {
        saved = { handle: await window.showDirectoryPicker({ mode: "read" }), id: makeId() };
      }
      catch (error) {
        throw errorFor(error);
      }
    }
    checkCancelled(signal);
    workspace = new Workspace(saved.handle.name, "connected", saved.id);
    workspace.root = saved.handle;
    await workspace.scan(signal);
    if (rememberWorkspace) {
      remembered = saved;
      try {
        await handleStorage(saved);
        workspace.handleRemembered = true;
      } catch {
        workspace.handleRemembered = false;
      }
    }
  } else if (mode === "imported" || mode === "files") {
    const selected = await pickFiles({ directory: mode === "imported", multiple: true }, signal);
    const root = mode === "imported" ? selected[0]?.webkitRelativePath.split("/")[0] : "";
    if (mode === "imported" && (!root || selected.some(file => !file.webkitRelativePath || !file.webkitRelativePath.startsWith(root + "/")))) {
      throw new WorkspaceError("missing-relative-paths", "This selection did not include folder paths. Import a documentation ZIP instead.");
    }
    workspace = new Workspace(mode === "imported" ? root : selected.length === 1 ? selected[0].name : "Selected files", mode);
    for (const file of selected) {
      const path = mode === "imported" ? file.webkitRelativePath.slice(root.length + 1) : file.name;
      workspace.add(path, { file, size: file.size });
    }
  } else if (mode === "zip") {
    const [file] = await pickFiles({ multiple: false, accept: ".zip,application/zip" }, signal);
    const { inspectZip } = await import("./zip.js");
    workspace = new Workspace(file.name.replace(/\.zip$/iu, ""), "zip");
    workspace.file = file;
    workspace.entries = await inspectZip(file, signal);
  } else if (mode === "demo") {
    if (workspaces.has("demo")) {
      return workspaces.get("demo").info();
    }
    workspace = new Workspace("Field notes", "demo", "demo");
    for (const path of demoPaths) {
      workspace.add(path, {});
    }
  } else {
    throw new WorkspaceError("unavailable", "Choose files, a folder or a documentation ZIP.");
  }
  if (!["demo", "connected"].includes(workspace.mode)) {
    // Imported content is not persisted. A stable identity restores the selected page
    // after the user reselects the same named workspace with the same normalized paths.
    const identity = JSON.stringify([workspace.mode, workspace.name, [...workspace.entries.keys()].filter(path => !isMapLayout(path)).sort()]);
    const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(identity));
    workspace.id = "import:" + [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, "0")).join("");
  }
  checkCancelled(signal);
  const old = workspaces.get(workspace.id);
  if (old) {
    releaseWorkspace(old.id);
  }
  workspaces.set(workspace.id, workspace);
  return workspace.info();
}

/** Report whether the provider can write its persistent map or request an explicit folder write grant. */
async function mapAccess(workspace) {
  const canEnableWrite = workspace.mode === "connected" && typeof workspace.root?.requestPermission === "function";
  const canWrite = workspace.mode === "connected" && await workspace.root.queryPermission({ mode: "readwrite" }) === "granted";
  return { canWrite, canEnableWrite };
}
/** Read the map sidecar and its exact content revision using the current provider's capabilities. */
export async function readMapLayout(id, signal, accessOnly = false) {
  const workspace = getWorkspace(id);
  if (workspace.mode === "native") {
    return nativeCall("map.read", { workspaceId: id }, signal);
  }
  const access = await mapAccess(workspace);
  if (accessOnly) {
    return { json: null, revision: "", ...access };
  }
  let blob;
  if (workspace.mode === "connected") {
    await workspace.checkPermission();
    checkCancelled(signal);
    try {
      blob = await (await workspace.root.getFileHandle(mapLayoutFileName)).getFile();
    }
    catch (error) {
      if (error?.name === "NotFoundError") {
        return { json: null, revision: "", ...access };
      }
      throw errorFor(error, true);
    }
  } else if (workspace.entries.has(mapLayoutFileName)) {
    blob = await workspace.blob(mapLayoutFileName, limits.documentBytes, signal);
  }
  else {
    return { json: null, revision: "", ...access };
  }
  if (blob.size > limits.documentBytes) {
    throw new WorkspaceError("invalid-map", "This map layout exceeds 1 MiB. The original file is preserved.");
  }
  const bytes = await blob.arrayBuffer();
  checkCancelled(signal);
  let json;
  try {
    json = decode.decode(bytes);
  }
  catch {
    throw new WorkspaceError("invalid-map", "This map layout is not UTF-8. The original file is preserved.");
  }
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  const revision = [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, "0")).join("");
  return { json, revision, ...access };
}
/** Request connected-folder write permission directly from the user's save gesture. */
export function enableMapSaving(id) {
  const workspace = getWorkspace(id);
  if (workspace.mode === "native") {
    return Promise.resolve(true);
  }
  if (workspace.mode !== "connected" || !workspace.root?.requestPermission) {
    return Promise.reject(new WorkspaceError("map-read-only", "Download the layout and keep it in the top-level folder."));
  }
  // Call directly from the map button's click, before crossing the async C# bridge.
  return workspace.root.requestPermission({ mode: "readwrite" }).then(permission => {
    if (permission !== "granted") {
      throw new WorkspaceError("map-permission", "Folder write access was not granted. You can download the layout instead.");
    }
    return true;
  });
}
/** Write a contained map sidecar only when its expected revision still matches. */
export async function writeMapLayout(args, signal) {
  const workspace = getWorkspace(args.workspaceId);
  if (workspace.mode === "native") {
    return nativeCall("map.write", args, signal);
  }
  if (typeof args.json !== "string" || new TextEncoder().encode(args.json).length > limits.documentBytes) {
    throw new WorkspaceError("invalid-map", "This map layout exceeds its saving limit.");
  }
  if (!(await mapAccess(workspace)).canWrite) {
    throw new WorkspaceError("map-permission", "Choose Save layout to allow folder write access, or download a JSON copy.");
  }
  /** Create the map conflict error while keeping the pre-existing folder file unchanged. */
  const conflict = () => new WorkspaceError("map-conflict", "The map layout changed in the folder. Download your layout before reopening; the existing file was preserved.");
  if ((await readMapLayout(workspace.id, signal)).revision !== args.revision) {
    throw conflict();
  }
  let stream;
  try {
    const handle = await workspace.root.getFileHandle(mapLayoutFileName, { create: true });
    stream = await handle.createWritable();
    await stream.write(args.json);
    checkCancelled(signal);
    // Existing contents are unchanged until close commits the temporary stream.
    const current = await readMapLayout(workspace.id, signal);
    if (args.revision ? current.revision !== args.revision : current.json !== "") {
      throw conflict();
    }
    await stream.close();
    stream = null;
    const bytes = new TextEncoder().encode(args.json);
    const digest = await crypto.subtle.digest("SHA-256", bytes);
    return [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, "0")).join("");
  } catch (error) {
    if (stream) {
      try {
        await stream.abort();
      } catch {
      }
    }
    throw errorFor(error, true);
  }
}

/** Add selected relevant files to the existing workspace using the established path matching rules. */
export async function addFiles(id, desiredPath, signal) {
  const workspace = getWorkspace(id);
  if (!workspace.info().capabilities.canAddFiles) {
    throw new WorkspaceError("unavailable", "Reopen the containing folder to include this resource.");
  }
  const files = await pickFiles({ multiple: !desiredPath }, signal);
  if (desiredPath && files.length !== 1) {
    throw new WorkspaceError("invalid-selection", "Choose the missing file.");
  }
  const additions = [];
  for (const file of files) {
    const path = normalizePath(desiredPath || file.name, true);
    if (!isRelevant(path)) {
      throw new WorkspaceError("unsupported-file", "Choose Markdown, a supported image or .markvibe-map.json.");
    }
    if (workspace.entries.has(path)) {
      throw new WorkspaceError("duplicate-path", "This path is already open. Select the containing folder to refresh it.");
    }
    if (additions.some(item => item.path === path)) {
      throw new WorkspaceError("duplicate-path", "Select the containing folder to preserve these filenames.");
    }
    additions.push({ path, file });
  }
  if (workspace.entries.size + additions.length > limits.entries) {
    throw new WorkspaceError("too-many-files", "This workspace has reached its file limit.");
  }
  if ([...workspace.entries.keys()].filter(isMarkdown).length + additions.filter(item => isMarkdown(item.path)).length > 1024) {
    throw new WorkspaceError("too-many-documents", "This workspace has reached its 1,024 document limit.");
  }
  for (const { path, file } of additions) {
    workspace.add(path, { file, size: file.size });
  }
  return additions.map(entry => entry.path);
}

/** Remove executable and external SVG content and retain bounded image dimensions. */
async function sanitizeSvg(blob) {
  if (blob.size > 256 * 1024) {
    throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
  }
  const text = decode.decode(await blob.arrayBuffer());
  const clean = DOMPurify.sanitize(text, {
    USE_PROFILES: { svg: true, svgFilters: false },
    FORBID_TAGS: ["script", "style", "foreignObject", "image", "use", "a", "animate", "animateMotion", "animateTransform", "set", "filter"],
    FORBID_ATTR: ["style", "href", "xlink:href"],
    RETURN_DOM: true
  });
  const svg = clean.querySelector("svg");
  if (!svg || clean.querySelectorAll("*").length > 2048) {
    throw new WorkspaceError("invalid-image", "This SVG cannot be displayed safely.");
  }
  for (const element of clean.querySelectorAll("*")) {
    for (const attribute of [...element.attributes])
      if (/url\s*\(/iu.test(attribute.value) && !/^url\(\s*#[a-zA-Z0-9_-]+\s*\)$/u.test(attribute.value)) {
        element.removeAttribute(attribute.name);
      }
  }
  const box = (svg.getAttribute("viewBox") || "").trim().split(/[\s,]+/u).map(Number);
  if (box.length === 4 && (!box.every(Number.isFinite) || box[2] <= 0 || box[3] <= 0 || box[2] > 10000 || box[3] > 10000)) {
    throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
  }
  /** Read a finite bounded SVG dimension or use the established safe fallback. */
  const dimension = (name, fallback) => {
    const value = svg.getAttribute(name);
    return value && /^\d+(?:\.\d+)?(?:px)?$/u.test(value) ? parseFloat(value) : fallback;
  };
  const width = dimension("width", box.length === 4 ? box[2] : 300);
  const height = dimension("height", box.length === 4 ? box[3] : 150);
  if (!Number.isFinite(width * height) || width <= 0 || height <= 0 || width > 10000 || height > 10000 || width * height > limits.imagePixels) {
    throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
  }
  svg.setAttribute("width", String(width));
  svg.setAttribute("height", String(height));
  return {
    blob: new Blob([new XMLSerializer().serializeToString(svg)], { type: "image/svg+xml" }),
    pixels: Math.ceil(width * height)
  };
}

/** Validate a local image's format and dimensions before creating a displayable resource. */
async function imageBlob(blob, path) {
  if (/\.svg$/iu.test(path)) {
    return sanitizeSvg(blob);
  }
  const header = new Uint8Array(await blob.slice(0, 65536).arrayBuffer());
  const view = new DataView(header.buffer);
  let width = 0;
  let height = 0;
  let type = "";
  if (/\.png$/iu.test(path) && header.length >= 24 && view.getUint32(0) === 0x89504e47 && view.getUint32(4) === 0x0d0a1a0a) {
    width = view.getUint32(16);
    height = view.getUint32(20);
    type = "image/png";
  } else if (/\.gif$/iu.test(path) && header.length >= 10 && new TextDecoder().decode(header.subarray(0, 6)).match(/^GIF8[79]a$/u)) {
    width = view.getUint16(6, true);
    height = view.getUint16(8, true);
    type = "image/gif";
  } else if (/\.jpe?g$/iu.test(path) && header[0] === 255 && header[1] === 216) {
    let cursor = 2;
    while (cursor + 4 < header.length) {
      if (header[cursor] !== 255) {
        break;
      }
      while (header[cursor] === 255) {
        cursor++;
      }
      const marker = header[cursor++];
      if (marker === 217 || marker === 218) {
        break;
      }
      if (marker === 1 || marker >= 208 && marker <= 215) {
        continue;
      }
      if (cursor + 2 > header.length) {
        break;
      }
      const length = view.getUint16(cursor);
      if (length < 2 || cursor + length > header.length) {
        break;
      }
      if ([192, 193, 194, 195, 197, 198, 199, 201, 202, 203, 205, 206, 207].includes(marker) && length >= 7) {
        height = view.getUint16(cursor + 3);
        width = view.getUint16(cursor + 5);
        type = "image/jpeg";
        break;
      }
      cursor += length;
    }
  } else if (/\.webp$/iu.test(path) && header.length >= 30 && view.getUint32(0) === 0x52494646 && view.getUint32(8) === 0x57454250) {
    const kind = new TextDecoder().decode(header.subarray(12, 16));
    if (kind === "VP8X") {
      width = 1 + header[24] + (header[25] << 8) + (header[26] << 16);
      height = 1 + header[27] + (header[28] << 8) + (header[29] << 16);
    }
    else if (kind === "VP8 " && header[23] === 157 && header[24] === 1 && header[25] === 42) {
      width = view.getUint16(26, true) & 16383;
      height = view.getUint16(28, true) & 16383;
    }
    else if (kind === "VP8L" && header[20] === 47) {
      const bits = view.getUint32(21, true);
      width = (bits & 16383) + 1;
      height = ((bits >>> 14) & 16383) + 1;
    }
    type = "image/webp";
  }
  if (!width || !height || width > 10000 || height > 10000 || width * height > limits.imagePixels) {
    throw new WorkspaceError("invalid-image", "Choose a supported image smaller than 16 megapixels.");
  }
  return { blob: new Blob([blob], { type }), pixels: width * height };
}

/** Acquire a bounded local image lease and track its owner, byte and pixel budgets. */
export async function acquireAsset(workspaceId, path, owner, signal) {
  const workspace = getWorkspace(workspaceId);
  path = normalizePath(path);
  if (!/\.(png|jpe?g|gif|webp|svg)$/iu.test(path)) {
    throw new WorkspaceError("unsupported-image", "Choose a PNG, JPEG, GIF, WebP or SVG image.");
  }
  if (leases.size >= 96 || assetBytes + pendingAssetBytes + limits.assetBytes > limits.assetBudget) {
    throw new WorkspaceError("asset-budget", "Some images were paused to keep this workspace responsive.");
  }
  pendingAssetBytes += limits.assetBytes;
  let blob;
  try {
    blob = await workspace.blob(path, limits.assetBytes, signal);
    const image = await imageBlob(blob, path);
    blob = image.blob;
    checkCancelled(signal);
    if (leases.size >= 96 || assetBytes + blob.size > limits.assetBudget || assetPixels + image.pixels > limits.imagePixelBudget) {
      throw new WorkspaceError("asset-budget", "Some images were paused to keep this page responsive.");
    }
    const leaseId = makeId();
    const url = URL.createObjectURL(blob);
    leases.set(leaseId, { owner, workspaceId, bytes: blob.size, pixels: image.pixels, url });
    assetBytes += blob.size;
    assetPixels += image.pixels;
    return { url, leaseId, bytes: blob.size };
  } finally {
    pendingAssetBytes -= limits.assetBytes;
  }
}
/** Read a validated local image for export without retaining a reader object-URL lease. */
export async function readExportImage(workspaceId, path, signal) {
  // Return the same sanitized image representation without duplicating reader leases.
  // The export adapter enforces its own aggregate byte/pixel budget sequentially.
  path = normalizePath(path);
  if (!/\.(png|jpe?g|gif|webp|svg)$/iu.test(path)) {
    throw new WorkspaceError("unsupported-image", "This image format cannot be exported.");
  }
  return imageBlob(await getWorkspace(workspaceId).blob(path, limits.assetBytes, signal), path);
}
/** Release every asset lease associated with the specified resource owner. */
export function releaseAssets(owner) {
  for (const [id, lease] of leases)
    if (lease.owner === owner) {
      URL.revokeObjectURL(lease.url);
      assetBytes -= lease.bytes;
      assetPixels -= lease.pixels;
      leases.delete(id);
    }
}
/** Dispose the retained provider and leases for the requested workspace. */
export function releaseWorkspace(id) {
  for (const lease of [...leases.values()]) if (lease.workspaceId === id) {
    releaseAssets(lease.owner);
  }
  workspaces.delete(id);
}
/** Release all retained workspace providers and associated browser resources. */
export function disposeWorkspaces() {
  for (const lease of [...leases.values()]) {
    releaseAssets(lease.owner);
  }
  workspaces.clear();
  remembered = null;
}
