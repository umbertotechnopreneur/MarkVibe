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
 * Modified with AI: OpenAI Codex; explicit Markdown copy downloads; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { WorkspaceError, checkCancelled, makeId } from "./path.js";

const pending = new Map();
/** Check whether the versioned Windows bridge and WebView message channel are available. */
export const isDesktop = () => window.markvibeDesktop?.version === 1 && !!window.chrome?.webview;
window.chrome?.webview?.addEventListener("message", event => {
  const reply = event.data;
  if (reply?.version === 1 && reply.notification === "appearance") {
    window.dispatchEvent(new CustomEvent("markvibe-native-appearance"));
    return;
  }
  if (reply?.version !== 1 || typeof reply.id !== "string") {
    return;
  }
  const request = pending.get(reply.id);
  if (!request) {
    return;
  }
  request.finish(reply.ok ? null : new WorkspaceError(reply.code || "native-failed", reply.message || "Local operation failed."), reply.value);
});

/** Send an authenticated native request and retain its timeout, response and cancellation lifecycle. */
export function nativeCall(operation, args = {}, signal) {
  checkCancelled(signal);
  if (!isDesktop()) {
    throw new WorkspaceError("native-unavailable", "This action requires the Windows desktop app.");
  }
  return new Promise((resolve, reject) => {
    const id = makeId();
    let timer;
    /** Post a versioned bridge message with the current desktop capability and request identifier. */
    const send = (name, values) => window.chrome.webview.postMessage({
      version: 1,
      capability: window.markvibeDesktop.capability,
      id,
      operation: name,
      args: values
    });
    /** Settle the native request and release its timer, cancellation listener and pending entry. */
    const finish = (error, value) => {
      // Every completion path releases the same resources, including cancellation and timeout.
      clearTimeout(timer);
      signal?.removeEventListener("abort", abort);
      pending.delete(id);
      error ? reject(error) : resolve(value);
    };
    /** Forward cancellation to the native host and reject the pending browser request. */
    const abort = () => {
      send("cancel", { requestId: id });
      finish(new WorkspaceError("cancelled", "Action cancelled."));
    };
    pending.set(id, { finish });
    signal?.addEventListener("abort", abort, { once: true });
    timer = setTimeout(() => {
      send("cancel", { requestId: id });
      finish(new WorkspaceError("native-timeout", "The local operation timed out. Check its dialog and retry."));
    }, 300000);
    send(operation, args);
  });
}

/** Read native startup state and seed remembered browser metadata for its restored document. */
export async function initializeDesktop() {
  if (!isDesktop()) {
    return null;
  }
  const initial = await nativeCall("initialize");
  if (initial.session?.entry) {
    const entry = initial.session.entry;
    localStorage.setItem("markvibe.lastDocument", JSON.stringify(entry));
    localStorage.setItem("markvibe.lastWorkspace", JSON.stringify({ version: 1, id: entry.workspaceId, name: "Local documentation", mode: "native" }));
  }
  return initial;
}

/** Persist or clear native document memory through the desktop session operation. */
export function rememberNative(entry, enabled = true) {
  if (!isDesktop()) {
    return;
  }
  // Navigation is persisted immediately; scrolling updates already have a debounce.
  return nativeCall("session.remember", { entry, enabled });
}

/** Download a JSON sidecar copy and release its temporary object URL after the save gesture. */
export function downloadSidecar(json, filename) {
  downloadText(json, filename, "application/json");
}

/** Start an explicit Markdown-copy download; browser initiation does not establish a completed save. */
export function downloadMarkdown(source, filename) {
  downloadText(source, filename, "text/markdown;charset=utf-8");
}

/** Initiate one explicit local text download and release the temporary browser URL. */
function downloadText(source, filename, contentType) {
  const url = URL.createObjectURL(new Blob([source], { type: contentType }));
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10000);
}
