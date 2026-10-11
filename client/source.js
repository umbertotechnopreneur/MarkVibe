import { createRoot, textElement, actionButton } from "./markdown.js";
import { setButtonText } from "./icons.js";
import { checkCancelled, nextFrame } from "./path.js";

const modelCache = new Map();
let monacoPromise;
function monaco() { return monacoPromise ??= import("./monaco-adapter.js").then(module => module.monaco); }
export function disposeSourceModels() {
  for (const value of modelCache.values()) value.model.dispose();
  modelCache.clear();
}
export function releaseSourceWorkspace(workspaceId) {
  for (const [key, value] of modelCache) if (value.workspaceId === workspaceId) { value.model.dispose(); modelCache.delete(key); }
}
function remember(key, entry) {
  modelCache.delete(key); modelCache.set(key, entry);
  while (modelCache.size > 3) {
    const [oldest, oldEntry] = modelCache.entries().next().value;
    if (oldEntry.users) { modelCache.delete(oldest); modelCache.set(oldest, oldEntry); break; }
    oldEntry.model.dispose(); modelCache.delete(oldest);
  }
}

export class SourceHost {
  constructor(element, id, emit) {
    Object.assign(this, createRoot(element, "source"));
    this.id = id; this.emit = emit; this.revision = 0;
    this.toolbar = document.createElement("header"); this.toolbar.className = "source-toolbar";
    this.title = textElement("span", "Markdown source", "eyebrow");
    this.findInput = document.createElement("input"); this.findInput.type = "search"; this.findInput.placeholder = "Find in source";
    this.findInput.setAttribute("aria-label", "Find in current source");
    this.findInput.addEventListener("input", () => this.find(this.findInput.value));
    this.toggle = actionButton("Simple view", () => this.emit({ type: "source-preference", simple: !this.simple }));
    this.copy = actionButton("Copy", async () => {
      try { await navigator.clipboard.writeText(this.source || ""); this.emit({ type: "status", message: "Source copied." }); }
      catch { this.emit({ type: "status", message: "Select the source text and copy it with your keyboard or touch menu." }); }
    });
    this.toolbar.append(this.title, this.findInput, this.toggle, this.copy);
    this.content = document.createElement("div"); this.content.className = "source-content";
    this.content.setAttribute("aria-label", "Read-only Markdown source");
    this.body.append(this.toolbar, this.content);
    this.resizeObserver = new ResizeObserver(() => this.resize()); this.resizeObserver.observe(this.container);
  }
  saveEditor() {
    if (this.editor && this.key && modelCache.has(this.key)) {
      const entry = modelCache.get(this.key); entry.state = this.editor.saveViewState(); entry.users = 0;
    }
    this.editor?.dispose(); this.editor = null; this.scrollSubscription?.dispose(); this.scrollSubscription = null;
  }
  async render({ workspaceId, path, source, preferences }, signal) {
    const revision = ++this.revision;
    // A newly visible OpenSilver presenter can still have a zero width until layout.
    // That transient state must not force the mobile fallback on a desktop.
    for (let frame = 0; frame < 32 && this.container.clientWidth === 0; frame++) {
      await nextFrame(); checkCancelled(signal);
      if (revision !== this.revision || !this.container.isConnected) return;
    }
    if (!this.container.clientWidth) return;
    const simple = preferences.simpleSource || matchMedia("(pointer:coarse)").matches || this.container.clientWidth < 450 || (navigator.deviceMemory && navigator.deviceMemory < 4);
    const key = workspaceId + "/" + path;
    // A matching document is reusable only after its asynchronous renderer is ready.
    if (this.key === key && this.source === source && this.simple === !!simple && (this.editor || this.pane?.isConnected)) { this.editor?.layout(); return; }
    const position = this.capture();
    const same = this.key === key;
    this.saveEditor();
    this.key = key; this.workspaceId = workspaceId; this.source = source; this.path = path; this.simple = !!simple;
    this.title.textContent = path;
    setButtonText(this.toggle, simple ? "Editor view" : "Simple view");
    this.toggle.setAttribute("aria-label", this.toggle.textContent);
    this.findInput.value = "";
    this.content.replaceChildren(textElement("p", "Preparing your source…", "source-loading"));
    const dark = document.documentElement.dataset.theme === "dark";
    if (!simple) {
      try {
        const api = await monaco();
        checkCancelled(signal);
        if (revision !== this.revision) return;
        let entry = modelCache.get(key);
        if (!entry || entry.model.getValue() !== source) {
          entry?.model.dispose();
          entry = { workspaceId, model: api.editor.createModel(source, "markdown", api.Uri.parse("markvibe://" + encodeURIComponent(workspaceId) + "/" + path.split("/").map(encodeURIComponent).join("/"))), users: 0 };
        }
        entry.users = 1; remember(key, entry);
        this.content.replaceChildren();
        this.editor = api.editor.create(this.content, { model: entry.model, readOnly: true, domReadOnly: true, wordWrap: "on",
          lineNumbers: "on", minimap: { enabled: false }, fontSize: 13, lineHeight: 22, scrollBeyondLastLine: false,
          padding: { top: 20, bottom: 20 }, theme: dark ? "vs-dark" : "vs", automaticLayout: false,
          ariaLabel: "Read-only Markdown source", renderLineHighlight: "none" });
        if (entry.state) this.editor.restoreViewState(entry.state);
        this.scrollSubscription = this.editor.onDidScrollChange(() => this.scrollListener?.());
        this.editor.layout();
        if (same) this.restore(position);
        return;
      } catch (error) {
        if (signal?.aborted || revision !== this.revision) return;
        this.simple = true; setButtonText(this.toggle, "Editor view");
        this.toggle.setAttribute("aria-label", this.toggle.textContent);
        this.emit({ type: "status", message: "The lightweight source view is ready. Select and copy text as usual." });
      }
    }
    if (revision !== this.revision) return;
    this.renderPlain(source);
    await nextFrame(); if (same) this.restore(position);
  }
  renderPlain(source) {
    const lines = source.split("\n");
    const pane = document.createElement("div"); pane.className = "plain-source"; pane.tabIndex = 0;
    pane.setAttribute("role", "textbox"); pane.setAttribute("aria-readonly", "true"); pane.setAttribute("aria-multiline", "true"); pane.setAttribute("aria-label", "Read-only Markdown source");
    if (lines.length > 12000) {
      const textarea = document.createElement("textarea"); textarea.readOnly = true; textarea.wrap = "off";
      textarea.className = "plain-textarea"; textarea.value = source; textarea.setAttribute("aria-label", "Read-only Markdown source");
      pane.append(textarea); this.textarea = textarea; this.lineElements = null;
      textarea.addEventListener("scroll", () => this.scrollListener?.(), { passive: true });
    } else {
      this.textarea = null;
      const fragment = document.createDocumentFragment();
      lines.forEach((line, index) => {
        const row = document.createElement("div"); row.className = "source-line"; row.dataset.sourceLine = String(index + 1);
        const number = textElement("span", String(index + 1), "line-number"); number.setAttribute("aria-hidden", "true");
        row.append(number, textElement("span", line || "\u200b", "line-text")); fragment.append(row);
      });
      pane.append(fragment); this.lineElements = [...pane.children];
      pane.addEventListener("scroll", () => this.scrollListener?.(), { passive: true });
    }
    this.pane = pane; this.content.replaceChildren(pane);
  }
  capture() {
    if (this.editor) {
      const ranges = this.editor.getVisibleRanges();
      const line = ranges[0]?.startLineNumber || 1;
      const top = this.editor.getTopForLineNumber(line), next = this.editor.getTopForLineNumber(Math.min(line + 1, this.editor.getModel().getLineCount()));
      return { blockId: "", fraction: 0, sourceLine: line + Math.max(0, this.editor.getScrollTop() - top) / Math.max(1, next - top) };
    }
    if (this.textarea) return { blockId: "", fraction: 0, sourceLine: 1 + this.textarea.scrollTop / 22 };
    if (!this.lineElements?.length) return { blockId: "", fraction: 0, sourceLine: 1 };
    const top = this.pane.scrollTop;
    let low = 0, high = this.lineElements.length - 1;
    while (low < high) { const middle = Math.ceil((low + high) / 2); if (this.lineElements[middle].offsetTop - this.lineElements[0].offsetTop <= top) low = middle; else high = middle - 1; }
    const row = this.lineElements[low], start = row.offsetTop - this.lineElements[0].offsetTop;
    return { blockId: "", fraction: 0, sourceLine: low + 1 + Math.max(0, top - start) / Math.max(1, row.offsetHeight) };
  }
  restore(position) { this.scrollToLine(position.sourceLine || 1); }
  scrollToLine(value) {
    const line = Math.max(1, Math.floor(value)), fraction = Math.max(0, value - line);
    if (this.editor) {
      const count = this.editor.getModel().getLineCount(), target = Math.min(line, count);
      const top = this.editor.getTopForLineNumber(target), next = this.editor.getTopForLineNumber(Math.min(target + 1, count));
      this.editor.setScrollTop(top + Math.max(1, next - top) * fraction);
    } else if (this.textarea) this.textarea.scrollTop = (line - 1 + fraction) * 22;
    else if (this.lineElements?.length) {
      const row = this.lineElements[Math.min(line - 1, this.lineElements.length - 1)];
      this.pane.scrollTop = row.offsetTop - this.lineElements[0].offsetTop + row.offsetHeight * fraction;
    }
  }
  find(query) {
    if (!query) return;
    if (this.editor) {
      const match = this.editor.getModel().findMatches(query, false, false, false, null, false, 1)[0];
      if (!match) return this.emit({ type: "status", message: "No source matches found." });
      this.editor.setSelection(match.range); this.editor.revealRangeInCenter(match.range);
      return;
    }
    const start = (this.source || "").toLowerCase().indexOf(query.toLowerCase());
    if (start < 0) return this.emit({ type: "status", message: "No source matches found." });
    const line = this.source.slice(0, start).split("\n").length; this.scrollToLine(line);
    const row = this.lineElements?.[line - 1];
    this.root.querySelector(".source-match")?.classList.remove("source-match");
    row?.classList.add("source-match");
    if (this.textarea) { this.textarea.focus(); this.textarea.setSelectionRange(start, start + query.length); }
  }
  async theme() { if (this.editor) (await monaco()).editor.setTheme(document.documentElement.dataset.theme === "dark" ? "vs-dark" : "vs"); }
  resize() { this.editor?.layout(); }
  dispose() { this.revision++; this.saveEditor(); this.resizeObserver.disconnect(); this.scrollListener = null; this.container.remove(); }
}
