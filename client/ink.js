import { setButtonText } from "./icons.js";
const ns = "http://www.w3.org/2000/svg";
const svgNode = (name, attributes = {}) => {
  const node = document.createElementNS(ns, name);
  for (const [key, value] of Object.entries(attributes)) node.setAttribute(key, String(value));
  return node;
};

export function inkSvg(annotation) {
  const canvas = annotation.canvas;
  const svg = svgNode("svg", { viewBox: `0 0 ${canvas.width} ${canvas.height}`, role: "img", "aria-label": "Handwritten annotation", preserveAspectRatio: "xMidYMid meet" });
  svg.style.cssText = "display:block;width:100%;height:auto;background:#fff;max-width:100%;";
  annotation.strokes.forEach((stroke, index) => {
    const d = stroke.points.map((point, i) => `${i ? "L" : "M"}${point.x.toFixed(2)},${point.y.toFixed(2)}`).join(" ");
    const pressure = stroke.points.reduce((sum, point) => sum + point.pressure, 0) / stroke.points.length;
    const path = svgNode("path", { d, fill: "none", stroke: stroke.color, "stroke-width": stroke.width * (stroke.tool === "pen" ? 0.5 + pressure : 1), "stroke-linecap": "round", "stroke-linejoin": "round", opacity: stroke.tool === "highlighter" ? 0.35 : 1 });
    path.dataset.stroke = String(index); svg.append(path);
  });
  return svg;
}

export function appendExportInk(article, resolved) {
  for (const item of resolved ?? []) {
    const block = [...article.querySelectorAll("section[data-block-id]")].find(node => node.dataset.blockId === item.blockId);
    if (!block) continue;
    const figure = article.ownerDocument.createElement("figure"); figure.className = "ink-annotation";
    figure.style.cssText = "margin:12px 0;break-inside:avoid;border:1px solid #dde2ea;background:#fff;";
    figure.append(inkSvg(item.annotation)); block.append(figure);
  }
}

export class InkView {
  constructor(host) {
    this.host = host; this.enabled = false; this.tool = "pen"; this.color = "#2458c7"; this.width = 4;
    this.abort = new AbortController(); this.bands = []; this.active = null; this.selectedBlock = null; this.pendingStrokes = 0;
    this.toolbar = document.createElement("div"); this.toolbar.className = "ink-toolbar";
    this.toolbar.setAttribute("role", "toolbar"); this.toolbar.setAttribute("aria-label", "Document annotations");
    const button = (label, action) => {
      const node = document.createElement("button"); node.type = "button"; setButtonText(node, label);
      node.addEventListener("click", action, { signal: this.abort.signal }); this.toolbar.append(node); return node;
    };
    this.toggle = button("Annotate", () => { this.enabled = !this.enabled; this.render(); });
    this.pen = button("Pen", () => { this.tool = "pen"; this.enabled = true; this.render(); });
    this.highlighter = button("Highlight", () => { this.tool = "highlighter"; this.enabled = true; this.render(); });
    this.eraser = button("Erase", () => { this.tool = "eraser"; this.enabled = true; this.render(); });
    this.colorInput = document.createElement("input"); this.colorInput.type = "color"; this.colorInput.value = this.color;
    this.colorInput.setAttribute("aria-label", "Ink color"); this.colorInput.addEventListener("input", () => { this.color = this.colorInput.value; }, { signal: this.abort.signal }); this.toolbar.append(this.colorInput);
    this.widthInput = document.createElement("select"); this.widthInput.setAttribute("aria-label", "Ink width");
    for (const [label, width] of [["Fine", 2], ["Medium", 4], ["Broad", 8]]) { const option = document.createElement("option"); option.textContent = label; option.value = String(width); this.widthInput.append(option); }
    this.widthInput.value = "4"; this.widthInput.addEventListener("change", () => { this.width = Number(this.widthInput.value); }, { signal: this.abort.signal }); this.toolbar.append(this.widthInput);
    this.undo = button("Undo", () => this.action("undo")); this.redo = button("Redo", () => this.action("redo"));
    this.save = button("Save ink", () => this.action("save"));
    this.backup = button("Export ink", () => this.action("download"));
    this.discard = button("Discard edits", () => { if (confirm("Discard unsaved ink edits and reload the annotation file from disk?")) this.action("discard"); });
    this.review = button("Use current document", () => this.action("rebase"));
    this.status = document.createElement("span"); this.status.className = "ink-status"; this.status.setAttribute("role", "status"); this.toolbar.append(this.status);
    host.body.insertBefore(this.toolbar, host.article);
  }
  action(action, values = {}) {
    if (this.active || this.state?.busy && !(action === "stroke" && this.pendingStrokes > 0)) return;
    if (action === "stroke") this.pendingStrokes++;
    if (["stroke", "erase", "undo", "redo", "rebase"].includes(action)) {
      this.state.busy = true; this.state.dirty = true; this.status.textContent = "Updating ink…";
      for (const control of this.toolbar.querySelectorAll("button,input,select")) control.disabled = true;
    }
    const operation = this.host.emit({ type: "ink", action, path: this.host.model.path, workspaceId: this.host.workspaceId, ...values });
    if (action === "stroke") Promise.resolve(operation).finally(() => { this.pendingStrokes--; if (this.pendingStrokes === 0) this.render(); });
  }
  update(state) { this.state = state; this.render(); }
  render() {
    if (!this.state || this.active || this.pendingStrokes > 0 || this.disposed) return;
    for (const band of this.bands) { band._controller.abort(); band.remove(); } this.bands = [];
    const state = this.state;
    this.toggle.setAttribute("aria-pressed", String(this.enabled));
    this.pen.setAttribute("aria-pressed", String(this.tool === "pen"));
    this.highlighter.setAttribute("aria-pressed", String(this.tool === "highlighter"));
    this.eraser.setAttribute("aria-pressed", String(this.tool === "eraser"));
    this.undo.disabled = state.busy || !state.canUndo; this.redo.disabled = state.busy || !state.canRedo;
    this.save.disabled = state.busy || state.readOnly || !state.dirty; this.backup.disabled = state.busy || !state.hasInk;
    this.discard.hidden = !state.dirty; this.discard.disabled = state.busy;
    this.review.hidden = !state.changed || state.unresolved > 0 || state.invalid;
    this.review.disabled = state.busy;
    for (const control of [this.toggle, this.pen, this.highlighter, this.eraser, this.colorInput, this.widthInput]) control.disabled = state.busy || state.readOnly;
    this.status.textContent = state.message || (state.busy ? "Saving ink…" : state.dirty ? "Unsaved ink" : "Ink saved");
    const byBlock = new Map();
    for (const item of state.resolved ?? []) {
      const list = byBlock.get(item.blockId) || []; list.push(item.annotation); byBlock.set(item.blockId, list);
    }
    for (const block of this.host.blocks) {
      const annotations = byBlock.get(block.dataset.blockId) || [];
      if (!annotations.length && (!this.enabled || state.readOnly)) continue;
      if (!annotations.length && this.selectedBlock !== block.dataset.blockId) {
        const add = document.createElement("button"); add.type = "button"; add.className = "ink-add";
        setButtonText(add, "Handwritten note", "Add handwritten note for this paragraph");
        const controller = new AbortController(); add._controller = controller;
        add.addEventListener("click", () => { this.selectedBlock = block.dataset.blockId; this.render(); }, { signal: controller.signal });
        block.append(add); this.bands.push(add); continue;
      }
      if (!annotations.length) annotations.push({ id: "", canvas: { width: 1000, height: 240 }, strokes: [] });
      for (const annotation of annotations) {
        const band = document.createElement("div"); band.className = "ink-band";
        band.dataset.inkBlock = block.dataset.blockId;
        const label = document.createElement("span"); label.className = "ink-label"; label.textContent = annotation.strokes.length ? "Handwritten note" : "Write a note for this paragraph";
        const svg = inkSvg(annotation);
        svg.style.touchAction = this.enabled && !state.readOnly ? "none" : "pan-y";
        svg.style.cursor = this.enabled && !state.readOnly ? this.tool === "eraser" ? "crosshair" : "pen" : "default";
        const controller = new AbortController(); band._controller = controller;
        svg.addEventListener("pointerdown", event => this.start(event, svg, annotation, block.dataset.blockId), { signal: controller.signal });
        svg.addEventListener("pointermove", event => this.move(event), { signal: controller.signal });
        svg.addEventListener("pointerup", event => this.finish(event), { signal: controller.signal });
        svg.addEventListener("pointercancel", event => this.finish(event, true), { signal: controller.signal });
        svg.addEventListener("lostpointercapture", event => { if (this.active?.id === event.pointerId) this.finish(event, true); }, { signal: controller.signal });
        band.append(label, svg); block.append(band); this.bands.push(band);
      }
    }
  }
  start(event, svg, annotation, blockId) {
    if (!this.enabled || this.state?.readOnly || this.state?.busy && this.pendingStrokes === 0 || this.active || event.pointerType === "touch") return;
    if (this.pendingStrokes >= 16) { this.status.textContent = "Ink is still saving. Wait before adding another stroke."; return; }
    if (this.tool === "eraser" || event.button === 5 || event.buttons === 32) {
      const path = event.target.closest("path[data-stroke]");
      if (path && annotation.id) this.action("erase", { annotationId: annotation.id, strokeIndex: Number(path.dataset.stroke) });
      event.preventDefault(); return;
    }
    if (event.button !== 0) return;
    event.preventDefault(); svg.setPointerCapture(event.pointerId);
    const path = svgNode("path", { fill: "none", stroke: this.color, "stroke-width": this.width * (this.tool === "highlighter" ? 4 : 1), "stroke-linecap": "round", "stroke-linejoin": "round", opacity: this.tool === "highlighter" ? 0.35 : 1 }); svg.append(path);
    this.active = { id: event.pointerId, svg, path, blockId, canvas: annotation.canvas, start: event.timeStamp, points: [], color: this.color, width: this.width * (this.tool === "highlighter" ? 4 : 1), tool: this.tool };
    this.sample(event);
  }
  sample(event) {
    const active = this.active, rect = active.svg.getBoundingClientRect();
    if (active.points.length >= 4000 || rect.width === 0 || rect.height === 0) return;
    const point = { x: Math.min(active.canvas.width, Math.max(0, (event.clientX - rect.left) / rect.width * active.canvas.width)), y: Math.min(active.canvas.height, Math.max(0, (event.clientY - rect.top) / rect.height * active.canvas.height)), pressure: event.pointerType === "pen" ? Math.min(1, Math.max(0, event.pressure)) : 0.5, elapsedMs: Math.min(3600000, Math.max(active.points.at(-1)?.elapsedMs || 0, Math.round(event.timeStamp - active.start))) };
    active.points.push(point); active.path.setAttribute("d", active.points.map((p, i) => `${i ? "L" : "M"}${p.x},${p.y}`).join(" "));
  }
  move(event) {
    if (this.active?.id !== event.pointerId) return;
    event.preventDefault();
    const samples = event.getCoalescedEvents?.();
    for (const sample of samples?.length ? samples : [event]) this.sample(sample);
  }
  finish(event, cancelled = false) {
    if (this.active?.id !== event.pointerId) return;
    if (!cancelled) this.sample(event);
    const active = this.active; this.active = null;
    if (active.svg.hasPointerCapture(event.pointerId)) active.svg.releasePointerCapture(event.pointerId);
    if (cancelled) { active.path.remove(); this.render(); return; }
    if (active.points.length === 1) active.points.push({ ...active.points[0], x: Math.min(active.canvas.width, active.points[0].x + 0.1) });
    this.action("stroke", { blockId: active.blockId, stroke: { color: active.color, width: active.width, tool: active.tool, points: active.points } });
  }
  dispose() {
    this.disposed = true;
    this.abort.abort(); for (const band of this.bands) { band._controller.abort(); band.remove(); }
    this.active = null; this.toolbar.remove();
  }
}
