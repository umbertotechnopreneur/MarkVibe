/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; dismissible startup presentation; 2026-10-25
 * Human guidance: Umberto Giacobbi; one extra second, immediate click dismissal
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */

/** Own the transient startup model and its timer independently of DOM rendering. */
class StartupService {
  /** Publish startup state through a renderer supplied by the host. */
  constructor(render) {
    this.state = { ready: false, skipDelay: false, visible: true, failed: false };
    this.render = render;
  }

  /** Start the optional extra second only after the managed workspace is available. */
  ready() {
    if (this.state.ready || this.state.failed) {
      return;
    }
    this.state.ready = true;
    this.render(this.state);
    if (this.state.skipDelay) {
      this.dismiss();
    } else {
      this.timer = setTimeout(() => this.dismiss(), 1000);
    }
  }

  /** Remember an early click, but never expose an uninitialized workspace. */
  dismiss() {
    this.state.skipDelay = true;
    if (!this.state.ready || this.state.failed) {
      return;
    }
    clearTimeout(this.timer);
    this.state.visible = false;
    this.render(this.state);
  }

  /** Leave the startup error and reload control visible if runtime loading fails. */
  fail() {
    clearTimeout(this.timer);
    this.state.failed = true;
    this.render(this.state);
  }
}

/** Bind startup commands and render service state; the view owns no readiness state. */
export function attachStartup() {
  const overlay = document.getElementById("startup");
  const button = document.getElementById("startup-continue");
  const service = new StartupService(state => {
    overlay.hidden = !state.visible;
    button.hidden = !state.ready || state.failed;
    overlay.classList.toggle("startup-ready", state.ready && !state.failed);
  });
  overlay.addEventListener("click", () => service.dismiss());
  window.addEventListener("markvibe-startup-ready", () => service.ready(), { once: true });
  return service;
}
