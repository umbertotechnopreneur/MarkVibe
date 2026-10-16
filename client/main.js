/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; readability and OpenAI key settings; 2026-10-11
 * Modified with AI: OpenAI Codex; dispatch explicit API-key and desktop Codex AI connections; 2026-10-16
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Modified with AI: OpenAI Codex; dispatch session-scoped exploration controls; 2026-10-16
 * Modified with AI: OpenAI Codex; Markdown AI dispatch, waiting popup and document selection lifecycle; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Modified with AI: OpenAI Codex; reliable map updates, metadata controls and installed UI fonts; 2026-10-16
 * Modified with AI: OpenAI Codex; forward managed atlas control data to the map renderer; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement the approved document atlas controls
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { MarkdownHost } from "./markdown.js";
import { SourceHost, releaseSourceWorkspace, disposeSourceModels } from "./source.js";
import { MapHost } from "./map.js";
import { observeDocumentSelection } from "./document-selection.js";
import {
  openWorkspace, getWorkspace, initializeWorkspaceStorage, capabilities, addFiles, acquireAsset,
  releaseAssets, releaseWorkspace, disposeWorkspaces, setWorkspaceMemory, canResumeWorkspace,
  readMapLayout, writeMapLayout
} from "./workspaces.js";
import { readPreferences, savePreferences, readLayout, saveLayout } from "./settings.js";
import {
  openAiKeyStatus, validateAndSaveOpenAiKey, openAiModels, prepareMarkdownWithOpenAi, editMarkdownWithOpenAi,
  aiConnectionStatus, testAiConnection, codexModels, prepareMarkdownWithCodex, editMarkdownWithCodex
} from "./openai.js";
import { isDesktop, nativeCall, initializeDesktop, rememberNative, downloadSidecar, downloadMarkdown } from "./native.js";
import { WorkspaceError, checkCancelled, errorFor, nextFrame, normalizePath, makeId } from "./path.js";
import { setLanguage, setSystemLanguage, t, localize, refreshTranslations } from "./i18n.js";

const views = new Map();
const snapshots = new Map();
const jobs = new Map();
const attachments = new Map();
const mapEvents = new Set();
const mapStates = new Map();
let mapSuspended = false;
let mapSuspensionCount = 0;
let documentEditSuspensionCount = 0;
let endpoint = null;
let syncPair = null;
let syncLocked = false;
let firstHistory = true;
let preferenceRevision = 0;
let exportAdapter;
let preferences = readPreferences();
const colorScheme = matchMedia("(prefers-color-scheme: dark)");

/** Translates startup and runtime-error UI before the OpenSilver shell is ready. */
function translateBootstrap() {
  for (const element of document.querySelectorAll("[data-mv-i18n]")) {
    localize(element, element.dataset.mvI18n);
    if (element.tagName === "A") {
      localize(element, element.dataset.mvI18n, "title");
    }
  }
  refreshTranslations(document);
}

setLanguage(readPreferences().language);
translateBootstrap();
window.addEventListener("markvibe-language-changed", translateBootstrap);
/** Tracks managed map operations so navigation can await their acknowledgments. */
const emit = value => {
  const operation = endpoint?.invokeMethodAsync("Receive", JSON.stringify(value)).catch(() => {
    // Keep transport failures diagnosable without logging document text, prompts or the rejected payload.
    console.error("MarkVibe managed event failed", { type: value.type, action: value.action ?? "" });
  }) ?? Promise.resolve();
  if (value.type === "map") {
    mapEvents.add(operation);
    operation.finally(() => mapEvents.delete(operation));
  }
  return operation;
};
/** Read bounded remembered history for the requested workspace without failing on unavailable storage. */
function savedEntry(workspaceId) {
  if (!preferences.rememberLastDocument) {
    return null;
  }
  try {
    const stored = localStorage.getItem("markvibe.lastDocument");
    const entries = [history.state?.markvibe, stored && stored.length < 20000 ? JSON.parse(stored) : null];
    for (const entry of entries) {
      if (!entry || workspaceId && entry.workspaceId !== workspaceId || typeof entry.workspaceId !== "string" ||
        typeof entry.id !== "string" || typeof entry.path !== "string" || typeof entry.anchor !== "string") {
        continue;
      }
      if (normalizePath(entry.path, true) !== entry.path) {
        continue;
      }
      const position = entry.position;
      if (!position || typeof position.blockId !== "string" || !Number.isFinite(position.sourceLine) ||
        !Number.isFinite(position.fraction) || position.fraction < 0 || position.fraction > 1 || position.sourceLine < 1) {
        continue;
      }
      return entry;
    }
  } catch {
    return null;
  }
  return null;
}
/** Persist the current history entry when document remembering is enabled. */
function rememberEntry(entry) {
  if (!preferences.rememberLastDocument) {
    return;
  }
  try {
    const info = getWorkspace(entry.workspaceId).info();
    localStorage.setItem("markvibe.lastWorkspace", JSON.stringify({ version: 1, id: info.id, name: info.name, mode: info.mode }));
    localStorage.setItem("markvibe.lastDocument", JSON.stringify(entry));
    if (isDesktop()) {
      rememberNative(info.mode === "native" ? entry : null, info.mode === "native").catch(() => emit({ type: "status", message: "The last document could not be remembered on disk." }));
    }
  } catch {
  }
}
/** Describe the remembered workspace and whether its provider can resume without a new prompt. */
async function resumeInfo(entry) {
  try {
    const raw = localStorage.getItem("markvibe.lastWorkspace");
    if (!entry || entry.workspaceId === "demo" || !raw || raw.length > 4096) {
      return null;
    }
    const saved = JSON.parse(raw);
    if (saved.version !== 1 || saved.id !== entry.workspaceId || typeof saved.name !== "string" ||
      !["native", "connected", "imported", "files", "zip"].includes(saved.mode)) {
      return null;
    }
    return {
      mode: saved.mode,
      name: saved.name,
      workspaceId: saved.id,
      path: entry.path,
      anchor: entry.anchor,
      position: entry.position,
      canResume: saved.mode === "native" ? isDesktop() : saved.mode === "connected" && await canResumeWorkspace(saved.id)
    };
  } catch {
    return null;
  }
}
/** Encode the document path and anchor in the existing browser history fragment. */
function navigationHash(entry) {
  return "#doc=" + encodeURIComponent(entry.path) + "&anchor=" + encodeURIComponent(entry.anchor || "");
}
/** Push or replace browser history and retain its associated document position. */
function pushHistory(entry, replace = false) {
  const state = {
    ...(typeof history.state === "object" && history.state ? history.state : {}),
    markvibe: entry
  };
  if (replace || firstHistory) {
    history.replaceState(state, "", navigationHash(entry));
  }
  else {
    history.pushState(state, "", navigationHash(entry));
  }
  firstHistory = false;
  rememberEntry(entry);
}
/** Update remembered reading position without creating a new navigation entry. */
function persistPosition(position) {
  const entry = history.state?.markvibe;
  if (!entry) {
    return;
  }
  const updated = { ...entry, position };
  history.replaceState({ ...history.state, markvibe: updated }, "", location.href);
  rememberEntry(updated);
}
/** Connect reader and source position callbacks for the currently enabled split-view pair. */
function configureSync() {
  if (!syncPair) {
    return;
  }
  const reader = views.get(syncPair.readerId);
  const source = views.get(syncPair.sourceId);
  if (!reader || !source) {
    return;
  }
  /** Transfer a source-backed position between paired views while avoiding recursive scroll feedback. */
  const synchronize = (from, to) => {
    if (!syncPair.enabled || syncLocked || reader.workspaceId !== source.workspaceId || reader.model?.path !== source.path) {
      return;
    }
    syncLocked = true;
    to.scrollToLine(from.capture().sourceLine);
    requestAnimationFrame(() => requestAnimationFrame(() => {
      syncLocked = false;
    }));
  };
  reader.scrollListener = () => synchronize(reader, source);
  source.scrollListener = () => {
    synchronize(source, reader);
    const entry = history.state?.markvibe;
    if (!syncPair.enabled && source.container.getBoundingClientRect().height > 0 &&
      source.workspaceId === entry?.workspaceId && source.path === entry?.path) {
      persistPosition(source.capture());
    }
  };
}
/** Wait for a connected view host or stop when the caller's operation is cancelled. */
async function getView(id, signal) {
  for (let frame = 0; frame < 32; frame++) {
    checkCancelled(signal);
    const view = views.get(id);
    if (view?.container.isConnected) {
      return view;
    }
    if (view) {
      window.markvibe.detach(id);
    }
    if (!attachments.has(id) && document.querySelector('[data-mv-host="' + id + '"]')) {
      await window.markvibe.attach(id, snapshots.get(id)?.kind);
    }
    await nextFrame();
  }
  throw new WorkspaceError("view-unavailable", "This view is still loading. Choose it again to continue.");
}
/** Apply validated appearance and language settings while rejecting obsolete asynchronous results. */
async function applyPreferences(args) {
  const revision = ++preferenceRevision;
  const rememberedBefore = preferences.rememberLastDocument;
  preferences = { ...preferences, ...args.preferences };
  const locale = setLanguage(preferences.language);
  const reader = views.get(args.readerId);
  const source = views.get(args.sourceId);
  if (reader) {
    reader.preferences = preferences;
  }
  const readerVisible = reader?.container.getBoundingClientRect().height > 0;
  const position = reader?.model && readerVisible ? reader.capture() : null;
  const readerRevision = reader?.revision;
  const readerModel = reader?.model;
  const readerArticle = reader?.article;
  /** Preserve scroll only while this preference operation still owns the same reader revision. */
  const readerUnchanged = () => revision === preferenceRevision && views.get(args.readerId) === reader
    && reader?.revision === readerRevision && reader?.model === readerModel && reader?.article === readerArticle;
  const dark = preferences.appearance === "dark" || preferences.appearance === "system" && colorScheme.matches;
  document.documentElement.dataset.theme = dark ? "dark" : "light";
  document.documentElement.dataset.documentStyle = preferences.documentStyle;
  document.documentElement.style.setProperty("--reading-scale", String(preferences.zoom / 100));
  const windowStyle = isDesktop() ? await nativeCall("window.appearance", { acrylic: preferences.acrylic, dark, language: locale }) : { acrylic: false, acrylicAvailable: false };
  const fonts = await installedUiFonts();
  const chosenFont = fonts.find(font => font.id === preferences.uiFont);
  const uiFont = chosenFont?.id ?? "system";
  if (revision !== preferenceRevision) {
    return { dark, persisted: true, locale, uiFont, ...windowStyle };
  }
  const fontStack = uiFont === "system" ? '"Segoe UI", system-ui, sans-serif' : '"' + chosenFont.label + '", "Segoe UI", system-ui, sans-serif';
  document.documentElement.style.setProperty("--ui-font", fontStack);
  document.documentElement.style.setProperty("--ui-monospace", uiFont === "system" ? '"Cascadia Mono"' : '"' + chosenFont.label + '"');
  if (/^#[\da-f]{6}$/iu.test(windowStyle.accent ?? "")) {
    document.documentElement.style.setProperty("--accent", windowStyle.accent);
  } else {
    document.documentElement.style.removeProperty("--accent");
  }
  document.documentElement.dataset.acrylic = windowStyle.acrylic ? "true" : "false";
  for (const view of views.values()) {
    view.container.dataset.theme = dark ? "dark" : "light";
    view.container.dataset.documentStyle = preferences.documentStyle;
    view.container.dataset.acrylic = document.documentElement.dataset.acrylic;
  }
  const persisted = savePreferences(preferences);
  if (rememberedBefore !== preferences.rememberLastDocument) {
    await setWorkspaceMemory(preferences.rememberLastDocument);
    if (!preferences.rememberLastDocument) {
      try {
        localStorage.removeItem("markvibe.lastDocument");
        localStorage.removeItem("markvibe.lastWorkspace");
      } catch {
      }
      if (isDesktop()) {
        await rememberNative(null, false);
      }
    } else if (history.state?.markvibe) {
      rememberEntry(history.state.markvibe);
    }
  }
  await source?.theme();
  if (reader?.model && readerVisible && readerUnchanged()) {
    await reader.retheme(readerUnchanged);
    await nextFrame();
    if (position && readerUnchanged()) {
      reader.restore(position);
    }
  }
  return { dark, persisted, locale, uiFont, ...windowStyle };
}
/** Read only the native host's verified installed monospace choices; browsers retain the system UI font. */
async function installedUiFonts() {
  if (!isDesktop()) {
    return [{ id: "system", label: "System default" }];
  }
  try {
    return await nativeCall("ui.fonts", {});
  } catch {
    return [{ id: "system", label: "System default" }];
  }
}
/** Routes managed browser operations to workspace, renderer, history and export adapters. */
async function dispatch(operation, args, signal) {
  switch (operation) {
    case "ui.fonts":
      return installedUiFonts();
    case "initialize":
      {
        preferences = readPreferences();
        const desktop = await initializeDesktop();
        setSystemLanguage(desktop?.systemLanguage);
        setLanguage(preferences.language);
        if (desktop && !preferences.rememberLastDocument) {
          await rememberNative(null, false);
          localStorage.removeItem("markvibe.lastDocument");
          localStorage.removeItem("markvibe.lastWorkspace");
        }
        await initializeWorkspaceStorage();
        await setWorkspaceMemory(preferences.rememberLastDocument);
        const hash = new URLSearchParams(location.hash.slice(1));
        const current = savedEntry();
        const path = hash.get("doc") || current?.path || "";
        const anchor = hash.has("anchor") ? hash.get("anchor") : current?.anchor || "";
        const position = current?.path === path && current.anchor === anchor ? current.position : null;
        return {
          preferences,
          layout: readLayout(),
          dark: preferences.appearance === "dark" || preferences.appearance === "system" && colorScheme.matches,
          rememberedFolder: capabilities().rememberedFolder,
          path,
          anchor,
          position,
          resume: await resumeInfo(current),
          desktop: isDesktop(),
          startup: !!desktop?.startup
        };
      }
    case "preferences.apply":
      return applyPreferences(args);
    case "layout.save":
      return saveLayout(args);
    case "openai.status":
      return openAiKeyStatus(signal);
    case "openai.validate":
      return validateAndSaveOpenAiKey(args.apiKey, signal);
    case "openai.models":
      return openAiModels(signal);
    case "openai.prepare":
      return prepareMarkdownWithOpenAi(args, signal);
    case "openai.edit":
      return editMarkdownWithOpenAi(args, signal);
    case "ai.connection.status":
      return aiConnectionStatus(args.provider, signal);
    case "ai.connection.test":
      return testAiConnection(args, signal);
    case "codex.models":
      return codexModels(signal);
    case "codex.prepare":
      return prepareMarkdownWithCodex(args, signal);
    case "codex.edit":
      return editMarkdownWithCodex(args, signal);
    case "ai.state":
      snapshots.set(args.hostId, { ...args, kind: "markdown-ai" });
      return true;
    case "source.find":
      (await getView(args.hostId, signal)).find(args.query);
      return true;
    case "source.copy":
      try { await navigator.clipboard.writeText(views.get(args.hostId)?.source ?? ""); }
      catch { throw new WorkspaceError("clipboard", "Select the source text and copy it with your keyboard."); }
      return true;
    case "document.selection.clear":
      for (const view of views.values()) {
        if (view.editor) {
          const position = view.editor.getPosition();
          if (position) {
            view.editor.setSelection({ startLineNumber: position.lineNumber, endLineNumber: position.lineNumber,
              startColumn: position.column, endColumn: position.column });
          }
        }
        if (view.textarea) {
          view.textarea.setSelectionRange(view.textarea.selectionEnd, view.textarea.selectionEnd);
        }
        view.root.getSelection?.()?.removeAllRanges();
      }
      document.getSelection()?.removeAllRanges();
      return true;
    case "document.saveCopy":
      if (isDesktop()) {
        return nativeCall("document.saveCopy", args, signal);
      }
      downloadMarkdown(args.source, args.filename);
      return { saved: false, cancelled: false };
    case "document.export":
      exportAdapter ??= await import("./export.js");
      return exportAdapter.exportDocument(args, signal);
    case "document.browser":
      exportAdapter ??= await import("./export.js");
      return exportAdapter.openBrowserPreview(args, signal);
    case "file.reveal":
    case "file.terminal":
    case "file.deleteRevision":
    case "file.delete":
      return nativeCall(operation, args, signal);
    case "workspace.open":
      return openWorkspace(args.mode, signal);
    case "map.command": {
      const view = await getView(args.hostId, signal);
      if (args.action.startsWith("explore-")) await view.effects.command(args.action.slice(8), args.data);
      else if (args.action === "fit") view.fit();
      else if (args.action === "save") await view.save();
      else await view.action(args.action, args.data);
      return true;
    }
    case "map.appearance": {
      const changes = {};
      if (args.color != null) changes.color = args.color;
      if (args.icon != null) changes.icon = args.icon;
      await (await getView(args.hostId, signal)).change(args.path, changes, "style");
      return true;
    }
    case "map.details": {
      const view = await getView(args.hostId, signal);
      const snapshot = snapshots.get(args.hostId);
      if (snapshot?.kind === "map") {
        snapshot.advanced = Boolean(args.advanced);
      }
      view.toggleDetails(args.advanced);
      return true;
    }
    case "map.depth": {
      const view = await getView(args.hostId, signal);
      const snapshot = snapshots.get(args.hostId);
      if (snapshot?.kind === "map") {
        snapshot.depth = Boolean(args.depth);
      }
      view.toggleDepth(args.depth);
      return true;
    }
    case "map.read":
      return readMapLayout(args.workspaceId, signal);
    case "map.access":
      return readMapLayout(args.workspaceId, signal, true);
    case "map.write":
      return writeMapLayout(args, signal);
    case "map.download":
      if (isDesktop()) {
        await nativeCall("map.export", args, signal);
        return { saved: true };
      }
      downloadSidecar(args.json, args.filename);
      return { saved: false };
    case "map.suspend":
      if ([...views.values()].some(view => view.cardDrag?.moved || view.cardResize?.moved || view.permissionPending)) {
        throw new WorkspaceError("map-active", "Finish moving the card or allowing folder access before opening a workspace.");
      }
      mapSuspensionCount++;
      suspendMaps(true);
      return true;
    case "map.resume":
      mapSuspensionCount = Math.max(0, mapSuspensionCount - 1);
      suspendMaps(mapSuspensionCount > 0);
      return true;
    case "map.flush":
      if ([...views.values()].some(view => view.cardDrag?.moved || view.cardResize?.moved)) {
        throw new WorkspaceError("map-active", "Finish moving the card before changing views or workspaces.");
      }
      await Promise.all([...mapEvents]);
      return true;
    case "map.state":
      {
        const state = retainMapState(args.workspaceId, args.state);
        const snapshot = snapshots.get(args.hostId);
        if (snapshot?.workspaceId === args.workspaceId) {
          snapshot.state = state;
        }
        const view = views.get(args.hostId);
        if (view?.workspaceId === args.workspaceId) {
          view.setState(args.state);
        }
        return true;
      }
    case "document.edit.suspend":
      if ([...views.values()].some(view => view.cardDrag || view.cardResize || view.permissionPending)) {
        throw new WorkspaceError("document-active", "Finish the current card interaction before replacing Markdown.");
      }
      documentEditSuspensionCount++;
      suspendDocumentInput(true);
      return true;
    case "document.edit.resume":
      documentEditSuspensionCount = Math.max(0, documentEditSuspensionCount - 1);
      suspendDocumentInput(documentEditSuspensionCount > 0);
      return true;
    case "history.saved":
      {
        const path = getWorkspace(args.workspaceId).nativeInfo?.selectedPath;
        return path ? {
          id: makeId(),
          workspaceId: args.workspaceId,
          path,
          anchor: "",
          position: { blockId: "", fraction: 0, sourceLine: 1 }
        } : savedEntry(args.workspaceId);
      }
    case "workspace.entries":
      return getWorkspace(args.workspaceId).list();
    case "workspace.text":
      return getWorkspace(args.workspaceId).text(args.path, signal);
    case "workspace.refresh":
      {
        const workspace = getWorkspace(args.workspaceId);
        if (!workspace.info().capabilities.canRefresh) {
          throw new WorkspaceError("reimport-needed", "Select this folder or bundle again to refresh its contents.");
        }
        await workspace.scan(signal);
        releaseAssetsForWorkspace(args.workspaceId);
        return true;
      }
    case "workspace.release":
      mapStates.delete(args.workspaceId);
      for (const [id, snapshot] of snapshots) if (snapshot.workspaceId === args.workspaceId) {
        snapshots.delete(id);
      }
      releaseSourceWorkspace(args.workspaceId);
      releaseWorkspace(args.workspaceId);
      return true;
    case "workspace.addFiles":
      return addFiles(args.workspaceId, "", signal);
    case "workspace.addMissing":
      return addFiles(args.workspaceId, normalizePath(args.path, true), signal);
    case "assets.acquire":
      return acquireAsset(args.workspaceId, args.path, args.owner, signal);
    case "assets.release":
      releaseAssets(args.owner);
      return true;
    case "markdown.render":
      {
        snapshots.set(args.hostId, { ...args, kind: "markdown" });
        const reader = await getView(args.hostId, signal);
        reader.preferences = preferences;
        await reader.render(args, signal);
        return true;
      }
    case "markdown.capture":
      return views.get(args.hostId)?.capture() ?? snapshots.get(args.hostId)?.position ?? { blockId: "", fraction: 0, sourceLine: 1 };
    case "source.render":
      snapshots.set(args.hostId, { ...args, kind: "source" });
      await (await getView(args.hostId, signal)).render(args, signal);
      return true;
    case "source.capture":
      return views.get(args.hostId)?.capture() ?? { blockId: "", fraction: 0, sourceLine: 1 };
    case "source.restore":
      (await getView(args.hostId, signal)).restore(args.position);
      return true;
    case "map.render":
    case "map.selection":
      {
        args.state = retainMapState(args.workspaceId, args.state);
        const snapshot = { ...args, kind: "map" };
        snapshots.set(args.hostId, snapshot);
        const view = await getView(args.hostId, signal);
        if (snapshots.get(args.hostId) !== snapshot) {
          return true;
        }
        args.state = retainMapState(args.workspaceId, args.state);
        if (operation === "map.selection") {
          view.applySelection(args);
        } else {
          view.render(args);
        }
        return true;
      }
    case "history.push":
      pushHistory(args);
      return true;
    case "history.replace":
      pushHistory(args, true);
      return true;
    case "history.go":
      history.go(Math.max(-1, Math.min(1, args.delta)));
      return true;
    default:
      throw new WorkspaceError("unknown-action", "This action is unavailable.");
  }
}
/** Release view-owned asset leases associated with the replaced workspace. */
function releaseAssetsForWorkspace(workspaceId) {
  for (const view of views.values()) if (view.workspaceId === workspaceId) {
    releaseAssets(view.id);
  }
}
/** Execute a cancellable identified browser operation and return the established JSON success or failure shape. */
async function call(operation, argumentsJson, request) {
  const controller = new AbortController();
  jobs.set(request, controller);
  try {
    if (argumentsJson.length > 6 * 1048576) {
      throw new WorkspaceError("payload-too-large", "This page exceeds the display limit.");
    }
    const operationArguments = JSON.parse(argumentsJson);
    const value = await dispatch(operation, operationArguments, controller.signal);
    checkCancelled(controller.signal);
    return JSON.stringify({ ok: true, value: value ?? null });
  } catch (error) {
    const issue = errorFor(error);
    return JSON.stringify({ ok: false, code: issue.code, message: issue.message });
  } finally {
    jobs.delete(request);
  }
}
/** Forward browser history navigation to the managed application state. */
function popState(event) {
  if (event.state?.markvibe) {
    rememberEntry(event.state.markvibe);
    emit({ type: "history", entry: event.state.markvibe });
  }
}
/** Notify managed appearance state that the system color preference changed. */
function systemAppearance() {
  emit({ type: "appearance" });
}
/** Notify managed appearance state of a system language change when system language is selected. */
function systemLanguage() {
  if (preferences.language === "system") {
    emit({ type: "appearance" });
  }
}
/** Retain the active document position when the page is hidden or leaving. */
function persistVisiblePosition() {
  const entry = history.state?.markvibe;
  if (!entry) {
    return;
  }
  for (const kind of ["source", "markdown"]) {
    for (const [id, view] of views) {
      if (snapshots.get(id)?.kind !== kind || view.workspaceId !== entry.workspaceId ||
        (view.path ?? view.model?.path) !== entry.path || view.container.getBoundingClientRect().height <= 0) {
        continue;
      }
      persistPosition(view.capture());
      return;
    }
  }
}
/** Capture reading position when the browser page becomes hidden. */
function visibilityChanged() {
  if (document.hidden) {
    persistVisiblePosition();
  }
}
/** Suspend or resume input on all retained map hosts. */
function suspendMaps(value) {
  mapSuspended = value;
  for (const view of views.values()) {
    view.setSuspended?.(value);
  }
}

/** Freeze renderer input during an atomic managed document replacement. */
function suspendDocumentInput(value) {
  for (const view of views.values()) view.body.inert = value;
}

/** Keep the latest managed map state for the workspace's retained hosts. */
function retainMapState(workspaceId, state) {
  const latest = mapStates.get(workspaceId);
  if (latest?.stateId === state.stateId && latest.generation > state.generation) {
    return latest;
  }
  mapStates.set(workspaceId, state);
  return state;
}
/** Report pending or dirty map state that must be resolved before leaving. */
function unsavedMap() {
  return mapEvents.size > 0 || [...mapStates.values()].some(state => state.dirty || state.busy) ||
    [...views.values()].some(view => view.cardDrag?.moved || view.cardResize?.moved || view.permissionPending || view.pendingEdits?.size > 0);
}
/** Request the browser's unsaved-change prompt when retained map edits require attention. */
function beforeUnload(event) {
  if (unsavedMap() || unsavedMarkdownAi()) {
    event.preventDefault();
    event.returnValue = "";
  }
}
/** Protect local Markdown drafts and unsent composer text without persisting AI conversation history. */
function unsavedMarkdownAi() {
  return [...snapshots.values()].some(snapshot => snapshot.kind === "markdown-ai"
    && (snapshot.state.unsavedDocuments > 0 || snapshot.state.busy || snapshot.state.prompt?.trim()))
;
}
window.addEventListener("popstate", popState);
window.addEventListener("pagehide", persistVisiblePosition);
window.addEventListener("beforeunload", beforeUnload);
document.addEventListener("visibilitychange", visibilityChanged);
colorScheme.addEventListener("change", systemAppearance);
window.addEventListener("languagechange", systemLanguage);
window.addEventListener("markvibe-native-appearance", systemAppearance);

window.markvibe = {
  translate: t,
  /** Retain the XAML prompt synchronously so native closing cannot outrun delayed estimates. */
  updateAiPrompt(hostId, prompt) {
    const snapshot = snapshots.get(hostId);
    if (snapshot?.kind === "markdown-ai") snapshot.state.prompt = prompt;
  },
  /** Reports whether map changes, Markdown drafts or unsent prompts prevent desktop closing. */
  canClose() {
    return !unsavedMap() && !unsavedMarkdownAi();
  },
  call,
  /** Retain the managed event endpoint for browser-to-managed application events. */
  bind(reference) {
    endpoint = reference;
  },
  /** Abort the identified browser operation if it is still active. */
  cancel(request) {
    jobs.get(request)?.abort();
  },
  /** Attach the requested browser view to its connected managed DOM host. */
  async attach(id, kind) {
    this.detach(id);
    const View = { markdown: MarkdownHost, source: SourceHost, map: MapHost }[kind];
    if (!View) {
      return;
    }
    const attachment = {};
    attachments.set(id, attachment);
    // OpenSilver queues DOM insertion after Loaded. Resolve the live presenter,
    // rather than attaching to a disconnected placeholder from synchronous interop.
    let element;
    for (let frame = 0; frame < 32; frame++) {
      if (attachments.get(id) !== attachment) {
        return;
      }
      element = document.querySelector('[data-mv-host="' + id + '"]');
      if (element?.isConnected) {
        break;
      }
      await nextFrame();
    }
    if (attachments.get(id) !== attachment) {
      return;
    }
    attachments.delete(id);
    // Collapsed presenters are attached on demand by getView when they become visible.
    if (!element?.isConnected) {
      return;
    }
    views.set(id, new View(element, id, emit));
    views.get(id).body.inert = documentEditSuspensionCount > 0;
    if (kind === "markdown" || kind === "source") {
      views.get(id).selectionObserver = observeDocumentSelection(views.get(id), kind === "markdown");
    }
    views.get(id).setSuspended?.(mapSuspended);
    if (kind === "markdown") {
      views.get(id).positionListener = position => {
        const view = views.get(id);
        const entry = history.state?.markvibe;
        if (view?.workspaceId === entry?.workspaceId && view?.model?.path === entry?.path &&
          view.container.getBoundingClientRect().height > 0) {
          persistPosition(position);
        }
      };
    }
    const snapshot = snapshots.get(id);
    const view = views.get(id);
    if (snapshot) {
      Promise.resolve(view.render(snapshot, new AbortController().signal))
        .then(() => {
          if (snapshots.get(id) !== snapshot || views.get(id) !== view) {
            return;
          }
          if (snapshot.position && kind === "source") {
            view.restore(snapshot.position);
          }
        })
        .catch(() => emit({ type: "status", message: "Choose this view again to reload its contents." }));
    }
    configureSync();
  },
  /** Dispose the identified browser view and remove its retained host state. */
  detach(id) {
    attachments.delete(id);
    const view = views.get(id);
    const snapshot = snapshots.get(id);
    if (snapshot && view?.capture) {
      snapshot.position = view.capture();
    }
    view?.selectionObserver?.abort();
    view?.dispose();
    views.delete(id);
  },
  /** Notify the retained view that its managed host dimensions changed. */
  resize(id) {
    views.get(id)?.resize();
  },
  /** Replace the reader and source synchronization pair and configure its callbacks. */
  sync(readerId, sourceId, enabled) {
    syncPair = { readerId, sourceId, enabled };
    configureSync();
  },
  /** Release the managed endpoint, active jobs, views and retained browser resources. */
  unbind() {
    endpoint = null;
    attachments.clear();
    for (const controller of jobs.values()) {
      controller.abort();
    }
    jobs.clear();
    for (const view of views.values()) {
      view.selectionObserver?.abort();
      view.dispose();
    }
    views.clear();
    snapshots.clear();
    mapStates.clear();
    mapSuspended = false;
    mapSuspensionCount = 0;
    disposeSourceModels();
    disposeWorkspaces();
    exportAdapter?.disposeExports();
    window.removeEventListener("popstate", popState);
    window.removeEventListener("pagehide", persistVisiblePosition);
    window.removeEventListener("beforeunload", beforeUnload);
    document.removeEventListener("visibilitychange", visibilityChanged);
    colorScheme.removeEventListener("change", systemAppearance);
    window.removeEventListener("languagechange", systemLanguage);
    window.removeEventListener("markvibe-native-appearance", systemAppearance);
  }
};
