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
 * Modified with AI: OpenAI Codex; map zoom, progressive reveals and arrangement coordination; 2026-10-16
 * Human guidance: Umberto Giacobbi; cinematic black map and accent selection elevation
 * Modified with AI: OpenAI Codex; card selection, resize, connection hover and metadata strip; 2026-10-16
 * Modified with AI: OpenAI Codex; compact icon statistics with emphasized context estimates; 2026-10-16
 * Modified with AI: OpenAI Codex; managed connection-depth planes and animated edge tracking; 2026-10-16
 * Modified with AI: OpenAI Codex; graduated spatial depth and fading toward distant endpoints; 2026-10-16
 * Modified with AI: OpenAI Codex; integrate semantic zoom, folder islands, minimap and atlas routes; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement the seven approved document atlas improvements
 * Modified with AI: OpenAI Codex; retain card content and use stable planar depth during navigation; 2026-10-16
 * Human guidance: Umberto Giacobbi; remove gray flashes and flicker when navigating the map
 * Modified with AI: OpenAI Codex; recognize document double activation across selection redraws; 2026-10-16
 * Modified with AI: OpenAI Codex; integrate bounded exploration gestures and finite directional effects; 2026-10-16
 * Modified with AI: OpenAI Codex; cancel stale graph effects and avoid repeated geometry-mode queries; 2026-10-16
 * Modified with AI: OpenAI Codex; measured camera batching and bounded gesture presentation; 2026-10-23
 * Human guidance: Umberto Giacobbi; profile and optimize the graph in an isolated Debug browser
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { createRoot, textElement, uiText } from "./markdown.js";
import { t, localize } from "./i18n.js";
import { iconSvg } from "./icons.js";
import { enableMapSaving } from "./workspaces.js";
import { makeId } from "./path.js";
import { MapAtlasRenderer } from "./map-atlas.js";
import { MapExplorationRenderer } from "./map-effects.js";

const svgNamespace = "http://www.w3.org/2000/svg";
// Presentation constants are shared by transforms, world bounds and search centering.
const depthAppearance = Object.freeze({
  maximumLevel: 12,
  planeSpacing: 85,
  perspectiveDistance: 900,
  opacityStep: .042,
  selectedScale: 1.28,
  flatSelectedScale: 1.14
});
/** Create an SVG node and assign its supplied attributes in the SVG namespace. */
function svgElement(name, attributes = {}) {

  const element = document.createElementNS(svgNamespace, name);

  for (const [key, value] of Object.entries(attributes)) {
    element.setAttribute(key, String(value));
  }

  return element;
}
/** Retains the graph's DOM, camera and pointer gestures while C# owns document and layout state. */
export class MapHost {
  /** Mount graph controls and register the map's pointer, keyboard and resize handlers. */
  constructor(element, id, emit) {

    Object.assign(this, createRoot(element, "document-map"));

    this.id = id;
    this.emit = emit;
    this.scale = 1;
    this.x = 30;
    this.y = 30;
    this.focused = false;

    this.cards = new Map();
    this.pendingEdits = new Map();
    this.editOperations = new Set();
    this.nodes = new Map();
    this.revealAnimations = [];
    this.reducedMotion = matchMedia("(prefers-reduced-motion: reduce)");
    this.coarsePointer = matchMedia("(pointer:coarse)");
    this.motionChanged = () => {
      this.cancelReveal();
      this.cancelCamera();
    };
    this.reducedMotion.addEventListener("change", this.motionChanged);

    this.viewport = document.createElement("div");
    this.viewport.className = "map-viewport";
    this.viewport.tabIndex = 0;

    localize(this.viewport, "Document connections. Drag cards to arrange them or drag the background to pan.", "aria-label");

    this.world = document.createElement("div");
    this.world.className = "map-world";
    this.viewport.append(this.world);

    this.body.append(this.viewport);
    this.atlas = new MapAtlasRenderer(this);
    this.effects = new MapExplorationRenderer(this);
    // Capture before card gestures as well as background panning can change their coordinate frame.
    this.viewport.addEventListener("pointerdown", () => this.cancelCamera(), { capture: true });
    this.cancelCameraOnEscape = event => {
      if (event.key === "Escape") {
        this.cancelCamera();
      }
    };
    document.addEventListener("keydown", this.cancelCameraOnEscape, { capture: true });

    this.viewport.addEventListener("pointerdown", event => {

      if (this.small() || this.suspended || this.arranging || event.target.closest("[data-map-node], [data-map-folder], .map-minimap") || event.button !== 0) {
        return;
      }

      this.setHoveredEdge(null);
      this.lastCardClick = null;
      this.drag = { pointer: event.pointerId, x: event.clientX - this.x, y: event.clientY - this.y };

      this.viewport.setPointerCapture(event.pointerId);

    });

    this.viewport.addEventListener("pointermove", event => {
      this.pointerPosition = { x: event.clientX, y: event.clientY };

      if (!this.drag || this.drag.pointer !== event.pointerId) {
        return;
      }

      this.beginCameraGesture();
      this.x = event.clientX - this.drag.x;
      this.y = event.clientY - this.drag.y;
      this.scheduleTransform();

    });
    /** Finish the matching pointer gesture while retaining its established pan and card-drag behavior. */
    const release = event => {

      if (this.drag?.pointer !== event.pointerId) {
        return;
      }

      this.drag = null;
      if (this.cameraGesture) {
        this.finishCameraGesture();
      } else {
        this.transform();
      }
      if (this.viewport.hasPointerCapture(event.pointerId)) {
        this.viewport.releasePointerCapture(event.pointerId);
      }

    };

    this.viewport.addEventListener("pointerup", release);
    this.viewport.addEventListener("pointercancel", release);

    this.viewport.addEventListener("lostpointercapture", () => {
      this.drag = null;
    });

    this.viewport.addEventListener("wheel", event => {
      this.pointerPosition = { x: event.clientX, y: event.clientY };
      this.cancelCamera();
      if (this.small() || this.suspended || this.arranging || event.deltaY === 0) {
        return;
      }

      event.preventDefault();
      // A drag retains its starting scale; consume wheel input without changing it mid-gesture.
      if (this.cardDrag || this.cardResize || this.drag || this.effects.lasso) {
        return;
      }

      const deltaUnit = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? this.viewport.clientHeight : 1;
      const delta = Math.max(-240, Math.min(240, event.deltaY * deltaUnit));
      const bounds = this.viewport.getBoundingClientRect();
      this.zoom(Math.exp(-delta * .0018), {
        x: (event.clientX - bounds.left) * this.viewport.clientWidth / bounds.width,
        y: (event.clientY - bounds.top) * this.viewport.clientHeight / bounds.height
      });
    }, { passive: false });

    this.viewport.addEventListener("keydown", event => {
      this.cancelCamera();

      if (event.target.closest("[data-map-node]")) {
        return;
      }

      const offsets = { ArrowLeft: [50, 0], ArrowRight: [-50, 0], ArrowUp: [0, 50], ArrowDown: [0, -50] };

      if (offsets[event.key]) {
        event.preventDefault();
        this.x += offsets[event.key][0];
        this.y += offsets[event.key][1];
        this.transform();
      }

      if (event.key === "0") {
        this.fit();
      }


    });

    this.resizeObserver = new ResizeObserver(() => this.resize());
    this.resizeObserver.observe(this.container);
    this.resizeObserver.observe(this.viewport);

  }
  /** Choose the compact map layout for narrow containers or coarse pointer devices. */
  small() {
    return this.container.clientWidth < 650 || this.coarsePointer.matches;
  }
  /** Drain card edits before a whole-map arrangement and retain the originating workspace session. */
  async action(action, data = {}) {
    if (this.suspended || this.arranging) {
      return;
    }

    const workspaceId = this.workspaceId;
    const stateId = this.state?.stateId;
    if (action.startsWith("atlas-")) {
      return this.atlas.command(action, data);
    }
    const isArrangement = ["arrange-star", "arrange-circle", "arrange-grid", "arrange-tree", "arrange-folders", "map-reset"].includes(action);
    if (!isArrangement) {
      return this.emit({ type: "map", action, workspaceId, stateId });
    }

    this.arranging = true;
    this.cancelCamera();
    this.effects.cancelPulses();
    this.effects.cancelRectangle();
    this.body.inert = true;
    this.setHoveredEdge(null);
    this.cancelCardResize();
    this.cancelCardDrag();
    if (this.drag && this.viewport.hasPointerCapture(this.drag.pointer)) {
      this.viewport.releasePointerCapture(this.drag.pointer);
    }
    this.drag = null;

    try {
      await Promise.all([...this.editOperations]);
      if (!this.suspended && this.workspaceId === workspaceId && this.state?.stateId === stateId) {
        await this.emit({ type: "map", action, workspaceId, stateId });
      }
    } finally {
      this.arranging = false;
      this.body.inert = Boolean(this.suspended);
    }
  }
  /** Request write permission directly from the save gesture before sending the managed save command. */
  async save() {

    if (this.suspended) {
      return;
    }

    const workspaceId = this.workspaceId;
    const stateId = this.state?.stateId;

    this.permissionPending = true;

    try {

      if (!this.state.canWrite) {
        await enableMapSaving(workspaceId);
      }

      if (this.workspaceId === workspaceId && this.state?.stateId === stateId) {
        await this.action("save");
      }

    } catch (error) {

      if (this.workspaceId === workspaceId && this.state?.stateId === stateId) {
        this.state.message = error.message;
        this.emit({ type: "status", severity: "error", message: error.message });
      }

    } finally {
      this.permissionPending = false;
    }

  }
  /** Merge managed layout acknowledgments with newer pending local card changes. */
  setState(state) {

    if (this.state && this.state.stateId !== state.stateId) {
      this.pendingEdits.clear();
      this.lastCardClick = null;
    }

    if (state.acknowledged) {
      this.pendingEdits.delete(state.acknowledged);
    }
    if (state.rejected) {
      this.pendingEdits.delete(state.rejected);
    }

    if (this.state?.stateId === state.stateId && this.state.generation > state.generation) {
      state = this.state;
    }

    const arranged = this.state?.stateId === state.stateId
      && (state.arrangementRevision ?? 0) > (this.state.arrangementRevision ?? 0);
    this.state = state;
    this.metrics = state.metrics;

    this.nodes = new Map(Object.entries(state.layout.nodes).map(([path, value]) => [path, { ...value }]));

    for (const { path, changes } of this.pendingEdits.values()) {

      const value = this.nodes.get(path);
      if (value) {
        Object.assign(value, changes);
      }

    }

    if (this.cardDrag?.moved) {
      Object.assign(this.nodes.get(this.cardDrag.path) ?? {}, this.cardDrag.current);
    }
    if (this.cardResize?.moved) {
      Object.assign(this.nodes.get(this.cardResize.path) ?? {}, this.cardResize.current);
    }



    if (arranged && !this.reducedMotion.matches) {
      this.body.classList.add("map-rearranging");
      clearTimeout(this.arrangementTimer);
      this.arrangementTimer = setTimeout(() => this.body.classList.remove("map-rearranging"), 460);
    }
    this.syncCards();
    this.atlas.acceptState();
    this.effects.acceptState();
    this.updateBounds();
    if (!this.cardDrag && !this.cardResize) {
      this.applyDepth();
    }
    this.updateEdges();
    if (arranged) {
      this.fit();
      this.revealCards();
    }
    this.atlas.restoreCamera();
  }
  /** Render graph cards and connections from the current catalog projection and saved layout. */
  render({ map, selected, workspaceId, state, focused = false, advanced = false, depth = true, preserveCamera = false }) {
    this.cancelCameraGesture();
    this.cancelScheduledTransform();

    this.cancelGeometryUpdate();
    this.rendering = true;
    this.cancelCamera();
    // Rebuilt connections must not leave pulses attached to the previous SVG or
    // commit a rectangle measured against cards that are about to be replaced.
    this.effects.cancelPulses();
    this.effects.cancelRectangle();

    const changedWorkspace = this.workspaceId !== workspaceId;
    const previousSelected = this.selected;
    const previousFocus = this.focused;

    const renderedNodeSignature = map.nodes.map(node => node.path).join("\n");
    const nodesChanged = renderedNodeSignature !== this.nodeSignature;

    this.cancelCardDrag();
    this.cancelCardResize();
    this.setHoveredEdge(null);

    if (changedWorkspace) {
      this.pendingEdits.clear();
      this.lastCardClick = null;
    }

    this.workspaceId = workspaceId;
    this.focused = focused;
    this.selected = selected;
    this.nodeSignature = renderedNodeSignature;
    this.map = map;
    this.advanced = advanced;
    this.depth = Boolean(depth);
    this.body.classList.toggle("advanced-map", advanced);

    this.cancelReveal();
    const retainedCards = preserveCamera ? new Map(this.cards) : new Map();
    this.cards.clear();
    this.edgePaths = [];




    this.graph = svgElement("svg", { class: "map-edges", role: "group" });
    localize(this.graph, "Document connections", "aria-label");

    const markerId = "arrow-" + this.id;
    const definitions = svgElement("defs");

    const marker = svgElement("marker", {
      id: markerId,
      markerWidth: 8,
      markerHeight: 8,
      refX: 7,
      refY: 3,
      orient: "auto",
      markerUnits: "strokeWidth"
    });

    marker.append(svgElement("path", { d: "M0,0 L7,3 L0,6 Z", fill: "var(--accent)" }));
    const routeMarker = marker.cloneNode(true);
    routeMarker.id = "arrow-route-" + this.id;
    routeMarker.firstElementChild.setAttribute("fill", "#e8c87a");
    const incomingMarker = marker.cloneNode(true);
    incomingMarker.id = "arrow-incoming-" + this.id;
    incomingMarker.firstElementChild.setAttribute("fill", "#55d7cf");
    const outgoingMarker = marker.cloneNode(true);
    outgoingMarker.id = "arrow-outgoing-" + this.id;
    outgoingMarker.firstElementChild.setAttribute("fill", "#b7a3ff");
    definitions.append(marker, routeMarker, incomingMarker, outgoingMarker);
    this.graph.append(definitions);

    for (const edge of map.edges) {

      const highlighted = edge.source === selected || edge.target === selected;

      const path = svgElement("path", {
        class: highlighted ? "map-connection highlighted" : "map-connection",
        fill: "none",
        stroke: "var(--accent)",
        "stroke-width": highlighted ? 2.4 : 1.3,

        opacity: highlighted ? .9 : .32,
        "marker-end": "url(#" + markerId + ")"
      });
      // The invisible stroke provides a usable target without changing the visible graph geometry.
      const hitPath = svgElement("path", { class: "map-connection-hit", fill: "none", stroke: "transparent" });
      const group = svgElement("g");
      const item = { edge, path, hitPath, group };
      hitPath.setAttribute("role", "link");
      localize(hitPath, "Connection from {0} to {1}. Source line {2}. {3}", "aria-label", edge.source, edge.target, edge.line, edge.excerpt);
      hitPath.addEventListener("focus", () => this.setHoveredEdge(item));
      hitPath.addEventListener("blur", () => this.setHoveredEdge(null));
      hitPath.addEventListener("keydown", event => {
        if (event.key === "Enter" && !this.suspended && !this.arranging) {
          event.preventDefault();
          event.stopPropagation();
          this.emit({ type: "navigate", path: edge.target, anchor: edge.anchor });
        } else if (event.key === "Escape") {
          hitPath.blur();
        }
      });
      hitPath.addEventListener("pointerenter", event => {
        if (event.pointerType !== "touch" && !this.drag && !this.cardDrag && !this.cardResize && !this.suspended && !this.arranging) {
          this.setHoveredEdge(item);
        }
      });
      hitPath.addEventListener("pointerleave", () => {
        if (this.hoveredEdge === item) {
          this.setHoveredEdge(null);
        }
      });
      group.append(path, hitPath);
      this.edgePaths.push(item);
      this.graph.append(group);

    }

    const fragment = document.createDocumentFragment();

    for (const node of map.nodes) {

      const retained = retainedCards.get(node.path);
      if (retained) {
        const isSelected = node.path === selected;
        retained.container.classList.toggle("selected", isSelected);
        retained.card.classList.toggle("selected", isSelected);
        retained.card.setAttribute("aria-pressed", String(isSelected));
        this.refreshCardContent(retained, node);
        fragment.append(retained.container);
        this.cards.set(node.path, retained);
        continue;
      }

      const container = document.createElement("article");
      container.className = "map-card" + (node.path === selected ? " selected" : "");
      container.dataset.mapNode = node.path;

      const card = document.createElement("div");
      card.tabIndex = 0;
      card.dataset.mapNode = node.path;
      card.setAttribute("role", "button");
      card.setAttribute("aria-pressed", String(node.path === selected));
      card.className = "page-card" + (node.path === selected ? " selected" : "");

      localize(card, "Select {0}, {1}", "aria-label", node.title, node.path);

      localize(card, "Click to select · Double-click to open · Drag to move · Right-click to customize · Space to select · Enter to open · Alt+arrows to move", "title");

      const badge = iconSvg("map-file");
      badge.classList.add("page-icon");
      const header = document.createElement("div");
      header.className = "map-card-header";
      header.append(badge, textElement("strong", node.title));
      card.append(header, textElement("span", node.introduction, "page-introduction"), textElement("code", node.path));
      card.append(this.statisticsStrip(node.statistics));
      const outline = document.createElement("div");
      outline.className = "map-card-outline";
      for (const heading of (node.headings ?? []).slice(0, 3)) {
        outline.append(textElement("span", heading.text));
      }
      card.append(outline);

      // Keyboard focus must never land on a card still waiting for its entrance delay.
      card.addEventListener("focus", () => {
        this.cancelReveal();
        this.effects.focus(node.path);
      });
      card.addEventListener("blur", () => this.effects.focus(null));

      card.addEventListener("click", event => this.activateCard(node.path, event));
      container.addEventListener("pointerenter", event => {
        if (event.pointerType !== "touch" && !this.cameraGesture && !this.drag && !this.cardDrag && !this.cardResize) {
          this.cancelReveal();
          container.classList.add("hovered");
          this.effects.focus(node.path);
          if (node.path !== this.selected) {
            this.animateConnections();
          }
        }
      });
      container.addEventListener("pointerleave", () => {
        container.classList.remove("hovered");
        this.effects.focus(null);
        if (!this.cameraGesture && node.path !== this.selected) {
          this.animateConnections();
        }
      });

      card.addEventListener("contextmenu", event => {

        event.preventDefault();
        event.stopPropagation();
        const currentNode = this.map.nodes.find(candidate => candidate.path === node.path) ?? node;
        this.openPicker(currentNode, { x: event.clientX, y: event.clientY });

      });

      card.addEventListener("keydown", event => {
        if (event.key === "Enter") { event.preventDefault(); this.emit({ type: "navigate", path: node.path, anchor: "" }); }
        if (event.key === " ") {
          event.preventDefault();
          if (this.state.exploration?.selectionEnabled || event.ctrlKey || event.metaKey || event.shiftKey) {
            void this.effects.command("toggle", { path: node.path });
          } else {
            this.selectCard(node.path);
          }
        }

        if (event.key === "Escape" && this.cardDrag) {
          event.preventDefault();
          this.cancelCardDrag();
          return;
        }

        if (event.key === "ContextMenu" || event.shiftKey && event.key === "F10") {

          event.preventDefault();
          const bounds = card.getBoundingClientRect();
          const currentNode = this.map.nodes.find(candidate => candidate.path === node.path) ?? node;
          this.openPicker(currentNode, { x: bounds.left + 20, y: bounds.top + 32 });

        }

        const offsets = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };

        if (event.altKey && offsets[event.key] && !this.small()) {

          event.preventDefault();
          const value = this.nodes.get(node.path);
          const step = event.shiftKey ? 100 : 20;

          this.change(node.path, {
            x: this.coordinate(value.x + offsets[event.key][0] * step),
            y: this.coordinate(value.y + offsets[event.key][1] * step)
          }, "move");

        }

      });

      card.addEventListener("pointerdown", event => this.startCardDrag(event, node.path, card));

      card.addEventListener("pointermove", event => this.moveCard(event));

      card.addEventListener("pointerup", event => this.finishCardDrag(event));

      card.addEventListener("pointercancel", () => this.cancelCardDrag());

      card.addEventListener("lostpointercapture", () => this.cancelCardDrag());

      const grip = this.resizeGrip(node.path, node.title);
      container.append(card, grip);
      fragment.append(container);

      this.cards.set(node.path, { container, card, badge, grip, icon: "file", contentSignature: JSON.stringify(node) });

    }

    this.folderLayer = document.createElement("div");
    this.folderLayer.className = "map-folder-layer";
    this.world.replaceChildren(this.folderLayer, this.graph, fragment);

    if (!map.nodes.length) {
      this.world.append(uiText("p", "No pages match this filter. Try a different search.", "map-empty"));
    }

    this.setState(state);
    this.needsReveal = !preserveCamera;
    this.resize();

    if (!preserveCamera && (changedWorkspace || previousSelected !== selected || previousFocus !== focused || nodesChanged)) {
      this.fit();
    } else {
      this.transform();
    }
    this.animateConnections();
    this.rendering = false;
    this.atlas.restoreCamera();

  }
  /** Publish a managed map selection without navigating away from the current document. */
  selectCard(path, center = false) {
    if (this.suspended || this.arranging || this.cardResize || this.selected === path && !center) {
      return;
    }
    this.emit({ type: "map-select", path, center, workspaceId: this.workspaceId, stateId: this.state?.stateId });
  }
  /** Select once or open on a second nearby click, even when selection changes the DOM target. */
  activateCard(path, event, center = false) {
    if (this.suspended || this.arranging || this.cardResize) {
      this.lastCardClick = null;
      return;
    }
    if (event.detail > 0 && this.suppressClick === path) {
      this.suppressClick = null;
      this.lastCardClick = null;
      return;
    }
    if (this.state.exploration?.selectionEnabled || event.ctrlKey || event.metaKey || event.shiftKey) {
      this.lastCardClick = null;
      void this.effects.command("toggle", { path });
      return;
    }

    const previousClick = this.lastCardClick;
    this.lastCardClick = null;

    // Keyboard-generated clicks select only; Enter keeps its explicit opening action.
    if (event.detail > 0 && performance.now() >= (this.suppressOpenUntil ?? 0)) {
      const elapsed = previousClick ? event.timeStamp - previousClick.time : Infinity;
      const repeatedCard = previousClick?.path === path
        && elapsed >= 0 && elapsed <= 500
        && Math.hypot(event.clientX - previousClick.x, event.clientY - previousClick.y) <= 6;

      // Browser click counts honor the OS interval. The path-based fallback survives
      // a changed child target, retained-card reparenting and native composition input.
      if (previousClick?.path === path && event.detail === 2 || repeatedCard) {
        event.preventDefault();
        this.emit({ type: "navigate", path, anchor: "" });
        return;
      }
      this.lastCardClick = { path, time: event.timeStamp, x: event.clientX, y: event.clientY };
    }

    this.selectCard(path, center);
  }
  /** Apply managed selection in place, preserving pointer identity for the following double click. */
  applySelection(args) {
    // A newly mounted renderer may receive navigation before its first full presentation.
    if (!this.state) {
      this.render(args);
      return;
    }
    if (args.workspaceId !== this.workspaceId || args.state.stateId !== this.state?.stateId) {
      return;
    }
    const incomingNodes = new Map(args.map.nodes.map(node => [node.path, node]));
    const incomingEdges = JSON.stringify(args.map.edges);
    const edgesChanged = incomingEdges !== JSON.stringify(this.map.edges);
    if (edgesChanged || incomingNodes.size !== this.cards.size || [...this.cards.keys()].some(path => !incomingNodes.has(path))) {
      this.render({ ...args, preserveCamera: true });
      if (args.center) {
        this.centerCard(args.selected);
      }
      return;
    }
    this.map = args.map;
    this.nodeSignature = args.map.nodes.map(node => node.path).join("\n");
    this.focused = args.focused;
    this.selected = args.selected;
    this.cancelReveal();
    for (const [path, item] of this.cards) {
      this.refreshCardContent(item, incomingNodes.get(path));
      const selected = path === args.selected;
      item.container.classList.toggle("selected", selected);
      item.card.classList.toggle("selected", selected);
      item.card.setAttribute("aria-pressed", String(selected));
    }
    for (const item of this.edgePaths) {
      const highlighted = item.edge.source === args.selected || item.edge.target === args.selected;
      item.path.classList.toggle("highlighted", highlighted);
      item.path.setAttribute("opacity", highlighted ? .9 : .32);
      item.path.setAttribute("stroke-width", highlighted ? 2.4 : 1.3);
    }
    this.depth = args.depth ?? this.depth;
    this.setState(args.state);
    this.toggleDetails(args.advanced ?? this.advanced);
    this.animateConnections();
    if (args.center) {
      this.centerCard(args.selected);
    }
  }
  /** Refresh changed document metadata without replacing the focusable card or replaying its entrance. */
  refreshCardContent(item, node) {
    const signature = JSON.stringify(node);
    if (item.contentSignature === signature) {
      return;
    }

    item.card.querySelector(".map-card-header > strong").textContent = node.title;
    item.card.querySelector(".page-introduction").textContent = node.introduction;
    const outline = item.card.querySelector(".map-card-outline");
    outline.replaceChildren(...(node.headings ?? []).slice(0, 3).map(heading => textElement("span", heading.text)));
    item.card.querySelector(".map-statistics").replaceWith(this.statisticsStrip(node.statistics));
    localize(item.card, "Select {0}, {1}", "aria-label", node.title, node.path);
    localize(item.grip, "Resize {0}", "aria-label", node.title);
    item.contentSignature = signature;
  }
  /** Toggle the compact metadata strip without changing the document content or camera. */
  toggleDetails(advanced) {
    this.advanced = Boolean(advanced);
    this.body.classList.toggle("advanced-map", this.advanced);
  }
  /** Toggle perspective without changing saved positions, freezing no unfinished pointer gesture. */
  toggleDepth(depth) {
    this.cancelCardResize();
    this.cancelCardDrag();
    this.cancelReveal();
    this.depth = Boolean(depth);
    this.updateBounds();
    this.applyDepth();
  }
  /** Use one selected-card magnification for CSS, fitted bounds and animated camera centering. */
  selectedCardScale() {
    return this.depth ? depthAppearance.selectedScale : depthAppearance.flatSelectedScale;
  }
  /** Combine managed graph distance with layout distance, without moving or persisting any card coordinates. */
  depthLevels() {
    const levels = new Map();
    const selected = this.nodes.get(this.selected);
    const selectedCenterX = selected ? selected.x + (selected.width ?? this.metrics.width) / 2 : 0;
    const selectedCenterY = selected ? selected.y + (selected.height ?? this.metrics.height) / 2 : 0;
    const distanceUnit = Math.hypot(this.metrics.width, this.metrics.height);
    for (const path of this.cards.keys()) {
      if (!this.depth || path === this.selected) {
        levels.set(path, 0);
        continue;
      }

      const graphDistance = this.map.depths?.[path] ?? -1;
      const position = this.nodes.get(path);
      const centerX = position ? position.x + (position.width ?? this.metrics.width) / 2 : selectedCenterX;
      const centerY = position ? position.y + (position.height ?? this.metrics.height) / 2 : selectedCenterY;
      const spatialDistance = selected ? Math.hypot(centerX - selectedCenterX, centerY - selectedCenterY) / distanceUnit : 0;

      // Logarithmic growth adds visible steps without flattening large graphs at the depth limit.
      // Disconnected pages start behind connected neighbors, but retain distinct spatial planes.
      const graphLevel = graphDistance < 0 ? 6 : 2 * Math.log2(1 + graphDistance);
      const spatialLevel = 1.5 * Math.log2(1 + spatialDistance);
      const level = Math.max(1, Math.min(depthAppearance.maximumLevel, Math.round(graphLevel + spatialLevel)));
      levels.set(path, level);
    }
    return levels;
  }
  /** Apply graduated depth to whole cards and fade every connection toward its more distant endpoint. */
  applyDepth() {
    if (!this.metrics || !this.map) {
      return;
    }
    this.body.classList.toggle("depth-map", this.depth);
    this.body.style.setProperty("--card-selected-scale", String(this.selectedCardScale()));
    const levels = this.depthLevels();
    for (const [path, item] of this.cards) {
      const level = levels.get(path) ?? 0;
      // A centered perspective projection is the same size as this 2D scale. Keeping
      // hover, focus and depth in one transform space avoids recreating 3D layers on click.
      const depthDistance = depthAppearance.planeSpacing * level;
      const depthScale = depthAppearance.perspectiveDistance / (depthAppearance.perspectiveDistance + depthDistance);
      item.container.style.setProperty("--card-depth-scale", String(depthScale));
      item.container.style.setProperty("--card-depth-opacity", String(1 - level * depthAppearance.opacityStep));
      item.container.style.setProperty("--card-depth-order", String(this.depth ? 30 - level : 1));
    }
    for (const item of this.edgePaths) {
      const furthestLevel = Math.max(levels.get(item.edge.source) ?? 0, levels.get(item.edge.target) ?? 0);
      const highlighted = item.edge.source === this.selected || item.edge.target === this.selected;
      const baseOpacity = highlighted ? .9 : .32;
      const attenuation = 1 - furthestLevel / depthAppearance.maximumLevel * .72;
      item.path.style.setProperty("--edge-depth-opacity", String(baseOpacity * attenuation));
    }
    this.animateConnections();
  }
  /** Separate file size and context cost from secondary counts without clipping translated labels. */
  statisticsStrip(statistics) {
    const strip = document.createElement("div");
    strip.className = "map-statistics";
    if (!statistics) {
      strip.append(uiText("span", "Metadata unavailable", "map-statistic-unavailable"));
      return strip;
    }
    const number = new Intl.NumberFormat(document.documentElement.lang || "en");
    const bytes = statistics.sourceBytes;
    const size = bytes < 1024 ? number.format(bytes) + " B" : number.format(Math.round(bytes / 102.4) / 10) + " KiB";
    const primary = document.createElement("div");
    primary.className = "map-statistics-primary";
    primary.append(
      this.statisticItem("UTF-8 size", size, "file"),
      this.statisticItem("Est. tokens", "≈" + number.format(statistics.estimatedTokens), "sparkles", true)
    );
    const secondary = document.createElement("div");
    secondary.className = "map-statistics-secondary";
    const counts = [["Characters", statistics.characters, "code"], ["Words", statistics.words, "book"],
      ["Lines", statistics.lines, "list"], ["Links", statistics.outgoingLinks, "link"]];
    for (const [label, value, icon] of counts) {
      secondary.append(this.statisticItem(label, number.format(value), icon));
    }
    strip.append(primary, secondary);
    return strip;
  }
  /** Keep each full localized label in its tooltip and accessible text while the canvas shows an icon and value. */
  statisticItem(label, value, icon, estimated = false) {
    const item = document.createElement("span");
    item.className = "map-statistic" + (estimated ? " map-statistic-tokens" : "");
    item.append(iconSvg(icon), uiText("span", label, "map-statistic-label"), textElement("strong", value));
    localize(item, estimated
      ? "Token counts are estimates; the final AI request also includes instructions and reference documents."
      : label, "title");
    return item;
  }
  /** Emphasize a hovered connection and its two endpoint cards without altering the selected page. */
  setHoveredEdge(item) {
    if (this.hoveredEdge === item) {
      return;
    }
    if (this.hoveredEdge) {
      this.hoveredEdge.path.classList.remove("hovered");
      for (const path of [this.hoveredEdge.edge.source, this.hoveredEdge.edge.target]) {
        this.cards.get(path)?.container.classList.remove("edge-endpoint");
      }
    }
    this.hoveredEdge = item;
    this.atlas.showConnection(item);
    if (item) {
      this.cancelReveal();
      item.path.classList.add("hovered");
      for (const path of [item.edge.source, item.edge.target]) {
        this.cards.get(path)?.container.classList.add("edge-endpoint");
      }
    }
  }
  /** Create a keyboard-accessible resize grip whose gestures stay separate from card selection and movement. */
  resizeGrip(path, title) {
    const grip = document.createElement("div");
    grip.className = "map-resize-grip";
    grip.tabIndex = 0;
    grip.setAttribute("role", "button");
    localize(grip, "Resize {0}", "aria-label", title);
    localize(grip, "Drag to resize · Arrow keys to resize · Shift for larger steps · Escape to cancel", "title");
    grip.addEventListener("pointerdown", event => this.startCardResize(event, path, grip));
    grip.addEventListener("pointermove", event => this.resizeCard(event));
    grip.addEventListener("pointerup", event => this.finishCardResize(event));
    grip.addEventListener("pointercancel", () => this.cancelCardResize());
    grip.addEventListener("lostpointercapture", () => this.cancelCardResize());
    grip.addEventListener("click", event => event.stopPropagation());
    grip.addEventListener("dblclick", event => event.stopPropagation());
    grip.addEventListener("keydown", event => {
      event.stopPropagation();
      if (event.key === "Escape") {
        event.preventDefault();
        this.cancelCardResize();
        return;
      }
      const offsets = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
      if (!offsets[event.key] || this.small() || this.suspended || this.arranging) {
        return;
      }
      event.preventDefault();
      const value = this.nodes.get(path);
      const step = event.shiftKey ? 40 : 10;
      this.change(path, this.boundedSize((value.width ?? this.metrics.width) + offsets[event.key][0] * step,
        (value.height ?? this.metrics.height) + offsets[event.key][1] * step), "resize");
    });
    return grip;
  }
  /** Clamp persisted dimensions to the managed size contract and stable one-decimal coordinates. */
  boundedSize(width, height) {
    return {
      width: Math.round(Math.max(this.metrics.minimumWidth, Math.min(this.metrics.maximumWidth, width)) * 10) / 10,
      height: Math.round(Math.max(this.metrics.minimumHeight, Math.min(this.metrics.maximumHeight, height)) * 10) / 10
    };
  }
  /** Begin resizing at the current camera and card magnification without selecting or opening the page. */
  startCardResize(event, path, grip) {
    event.stopPropagation();
    if (this.small() || this.suspended || this.arranging || event.button !== 0 || !event.isPrimary || this.cardResize || this.cardDrag) {
      return;
    }
    event.preventDefault();
    this.cancelReveal();
    this.setHoveredEdge(null);
    grip.focus({ preventScroll: true });
    const original = { ...this.nodes.get(path) };
    const width = original.width ?? this.metrics.width;
    const item = this.cards.get(path);
    const magnification = item.container.getBoundingClientRect().width / (width * this.scale);
    // Center-origin scaling moves the bottom-right corner by half the unscaled and scaled size delta.
    const pointerScale = this.scale * (1 + magnification) / 2;
    this.cardResize = { path, grip, pointer: event.pointerId, clientX: event.clientX, clientY: event.clientY,
      original, width, height: original.height ?? this.metrics.height, pointerScale, moved: false };
    item.container.style.setProperty("--card-resize-scale", String(magnification));
    item.container.classList.add("resizing");
    grip.setPointerCapture(event.pointerId);
  }
  /** Preview bounded width and height while keeping all affected connections attached to the visible card. */
  resizeCard(event) {
    const resize = this.cardResize;
    if (!resize || resize.pointer !== event.pointerId) {
      return;
    }
    const deltaX = event.clientX - resize.clientX;
    const deltaY = event.clientY - resize.clientY;
    if (!resize.moved && Math.hypot(deltaX, deltaY) < 3) {
      return;
    }
    event.preventDefault();
    resize.moved = true;
    resize.current = this.boundedSize(resize.width + deltaX / resize.pointerScale, resize.height + deltaY / resize.pointerScale);
    Object.assign(this.nodes.get(resize.path), resize.current);
    this.scheduleGeometryUpdate();
  }
  /** Commit one resize mutation after releasing capture, preserving the existing autosave boundary. */
  finishCardResize(event) {
    const resize = this.cardResize;
    if (!resize || resize.pointer !== event.pointerId) {
      return;
    }
    this.cardResize = null;
    this.cards.get(resize.path)?.container.classList.remove("resizing");
    if (resize.grip.hasPointerCapture(event.pointerId)) {
      resize.grip.releasePointerCapture(event.pointerId);
    }
    if (resize.moved) {
      this.suppressOpenUntil = performance.now() + 500;
      this.lastCardClick = null;
      this.change(resize.path, resize.current, "resize");
    }
    this.animateConnections();
  }
  /** Cancel a size preview on Escape, lost capture or workspace changes without persisting partial dimensions. */
  cancelCardResize() {
    const resize = this.cardResize;
    if (!resize) {
      return;
    }
    this.cancelGeometryUpdate();
    this.cardResize = null;
    this.cards.get(resize.path)?.container.classList.remove("resizing");
    if (resize.grip.hasPointerCapture(resize.pointer)) {
      resize.grip.releasePointerCapture(resize.pointer);
    }
    if (resize.moved) {
      Object.assign(this.nodes.get(resize.path) ?? {}, { width: resize.original.width, height: resize.original.height });
      this.syncCards();
      this.updateBounds();
      this.updateEdges();
    }
    this.animateConnections();
  }
  /** Open the appearance picker for the selected map card at the requested position. */
  openPicker(node, point) {

    if (this.suspended) {
      return;
    }

    this.lastCardClick = null;
    this.cancelCardDrag();

    const value = this.nodes.get(node.path);
    if (value) {
      this.emit({ type: "map-appearance", workspaceId: this.workspaceId, stateId: this.state?.stateId, path: node.path, title: node.title, value, x: point.x, y: point.y });
    }

  }
  /** Clamp and round a logical map coordinate to the persisted layout contract. */
  coordinate(value) {
    return Math.round(Math.max(-this.metrics.maximumCoordinate, Math.min(this.metrics.maximumCoordinate, value)) * 10) / 10;
  }
  /** Begin a card gesture while retaining its pointer identity and original logical position. */
  startCardDrag(event, path, card) {

    this.suppressClick = null;

    if (this.small() || this.suspended || this.arranging || event.button !== 0 || !event.isPrimary || this.cardDrag || this.cardResize
        || this.state.exploration?.selectionEnabled || event.ctrlKey || event.metaKey || event.shiftKey) {
      return;
    }

    event.stopPropagation();
    this.setHoveredEdge(null);
    card.focus({ preventScroll: true });

    this.cardDrag = {
      pointer: event.pointerId,
      path,
      card,
      clientX: event.clientX,
      clientY: event.clientY,
      scale: this.scale,

      original: { ...this.nodes.get(path) },
      moved: false
    };

    card.setPointerCapture(event.pointerId);

  }
  /** Preview the active card gesture in logical coordinates using the current map scale. */
  moveCard(event) {

    const drag = this.cardDrag;
    if (!drag || drag.pointer !== event.pointerId) {
      return;
    }

    const dx = event.clientX - drag.clientX;
    const dy = event.clientY - drag.clientY;

    if (!drag.moved && Math.hypot(dx, dy) < 5) {
      return;
    }

    event.preventDefault();
    drag.moved = true;
    this.cancelReveal();

    drag.current = {
      x: this.coordinate(drag.original.x + dx / drag.scale),
      y: this.coordinate(drag.original.y + dy / drag.scale)
    };

    Object.assign(this.nodes.get(drag.path), drag.current);
    this.cards.get(drag.path).container.classList.add("dragging");

    this.scheduleGeometryUpdate();

  }
  /** Commit the completed card gesture through the managed map change boundary. */
  finishCardDrag(event) {

    const drag = this.cardDrag;
    if (!drag || drag.pointer !== event.pointerId) {
      return;
    }

    this.cardDrag = null;
    this.cards.get(drag.path)?.container.classList.remove("dragging");

    if (drag.card.hasPointerCapture(event.pointerId)) {
      drag.card.releasePointerCapture(event.pointerId);
    }

    if (drag.moved) {
      this.lastCardClick = null;
      this.suppressClick = drag.path;
      this.suppressOpenUntil = performance.now() + 500;
      this.change(drag.path, drag.current, "move");
    }

  }
  /** Cancel the active card gesture and restore its original local position. */
  cancelCardDrag() {

    const drag = this.cardDrag;
    if (!drag) {
      return;
    }
    this.cancelGeometryUpdate();

    this.cardDrag = null;
    this.cards.get(drag.path)?.container.classList.remove("dragging");

    if (drag.card.hasPointerCapture(drag.pointer)) {
      drag.card.releasePointerCapture(drag.pointer);
    }

    if (drag.moved) {

      this.lastCardClick = null;
      Object.assign(this.nodes.get(drag.path) ?? {}, drag.original);
      this.suppressClick = drag.path;

      this.syncCards();
      this.updateBounds();
      this.updateEdges();

    }

  }
  /** Retain an optimistic card change until its matching managed acknowledgment arrives. */
  change(path, changes, action) {

    if (this.suspended || this.arranging) {
      return;
    }

    const changeId = makeId();
    this.pendingEdits.set(changeId, { path, changes });

    Object.assign(this.nodes.get(path), changes);
    this.flushGeometryUpdate();


    const operation = this.emit({
      type: "map",
      action,
      workspaceId: this.workspaceId,
      stateId: this.state?.stateId,
      path,
      changeId,
      ...changes
    });
    this.editOperations.add(operation);
    // Track completion separately from optimistic coordinates, which require managed acknowledgment.
    operation.then(() => this.editOperations.delete(operation), () => this.editOperations.delete(operation));
    return operation;

  }
  /** Freeze map gestures and picker actions during workspace replacement or pending drains. */
  setSuspended(value) {

    this.suspended = value;
    this.body.inert = value || Boolean(this.arranging);


    if (!value) {
      return;
    }

    this.lastCardClick = null;
    this.cancelCameraGesture();
    this.cancelScheduledTransform();
    this.cancelCamera();
    this.transform();
    this.effects.cancelPulses();
    this.effects.cancelRectangle();
    this.cancelCardDrag();
    this.cancelCardResize();
    this.setHoveredEdge(null);

    if (this.drag && this.viewport.hasPointerCapture(this.drag.pointer)) {
      this.viewport.releasePointerCapture(this.drag.pointer);
    }

    this.drag = null;

  }
  /** Apply current logical card positions and appearance to the retained DOM nodes. */
  syncCards() {

    const aiContextPaths = new Set(this.state?.aiContextPaths ?? []);

    for (const [path, item] of this.cards) {

      const value = this.nodes.get(path);
      if (!value) {
        continue;
      }

      item.container.style.left = value.x + "px";
      item.container.style.top = value.y + "px";
      item.container.style.width = (value.width ?? this.metrics.width) + "px";
      item.container.style.height = (value.height ?? this.metrics.height) + "px";
      item.container.classList.toggle("compact-card", (value.height ?? this.metrics.height) < 160);
      item.container.classList.toggle("in-ai-context", aiContextPaths.has(path));
      localize(item.grip, "Size: {0} × {1}", "aria-description", value.width ?? this.metrics.width, value.height ?? this.metrics.height);

      item.container.style.setProperty("--card-color", value.color);

      if (item.icon !== value.icon) {
        item.badge.replaceWith(item.badge = iconSvg("map-" + value.icon));
        item.badge.classList.add("page-icon");
        item.icon = value.icon;
      }

    }

  }
  /** Coalesce high-frequency drag and resize events into one geometry/minimap redraw per animation frame. */
  scheduleGeometryUpdate() {
    if (!this.geometryFrame) {
      this.geometryFrame = requestAnimationFrame(() => {
        this.geometryFrame = null;
        this.flushGeometryUpdate();
      });
    }
  }

  /** Cancel a deferred preview before synchronous commits, restoration, rendering or disposal. */
  cancelGeometryUpdate() {
    if (this.geometryFrame) {
      cancelAnimationFrame(this.geometryFrame);
      this.geometryFrame = null;
    }
  }

  /** Draw the latest model coordinates and cancel redundant queued previews before persisting a gesture. */
  flushGeometryUpdate() {
    this.cancelGeometryUpdate();
    this.syncCards();
    this.updateBounds();
    this.updateEdges();
  }

  /** Fit logical card bounds plus selected-card magnification, retaining room for folder frames and shadows. */
  updateBounds() {

    if (!this.metrics) {
      return;
    }
    this.atlas.drawFolders();

    const bounds = [];
    for (const path of this.cards.keys()) {
      const node = this.nodes.get(path);
      if (!node || this.cards.get(path)?.container.hidden) {
        continue;
      }
      const width = node.width ?? this.metrics.width;
      const height = node.height ?? this.metrics.height;
      const magnification = path === this.selected ? this.selectedCardScale() : 1;
      const horizontalOutset = width * (magnification - 1) / 2;
      const verticalOutset = height * (magnification - 1) / 2;
      bounds.push({
        left: node.x - horizontalOutset,
        top: node.y - verticalOutset,
        right: node.x + width + horizontalOutset,
        bottom: node.y + height + verticalOutset
      });
    }

    bounds.push(...this.atlas.folderBounds());
    this.minX = bounds.length ? Math.min(...bounds.map(bound => bound.left)) - 50 : 0;
    this.minY = bounds.length ? Math.min(...bounds.map(bound => bound.top)) - 90 : 0;
    this.worldWidth = bounds.length ? Math.max(...bounds.map(bound => bound.right)) + 50 - this.minX : 300;
    this.worldHeight = bounds.length ? Math.max(...bounds.map(bound => bound.bottom)) + 50 - this.minY : 180;

    this.world.style.width = this.worldWidth + "px";
    this.world.style.height = this.worldHeight + "px";

    if (!this.graph) {
      return;
    }

    this.graph.setAttribute("width", this.worldWidth);
    this.graph.setAttribute("height", this.worldHeight);

    this.graph.setAttribute("viewBox", [this.minX, this.minY, this.worldWidth, this.worldHeight].join(" "));

    this.graph.style.left = this.minX + "px";
    this.graph.style.top = this.minY + "px";
    this.atlas.drawMinimap();

  }
  /** Reposition graph connections from the current card geometry. */
  updateEdges() {

    if (!this.metrics) {
      return;
    }

    const geometry = this.cardGeometry();
    for (const item of this.edgePaths ?? []) {
      const { edge, path, hitPath, group } = item;
      const source = this.atlas.connectionGeometry(edge.source, geometry.get(edge.source));
      const target = this.atlas.connectionGeometry(edge.target, geometry.get(edge.target));
      const hidden = !source || !target || source.island !== undefined && source.island === target.island;
      const display = hidden ? "none" : "";
      if (group.style.display !== display) {
        group.style.display = display;
      }
      if (hidden) {
        if (this.hoveredEdge?.group === group) {
          this.setHoveredEdge(null);
        }
        continue;
      }

      const sourceCenterX = source.x + source.width / 2;
      const sourceCenterY = source.y + source.height / 2;
      const targetCenterX = target.x + target.width / 2;
      const targetCenterY = target.y + target.height / 2;
      const dx = targetCenterX - sourceCenterX;
      const dy = targetCenterY - sourceCenterY;
      let connection;

      if (edge.source === edge.target) {

        const startX = source.x + source.width;
        connection = `M${startX},${sourceCenterY} C${startX + 45},${source.y - 85} ${source.x - 45},${source.y - 85} ${source.x},${sourceCenterY}`;

      } else if (Math.abs(dx) / ((source.width + target.width) / 2) >= Math.abs(dy) / ((source.height + target.height) / 2)) {

        const direction = dx >= 0 ? 1 : -1;
        const startX = source.x + (direction > 0 ? source.width : 0);
        const endX = target.x + (direction > 0 ? 0 : target.width);
        const bend = Math.max(45, Math.abs(endX - startX) * .45);
        connection = `M${startX},${sourceCenterY} C${startX + direction * bend},${sourceCenterY} ${endX - direction * bend},${targetCenterY} ${endX},${targetCenterY}`;

      } else {

        const direction = dy >= 0 ? 1 : -1;
        const startY = source.y + (direction > 0 ? source.height : 0);
        const endY = target.y + (direction > 0 ? 0 : target.height);
        const bend = Math.max(45, Math.abs(endY - startY) * .45);
        connection = `M${sourceCenterX},${startY} C${sourceCenterX},${startY + direction * bend} ${targetCenterX},${endY - direction * bend} ${targetCenterX},${endY}`;

      }
      // Stationary connections keep their SVG geometry through other cards' scale/drag frames.
      if (item.connection !== connection) {
        path.setAttribute("d", connection);
        hitPath.setAttribute("d", connection);
        item.connection = connection;
      }
    }

    this.effects.updateGeometry(geometry);
  }
  /** Measure each card once per frame so connection endpoints follow animated magnification and custom sizes. */
  cardGeometry() {
    const geometry = new Map();
    const viewportBounds = this.viewport.getBoundingClientRect();
    const screenScale = viewportBounds.width > 0 ? this.viewport.clientWidth / viewportBounds.width : 1;
    const compact = this.small();
    for (const [path, item] of this.cards) {
      const value = this.nodes.get(path);
      if (!value) {
        continue;
      }
      const bounds = item.container.getBoundingClientRect();
      if (bounds.width > 0 && !compact) {
        geometry.set(path, {
          x: ((bounds.left - viewportBounds.left) * screenScale - this.x) / this.scale,
          y: ((bounds.top - viewportBounds.top) * screenScale - this.y) / this.scale,
          width: bounds.width * screenScale / this.scale,
          height: bounds.height * screenScale / this.scale
        });
      } else {
        geometry.set(path, { ...value, width: value.width ?? this.metrics.width, height: value.height ?? this.metrics.height });
      }
    }
    return geometry;
  }
  /** Refresh edge geometry during the short card-scale transition, with no continuing animation loop. */
  animateConnections() {
    this.connectionAnimationUntil = performance.now() + (this.reducedMotion.matches ? 0 : 460);
    this.updateEdges();
    if (!this.connectionFrame && !this.reducedMotion.matches) {
      this.connectionFrame = requestAnimationFrame(now => this.refreshAnimatedConnections(now));
    }
  }
  /** Stop frame work as soon as the current magnification transition has completed. */
  refreshAnimatedConnections(now) {
    this.connectionFrame = null;
    this.updateEdges();
    if (now < this.connectionAnimationUntil) {
      this.connectionFrame = requestAnimationFrame(next => this.refreshAnimatedConnections(next));
    }
  }
  /** Zoom around the pointer, or the viewport center for non-pointer commands. */
  zoom(factor, anchor) {

    this.cancelCamera();

    if (!this.metrics || this.small()) {
      return;
    }

    this.beginCameraGesture();
    const previous = this.scale;
    this.scale = Math.max(.001, Math.min(1.8, previous * factor));

    const cx = anchor?.x ?? this.viewport.clientWidth / 2;
    const cy = anchor?.y ?? this.viewport.clientHeight / 2;

    this.x = cx - (cx - this.x) * this.scale / previous;
    this.y = cy - (cy - this.y) * this.scale / previous;

    this.scheduleTransform();

  }
  /** Fit the current map bounds within the available viewport. */
  fit() {

    this.cancelCamera();

    if (!this.metrics) {
      return;
    }

    // A new shadow host can render before its stylesheet establishes viewport height.
    // Defer fit instead of retaining a near-zero camera scale for a visible graph.
    if (this.viewport.clientWidth <= 60 || this.viewport.clientHeight <= 60) {
      this.needsFit = true;
      return;
    }
    this.needsFit = false;

    const topInset = Math.min(150, this.viewport.clientHeight * .3);
    const availableHeight = this.viewport.clientHeight - topInset - 30;
    this.scale = Math.max(.001, Math.min(1, (this.viewport.clientWidth - 60) / Math.max(1, this.worldWidth), availableHeight / Math.max(1, this.worldHeight)));

    this.x = (this.viewport.clientWidth - this.worldWidth * this.scale) / 2 - this.minX * this.scale;

    this.y = topInset + (availableHeight - this.worldHeight * this.scale) / 2 - this.minY * this.scale;
    this.transform();

  }
  /** Coalesce camera input while keeping its logical coordinates current for subsequent pointer events. */
  scheduleTransform() {
    if (!this.transformFrame) {
      this.transformFrame = requestAnimationFrame(() => {
        this.transformFrame = null;
        this.transform();
      });
    }
  }

  /** Keep camera gestures free of hover magnification and repeated SVG stroke/marker repainting. */
  beginCameraGesture() {
    clearTimeout(this.cameraGestureTimer);
    if (!this.cameraGesture) {
      this.cameraGesture = true;
      this.body.classList.add("map-camera-moving");
      for (const item of this.cards.values()) {
        item.container.classList.remove("hovered");
      }
      this.effects.focus(null);
      if (this.connectionFrame) {
        cancelAnimationFrame(this.connectionFrame);
        this.connectionFrame = null;
      }
      // Freeze connections at the resting card geometry before the camera starts moving.
      this.updateEdges();
    }
    this.cameraGestureTimer = setTimeout(() => this.finishCameraGesture(), 180);
  }

  /** Restore exact stroke widths and the card under a stationary pointer after the camera settles. */
  finishCameraGesture() {
    if (!this.cameraGesture) {
      return;
    }
    this.cancelCameraGesture();
    this.transform();
    const pointer = this.pointerPosition;
    const target = pointer && this.root.elementFromPoint(pointer.x, pointer.y)?.closest(".map-card");
    const path = target?.dataset.mapNode;
    if (path && this.cards.get(path)?.container === target && !this.drag && !this.cardDrag && !this.cardResize) {
      target.classList.add("hovered");
      this.effects.focus(path);
      if (path !== this.selected) {
        this.animateConnections();
      }
    }
  }

  /** Cancel transient camera presentation without scheduling work against replaced or disposed DOM. */
  cancelCameraGesture() {
    clearTimeout(this.cameraGestureTimer);
    this.cameraGestureTimer = null;
    this.cameraGesture = false;
    this.body.classList.remove("map-camera-moving");
  }

  /** Discard queued camera drawing before an immediate transform, replacement or disposal. */
  cancelScheduledTransform() {
    if (this.transformFrame) {
      cancelAnimationFrame(this.transformFrame);
      this.transformFrame = null;
    }
  }

  /** Apply pan and zoom after one layout read, limiting changing stroke variables to the SVG subtree. */
  transform() {
    this.cancelScheduledTransform();
    const viewport = this.atlas.viewportMetrics();
    // Rectangle bounds are captured in screen space; any camera change invalidates them.
    this.effects.cancelRectangle();
    this.world.style.transform = viewport.compact ? "none" : "translate(" + this.x + "px," + this.y + "px) scale(" + this.scale + ")";
    if (!this.cameraGesture && this.graph && (this.strokeGraph !== this.graph || this.strokeScale !== this.scale)) {
      this.graph.style.setProperty("--edge-hit-width", 14 / this.scale + "px");
      this.graph.style.setProperty("--edge-hover-width", 3 / this.scale + "px");
      this.strokeGraph = this.graph;
      this.strokeScale = this.scale;
    }
    this.atlas.updateZoom(viewport);
  }
  /** Stop an in-flight camera move immediately; the next user gesture continues from the current position. */
  cancelCamera() {
    if (this.cameraFrame) {
      cancelAnimationFrame(this.cameraFrame);
      this.cameraFrame = null;
    }
    this.pendingCenter = null;
  }
  /** Bring a search hit into readable focus with bounded easing, respecting reduced motion and compact scrolling. */
  centerCard(path) {
    this.cancelCamera();
    const node = this.nodes.get(path);
    const item = this.cards.get(path);
    if (!node || !item || !this.metrics) {
      return;
    }
    const viewportWidth = this.viewport.clientWidth;
    const viewportHeight = this.viewport.clientHeight;
    if (viewportWidth <= 60 || viewportHeight <= 60) {
      this.pendingCenter = path;
      return;
    }
    this.cancelReveal();
    const compact = this.small();
    const width = node.width ?? this.metrics.width;
    const height = node.height ?? this.metrics.height;
    const magnification = this.selectedCardScale();
    const targetScale = Math.max(.05, Math.min(1.15,
      (viewportWidth - 80) / (width * magnification),
      (viewportHeight - 80) / (height * magnification)));
    const start = { x: this.x, y: this.y, scale: this.scale, scroll: this.viewport.scrollTop };
    const target = {
      x: viewportWidth / 2 - (node.x + width / 2) * targetScale,
      y: viewportHeight / 2 - (node.y + height / 2) * targetScale,
      scroll: Math.max(0, item.container.offsetTop - (viewportHeight - item.container.offsetHeight) / 2)
    };
    const started = performance.now();
    /** Interpolate only camera state; card positions and selection remain managed by C#. */
    const advance = now => {
      const progress = this.reducedMotion.matches ? 1 : Math.min(1, (now - started) / 420);
      const eased = 1 - Math.pow(1 - progress, 3);
      if (compact) {
        this.viewport.scrollTop = start.scroll + (target.scroll - start.scroll) * eased;
      } else {
        this.x = start.x + (target.x - start.x) * eased;
        this.y = start.y + (target.y - start.y) * eased;
        this.scale = start.scale + (targetScale - start.scale) * eased;
        this.transform();
      }
      this.cameraFrame = progress < 1 ? requestAnimationFrame(advance) : null;
    };
    advance(started);
  }
  /** Cancel visual entry effects so dragging, rerendering and reduced motion leave cards fully visible. */
  cancelReveal() {
    for (const animation of this.revealAnimations) {
      animation.cancel();
    }
    this.revealAnimations = [];
  }
  /** Reveal cards from the layout center outward with a bounded stagger, respecting reduced motion. */
  revealCards() {
    this.cancelReveal();
    if (this.reducedMotion.matches || !this.cards.size) {
      return;
    }

    const centerX = this.minX + this.worldWidth / 2;
    const centerY = this.minY + this.worldHeight / 2;
    const orderedCards = [...this.cards.entries()].map(([path, item]) => {
      const position = this.nodes.get(path);
      const distance = position
        ? Math.hypot(position.x + this.metrics.width / 2 - centerX, position.y + this.metrics.height / 2 - centerY)
        : Number.POSITIVE_INFINITY;
      return { item, distance };
    }).sort((first, second) => first.distance - second.distance);

    // Large workspaces must not leave the last card invisible for several seconds.
    const stagger = Math.min(55, 1000 / Math.max(1, orderedCards.length - 1));
    for (const [index, { item }] of orderedCards.entries()) {
      this.revealAnimations.push(item.card.animate([
        { opacity: 0, transform: "translateY(14px)" },
        { opacity: 1, transform: "none" }
      ], { duration: 380, delay: index * stagger, easing: "cubic-bezier(.2,.7,.2,1)", fill: "backwards" }));
    }
    if (this.graph) {
      this.revealAnimations.push(this.graph.animate([{ opacity: 0 }, { opacity: 1 }], {
        duration: 800, delay: 120, easing: "ease-out", fill: "backwards"
      }));
    }
  }
  /** Recompute map presentation after the host's available dimensions change. */
  resize() {
    this.effects.cancelRectangle();
    if (this.small()) {
      this.effects.cancelPulses();
      this.cancelCardDrag();
      this.cancelCardResize();
    }

    this.body.classList.toggle("small-map", this.small());
    this.atlas.acceptState();
    this.effects.acceptState();
    this.updateBounds();
    this.updateEdges();
    const pendingCenter = this.pendingCenter;
    if (this.needsFit) {
      this.fit();
    }
    this.transform();
    this.atlas.restoreCamera();
    if (pendingCenter) {
      this.centerCard(pendingCenter);
    }
    const visible = this.viewport.clientWidth > 0 && this.viewport.clientHeight > 0;
    if (!visible) {
      this.needsReveal = true;
    } else if (this.needsReveal) {
      this.needsReveal = false;
      this.revealCards();
    }

  }
  /** Cancel gestures and release observers, picker resources and the graph DOM root. */
  dispose() {
    this.cancelCameraGesture();
    this.cancelScheduledTransform();
    this.cancelGeometryUpdate();
    this.effects.dispose();
    this.cancelCamera();
    clearTimeout(this.arrangementTimer);
    document.removeEventListener("keydown", this.cancelCameraOnEscape, { capture: true });
    this.setHoveredEdge(null);
    this.cancelReveal();
    this.reducedMotion.removeEventListener("change", this.motionChanged);
    this.cancelCardDrag();
    this.cancelCardResize();
    if (this.connectionFrame) {
      cancelAnimationFrame(this.connectionFrame);
      this.connectionFrame = null;
    }
    this.drag = null;
    this.resizeObserver.disconnect();
    this.disposeRoot();
    this.container.remove();
  }
}
