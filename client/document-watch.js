/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; current-document watch lifecycle; 2026-10-23
 * Human guidance: Umberto Giacobbi; ask Yes or No before reloading a changed file
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { isDesktop, nativeCall } from "./native.js";
import { getWorkspace } from "./workspaces.js";

/** Own one current-document listener; imported snapshots have no live external source. */
export class CurrentDocumentWatch {
  /** Register the native notification route without retaining document source text. */
  constructor(emit) {
    this.emit = emit;
    this.generation = 0;
    /** Forward only notifications belonging to this exact active document registration. */
    this.onNativeChange = event => {
      const change = event.detail;
      if (this.current && change.watchId === this.current.watchId
        && change.workspaceId === this.current.workspaceId && change.path === this.current.path) {
        this.emit({ type: "document-file-changed", ...change });
      }
    };
    window.addEventListener("markvibe-native-document-changed", this.onNativeChange);
  }

  /** Replace the previous registration, using a real native watcher or an existing browser read grant. */
  async start(args, signal) {
    const generation = ++this.generation;
    clearTimeout(this.timer);
    this.polling?.abort();
    this.current = { ...args };
    const workspace = getWorkspace(args.workspaceId);
    const mode = workspace.info().mode;
    if (isDesktop() && mode === "native") {
      await nativeCall("document.watch", args, signal);
      return true;
    }
    if (isDesktop()) {
      await nativeCall("document.watch.stop", {}, signal);
    }
    if (generation !== this.generation || mode !== "connected") {
      return false;
    }

    this.polling = new AbortController();
    /** Check one bounded file at a time; keep the latest observed hash after the user declines reload. */
    const poll = async () => {
      const current = this.current;
      if (!current || generation !== this.generation || this.polling.signal.aborted) {
        return;
      }
      try {
        if (!document.hidden) {
          const source = await workspace.text(current.path, this.polling.signal);
          current.unavailable = false;
          const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(source));
          const fingerprint = [...new Uint8Array(digest)].map(value => value.toString(16).padStart(2, "0")).join("");
          if (generation === this.generation && fingerprint !== current.fingerprint) {
            current.fingerprint = fingerprint;
            this.emit({ type: "document-file-changed", ...current, missing: false });
          }
        }
      } catch (error) {
        // Permission loss or temporary removal never discards the current document.
        // The next poll can discover recreation without requesting a new grant.
        if (error.name !== "AbortError" && generation === this.generation && !current.unavailable) {
          current.unavailable = true;
          this.emit({ type: "document-file-changed", ...current, missing: true });
        }
      } finally {
        if (generation === this.generation && !this.polling.signal.aborted) {
          this.timer = setTimeout(poll, 1800);
        }
      }
    };
    this.timer = setTimeout(poll, 600);
    return true;
  }

  /** Stop both native and browser monitoring without mutating any document data. */
  async stop() {
    this.generation++;
    this.current = null;
    clearTimeout(this.timer);
    this.polling?.abort();
    if (isDesktop()) {
      await nativeCall("document.watch.stop");
    }
  }

  /** Release event subscriptions and suppress shutdown-only native bridge errors. */
  dispose() {
    window.removeEventListener("markvibe-native-document-changed", this.onNativeChange);
    this.stop().catch(() => {});
  }
}
