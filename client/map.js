import { createRoot, textElement, actionButton } from "./markdown.js";
import { setButtonText, iconSvg } from "./icons.js";

const svgNamespace = "http://www.w3.org/2000/svg";
function svgElement(name, attributes = {}) {
  const element = document.createElementNS(svgNamespace, name);
  for (const [key, value] of Object.entries(attributes)) element.setAttribute(key, String(value));
  return element;
}
export class MapHost {
  constructor(element, id, emit) {
    Object.assign(this, createRoot(element, "document-map"));
    this.id = id; this.emit = emit; this.scale = 1; this.x = 30; this.y = 30; this.focused = false;
    this.toolbar = document.createElement("header"); this.toolbar.className = "map-toolbar";
    const titles = document.createElement("div");
    titles.append(textElement("h2", "A view of the whole story"), this.summary = textElement("p", "", "map-summary"));
    this.focusButton = actionButton("Focus on this page", () => { this.focused = !this.focused; this.emit({ type: "map-focus", focused: this.focused }); });
    this.toolbar.append(titles, this.focusButton, actionButton("−", () => this.zoom(.85), "Zoom map out"),
      actionButton("+", () => this.zoom(1.18), "Zoom map in"), actionButton("Fit", () => this.fit(), "Fit document map"));
    this.viewport = document.createElement("div"); this.viewport.className = "map-viewport"; this.viewport.tabIndex = 0; this.viewport.setAttribute("aria-label", "Document connections. Drag the background to pan.");
    this.world = document.createElement("div"); this.world.className = "map-world"; this.viewport.append(this.world);
    this.connections = document.createElement("aside"); this.connections.className = "map-connections"; this.connections.setAttribute("aria-label", "Incoming and outgoing document links");
    this.body.append(this.toolbar, this.viewport, this.connections);
    this.viewport.addEventListener("pointerdown", event => {
      if (this.small() || event.target.closest("button") || event.button !== 0) return;
      this.drag = { pointer: event.pointerId, x: event.clientX - this.x, y: event.clientY - this.y };
      this.viewport.setPointerCapture(event.pointerId);
    });
    this.viewport.addEventListener("pointermove", event => {
      if (!this.drag || this.drag.pointer !== event.pointerId) return;
      this.x = event.clientX - this.drag.x; this.y = event.clientY - this.drag.y; this.transform();
    });
    const release = event => { if (this.drag?.pointer === event.pointerId) this.drag = null; };
    this.viewport.addEventListener("pointerup", release); this.viewport.addEventListener("pointercancel", release);
    this.viewport.addEventListener("wheel", event => { if (event.ctrlKey && !this.small()) { event.preventDefault(); this.zoom(event.deltaY > 0 ? .9 : 1.1); } }, { passive: false });
    this.viewport.addEventListener("keydown", event => {
      if (event.target.closest("button")) return;
      const offsets = { ArrowLeft: [50, 0], ArrowRight: [-50, 0], ArrowUp: [0, 50], ArrowDown: [0, -50] };
      if (offsets[event.key]) { event.preventDefault(); this.x += offsets[event.key][0]; this.y += offsets[event.key][1]; this.transform(); }
      if (event.key === "0") this.fit();
    });
    this.resizeObserver = new ResizeObserver(() => this.resize()); this.resizeObserver.observe(this.container);
  }
  small() { return this.container.clientWidth < 650 || matchMedia("(pointer:coarse)").matches; }
  render({ map, selected, focused = false }) {
    this.focused = focused;
    const previousSelected = this.selected; this.selected = selected;
    this.summary.textContent = map.nodes.length + " of " + map.totalDocuments + " pages · " + map.edges.length + " visible connections" + (map.truncated ? " · filter to see more" : "");
    setButtonText(this.focusButton, this.focused ? "Show all pages" : "Focus on this page");
    this.focusButton.setAttribute("aria-pressed", String(this.focused));
    const columns = Math.max(1, Math.min(4, Math.ceil(Math.sqrt(map.nodes.length || 1))));
    this.worldWidth = columns * 288; this.worldHeight = Math.ceil(map.nodes.length / columns) * 194;
    this.world.style.width = this.worldWidth + "px"; this.world.style.height = this.worldHeight + "px";
    const graph = svgElement("svg", { width: this.worldWidth, height: this.worldHeight, "aria-hidden": "true", class: "map-edges" });
    const markerId = "arrow-" + this.id, definitions = svgElement("defs");
    const marker = svgElement("marker", { id: markerId, markerWidth: 8, markerHeight: 8, refX: 7, refY: 3, orient: "auto", markerUnits: "strokeWidth" });
    marker.append(svgElement("path", { d: "M0,0 L7,3 L0,6 Z", fill: "var(--accent)" })); definitions.append(marker); graph.append(definitions);
    const positions = new Map();
    const fragment = document.createDocumentFragment();
    map.nodes.forEach((node, index) => {
      const x = index % columns * 288, y = Math.floor(index / columns) * 194;
      positions.set(node.path, { x, y });
      const card = document.createElement("button"); card.type = "button"; card.className = "page-card" + (node.path === selected ? " selected" : "");
      card.style.left = x + "px"; card.style.top = y + "px";
      card.setAttribute("aria-label", "Open " + node.title + ", " + node.path);
      const badge = iconSvg("file"); badge.classList.add("page-icon");
      card.append(badge, textElement("strong", node.title), textElement("span", node.introduction, "page-introduction"), textElement("code", node.path));
      card.addEventListener("click", () => this.emit({ type: "navigate", path: node.path, anchor: "" }));
      fragment.append(card);
    });
    for (const edge of map.edges) {
      const source = positions.get(edge.source), target = positions.get(edge.target);
      if (!source || !target) continue;
      const sx = source.x + 260, sy = source.y + 85, tx = target.x, ty = target.y + 85;
      const d = edge.source === edge.target
        ? "M" + sx + "," + sy + " C" + (sx + 36) + "," + (sy - 96) + " " + (source.x - 36) + "," + (sy - 96) + " " + tx + "," + ty
        : "M" + sx + "," + sy + " C" + (sx + 70) + "," + sy + " " + (tx - 70) + "," + ty + " " + tx + "," + ty;
      graph.append(svgElement("path", { d, fill: "none", stroke: "var(--accent)", "stroke-width": edge.source === selected || edge.target === selected ? 2 : 1.2,
        opacity: edge.source === selected || edge.target === selected ? .75 : .25, "marker-end": "url(#" + markerId + ")" }));
    }
    this.world.replaceChildren(graph, fragment);
    if (!map.nodes.length) this.world.append(textElement("p", "No pages match this filter. Try a different search.", "map-empty"));
    this.connections.replaceChildren();
    for (const [title, items] of [["Links from this page", map.outgoing.map(link => ({ path: link.path, anchor: link.anchor, title: link.path }))],
      ["Links to this page", map.incoming.map(node => ({ path: node.path, title: node.title, anchor: "" }))]]) {
      const group = document.createElement("section"); group.append(textElement("h3", title));
      if (!items.length) group.append(textElement("p", "No links yet", "map-summary"));
      for (const item of items) group.append(actionButton(item.title, () => this.emit({ type: "navigate", path: item.path, anchor: item.anchor })));
      this.connections.append(group);
    }
    this.resize();
    if (previousSelected !== selected) this.fit(); else this.transform();
  }
  zoom(factor) { this.scale = Math.max(.4, Math.min(1.8, this.scale * factor)); this.transform(); }
  fit() {
    this.scale = Math.max(.4, Math.min(1, (this.viewport.clientWidth - 60) / Math.max(1, this.worldWidth), (this.viewport.clientHeight - 60) / Math.max(1, this.worldHeight)));
    this.x = 30; this.y = 30; this.transform();
  }
  transform() { this.world.style.transform = this.small() ? "none" : "translate(" + this.x + "px," + this.y + "px) scale(" + this.scale + ")"; }
  resize() { this.body.classList.toggle("small-map", this.small()); this.transform(); }
  dispose() { this.drag = null; this.resizeObserver.disconnect(); this.container.remove(); }
}
