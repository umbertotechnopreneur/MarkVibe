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
 * Modified with AI: OpenAI Codex; source fingerprints and selection notifications; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { createRoot, textElement, uiText } from "./markdown.js";
import { t, localize } from "./i18n.js";
import { checkCancelled, nextFrame } from "./path.js";
import { attachFileContext } from "./file-context.js";

const modelCache = new Map();
let monacoPromise;

/** Lazily load and retain the local Monaco adapter for source view hosts. */
function monaco() {
  return monacoPromise ??= import("./monaco-adapter.js").then(module => module.monaco);
}

/** Dispose every retained Monaco model and clear the source model cache. */
export function disposeSourceModels() {
  for (const value of modelCache.values()) {
    value.model.dispose();
  }

  modelCache.clear();
}

/** Dispose cached source models associated with a released workspace. */
export function releaseSourceWorkspace(workspaceId) {
  for (const [modelCacheKey, value] of modelCache) {
    if (value.workspaceId === workspaceId) {
      value.model.dispose();
      modelCache.delete(modelCacheKey);
    }
  }
}

/** Retain a source model and evict older cached entries above the established limit. */
function remember(key, entry) {
  modelCache.delete(key);
  modelCache.set(key, entry);

  while (modelCache.size > 3) {
    const [oldest, oldEntry] = modelCache.entries().next().value;
    // Keep a model still used by a visible host, even when the cache is over its limit.
    if (oldEntry.users) {
      modelCache.delete(oldest);
      modelCache.set(oldest, oldEntry);
      break;
    }
    oldEntry.model.dispose();
    modelCache.delete(oldest);
  }
}

/** Own the read-only source view, cached editor state and DOM subscriptions. */
export class SourceHost {

  /** Mount source controls and register editor, fallback and resize lifecycle handlers. */
  constructor(element, id, emit) {
    Object.assign(this, createRoot(element, "source"));
    this.id = id;
    this.emit = emit;
    this.revision = 0;

    this.content = document.createElement("div");
    this.content.className = "source-content";
    localize(this.content, "Read-only Markdown source", "aria-label");
    this.body.append(this.content);

    /** Resolve the current document when its context gesture occurs, after any navigation. */
    this.disposeFileContext = attachFileContext(this.body, emit, () => ({
      workspaceId: this.workspaceId,
      path: this.path,
      title: this.path?.split("/").at(-1)
    }));

    this.resizeObserver = new ResizeObserver(() => this.resize());
    this.resizeObserver.observe(this.container);
  }

  /** Retain the current editor view state before disposing or replacing its active model. */
  saveEditor() {
    if (this.editor && this.key && modelCache.has(this.key)) {
      const entry = modelCache.get(this.key);
      entry.state = this.editor.saveViewState();
      entry.users = 0;
    }

    this.editor?.dispose();
    this.editor = null;
    this.scrollSubscription?.dispose();
    this.scrollSubscription = null;
    this.selectionSubscription?.dispose();
    this.selectionSubscription = null;
  }

  /** Display read-only source with cached Monaco state or a usable plain-text fallback. */
  async render({ workspaceId, path, source, fingerprint, preferences }, signal) {
    const revision = ++this.revision;

    // A newly visible OpenSilver presenter can still have a zero width until layout.
    // That transient state must not force the mobile fallback on a desktop.
    for (let frame = 0; frame < 32 && this.container.clientWidth === 0; frame++) {
      await nextFrame();
      checkCancelled(signal);
      if (revision !== this.revision || !this.container.isConnected) {
        return;
      }
    }

    if (!this.container.clientWidth) {
      return;
    }

    const simple = preferences.simpleSource ||
      matchMedia("(pointer:coarse)").matches ||
      this.container.clientWidth < 450 ||
      (navigator.deviceMemory && navigator.deviceMemory < 4);
    const modelCacheKey = workspaceId + "/" + path;

    // A matching document is reusable only after its asynchronous renderer is ready.
    if (this.key === modelCacheKey &&
      this.source === source &&
      this.simple === !!simple &&
      (this.editor || this.pane?.isConnected)) {
      this.editor?.layout();
      return;
    }

    const position = this.capture();
    const same = this.key === modelCacheKey;

    this.saveEditor();
    this.key = modelCacheKey;
    this.workspaceId = workspaceId;
    this.source = source;
    this.fingerprint = fingerprint;
    this.path = path;
    this.simple = !!simple;
    this.content.replaceChildren(uiText("p", "Preparing your source…", "source-loading"));

    const dark = document.documentElement.dataset.theme === "dark";
    if (!simple) {
      try {
        const api = await monaco();
        checkCancelled(signal);
        if (revision !== this.revision) {
          return;
        }

        let entry = modelCache.get(modelCacheKey);
        if (!entry || entry.model.getValue() !== source) {
          entry?.model.dispose();
          entry = {
            workspaceId,
            model: api.editor.createModel(
              source,
              "markdown",
              api.Uri.parse(
                "markvibe://" + encodeURIComponent(workspaceId) + "/" +
                path.split("/").map(encodeURIComponent).join("/")
              )
            ),
            users: 0
          };
        }

        entry.users = 1;
        remember(modelCacheKey, entry);
        this.content.replaceChildren();
        this.editor = api.editor.create(this.content, {
          model: entry.model,
          readOnly: true,
          domReadOnly: true,
          wordWrap: "on",
          lineNumbers: "on",
          minimap: { enabled: false },
          fontSize: 13,
          lineHeight: 22,
          scrollBeyondLastLine: false,
          padding: { top: 20, bottom: 20 },
          theme: dark ? "vs-dark" : "vs",
          automaticLayout: false,
          ariaLabel: t("Read-only Markdown source"),
          contextmenu: false,
          renderLineHighlight: "none"
        });

        if (entry.state) {
          this.editor.restoreViewState(entry.state);
        }

        this.scrollSubscription = this.editor.onDidScrollChange(() => this.scrollListener?.());
        this.selectionSubscription = this.editor.onDidChangeCursorSelection(() => this.selectionChanged?.());
        this.editor.layout();
        if (same) {
          this.restore(position);
        }
        return;
      } catch (error) {
        if (signal?.aborted || revision !== this.revision) {
          return;
        }

        this.simple = true;
        this.emit({
          type: "status",
          message: "The lightweight source view is ready. Select and copy text as usual."
        });
      }
    }

    if (revision !== this.revision) {
      return;
    }

    this.renderPlain(source);
    await nextFrame();
    if (same) {
      this.restore(position);
    }
  }

  /** Render the read-only plain-text source fallback and retain its line navigation behavior. */
  renderPlain(source) {
    const lines = source.split("\n");
    const pane = document.createElement("div");
    pane.className = "plain-source";
    pane.tabIndex = 0;
    pane.setAttribute("role", "textbox");
    pane.setAttribute("aria-readonly", "true");
    pane.setAttribute("aria-multiline", "true");
    localize(pane, "Read-only Markdown source", "aria-label");

    if (lines.length > 12000) {
      const textarea = document.createElement("textarea");
      textarea.readOnly = true;
      textarea.wrap = "off";
      textarea.className = "plain-textarea";
      textarea.value = source;
      localize(textarea, "Read-only Markdown source", "aria-label");
      pane.append(textarea);
      this.textarea = textarea;
      this.lineElements = null;
      textarea.addEventListener("scroll", () => this.scrollListener?.(), { passive: true });
    } else {
      this.textarea = null;
      const fragment = document.createDocumentFragment();

      /** Keep each source line separately addressable for navigation and selection. */
      lines.forEach((line, index) => {
        const row = document.createElement("div");
        row.className = "source-line";
        row.dataset.sourceLine = String(index + 1);
        const number = textElement("span", String(index + 1), "line-number");
        number.setAttribute("aria-hidden", "true");
        // Empty lines still need a measurable row in the plain-text view.
        row.append(number, textElement("span", line || "\u200b", "line-text"));
        fragment.append(row);
      });

      pane.append(fragment);
      this.lineElements = [...pane.children];
      pane.addEventListener("scroll", () => this.scrollListener?.(), { passive: true });
    }

    this.pane = pane;
    this.content.replaceChildren(pane);
  }

  /** Capture the source view's current source-line reading position. */
  capture() {
    if (this.editor) {
      const ranges = this.editor.getVisibleRanges();
      const line = ranges[0]?.startLineNumber || 1;
      const top = this.editor.getTopForLineNumber(line);
      const next = this.editor.getTopForLineNumber(
        Math.min(line + 1, this.editor.getModel().getLineCount())
      );
      return {
        blockId: "",
        fraction: 0,
        sourceLine: line + Math.max(0, this.editor.getScrollTop() - top) / Math.max(1, next - top)
      };
    }

    if (this.textarea) {
      return {
        blockId: "",
        fraction: 0,
        sourceLine: 1 + this.textarea.scrollTop / 22
      };
    }

    if (!this.lineElements?.length) {
      return {
        blockId: "",
        fraction: 0,
        sourceLine: 1
      };
    }

    const top = this.pane.scrollTop;
    let low = 0;
    let high = this.lineElements.length - 1;

    // Find the last row starting above the viewport, then retain progress within it.
    while (low < high) {
      const middle = Math.ceil((low + high) / 2);
      if (this.lineElements[middle].offsetTop - this.lineElements[0].offsetTop <= top) {
        low = middle;
      } else {
        high = middle - 1;
      }
    }

    const row = this.lineElements[low];
    const start = row.offsetTop - this.lineElements[0].offsetTop;
    return {
      blockId: "",
      fraction: 0,
      sourceLine: low + 1 + Math.max(0, top - start) / Math.max(1, row.offsetHeight)
    };
  }

  /** Restore a retained reading position through its source line. */
  restore(position) {
    this.scrollToLine(position.sourceLine || 1);
  }

  /** Scroll the Monaco editor or plain-text fallback to the requested source line. */
  scrollToLine(value) {
    const line = Math.max(1, Math.floor(value));
    const fraction = Math.max(0, value - line);

    if (this.editor) {
      const count = this.editor.getModel().getLineCount();
      const target = Math.min(line, count);
      const top = this.editor.getTopForLineNumber(target);
      const next = this.editor.getTopForLineNumber(Math.min(target + 1, count));
      this.editor.setScrollTop(top + Math.max(1, next - top) * fraction);
    } else if (this.textarea) {
      this.textarea.scrollTop = (line - 1 + fraction) * 22;
    } else if (this.lineElements?.length) {
      const row = this.lineElements[Math.min(line - 1, this.lineElements.length - 1)];
      this.pane.scrollTop = row.offsetTop - this.lineElements[0].offsetTop + row.offsetHeight * fraction;
    }
  }

  /** Find the requested text using the active editor or the established fallback search. */
  find(query) {
    if (!query) {
      return;
    }

    if (this.editor) {
      const match = this.editor.getModel()
        .findMatches(query, false, false, false, null, false, 1)[0];
      if (!match) {
        return this.emit({ type: "status", message: "No source matches found." });
      }
      this.editor.setSelection(match.range);
      this.editor.revealRangeInCenter(match.range);
      return;
    }

    const start = (this.source || "").toLowerCase().indexOf(query.toLowerCase());
    if (start < 0) {
      return this.emit({ type: "status", message: "No source matches found." });
    }

    const line = this.source.slice(0, start).split("\n").length;
    this.scrollToLine(line);
    const row = this.lineElements?.[line - 1];
    this.root.querySelector(".source-match")?.classList.remove("source-match");
    row?.classList.add("source-match");
    if (this.textarea) {
      this.textarea.focus();
      this.textarea.setSelectionRange(start, start + query.length);
    }
  }

  /** Refresh Monaco's light or dark theme and translated accessible editor label. */
  async theme() {
    if (this.editor) {
      (await monaco()).editor.setTheme(
        document.documentElement.dataset.theme === "dark" ? "vs-dark" : "vs"
      );
      this.editor.updateOptions({ ariaLabel: t("Read-only Markdown source") });
    }
  }

  /** Recompute the active editor layout after its host dimensions change. */
  resize() {
    this.editor?.layout();
  }

  /** Invalidate pending renders and release editor state, observers and DOM resources. */
  dispose() {
    this.revision++;
    this.disposeFileContext();
    this.saveEditor();
    this.resizeObserver.disconnect();
    this.scrollListener = null;
    this.disposeRoot();
    this.container.remove();
  }
}
