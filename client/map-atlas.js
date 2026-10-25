/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; atlas rendering, semantic zoom, folder islands and minimap; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement all seven document atlas improvements
 * Modified with AI: OpenAI Codex; share document activation between overview labels and cards; 2026-10-16
 * Modified with AI: OpenAI Codex; batch viewport reads and retain semantic zoom and minimap elements; 2026-10-23
 * Modified with AI: OpenAI Codex; stable semantic detail during dense graph gestures; 2026-10-25
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { textElement, uiText } from "./markdown.js";
import { t, localize } from "./i18n.js";

const svgNamespace = "http://www.w3.org/2000/svg";
const islandColors = ["#009fbd", "#914bff", "#e8ac45", "#008f83", "#5865d9", "#d64578"];

/** Create a bounded SVG element without interpolating document content into HTML. */
function svgNode(name, attributes = {}) {
  const node = document.createElementNS(svgNamespace, name);
  for (const [key, value] of Object.entries(attributes)) {
    node.setAttribute(key, String(value));
  }
  return node;
}

/** Return the immediate canonical folder identifier used by the managed catalog. */
function folderOf(path) {
  const separator = path.lastIndexOf("/");
  return separator < 0 ? "" : path.slice(0, separator);
}

/** Derive a stable presentation color without changing any manually chosen card color. */
function folderColor(folder) {
  let hash = 0;
  for (const character of folder) {
    hash = (hash * 31 + character.codePointAt(0)) >>> 0;
  }
  return islandColors[hash % islandColors.length];
}

export class MapAtlasRenderer {
  /** Mount graph-specific overlays; application controls and persisted state remain managed by C#. */
  constructor(host) {
    this.host = host;
    this.groups = new Map();
    this.labels = new Map();
    this.miniMarks = new Map();
    this.cameraRevision = 0;
    this.overview = svgNode("svg", { class: "map-minimap", viewBox: "0 0 300 180", tabindex: 0, role: "img" });
    localize(this.overview, "Map overview. Click to navigate; use arrow keys to pan and Home to fit.", "aria-label");
    this.miniNodes = svgNode("g", { "aria-hidden": "true" });
    this.miniViewport = svgNode("rect", { class: "map-minimap-window", "pointer-events": "none" });
    this.overview.append(this.miniNodes, this.miniViewport);
    this.scaleLabel = textElement("div", "", "map-zoom-label");
    this.scaleValue = textElement("span", "");
    this.scaleMode = uiText("span", "Documents");
    this.scaleLabel.append(this.scaleValue, this.scaleMode);
    this.scaleLabel.setAttribute("aria-hidden", "true");
    this.tooltip = document.createElement("div");
    this.tooltip.className = "map-edge-tooltip";
    this.tooltip.hidden = true;
    this.overviewLabels = document.createElement("div");
    this.overviewLabels.className = "map-overview-labels";
    host.viewport.append(this.overviewLabels, this.overview, this.scaleLabel, this.tooltip);

    this.overview.addEventListener("pointerdown", event => {
      if (event.button !== 0 || host.suspended || host.arranging) {
        return;
      }
      event.preventDefault();
      event.stopPropagation();
      this.overview.setPointerCapture(event.pointerId);
      this.miniPointer = event.pointerId;
      this.navigateOverview(event);
    });
    this.overview.addEventListener("pointermove", event => {
      if (this.miniPointer === event.pointerId) {
        this.navigateOverview(event);
      }
    });
    /** Release only the matching minimap gesture, including cancellation and lost capture. */
    const release = event => {
      if (this.miniPointer === event.pointerId) {
        this.miniPointer = null;
        if (this.overview.hasPointerCapture(event.pointerId)) {
          this.overview.releasePointerCapture(event.pointerId);
        }
      }
    };
    this.overview.addEventListener("pointerup", release);
    this.overview.addEventListener("pointercancel", release);
    this.overview.addEventListener("lostpointercapture", release);
    this.overview.addEventListener("keydown", event => {
      if (host.suspended || host.arranging) {
        return;
      }
      const directions = { ArrowLeft: [60, 0], ArrowRight: [-60, 0], ArrowUp: [0, 60], ArrowDown: [0, -60] };
      if (directions[event.key]) {
        event.preventDefault();
        event.stopPropagation();
        host.cancelCamera();
        host.x += directions[event.key][0];
        host.y += directions[event.key][1];
        host.transform();
      } else if (event.key === "Home") {
        event.preventDefault();
        event.stopPropagation();
        host.fit();
      }
    });
  }

  /** Publish an explicit atlas action with current session identity and a logical camera when requested. */
  command(action, data = {}) {
    const host = this.host;
    if (host.suspended || host.arranging || host.cardDrag || host.cardResize) {
      return;
    }
    const payload = { ...data };
    if (action === "atlas-save-view") {
      payload.camera = {
        centerX: (host.viewport.clientWidth / 2 - host.x) / host.scale,
        centerY: (host.viewport.clientHeight / 2 - host.y) / host.scale,
        scale: host.scale
      };
    }
    return host.emit({ ...payload, type: "map", action, workspaceId: host.workspaceId, stateId: host.state?.stateId });
  }

  /** Reset view-local caches when the managed layout session changes, including reopening the same folder. */
  acceptState() {
    const host = this.host;
    const viewport = this.viewportMetrics();
    if (this.stateId !== host.state?.stateId) {
      this.stateId = host.state?.stateId;
      this.cameraRevision = 0;
      for (const group of this.groups.values()) {
        group.frame.remove();
      }
      this.groups.clear();
      this.labels.clear();
      this.overviewLabels.replaceChildren();
    }
    this.collapsed = new Set(host.state?.layout.atlas?.collapsedFolders ?? []);
    this.focusDepth = host.state?.focusDepth ?? 0;
    this.routePairs = new Set();
    const route = host.map?.route ?? [];
    this.routePaths = new Set(route);
    for (let index = 1; index < route.length; index++) {
      this.routePairs.add(JSON.stringify([route[index - 1], route[index]]));
      this.routePairs.add(JSON.stringify([route[index], route[index - 1]]));
    }
    for (const [path, item] of host.cards) {
      const distance = host.map?.depths?.[path] ?? -1;
      const nearby = this.focusDepth === 0 || distance >= 0 && distance <= this.focusDepth || this.routePaths.has(path);
      item.container.style.setProperty("--atlas-focus-opacity", nearby ? "1" : ".18");
      item.container.classList.toggle("route-member", this.routePaths.has(path));
      item.container.hidden = !viewport.compact && host.state?.folderGroups && this.collapsed.has(folderOf(path));
    }
    for (const item of host.edgePaths ?? []) {
      const onRoute = this.routePairs.has(JSON.stringify([item.edge.source, item.edge.target]));
      item.path.classList.toggle("route-connection", onRoute);
      item.path.setAttribute("marker-end", "url(#arrow-" + (onRoute ? "route-" : "") + host.id + ")");
      const distances = [host.map?.depths?.[item.edge.source] ?? -1, host.map?.depths?.[item.edge.target] ?? -1];
      const nearby = this.focusDepth === 0 || distances.every(distance => distance >= 0 && distance <= this.focusDepth) || onRoute;
      item.group.style.setProperty("--atlas-edge-opacity", nearby ? "1" : ".12");
      item.hitPath.setAttribute("tabindex", item.edge.source === host.selected || item.edge.target === host.selected ? "0" : "-1");
    }
    this.updateZoom(viewport);
  }

  /** Draw stable, keyboard-accessible islands using full-layout bounds and full-catalog folder counts. */
  drawFolders() {
    const host = this.host;
    if (!host.folderLayer) {
      return;
    }
    const active = new Set();
    if (host.state?.folderGroups && !host.small()) {
      const membersByFolder = new Map();
      for (const [path, node] of host.nodes) {
        const folder = folderOf(path);
        if (!membersByFolder.has(folder)) {
          membersByFolder.set(folder, []);
        }
        membersByFolder.get(folder).push(node);
      }
      for (const folder of host.map?.folders ?? []) {
        const members = membersByFolder.get(folder.path) ?? [];
        if (!members.length) {
          continue;
        }
        const left = Math.min(...members.map(node => node.x)) - 32;
        const top = Math.min(...members.map(node => node.y)) - 62;
        const right = Math.max(...members.map(node => node.x + (node.width ?? host.metrics.width))) + 32;
        const bottom = Math.max(...members.map(node => node.y + (node.height ?? host.metrics.height))) + 32;
        let group = this.groups.get(folder.path);
        if (!group) {
          const frame = document.createElement("section");
          frame.className = "map-folder-group";
          frame.dataset.mapFolder = folder.path;
          const button = document.createElement("button");
          button.type = "button";
          button.className = "map-folder-toggle";
          const label = textElement("span", folder.path || t("Workspace root"), "map-folder-label");
          const count = textElement("span", "", "map-folder-count");
          button.append(label, count);
          button.addEventListener("pointerdown", event => event.stopPropagation());
          button.addEventListener("click", event => {
            event.stopPropagation();
            void this.command("atlas-folder", { folder: folder.path });
          });
          frame.append(button);
          group = { frame, button, label, count };
          this.groups.set(folder.path, group);
        }
        const collapsed = this.collapsed?.has(folder.path);
        group.bounds = { left, top, right: collapsed ? left + 300 : right, bottom: collapsed ? top + 110 : bottom };
        group.frame.classList.toggle("collapsed", Boolean(collapsed));
        group.frame.style.setProperty("--island-color", folderColor(folder.path));
        group.frame.style.left = left + "px";
        group.frame.style.top = top + "px";
        group.frame.style.width = group.bounds.right - left + "px";
        group.frame.style.height = group.bounds.bottom - top + "px";
        group.button.setAttribute("aria-expanded", String(!collapsed));
        const label = folder.path || t("Workspace root");
        group.label.textContent = (collapsed ? "▸ " : "▾ ") + label;
        localize(group.count, "{0} documents", "textContent", folder.count);
        localize(group.button, collapsed ? "Expand folder {0}" : "Collapse folder {0}", "aria-label", label);
        if (group.frame.parentNode !== host.folderLayer) {
          host.folderLayer.append(group.frame);
        }
        active.add(folder.path);
      }
    }
    for (const [folder, group] of this.groups) {
      if (!active.has(folder)) {
        group.frame.remove();
        this.groups.delete(folder);
      }
    }
  }

  /** Return folder rectangles for fitting the map without moving or resizing any document card. */
  folderBounds() {
    return [...this.groups.values()].map(group => group.bounds);
  }

  /** Anchor collapsed documents at their island while suppressing connections internal to that same island. */
  connectionGeometry(path, fallback) {
    const host = this.host;
    if (!host.cards.get(path)?.container.hidden) {
      return fallback;
    }
    const bounds = this.groups.get(folderOf(path))?.bounds;
    return bounds ? { x: bounds.left, y: bounds.top, width: bounds.right - bounds.left, height: bounds.bottom - bounds.top,
      island: folderOf(path) } : null;
  }

  /** Read viewport dimensions together before overlay writes can invalidate their layout. */
  viewportMetrics() {
    const host = this.host;
    return { compact: host.small(), width: host.viewport.clientWidth, height: host.viewport.clientHeight };
  }

  /** Update zoom labels only when their meaning changes; camera movement retains their DOM and text. */
  updateZoom(viewport = this.viewportMetrics()) {
    const host = this.host;
    // Keep text layout stable during dense-map gestures. The camera, labels and
    // minimap still move on every frame; semantic detail catches up on settling.
    const presentationScale = host.dense && host.cameraGesture
      ? this.presentationScale ?? host.scale
      : host.scale;
    this.presentationScale = presentationScale;
    const overview = !viewport.compact && presentationScale < .4;
    const compact = !viewport.compact && presentationScale >= .4 && presentationScale < .72;
    const detail = !viewport.compact && presentationScale >= 1.05;
    const signature = [viewport.compact, overview, compact, detail, document.documentElement.lang].join(":");
    if (this.zoomSignature !== signature) {
      host.body.classList.toggle("map-overview", overview);
      host.body.classList.toggle("map-compact", compact);
      host.body.classList.toggle("map-detail", detail);
      localize(this.scaleMode, overview ? "Overview" : detail ? "Detail" : "Documents");
      this.overview.hidden = viewport.compact;
      this.scaleLabel.hidden = viewport.compact;
      this.overviewLabels.hidden = !overview;
      this.zoomSignature = signature;
    }
    // The title size only affects overview cards; avoid inherited CSS invalidation at other zoom levels.
    const titleSize = Math.min(32, 13 / Math.max(.1, presentationScale)) + "px";
    if (overview && this.overviewTitleSize !== titleSize) {
      host.world.style.setProperty("--overview-title-size", titleSize);
      this.overviewTitleSize = titleSize;
    }
    const percentage = Math.round(host.scale * 100);
    if (this.zoomPercentage !== percentage) {
      this.scaleValue.textContent = percentage + "% · ";
      this.zoomPercentage = percentage;
    }
    this.drawOverviewLabels(overview, viewport);
    this.updateMiniViewport(viewport);
  }

  /** Keep selected/hub and island names at a readable screen size when the graph itself becomes very small. */
  drawOverviewLabels(visible, viewport = this.viewportMetrics()) {
    if (!visible) {
      return;
    }
    const host = this.host;
    const candidates = [];
    const selected = host.map?.nodes.find(node => node.path === host.selected);
    const documents = selected ? [selected, ...host.map.nodes.filter(node => node.path !== selected.path)] : host.map?.nodes ?? [];
    // Choose visible documents before limiting labels, so panning away from the hub still reveals readable names.
    for (const document of documents.slice(0, host.state?.folderGroups ? 1 : documents.length)) {
      const node = host.nodes.get(document.path);
      if (node && !host.cards.get(document.path)?.container.hidden) {
        candidates.push({ key: "document:" + document.path, title: document.title, path: document.path,
          x: node.x + (node.width ?? host.metrics.width) / 2, y: node.y + (node.height ?? host.metrics.height) / 2,
          selected: document.path === host.selected });
      }
    }
    for (const [folder, group] of this.groups) {
      candidates.push({ key: "folder:" + folder, title: folder || t("Workspace root"), folder,
        x: (group.bounds.left + group.bounds.right) / 2, y: group.bounds.top });
    }
    const occupied = [];
    const active = new Set();
    for (const candidate of candidates) {
      const x = host.x + candidate.x * host.scale;
      const y = host.y + candidate.y * host.scale;
      if (x < 16 || x > viewport.width - 16 || y < 145 || y > viewport.height - 55) {
        continue;
      }
      const left = Math.max(16, Math.min(viewport.width - 164, x - 74));
      const top = y - 14;
      if (occupied.some(rectangle => Math.abs(rectangle.left - left) < 158 && Math.abs(rectangle.top - top) < 36)) {
        continue;
      }
      let label = this.labels.get(candidate.key);
      if (!label) {
        label = document.createElement("button");
        label.type = "button";
        label.className = "map-overview-label";
        label.addEventListener("pointerdown", event => event.stopPropagation());
        label.addEventListener("click", event => {
          event.stopPropagation();
          if (candidate.folder !== undefined) {
            void this.command("atlas-folder", { folder: candidate.folder });
          } else if (!host.suspended && !host.arranging) {
            host.activateCard(candidate.path, event, true);
          }
        });
        this.labels.set(candidate.key, label);
        this.overviewLabels.append(label);
      }
      if (label.textContent !== candidate.title) {
        label.textContent = candidate.title;
        label.setAttribute("title", candidate.title);
      }
      label.classList.toggle("selected", Boolean(candidate.selected));
      if (label.style.left !== left + "px") {
        label.style.left = left + "px";
      }
      if (label.style.top !== top + "px") {
        label.style.top = top + "px";
      }
      active.add(candidate.key);
      occupied.push({ left, top });
      if (active.size >= 12) {
        break;
      }
    }
    for (const [key, label] of this.labels) {
      if (!active.has(key)) {
        label.remove();
        this.labels.delete(key);
      }
    }
  }

  /** Retain minimap marks across moves and update only changed coordinates, appearance and membership. */
  drawMinimap() {
    const host = this.host;
    if (!host.metrics || !Number.isFinite(host.worldWidth)) {
      return;
    }
    const viewport = this.viewportMetrics();
    this.miniBounds = { x: host.minX, y: host.minY, width: Math.max(1, host.worldWidth), height: Math.max(1, host.worldHeight) };
    const { x, y, width, height } = this.miniBounds;
    const viewBox = [x, y, width, height].join(" ");
    if (this.overview.getAttribute("viewBox") !== viewBox) {
      this.overview.setAttribute("viewBox", viewBox);
    }
    const active = new Set();
    for (const [path, item] of host.cards) {
      const node = host.nodes.get(path);
      if (!node || item.container.hidden) {
        continue;
      }
      this.updateMiniMark("document:" + path, { x: node.x, y: node.y, width: node.width ?? host.metrics.width,
        height: node.height ?? host.metrics.height, rx: 10, fill: path === host.selected ? "var(--accent)" : node.color,
        opacity: path === host.selected ? 1 : .65 }, active);
    }
    for (const [folder, group] of this.groups) {
      if (group.frame.classList.contains("collapsed")) {
        const bounds = group.bounds;
        this.updateMiniMark("folder:" + folder, { x: bounds.left, y: bounds.top, width: bounds.right - bounds.left,
          height: bounds.bottom - bounds.top, rx: 12, fill: group.frame.style.getPropertyValue("--island-color") }, active);
      }
    }
    for (const [key, mark] of this.miniMarks) {
      if (!active.has(key)) {
        mark.remove();
        this.miniMarks.delete(key);
      }
    }
    // Preserve document/folder paint order when membership changes, without reparenting marks during moves.
    const order = JSON.stringify([...active]);
    if (this.miniOrder !== order) {
      this.miniNodes.replaceChildren(...[...active].map(key => this.miniMarks.get(key)));
      this.miniOrder = order;
    }
    this.updateMiniViewport(viewport);
  }

  /** Keep one SVG rectangle per document or collapsed folder, including through selection changes. */
  updateMiniMark(key, attributes, active) {
    let mark = this.miniMarks.get(key);
    if (!mark) {
      mark = svgNode("rect", attributes);
      this.miniMarks.set(key, mark);
      this.miniNodes.append(mark);
    } else {
      for (const [name, value] of Object.entries(attributes)) {
        const text = String(value);
        if (mark.getAttribute(name) !== text) {
          mark.setAttribute(name, text);
        }
      }
    }
    active.add(key);
  }

  /** Move the camera rectangle using existing SVG nodes so zoom and pan do not rebuild the minimap. */
  updateMiniViewport(viewport = this.viewportMetrics()) {
    const host = this.host;
    const attributes = { x: -host.x / host.scale, y: -host.y / host.scale,
      width: viewport.width / host.scale, height: viewport.height / host.scale };
    for (const [name, value] of Object.entries(attributes)) {
      const text = String(value);
      if (this.miniViewport.getAttribute(name) !== text) {
        this.miniViewport.setAttribute(name, text);
      }
    }
  }

  /** Convert a minimap pointer through its SVG matrix, respecting letterboxing and desktop scaling. */
  navigateOverview(event) {
    const matrix = this.overview.getScreenCTM();
    if (!matrix || !this.miniBounds) {
      return;
    }
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    const host = this.host;
    const bounds = this.miniBounds;
    point.x = Math.max(bounds.x, Math.min(bounds.x + bounds.width, point.x));
    point.y = Math.max(bounds.y, Math.min(bounds.y + bounds.height, point.y));
    host.cancelCamera();
    host.x = host.viewport.clientWidth / 2 - point.x * host.scale;
    host.y = host.viewport.clientHeight / 2 - point.y * host.scale;
    host.transform();
  }

  /** Restore each managed camera revision once, deferring until a hidden host has measurable dimensions. */
  restoreCamera() {
    const host = this.host;
    const revision = host.state?.cameraRevision ?? 0;
    if (host.rendering || revision <= this.cameraRevision || host.viewport.clientWidth <= 60 || host.viewport.clientHeight <= 60) {
      return;
    }
    const camera = host.state?.camera;
    if (!camera) {
      this.cameraRevision = revision;
      return;
    }
    if (host.small()) {
      return;
    }
    this.cameraRevision = revision;
    host.cancelCamera();
    host.scale = camera.scale;
    host.x = host.viewport.clientWidth / 2 - camera.centerX * camera.scale;
    host.y = host.viewport.clientHeight / 2 - camera.centerY * camera.scale;
    host.transform();
  }

  /** Show the directed source relationship and its managed source-line excerpt without rendering Markdown or HTML. */
  showConnection(item) {
    this.tooltip.hidden = !item;
    this.tooltip.replaceChildren();
    for (const [folder, group] of this.groups) {
      const connected = item && [item.edge.source, item.edge.target].some(path => folderOf(path) === folder);
      group.frame.classList.toggle("edge-endpoint", Boolean(connected));
    }
    if (!item) {
      return;
    }
    const host = this.host;
    const edge = item.edge;
    const source = host.map.nodes.find(node => node.path === edge.source)?.title ?? edge.source;
    const target = host.map.nodes.find(node => node.path === edge.target)?.title ?? edge.target;
    this.tooltip.append(textElement("strong", source + " → " + target));
    if (edge.line > 0) {
      this.tooltip.append(uiText("span", "Source line {0}", "map-edge-source", edge.line));
    }
    this.tooltip.append(edge.excerpt ? textElement("span", edge.excerpt) : uiText("span", "Source excerpt unavailable"));
  }
}
