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
 * Modified with AI: OpenAI Codex; sanitized exact source selection attributes; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import DOMPurify from "dompurify";
import hljs from "highlight.js/lib/common";
import { acquireAsset, releaseAssets, getWorkspace, addFiles } from "./workspaces.js";
import { resourceUrl, safeExternal, checkCancelled, nextFrame } from "./path.js";
import { t, localize, refreshTranslations } from "./i18n.js";
import { applyReaderTheme, resolveReaderTheme } from "./themes.js";
import { attachFileContext } from "./file-context.js";

const allowedTags = ["p", "h1", "h2", "h3", "h4", "h5", "h6", "a", "em", "strong", "del", "s", "sup", "sub", "mark",
  "ul", "ol", "li", "blockquote", "pre", "code", "table", "thead", "tbody", "tr", "th", "td", "hr", "br", "img", "input", "section", "div", "span",
  "details", "summary", "kbd", "ins", "small", "b", "i"];
const allowedAttributes = ["id", "class", "alt", "title", "href", "src", "start", "colspan", "rowspan", "type", "checked", "disabled",
  "data-block-id", "data-element-id", "data-source-start", "data-source-end", "data-md-start", "data-md-end", "data-mv-asset", "data-mv-path", "data-mv-anchor", "data-mv-external", "data-mv-blocked", "data-mv-attachment"];
/** Create a view root and its existing shared lifecycle cleanup boundary. */
export function createRoot(element, kind) {
  const container = document.createElement("div");
  container.className = "mv-dom-host";
  container.style.cssText = "position:absolute;inset:0;overflow:hidden;";
  container.dataset.theme = document.documentElement.dataset.theme || "light";
  container.dataset.documentStyle = document.documentElement.dataset.documentStyle || "technical";
  container.dataset.acrylic = document.documentElement.dataset.acrylic || "false";
  element.append(container);
  const root = container.attachShadow({ mode: "open" });
  const stylesheet = document.createElement("link");
  stylesheet.rel = "stylesheet";
  stylesheet.href = resourceUrl("css/content.css");
  root.append(stylesheet);
  if (kind === "source") {
    const editorStyles = document.createElement("link");
    editorStyles.rel = "stylesheet";
    editorStyles.href = resourceUrl("js/markvibe.css");
    root.append(editorStyles);
  }
  const body = document.createElement("div");
  body.className = "mv-view " + kind;
  const layout = body;
  root.append(layout);
  const localeController = new AbortController();
  window.addEventListener("markvibe-language-changed", () => refreshTranslations(root), { signal: localeController.signal });
  return { root, body, layout, container, disposeRoot: () => localeController.abort() };
}
/** Create an element whose caller-provided content is assigned as text rather than HTML. */
export function textElement(tag, text, className = "") {
  const element = document.createElement(tag);
  element.textContent = text;
  element.className = className;
  return element;
}
/** Create a UI text element and retain its translation key for language refreshes. */
export function uiText(tag, key, className = "", ...values) {
  return localize(textElement(tag, "", className), key, "textContent", ...values);
}
/** Sanitize managed-rendered Markdown and retain only the supported local content structure. */
export function prepareMarkdown(model) {
  // model: the shared C# Markdig model. Reader and export use the same DOM boundary.
  const fragment = DOMPurify.sanitize(model.html, {
    ALLOWED_TAGS: allowedTags,
    ALLOWED_ATTR: allowedAttributes,
    ALLOW_DATA_ATTR: false,
    RETURN_DOM_FRAGMENT: true
  });
  for (const image of fragment.querySelectorAll("img")) {
    image.removeAttribute("src");
    image.removeAttribute("srcset");
    image.loading = "lazy";
    image.decoding = "async";
  }
  for (const input of fragment.querySelectorAll("input")) {
    input.type = "checkbox";
    input.disabled = true;
    input.removeAttribute("name");
  }
  for (const element of fragment.querySelectorAll("[id]")) {
    element.removeAttribute("id");
  }
  [...fragment.querySelectorAll("h1,h2,h3,h4,h5,h6")].forEach((heading, index) => {
    if (model.headings[index]) {
      heading.id = model.headings[index].id;
    }
  });
  for (const link of fragment.querySelectorAll("a")) {
    const external = safeExternal(link.dataset.mvExternal || link.getAttribute("href") || "");
    link.removeAttribute("href");
    if (link.dataset.mvPath) {
      link.href = "#doc=" + encodeURIComponent(link.dataset.mvPath) + "&anchor=" + encodeURIComponent(link.dataset.mvAnchor || "");
    }
    else if (external) {
      link.href = external;
      link.target = "_blank";
      link.rel = "noopener noreferrer";
      link.dataset.mvExternal = external;
    }
    else {
      link.setAttribute("aria-disabled", "true");
      link.tabIndex = 0;
    }
  }
  return fragment;
}

/** Highlight bounded supported code blocks without executing document content. */
export function highlightCode(article) {
  for (const code of article.querySelectorAll("pre code:not(.language-mermaid)")) {
    if (code.textContent.length > 60000) {
      continue;
    }
    try {
      hljs.highlightElement(code);
    } catch {
    }
  }
}

/** Open containing disclosures before measuring a source or outline navigation target. */
function revealDetails(element) {
  let disclosure = element.closest("details");
  while (disclosure) {
    disclosure.open = true;
    disclosure = disclosure.parentElement?.closest("details");
  }
}

export class MarkdownHost {
  /** Mount the reader host and register navigation, position and resize handlers. */
  constructor(element, id, emit) {
    Object.assign(this, createRoot(element, "reader"));
    this.id = id;
    this.emit = emit;
    this.revision = 0;
    this.abort = new AbortController();
    this.body.tabIndex = 0;
    this.body.setAttribute("role", "document");
    localize(this.body, "Rendered Markdown document", "aria-label");
    this.body.addEventListener("click", event => this.click(event));
    this.disposeFileContext = attachFileContext(this.body, emit, target => {
      const link = target.closest?.("a");
      const path = link?.dataset.mvPath || target.closest?.("img[data-mv-asset]")?.dataset.mvAsset || this.model?.path;
      return { workspaceId: this.workspaceId, path, title: path?.split("/").at(-1) };
    });
    this.body.addEventListener("scroll", () => {
      this.scrollListener?.();
      clearTimeout(this.historyTimer);
      this.historyTimer = setTimeout(() => this.positionListener?.(this.capture()), 220);
    }, { passive: true });
  }
  /** Replace reader content and its local asset leases while rejecting obsolete asynchronous work. */
  async render({ document: model, workspaceId, position, anchor }, signal) {
    const revision = ++this.revision;
    this.abort.abort();
    const rendering = new AbortController();
    this.abort = rendering;
    /** Release queued reader asset work when its render operation is cancelled. */
    const abort = () => rendering.abort();
    signal?.addEventListener("abort", abort, { once: true });
    this.observer?.disconnect();
    releaseAssets(this.id);
    this.workspaceId = workspaceId;
    this.model = model;
    const fragment = prepareMarkdown(model);
    const top = document.createElement("header");
    top.className = "document-meta";
    top.append(textElement("span", model.path, "eyebrow"), uiText("span", "{0} min read", "reading-time", Math.max(1, Math.ceil(fragment.textContent.split(/\s+/u).length / 220))));
    this.article = document.createElement("article");
    this.article.className = "markdown";
    this.article.append(fragment);
    this.body.replaceChildren(top, this.article);
    highlightCode(this.article);
    applyReaderTheme(this.root, this.article, resolveReaderTheme(this.preferences, this.container.dataset.theme));
    this.blocks = [...this.article.querySelectorAll("section[data-source-start]")];
    this.assetQueue = [];
    const images = [...this.article.querySelectorAll("img")].slice(0, 96);
    for (const image of [...this.article.querySelectorAll("img")].slice(96)) {
      this.missingImage(image, "This page has reached its image display limit.");
    }
    /** Queue a visible local image for bounded asynchronous loading. */
    const enqueue = image => {
      if (image.dataset.loading) {
        return;
      }
      image.dataset.loading = "true";
      this.assetQueue.push(image);
      this.loadNextAssets(revision);
    };
    if (typeof IntersectionObserver === "function") {
      this.observer = new IntersectionObserver(entries => {
        for (const item of entries) if (item.isIntersecting) {
          this.observer.unobserve(item.target);
          enqueue(item.target);
        }
      }, { root: this.body, rootMargin: "400px" });
      for (const image of images) {
        this.observer.observe(image);
      }
    } else {
      for (const image of images) {
        enqueue(image);
      }
    }
    try {
      const diagrams = [...this.article.querySelectorAll(".mv-diagram")];
      if (diagrams.length) {
        const { renderDiagrams } = await import("./diagrams.js");
        await renderDiagrams(diagrams, () => revision === this.revision && !this.abort.signal.aborted);
      }
      checkCancelled(signal);
      if (revision !== this.revision) {
        return;
      }
      await nextFrame();
      if (position) {
        this.restore(position);
      }
      else if (anchor) {
        this.anchor(anchor);
      }
      else {
        this.body.scrollTop = 0;
      }
    } finally {
      signal?.removeEventListener("abort", abort);
    }
  }
  /** Load queued local images within the retained reader asset concurrency limit. */
  async loadNextAssets(revision) {
    this.activeAssets ??= 0;
    while (this.assetQueue.length && this.activeAssets < 2 && revision === this.revision) {
      const image = this.assetQueue.shift();
      this.activeAssets++;
      (async () => {
        try {
          if (!image.dataset.mvAsset) {
            return this.missingImage(image, "Remote or unsafe image not loaded.");
          }
          const lease = await acquireAsset(this.workspaceId, image.dataset.mvAsset, this.id, this.abort.signal);
          if (revision !== this.revision) {
            return;
          }
          image.src = lease.url;
          image.addEventListener("error", () => this.missingImage(image, "This image could not be displayed."), { once: true });
          image.addEventListener("load", () => this.resize(), { once: true });
        } catch (error) {
          if (revision === this.revision && !this.abort.signal.aborted) {
            this.missingImage(image, error.message);
          }
        } finally {
          this.activeAssets--;
          if (this.assetQueue?.length) {
            this.loadNextAssets(this.revision);
          }
        }
      })();
    }
  }
  /** Replace an unavailable local image with its existing resource recovery message. */
  missingImage(image, message) {
    if (!image.isConnected && this.article && !this.article.contains(image)) {
      return;
    }
    const figure = document.createElement("figure");
    figure.className = "missing-resource";
    const path = image.dataset.mvAsset;
    figure.append(textElement("strong", image.alt || "Image unavailable"), textElement("span", message));
    if (path) {
      figure.append(textElement("code", path));
      if (getWorkspace(this.workspaceId).info().capabilities.canAddFiles) {
        this.emit({ type: "missing-resource", path: this.model.path, assetPath: path, workspaceId: this.workspaceId });
      }
    }
    if (image.dataset.elementId) {
      figure.dataset.elementId = image.dataset.elementId;
    }
    image.replaceWith(figure);
  }
  /** Route allowed document links, anchors and external URLs without executing Markdown content. */
  click(event) {
    const link = event.target.closest("a");
    if (!link || !this.article.contains(link)) {
      return;
    }
    if (link.dataset.mvExternal) {
      return;
    }
    event.preventDefault();
    if (link.dataset.mvPath) {
      this.emit({ type: "navigate", path: link.dataset.mvPath, anchor: link.dataset.mvAnchor || "" });
    }
    else {
      this.emit({
        type: "status",
        message: link.dataset.mvAttachment ? "This attachment is available in the opened folder. The viewer opens Markdown pages and images." : "This link is unavailable or points outside the opened workspace."
      });
    }
  }
  /** Read a source block's offset within the reader's scrollable document area. */
  topOf(block) {
    return block.getBoundingClientRect().top - this.body.getBoundingClientRect().top + this.body.scrollTop;
  }
  /** Capture a source-backed block, fraction and source line for the visible reader position. */
  capture() {
    const visibleBlocks = this.blocks?.filter(block => !block.closest("details:not([open])")) || [];
    if (!visibleBlocks.length) {
      return { blockId: "", fraction: 0, sourceLine: 1 };
    }
    const target = this.body.scrollTop + 24;
    let index = 0;
    while (index + 1 < visibleBlocks.length && this.topOf(visibleBlocks[index + 1]) <= target) {
      index++;
    }
    const block = visibleBlocks[index];
    const next = visibleBlocks[index + 1];
    const start = this.topOf(block);
    const end = next ? this.topOf(next) : start + block.offsetHeight;
    const fraction = Math.min(1, Math.max(0, (target - start) / Math.max(1, end - start)));
    const first = Number(block.dataset.sourceStart);
    const last = next ? Number(next.dataset.sourceStart) : Number(block.dataset.sourceEnd) + 1;
    return { blockId: block.dataset.blockId, fraction, sourceLine: first + (last - first) * fraction };
  }
  /** Restore a retained reader position against the current source-backed blocks. */
  restore(position) {
    const block = this.blocks?.find(item => item.dataset.blockId === position.blockId);
    if (!block) {
      this.scrollToLine(position.sourceLine || 1);
      return;
    }
    revealDetails(block);
    const index = this.blocks.indexOf(block);
    const next = this.blocks.slice(index + 1).find(item => !item.closest("details:not([open])"));
    const top = this.topOf(block);
    const bottom = next ? this.topOf(next) : top + block.offsetHeight;
    this.body.scrollTop = Math.max(0, top + Math.min(1, Math.max(0, position.fraction || 0)) * (bottom - top) - 24);
  }
  /** Scroll to the rendered block corresponding to the requested source line. */
  scrollToLine(line) {
    if (!this.blocks?.length) {
      return;
    }
    let index = 0;
    while (index + 1 < this.blocks.length && Number(this.blocks[index + 1].dataset.sourceStart) <= line) {
      index++;
    }
    const block = this.blocks[index];
    const next = this.blocks[index + 1];
    const start = Number(block.dataset.sourceStart);
    const end = next ? Number(next.dataset.sourceStart) : Number(block.dataset.sourceEnd) + 1;
    const fraction = Math.max(0, Math.min(1, (line - start) / Math.max(1, end - start)));
    this.restore({ blockId: block.dataset.blockId, fraction, sourceLine: line });
  }
  /** Scroll to a heading or source-backed outline element without interpreting an arbitrary selector. */
  anchor(id) {
    const heading = this.root.getElementById(id)
      || [...this.article.querySelectorAll("[data-element-id]")].find(element => element.dataset.elementId === id);
    if (!heading) {
      this.emit({
        type: "status",
        message: "This heading is no longer available. The page has been opened from the beginning."
      });
      this.body.scrollTop = 0;
      return;
    }
    revealDetails(heading);
    this.body.scrollTop = heading.getBoundingClientRect().top - this.body.getBoundingClientRect().top + this.body.scrollTop - 24;
    heading.tabIndex = -1;
    heading.focus({ preventScroll: true });
  }
  /** Refresh local code and diagram presentation while rejecting obsolete appearance work. */
  async retheme(isCurrent = () => true) {
    if (!this.article) {
      return;
    }
    const revision = this.revision;
    const article = this.article;
    /** Reject diagram work after navigation, disposal or a newer appearance operation. */
    const current = () => revision === this.revision && article === this.article && !this.abort.signal.aborted && isCurrent();
    applyReaderTheme(this.root, this.article, resolveReaderTheme(this.preferences, this.container.dataset.theme));
    const diagrams = [...this.article.querySelectorAll(".mv-diagram")];
    if (diagrams.length) {
      const { renderDiagrams } = await import("./diagrams.js");
      await renderDiagrams(diagrams, current);
    }
  }
  /** Refresh the captured position after the reader host's layout changes. */
  resize() {
    this.scrollListener?.();
  }


  /** Cancel reader work and release local asset leases, observers and DOM resources. */
  dispose() {
    this.revision++;
    this.disposeFileContext();
    this.abort.abort();
    this.observer?.disconnect();
    clearTimeout(this.historyTimer);
    releaseAssets(this.id);
    this.scrollListener = null;
    this.positionListener = null;
    this.disposeRoot();
    this.container.remove();
  }
}
