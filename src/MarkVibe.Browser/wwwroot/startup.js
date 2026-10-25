/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; dismissible startup presentation; 2026-10-25
 * Human guidance: Umberto Giacobbi; one extra second, immediate click dismissal
 * Modified with AI: OpenAI Codex; load packaged build metadata into the startup model; 2026-10-25
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */

/** Own the transient startup model and its timer independently of DOM rendering. */
class StartupService {
  /** Publish startup state through a renderer supplied by the host. */
  constructor(render) {
    this.state = { ready: false, skipDelay: false, visible: true, failed: false, buildInfo: null };
    this.render = render;
  }

  /** Load packaged build-host facts without delaying or blocking application startup. */
  async loadBuildInfo() {
    try {
      const response = await fetch(new URL("build-info.json", document.baseURI), { cache: "no-store" });
      if (!response.ok) {
        return;
      }
      const info = await response.json();
      const fields = ["version", "configuration", "architecture", "machineName", "builtAtLocalDisplay", "timeZoneId", "utcOffset"];
      if (!fields.every(field => typeof info[field] === "string"
        && info[field].length > 0 && info[field].length <= 160
        && !/[\u0000-\u001f]/u.test(info[field]))) {
        return;
      }
      this.state.buildInfo = {
        version: `${info.version} · ${info.configuration} ${info.architecture}`,
        machine: info.machineName,
        built: `${info.builtAtLocalDisplay} ${info.utcOffset} · ${info.timeZoneId}`
      };
      this.render(this.state);
    } catch {
      // Unpackaged development hosts have no release build identity. Never
      // substitute the viewer's hostname or current time as compilation facts.
    }
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
  const build = document.getElementById("startup-build");
  const buildVersion = document.getElementById("startup-build-version");
  const buildMachine = document.getElementById("startup-build-machine");
  const buildTime = document.getElementById("startup-build-time");
  const service = new StartupService(state => {
    overlay.hidden = !state.visible;
    button.hidden = !state.ready || state.failed;
    overlay.classList.toggle("startup-ready", state.ready && !state.failed);
    build.hidden = !state.buildInfo;
    if (state.buildInfo) {
      buildVersion.textContent = state.buildInfo.version;
      buildMachine.textContent = state.buildInfo.machine;
      buildTime.textContent = state.buildInfo.built;
    }
  });
  overlay.addEventListener("click", () => service.dismiss());
  window.addEventListener("markvibe-startup-ready", () => service.ready(), { once: true });
  void service.loadBuildInfo();
  return service;
}
