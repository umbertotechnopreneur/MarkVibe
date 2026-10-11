import { MarkdownHost } from "./markdown.js";
import { SourceHost, releaseSourceWorkspace, disposeSourceModels } from "./source.js";
import { MapHost } from "./map.js";
import { openWorkspace, getWorkspace, initializeWorkspaceStorage, capabilities, addFiles, acquireAsset,
  releaseAssets, releaseWorkspace, disposeWorkspaces, setWorkspaceMemory, canResumeWorkspace } from "./workspaces.js";
import { readPreferences, savePreferences } from "./settings.js";
import { isDesktop, nativeCall, initializeDesktop, rememberNative, downloadSidecar } from "./native.js";
import { WorkspaceError, checkCancelled, errorFor, nextFrame, normalizePath, makeId } from "./path.js";

const views = new Map(), snapshots = new Map(), jobs = new Map(), attachments = new Map();
const inkEvents = new Set();
let endpoint = null, syncPair = null, syncLocked = false, firstHistory = true, preferenceRevision = 0;
let exportAdapter;
let preferences = { appearance: "system", documentStyle: "technical", zoom: 100, simpleSource: false, rememberLastDocument: true };
const colorScheme = matchMedia("(prefers-color-scheme: dark)");
const emit = value => {
  const operation = endpoint?.invokeMethodAsync("Receive", JSON.stringify(value)).catch(() => {}) ?? Promise.resolve();
  if (value.type === "ink") { inkEvents.add(operation); operation.finally(() => inkEvents.delete(operation)); }
  return operation;
};
function savedEntry(workspaceId) {
  if (!preferences.rememberLastDocument) return null;
  try {
    const stored = localStorage.getItem("markvibe.lastDocument");
    const entries = [history.state?.markvibe, stored && stored.length < 20000 ? JSON.parse(stored) : null];
    for (const entry of entries) {
      if (!entry || workspaceId && entry.workspaceId !== workspaceId || typeof entry.workspaceId !== "string" ||
          typeof entry.id !== "string" || typeof entry.path !== "string" || typeof entry.anchor !== "string") continue;
      if (normalizePath(entry.path, true) !== entry.path) continue;
      const position = entry.position;
      if (!position || typeof position.blockId !== "string" || !Number.isFinite(position.sourceLine) ||
          !Number.isFinite(position.fraction) || position.fraction < 0 || position.fraction > 1 || position.sourceLine < 1) continue;
      return entry;
    }
  } catch { return null; }
  return null;
}
function rememberEntry(entry) {
  if (!preferences.rememberLastDocument) return;
  try {
    const info = getWorkspace(entry.workspaceId).info();
    localStorage.setItem("markvibe.lastWorkspace", JSON.stringify({ version: 1, id: info.id, name: info.name, mode: info.mode }));
    localStorage.setItem("markvibe.lastDocument", JSON.stringify(entry));
    if (isDesktop()) rememberNative(info.mode === "native" ? entry : null, info.mode === "native").catch(() => emit({ type: "status", message: "The last document could not be remembered on disk." }));
  } catch { }
}
async function resumeInfo(entry) {
  try {
    const raw = localStorage.getItem("markvibe.lastWorkspace");
    if (!entry || entry.workspaceId === "demo" || !raw || raw.length > 4096) return null;
    const saved = JSON.parse(raw);
    if (saved.version !== 1 || saved.id !== entry.workspaceId || typeof saved.name !== "string" ||
        !["native", "connected", "imported", "files", "zip"].includes(saved.mode)) return null;
    return { mode: saved.mode, name: saved.name, workspaceId: saved.id, path: entry.path, anchor: entry.anchor,
      position: entry.position, canResume: saved.mode === "native" ? isDesktop() : saved.mode === "connected" && await canResumeWorkspace(saved.id) };
  } catch { return null; }
}
function navigationHash(entry) { return "#doc=" + encodeURIComponent(entry.path) + "&anchor=" + encodeURIComponent(entry.anchor || ""); }
function pushHistory(entry, replace = false) {
  const state = { ...(typeof history.state === "object" && history.state ? history.state : {}), markvibe: entry };
  if (replace || firstHistory) history.replaceState(state, "", navigationHash(entry));
  else history.pushState(state, "", navigationHash(entry));
  firstHistory = false;
  rememberEntry(entry);
}
function persistPosition(position) {
  const entry = history.state?.markvibe;
  if (!entry) return;
  const updated = { ...entry, position };
  history.replaceState({ ...history.state, markvibe: updated }, "", location.href);
  rememberEntry(updated);
}
function configureSync() {
  if (!syncPair) return;
  const reader = views.get(syncPair.readerId), source = views.get(syncPair.sourceId);
  if (!reader || !source) return;
  const synchronize = (from, to) => {
    if (!syncPair.enabled || syncLocked || reader.workspaceId !== source.workspaceId || reader.model?.path !== source.path) return;
    syncLocked = true; to.scrollToLine(from.capture().sourceLine);
    requestAnimationFrame(() => requestAnimationFrame(() => { syncLocked = false; }));
  };
  reader.scrollListener = () => synchronize(reader, source);
  source.scrollListener = () => {
    synchronize(source, reader);
    const entry = history.state?.markvibe;
    if (!syncPair.enabled && source.container.getBoundingClientRect().height > 0 &&
        source.workspaceId === entry?.workspaceId && source.path === entry?.path) persistPosition(source.capture());
  };
}
async function getView(id, signal) {
  for (let frame = 0; frame < 32; frame++) {
    checkCancelled(signal);
    const view = views.get(id);
    if (view?.container.isConnected) return view;
    if (view) window.markvibe.detach(id);
    if (!attachments.has(id) && document.querySelector('[data-mv-host="' + id + '"]'))
      await window.markvibe.attach(id, snapshots.get(id)?.kind);
    await nextFrame();
  }
  throw new WorkspaceError("view-unavailable", "This view is still loading. Choose it again to continue.");
}
async function applyPreferences(args) {
  const revision = ++preferenceRevision;
  const rememberedBefore = preferences.rememberLastDocument;
  preferences = { ...preferences, ...args.preferences };
  const reader = views.get(args.readerId), source = views.get(args.sourceId);
  const readerVisible = reader?.container.getBoundingClientRect().height > 0;
  const position = reader?.model && readerVisible ? reader.capture() : null;
  const dark = preferences.appearance === "dark" || preferences.appearance === "system" && colorScheme.matches;
  document.documentElement.dataset.theme = dark ? "dark" : "light";
  document.documentElement.dataset.documentStyle = preferences.documentStyle;
  document.documentElement.style.setProperty("--reading-scale", String(preferences.zoom / 100));
  for (const view of views.values()) {
    view.container.dataset.theme = dark ? "dark" : "light";
    view.container.dataset.documentStyle = preferences.documentStyle;
  }
  const persisted = savePreferences(preferences);
  if (rememberedBefore !== preferences.rememberLastDocument) {
    await setWorkspaceMemory(preferences.rememberLastDocument);
    if (!preferences.rememberLastDocument) {
      try { localStorage.removeItem("markvibe.lastDocument"); localStorage.removeItem("markvibe.lastWorkspace"); } catch { }
      if (isDesktop()) await rememberNative(null, false);
    } else if (history.state?.markvibe) rememberEntry(history.state.markvibe);
  }
  await source?.theme();
  if (reader?.model && readerVisible && revision === preferenceRevision) {
    await reader.retheme(() => revision === preferenceRevision);
    if (position && revision === preferenceRevision) reader.restore(position);
  }
  return { dark, persisted };
}
async function dispatch(operation, args, signal) {
  switch (operation) {
    case "initialize": {
      preferences = readPreferences();
      const desktop = await initializeDesktop();
      if (desktop && !preferences.rememberLastDocument) {
        await rememberNative(null, false);
        localStorage.removeItem("markvibe.lastDocument"); localStorage.removeItem("markvibe.lastWorkspace");
      }
      await initializeWorkspaceStorage();
      await setWorkspaceMemory(preferences.rememberLastDocument);
      const hash = new URLSearchParams(location.hash.slice(1)), current = savedEntry();
      const path = hash.get("doc") || current?.path || "";
      const anchor = hash.has("anchor") ? hash.get("anchor") : current?.anchor || "";
      const position = current?.path === path && current.anchor === anchor ? current.position : null;
      return { preferences, dark: preferences.appearance === "dark" || preferences.appearance === "system" && colorScheme.matches,
        rememberedFolder: capabilities().rememberedFolder, path, anchor, position, resume: await resumeInfo(current), desktop: isDesktop(), startup: !!desktop?.startup };
    }
    case "preferences.apply": return applyPreferences(args);
    case "document.export":
      exportAdapter ??= await import("./export.js");
      return exportAdapter.exportDocument(args, signal);
    case "workspace.open": return openWorkspace(args.mode, signal);
    case "ink.read": {
      const workspace = getWorkspace(args.workspaceId);
      if (workspace.mode === "native") return nativeCall("ink.read", args, signal);
      const path = args.path + ".ink.json";
      return { json: workspace.entries.has(path) ? await workspace.text(path, signal) : null, revision: "" };
    }
    case "ink.flush":
      if ([...views.values()].some(view => view.inkView?.active)) throw new WorkspaceError("ink-active", "Finish the current stroke before changing documents or exporting.");
      await Promise.all([...inkEvents]); return true;
    case "ink.write": return nativeCall("ink.write", args, signal);
    case "ink.download": {
      if (isDesktop()) return nativeCall("export.sidecar", args, signal);
      downloadSidecar(args.json, args.filename); return true;
    }
    case "ink.render": {
      const snapshot = snapshots.get(args.hostId); if (snapshot) snapshot.ink = args;
      views.get(args.hostId)?.setInk(args); return true;
    }
    case "history.saved": {
      const path = getWorkspace(args.workspaceId).nativeInfo?.selectedPath;
      return path ? { id: makeId(), workspaceId: args.workspaceId, path, anchor: "", position: { blockId: "", fraction: 0, sourceLine: 1 } } : savedEntry(args.workspaceId);
    }
    case "workspace.entries": return getWorkspace(args.workspaceId).list();
    case "workspace.text": return getWorkspace(args.workspaceId).text(args.path, signal);
    case "workspace.refresh": {
      const workspace = getWorkspace(args.workspaceId);
      if (!workspace.info().capabilities.canRefresh) throw new WorkspaceError("reimport-needed", "Select this folder or bundle again to refresh its contents.");
      await workspace.scan(signal); releaseAssetsForWorkspace(args.workspaceId); return true;
    }
    case "workspace.release":
      for (const [id, snapshot] of snapshots) if (snapshot.workspaceId === args.workspaceId) snapshots.delete(id);
      releaseSourceWorkspace(args.workspaceId); releaseWorkspace(args.workspaceId); return true;
    case "workspace.addFiles": return addFiles(args.workspaceId, "", signal);
    case "workspace.addMissing": return addFiles(args.workspaceId, normalizePath(args.path, true), signal);
    case "assets.acquire": return acquireAsset(args.workspaceId, args.path, args.owner, signal);
    case "assets.release": releaseAssets(args.owner); return true;
    case "markdown.render": {
      const previous = snapshots.get(args.hostId);
      if (previous?.workspaceId === args.workspaceId && previous?.document?.path === args.document.path) args.ink ??= previous.ink;
      snapshots.set(args.hostId, { ...args, kind: "markdown" });
      await (await getView(args.hostId, signal)).render(args, signal); return true;
    }
    case "markdown.capture": return views.get(args.hostId)?.capture() ?? snapshots.get(args.hostId)?.position ?? { blockId: "", fraction: 0, sourceLine: 1 };
    case "source.render":
      snapshots.set(args.hostId, { ...args, kind: "source" });
      await (await getView(args.hostId, signal)).render(args, signal); return true;
    case "source.capture": return views.get(args.hostId)?.capture() ?? { blockId: "", fraction: 0, sourceLine: 1 };
    case "source.restore": views.get(args.hostId)?.restore(args.position); return true;
    case "map.render":
      snapshots.set(args.hostId, { ...args, kind: "map" });
      (await getView(args.hostId, signal)).render(args); return true;
    case "history.push": pushHistory(args); return true;
    case "history.replace": pushHistory(args, true); return true;
    case "history.go": history.go(Math.max(-1, Math.min(1, args.delta))); return true;
    default: throw new WorkspaceError("unknown-action", "This action is unavailable.");
  }
}
function releaseAssetsForWorkspace(workspaceId) {
  for (const view of views.values()) if (view.workspaceId === workspaceId) releaseAssets(view.id);
}
async function call(operation, argumentsJson, request) {
  const controller = new AbortController(); jobs.set(request, controller);
  try {
    if (argumentsJson.length > 6 * 1048576) throw new WorkspaceError("payload-too-large", "This page exceeds the display limit.");
    const args = JSON.parse(argumentsJson);
    const value = await dispatch(operation, args, controller.signal);
    checkCancelled(controller.signal);
    return JSON.stringify({ ok: true, value: value ?? null });
  } catch (error) {
    const issue = errorFor(error);
    return JSON.stringify({ ok: false, code: issue.code, message: issue.message });
  } finally { jobs.delete(request); }
}
function popState(event) {
  if (event.state?.markvibe) { rememberEntry(event.state.markvibe); emit({ type: "history", entry: event.state.markvibe }); }
}
function keyboard(event) {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
    event.preventDefault(); emit({ type: "search" });
  }
}
function systemAppearance() { if (preferences.appearance === "system") emit({ type: "appearance" }); }
function persistVisiblePosition() {
  const entry = history.state?.markvibe;
  if (!entry) return;
  for (const kind of ["source", "markdown"]) {
    for (const [id, view] of views) {
      if (snapshots.get(id)?.kind !== kind || view.workspaceId !== entry.workspaceId ||
          (view.path ?? view.model?.path) !== entry.path || view.container.getBoundingClientRect().height <= 0) continue;
      persistPosition(view.capture()); return;
    }
  }
}
function visibilityChanged() { if (document.hidden) persistVisiblePosition(); }
window.addEventListener("popstate", popState);
window.addEventListener("keydown", keyboard);
window.addEventListener("pagehide", persistVisiblePosition);
document.addEventListener("visibilitychange", visibilityChanged);
colorScheme.addEventListener("change", systemAppearance);

window.markvibe = {
  canClose() { return inkEvents.size === 0 && [...snapshots.values()].every(snapshot => !snapshot.ink?.dirty && !snapshot.ink?.busy) && [...views.values()].every(view => !view.inkView?.active); },
  call,
  bind(reference) { endpoint = reference; },
  cancel(request) { jobs.get(request)?.abort(); },
  async attach(id, kind) {
    this.detach(id);
    const View = { markdown: MarkdownHost, source: SourceHost, map: MapHost }[kind];
    if (!View) return;
    const attachment = {};
    attachments.set(id, attachment);
    // OpenSilver queues DOM insertion after Loaded. Resolve the live presenter,
    // rather than attaching to a disconnected placeholder from synchronous interop.
    let element;
    for (let frame = 0; frame < 32; frame++) {
      if (attachments.get(id) !== attachment) return;
      element = document.querySelector('[data-mv-host="' + id + '"]');
      if (element?.isConnected) break;
      await nextFrame();
    }
    if (attachments.get(id) !== attachment) return;
    attachments.delete(id);
    // Collapsed presenters are attached on demand by getView when they become visible.
    if (!element?.isConnected) return;
    views.set(id, new View(element, id, emit));
    if (kind === "markdown") views.get(id).positionListener = position => {
      const view = views.get(id), entry = history.state?.markvibe;
      if (view?.workspaceId === entry?.workspaceId && view?.model?.path === entry?.path &&
          view.container.getBoundingClientRect().height > 0) persistPosition(position);
    };
    const snapshot = snapshots.get(id), view = views.get(id);
    if (snapshot) Promise.resolve(view.render(snapshot, new AbortController().signal))
      .then(() => { if (snapshots.get(id) === snapshot && views.get(id) === view && snapshot.position && kind === "source") view.restore(snapshot.position); })
      .catch(() => emit({ type: "status", message: "Choose this view again to reload its contents." }));
    configureSync();
  },
  detach(id) {
    attachments.delete(id);
    const view = views.get(id), snapshot = snapshots.get(id);
    if (snapshot && view?.capture) snapshot.position = view.capture();
    view?.dispose(); views.delete(id);
  },
  resize(id) { views.get(id)?.resize(); },
  sync(readerId, sourceId, enabled) { syncPair = { readerId, sourceId, enabled }; configureSync(); },
  unbind() {
    endpoint = null;
    attachments.clear();
    for (const controller of jobs.values()) controller.abort(); jobs.clear();
    for (const view of views.values()) view.dispose(); views.clear();
    snapshots.clear(); disposeSourceModels(); disposeWorkspaces();
    exportAdapter?.disposeExports();
    window.removeEventListener("popstate", popState); window.removeEventListener("keydown", keyboard);
    window.removeEventListener("pagehide", persistVisiblePosition);
    document.removeEventListener("visibilitychange", visibilityChanged);
    colorScheme.removeEventListener("change", systemAppearance);
  }
};
