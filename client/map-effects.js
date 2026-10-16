/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; finite directional pulses, relation lens and rectangle selection; 2026-10-16
 * Modified with AI: OpenAI Codex; bounded effect cleanup and reusable attention presentation; 2026-10-16
 * Human guidance: Umberto Giacobbi; useful futuristic effects with stable black rendering
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { uiText, textElement } from "./markdown.js";
import { t } from "./i18n.js";

/** Create a decorative SVG overlay that never receives pointer or keyboard input. */
function overlaySvg(name, attributes = {}) {
  const element = document.createElementNS("http://www.w3.org/2000/svg", name);
  for (const [key, value] of Object.entries(attributes)) {
    element.setAttribute(key, String(value));
  }
  element.setAttribute("aria-hidden", "true");
  element.style.pointerEvents = "none";
  return element;
}

/** Renders managed exploration state and short-lived gestures without owning document or persisted map state. */
export class MapExplorationRenderer {
  /** Mount a compact direction legend and register cancellable, viewport-local rectangle gestures. */
  constructor(host) {
    this.host = host;
    this.pulses = [];
    this.rectangleBounds = [];
    this.legend = document.createElement("div");
    this.legend.className = "map-relation-legend";
    this.legend.append(
      uiText("span", "Incoming", "map-relation-incoming"),
      uiText("span", "Outgoing", "map-relation-outgoing")
    );
    this.rectangle = document.createElement("div");
    this.rectangle.className = "map-selection-rectangle";
    this.rectangle.hidden = true;
    this.rectangle.setAttribute("aria-hidden", "true");
    this.trailLayer = overlaySvg("g", { class: "map-visit-trail" });
    host.viewport.append(this.legend, this.rectangle);

    host.viewport.addEventListener("pointerdown", event => this.startRectangle(event), { capture: true });
    host.viewport.addEventListener("pointermove", event => this.moveRectangle(event));
    host.viewport.addEventListener("pointerup", event => this.finishRectangle(event));
    host.viewport.addEventListener("pointercancel", () => this.cancelRectangle());
    host.viewport.addEventListener("lostpointercapture", () => this.cancelRectangle());
    host.viewport.addEventListener("keydown", event => {
      if (event.key === "Escape" && this.lasso) {
        event.preventDefault();
        event.stopPropagation();
        this.cancelRectangle();
      }
    }, { capture: true });
    this.motionChanged = () => this.cancelPulses();
    this.visibilityChanged = () => {
      if (document.hidden) {
        this.cancelPulses();
        this.cancelRectangle();
      }
    };
    host.reducedMotion.addEventListener("change", this.motionChanged);
    document.addEventListener("visibilitychange", this.visibilityChanged);
  }

  /** Publish a typed temporary-state command with the current workspace and layout identity. */
  async command(action, data = {}) {
    const host = this.host;
    if (host.suspended || host.arranging || host.cardDrag || host.cardResize) {
      return;
    }
    await host.emit({ ...data, type: "map-explore", action,
      workspaceId: host.workspaceId, stateId: host.state?.stateId });
  }

  /** Reflect managed controls and attention flags without replacing graph cards or moving their coordinates. */
  acceptState() {
    const host = this.host;
    const state = host.state?.exploration;
    if (!state) {
      return;
    }
    const session = host.workspaceId + ":" + host.state.stateId;
    if (session !== this.session) {
      this.cancelPulses();
      this.cancelRectangle();
      this.hovered = null;
      this.lastSelection = null;
      this.session = session;
    }
    this.state = state;
    const picked = new Set(state.picked.map(document => document.path));
    const visited = new Set(state.trail.map(document => document.path));
    host.body.classList.toggle("map-selection-mode", state.selectionEnabled);
    this.legend.hidden = !state.lensEnabled || host.small();

    for (const [path, item] of host.cards) {
      item.container.classList.toggle("picked", picked.has(path));
      item.container.classList.toggle("visited-page", visited.has(path));
      this.updateAttention(item, host.map.attention?.[path], state.attentionMode);
    }
    this.applyLens();
    if (this.lastSelection !== host.selected) {
      this.lastSelection = host.selected;
      this.cancelPulses();
      // A render can publish state before measuring its new SVG. Start after layout.
      this.pulseFrame = requestAnimationFrame(() => {
        this.pulseFrame = null;
        this.pulseConnections(host.selected);
      });
    }
  }

  /** Give attention states text descriptions and distinct symbols, including a neutral no-match presentation. */
  updateAttention(item, attention, mode) {
    const missingLinks = attention?.missingLinks ?? 0;
    const missing = missingLinks > 0;
    const isolated = Boolean(attention?.isolated);
    const changed = Boolean(attention?.changed);
    const signature = JSON.stringify([mode, missingLinks, isolated, changed, document.documentElement.lang]);
    if (item.attentionSignature === signature) {
      return;
    }

    // Layout acknowledgments can repeat this state many times while a card moves.
    // Refresh translated labels only when their inputs or the UI language change.
    item.attentionSignature = signature;
    const matches = { attention: missing || isolated || changed, missing, isolated, changed };
    const matched = Boolean(matches[mode]);
    item.container.classList.toggle("attention-match", matched);
    item.container.classList.toggle("attention-muted", mode !== "all" && !matched);
    if (!item.attentionBadge) {
      item.attentionBadge = textElement("span", "", "map-attention-badge");
      item.card.append(item.attentionBadge);
    }
    const badge = item.attentionBadge;
    badge.hidden = !missing && !changed && !(isolated && mode !== "all");
    if (missing) {
      badge.textContent = "! " + missingLinks;
    } else {
      badge.textContent = changed ? "↻" : "◇";
    }
    const descriptions = [];
    if (missing) {
      descriptions.push(t("Missing links") + ": " + missingLinks);
    }
    if (isolated) {
      descriptions.push(t("Isolated documents"));
    }
    if (changed) {
      descriptions.push(t("Changed this session"));
    }
    const description = descriptions.join(" · ");
    badge.setAttribute("title", description);
    badge.setAttribute("aria-label", description);
    badge.classList.toggle("missing", missing);
  }

  /** Preview relations on hover or keyboard focus while keeping the selected page as the resting lens. */
  focus(path) {
    if (this.hovered === path) {
      return;
    }
    this.hovered = path;
    this.applyLens();
  }

  /** Classify visible incoming/outgoing neighbors without performing graph parsing or rearranging any card. */
  applyLens() {
    const host = this.host;
    const focal = host.cards.has(this.hovered) ? this.hovered : host.selected;
    const enabled = this.state?.lensEnabled && host.cards.has(focal);
    const related = new Set([focal]);
    for (const item of host.edgePaths ?? []) {
      const incoming = item.edge.target === focal;
      const outgoing = item.edge.source === focal;
      if (incoming || outgoing) {
        related.add(item.edge.source);
        related.add(item.edge.target);
      }
      item.group.classList.toggle("relation-incoming", Boolean(enabled && incoming && !outgoing));
      item.group.classList.toggle("relation-outgoing", Boolean(enabled && outgoing));
      item.group.classList.toggle("relation-muted", Boolean(enabled && !incoming && !outgoing));
      let marker = "";
      if (item.path.classList.contains("route-connection")) {
        marker = "route-";
      } else if (enabled && incoming && !outgoing) {
        marker = "incoming-";
      } else if (enabled && outgoing) {
        marker = "outgoing-";
      }
      item.path.setAttribute("marker-end", "url(#arrow-" + marker + host.id + ")");
    }
    for (const [path, item] of host.cards) {
      item.container.classList.toggle("relation-muted", Boolean(enabled && !related.has(path)));
    }
  }

  /** Animate at most 24 adjacent directed links once; there is no idle animation or requestAnimationFrame loop. */
  pulseConnections(path) {
    const host = this.host;
    if (host.reducedMotion.matches || host.suspended || document.hidden || host.small()) {
      return;
    }
    const adjacent = (host.edgePaths ?? []).filter(item =>
      item.group.style.display !== "none"
      && (item.edge.source === path || item.edge.target === path)
    );
    for (const [index, item] of adjacent.slice(0, 24).entries()) {
      const pulse = overlaySvg("path", {
        class: "map-link-pulse",
        fill: "none",
        pathLength: 1,
        d: item.path.getAttribute("d") ?? "",
        stroke: item.edge.source === path ? "#b7a3ff" : "#55d7cf"
      });
      item.group.append(pulse);
      const animation = pulse.animate([
        { strokeDashoffset: 0, opacity: 0 },
        { strokeDashoffset: -.12, opacity: 1, offset: .15 },
        { strokeDashoffset: -.9, opacity: 1, offset: .88 },
        { strokeDashoffset: -1, opacity: 0 }
      ], { duration: 850, delay: Math.min(index * 18, 180), iterations: 1, easing: "linear" });
      const active = { item, pulse, animation };
      this.pulses.push(active);
      animation.finished.then(() => this.removePulse(active), () => this.removePulse(active));
    }
  }

  /** Remove a completed/cancelled segment while allowing other segments to finish independently. */
  removePulse(active) {
    active.pulse.remove();
    this.pulses = this.pulses.filter(pulse => pulse !== active);
  }

  /** Cancel pending and active effects when selection, session, visibility or motion preference changes. */
  cancelPulses() {
    if (this.pulseFrame) {
      cancelAnimationFrame(this.pulseFrame);
      this.pulseFrame = null;
    }
    for (const active of this.pulses) {
      active.animation.cancel();
      active.pulse.remove();
    }
    this.pulses = [];
  }

  /** Follow existing card-scale geometry and draw a subdued trail only between consecutive visible visits. */
  updateGeometry(geometry) {
    const host = this.host;
    for (const active of this.pulses) {
      const connection = active.item.path.getAttribute("d") ?? "";
      if (active.pulse.getAttribute("d") !== connection) {
        active.pulse.setAttribute("d", connection);
      }
    }
    if (this.trailLayer.parentNode !== host.graph) {
      host.graph.insertBefore(this.trailLayer, host.graph.children[1] ?? null);
    }
    const trail = this.state?.trail ?? [];
    const segments = [];
    for (let index = 1; index < trail.length; index++) {
      const source = geometry.get(trail[index - 1].path);
      const target = geometry.get(trail[index].path);
      if (!source || !target || host.cards.get(trail[index - 1].path)?.container.hidden
          || host.cards.get(trail[index].path)?.container.hidden) {
        continue;
      }
      segments.push(`M${source.x + source.width / 2},${source.y + source.height / 2}L${target.x + target.width / 2},${target.y + target.height / 2}`);
    }
    if (!this.trailPath) {
      this.trailPath = overlaySvg("path", { fill: "none" });
      this.trailLayer.append(this.trailPath);
    }
    const connection = segments.join(" ");
    if (this.trailPath.getAttribute("d") !== connection) {
      this.trailPath.setAttribute("d", connection);
    }
  }

  /** Start a selection rectangle on empty canvas with Shift or the explicit selection tool. */
  startRectangle(event) {
    const host = this.host;
    if ((!this.state?.selectionEnabled && !event.shiftKey) || event.button !== 0 || !event.isPrimary
        || host.small() || host.suspended || host.arranging || host.cardDrag || host.cardResize
        || event.target.closest("[data-map-node], [data-map-folder], .map-minimap, .map-overview-label")) {
      return;
    }
    event.preventDefault();
    event.stopImmediatePropagation();
    host.cancelCamera();
    host.lastCardClick = null;
    const viewport = host.viewport.getBoundingClientRect();
    this.lasso = {
      pointer: event.pointerId,
      x: event.clientX,
      y: event.clientY,
      viewport,
      additive: event.ctrlKey || event.metaKey,
      paths: [],
      candidates: new Set()
    };
    // Camera and zoom stay frozen for the gesture, so each card is measured once.
    this.rectangleBounds = [...host.cards].filter(([, item]) => !item.container.hidden)
      .map(([path, item]) => ({ path, bounds: item.container.getBoundingClientRect() }));
    host.viewport.setPointerCapture(event.pointerId);
    this.rectangle.hidden = false;
    this.moveRectangle(event);
  }

  /** Compare pointer coordinates against captured bounds and preview the intersected documents. */
  moveRectangle(event) {
    const lasso = this.lasso;
    if (!lasso || lasso.pointer !== event.pointerId) {
      return;
    }
    const left = Math.min(lasso.x, event.clientX);
    const top = Math.min(lasso.y, event.clientY);
    const right = Math.max(lasso.x, event.clientX);
    const bottom = Math.max(lasso.y, event.clientY);
    const scale = this.host.viewport.clientWidth / lasso.viewport.width;
    Object.assign(this.rectangle.style, {
      left: (left - lasso.viewport.left) * scale + "px",
      top: (top - lasso.viewport.top) * scale + "px",
      width: (right - left) * scale + "px",
      height: (bottom - top) * scale + "px"
    });
    lasso.paths = this.rectangleBounds.filter(({ bounds }) =>
      bounds.right >= left && bounds.left <= right
      && bounds.bottom >= top && bounds.top <= bottom
    ).map(item => item.path);
    const candidates = new Set(lasso.paths);
    for (const path of lasso.candidates) {
      if (!candidates.has(path)) {
        this.host.cards.get(path)?.container.classList.remove("lasso-candidate");
      }
    }
    for (const path of candidates) {
      if (!lasso.candidates.has(path)) {
        this.host.cards.get(path)?.container.classList.add("lasso-candidate");
      }
    }
    lasso.candidates = candidates;
  }

  /** Commit one managed selection after releasing capture; cancelled gestures never change selection. */
  finishRectangle(event) {
    if (!this.lasso || this.lasso.pointer !== event.pointerId) {
      return;
    }
    this.moveRectangle(event);
    const { paths, additive } = this.lasso;
    this.cancelRectangle();
    void this.command("pick", { paths, additive });
  }

  /** Release the rectangle and all transient highlights without emitting a domain-state mutation. */
  cancelRectangle() {
    const lasso = this.lasso;
    if (!lasso) {
      return;
    }

    this.lasso = null;
    this.rectangleBounds = [];
    this.rectangle.hidden = true;
    if (this.host.viewport.hasPointerCapture(lasso.pointer)) {
      this.host.viewport.releasePointerCapture(lasso.pointer);
    }
    for (const path of lasso.candidates) {
      this.host.cards.get(path)?.container.classList.remove("lasso-candidate");
    }
  }

  /** Release document-level subscriptions and finite animations before the graph host is removed. */
  dispose() {
    this.cancelPulses();
    this.cancelRectangle();
    this.host.reducedMotion.removeEventListener("change", this.motionChanged);
    document.removeEventListener("visibilitychange", this.visibilityChanged);
  }
}
