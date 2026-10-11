import { WorkspaceError, checkCancelled, makeId } from "./path.js";

const pending = new Map();
export const isDesktop = () => window.markvibeDesktop?.version === 1 && !!window.chrome?.webview;
window.chrome?.webview?.addEventListener("message", event => {
  const reply = event.data;
  if (reply?.version !== 1 || typeof reply.id !== "string") return;
  const request = pending.get(reply.id);
  if (!request) return;
  request.finish(reply.ok ? null : new WorkspaceError(reply.code || "native-failed", reply.message || "Local operation failed."), reply.value);
});

export function nativeCall(operation, args = {}, signal) {
  checkCancelled(signal);
  if (!isDesktop()) throw new WorkspaceError("native-unavailable", "This action requires the Windows desktop app.");
  return new Promise((resolve, reject) => {
    const id = makeId();
    let timer;
    const send = (name, values) => window.chrome.webview.postMessage({ version: 1, capability: window.markvibeDesktop.capability, id, operation: name, args: values });
    const finish = (error, value) => {
      clearTimeout(timer); signal?.removeEventListener("abort", abort); pending.delete(id);
      error ? reject(error) : resolve(value);
    };
    const abort = () => { send("cancel", { requestId: id }); finish(new WorkspaceError("cancelled", "Action cancelled.")); };
    pending.set(id, { finish }); signal?.addEventListener("abort", abort, { once: true });
    timer = setTimeout(() => { send("cancel", { requestId: id }); finish(new WorkspaceError("native-timeout", "The local operation timed out. Check its dialog and retry.")); }, 300000);
    send(operation, args);
  });
}

export async function initializeDesktop() {
  if (!isDesktop()) return null;
  const initial = await nativeCall("initialize");
  if (initial.session?.entry) {
    const entry = initial.session.entry;
    localStorage.setItem("markvibe.lastDocument", JSON.stringify(entry));
    localStorage.setItem("markvibe.lastWorkspace", JSON.stringify({ version: 1, id: entry.workspaceId, name: "Local documentation", mode: "native" }));
  }
  return initial;
}

let rememberTimer;
export function rememberNative(entry, enabled = true) {
  if (!isDesktop()) return;
  clearTimeout(rememberTimer);
  // Navigation is persisted immediately; scrolling updates already have a debounce.
  return nativeCall("session.remember", { entry, enabled });
}

export function downloadSidecar(json, filename) {
  const url = URL.createObjectURL(new Blob([json], { type: "application/json" }));
  const link = document.createElement("a"); link.href = url; link.download = filename;
  document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 10000);
}
