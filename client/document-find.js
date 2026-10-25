/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; document search adapter and source-safe highlights; 2026-10-25
 * Human guidance: Umberto Giacobbi; expose context commands through services below the interface
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */

const searches = new WeakMap();
let nextHighlightId = 0;

/** Release search ranges and cached text before a host replaces or disposes its document. */
export function clearDocumentFind(host) {
  const search = searches.get(host);
  if (!search) {
    return;
  }
  globalThis.CSS?.highlights?.delete(search.highlightName);
  clearFallbackSelection(host, search);
  clearSourceSelection(host, search);
  search.style?.remove();
  host.root.querySelector(".source-match")?.classList.remove("source-match");
  searches.delete(host);
}

/** Clear only the fallback selection still owned by search, preserving a later user selection. */
function clearFallbackSelection(host, search) {
  if (!search.fallbackRange) {
    return;
  }
  const selection = host.root.getSelection?.() ?? document.getSelection();
  const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
  const previous = search.fallbackRange;
  if (range?.startContainer === previous.startContainer && range.startOffset === previous.startOffset &&
    range.endContainer === previous.endContainer && range.endOffset === previous.endOffset) {
    selection.removeAllRanges();
  }
  search.fallbackRange = null;
}

/** Remove a source highlight only while the editor selection still matches the search-owned range. */
function clearSourceSelection(host, search) {
  const selection = host.editor?.getSelection();
  const previous = search.editorRange;
  if (selection && previous && selection.startLineNumber === previous.startLineNumber &&
    selection.startColumn === previous.startColumn && selection.endLineNumber === previous.endLineNumber &&
    selection.endColumn === previous.endColumn) {
    host.editor.setSelection({
      startLineNumber: previous.endLineNumber,
      startColumn: previous.endColumn,
      endLineNumber: previous.endLineNumber,
      endColumn: previous.endColumn
    });
  }
  const textareaRange = search.textareaRange;
  if (host.textarea && textareaRange && host.textarea.selectionStart === textareaRange.start &&
    host.textarea.selectionEnd === textareaRange.end) {
    host.textarea.setSelectionRange(textareaRange.end, textareaRange.end);
  }
  search.editorRange = null;
  search.textareaRange = null;
}

/** Build a text index without inserting wrappers that would invalidate source mappings. */
function indexDocument(host) {
  if (typeof host.source === "string") {
    return { text: host.source, segments: [] };
  }

  const segments = [];
  const pieces = [];
  let length = 0;
  let previousBlock;
  const walker = document.createTreeWalker(host.article, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    if (node.nodeType === Node.ELEMENT_NODE) {
      if (node.tagName === "BR" && !node.closest("svg,button,[aria-hidden='true']")) {
        pieces.push("\n");
        length++;
      }
      continue;
    }
    const parent = node.parentElement;
    if (!parent || parent.closest("svg,style,script,button,[aria-hidden='true']")) {
      continue;
    }
    const block = parent.closest("p,h1,h2,h3,h4,h5,h6,li,td,th,pre,summary");
    if (previousBlock && block !== previousBlock) {
      pieces.push("\n");
      length++;
    }
    const text = node.textContent;
    segments.push({ node, start: length, end: length + text.length });
    pieces.push(text);
    length += text.length;
    previousBlock = block;
  }
  return { text: pieces.join(""), segments };
}

/** Locate text without changing source offsets; reader whitespace follows rendered line wrapping. */
function matchOffsets(text, query, reader) {
  let escaped = query.replace(/[.*+?^${}()|[\]\\]/gu, "\\$&");
  if (reader) {
    // Markdig soft line breaks remain newlines in text nodes but display as spaces in paragraphs.
    escaped = escaped.replace(/\s+/gu, "\\s+");
  }
  const expression = new RegExp(escaped, "giu");
  // Compact offsets keep a repeated single-character query bounded even in a 1 MiB document.
  let offsets = new Uint32Array(256);
  let length = 0;
  for (let match = expression.exec(text); match; match = expression.exec(text)) {
    if (length === offsets.length) {
      const expanded = new Uint32Array(offsets.length * 2);
      expanded.set(offsets);
      offsets = expanded;
    }
    offsets[length++] = match.index;
    offsets[length++] = match.index + match[0].length;
  }
  return offsets.subarray(0, length);
}

/** Open enclosing disclosure blocks before moving a result into the visible reading area. */
function revealMatch(element) {
  for (let disclosure = element.closest("details"); disclosure; disclosure = disclosure.parentElement?.closest("details")) {
    disclosure.open = true;
  }
  element.scrollIntoView({ block: "center", inline: "nearest" });
}

/** Highlight the current DOM match without altering content or stealing keyboard focus from XAML. */
function showRange(host, search, range) {
  if (globalThis.CSS?.highlights && typeof globalThis.Highlight === "function") {
    if (!search.style) {
      search.style = document.createElement("style");
      search.style.textContent = `::highlight(${search.highlightName}){background:#facc15;color:#111827;}`;
      host.root.append(search.style);
    }
    CSS.highlights.set(search.highlightName, new Highlight(range));
  } else {
    const selection = host.root.getSelection?.() ?? document.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);
    search.fallbackRange = range;
  }
  revealMatch(range.startContainer.parentElement);
}

/** Resolve source offsets to line and column without keeping a second parsed Markdown model. */
function sourcePosition(source, offset) {
  const prefix = source.slice(0, offset);
  return { lineNumber: prefix.split("\n").length, column: offset - prefix.lastIndexOf("\n") };
}

/** Show a match in Monaco, its plain-source fallback, or the reader's existing text nodes. */
function showMatch(host, search, match) {
  if (host.editor) {
    const model = host.editor.getModel();
    const start = model.getPositionAt(match.start);
    const end = model.getPositionAt(match.end);
    const range = {
      startLineNumber: start.lineNumber,
      startColumn: start.column,
      endLineNumber: end.lineNumber,
      endColumn: end.column
    };
    host.editor.setSelection(range);
    search.editorRange = range;
    host.editor.revealRangeInCenter(range);
    return;
  }
  if (host.textarea) {
    host.textarea.setSelectionRange(match.start, match.end);
    search.textareaRange = match;
    host.scrollToLine(sourcePosition(host.source, match.start).lineNumber);
    return;
  }

  const range = document.createRange();
  if (host.pane) {
    const start = sourcePosition(host.source, match.start);
    const end = sourcePosition(host.source, match.end);
    const first = host.lineElements[start.lineNumber - 1]?.querySelector(".line-text")?.firstChild;
    const last = host.lineElements[end.lineNumber - 1]?.querySelector(".line-text")?.firstChild;
    if (!first || !last) {
      return;
    }
    range.setStart(first, Math.min(start.column - 1, first.length));
    range.setEnd(last, Math.min(end.column - 1, last.length));
  } else {
    const first = search.segments.find(segment => segment.end > match.start);
    const last = search.segments.find(segment => segment.end >= match.end);
    if (!first || !last) {
      return;
    }
    range.setStart(first.node, Math.max(0, match.start - first.start));
    range.setEnd(last.node, Math.max(0, match.end - last.start));
  }
  showRange(host, search, range);
}

/** Search the requested visible document, returning one-based current result and the full match count. */
export function findDocument(views, args) {
  const candidates = [...views].filter(host => host.workspaceId === args.workspaceId &&
    (host.path ?? host.model?.path) === args.path &&
    (host.article || typeof host.source === "string"));
  if (!args.query) {
    candidates.forEach(clearDocumentFind);
    return { current: 0, total: 0 };
  }
  const visible = candidates.filter(host => host.container.isConnected && host.container.clientHeight > 0 && host.container.clientWidth > 0);
  const host = visible.find(candidate => args.view === "source" ? typeof candidate.source === "string" : !!candidate.article)
    ?? visible[0];
  if (!host) {
    return { current: 0, total: 0 };
  }
  for (const other of candidates) {
    if (other !== host) {
      clearDocumentFind(other);
    }
  }

  let search = searches.get(host);
  if (!search) {
    search = { ...indexDocument(host), query: "", matches: [], current: -1, highlightName: `mv-find-${++nextHighlightId}` };
    searches.set(host, search);
  }
  const query = String(args.query);
  if (search.query !== query) {
    clearFallbackSelection(host, search);
    clearSourceSelection(host, search);
    search.query = query;
    search.matches = matchOffsets(search.text, query, !!host.article);
    search.current = -1;
    globalThis.CSS?.highlights?.delete(search.highlightName);
  }
  const total = search.matches.length / 2;
  if (!total) {
    return { current: 0, total: 0 };
  }
  if (search.current < 0) {
    search.current = args.direction < 0 ? total - 1 : 0;
  } else if (args.direction) {
    search.current = (search.current + (args.direction < 0 ? -1 : 1) + total) % total;
  }
  showMatch(host, search, {
    start: search.matches[search.current * 2],
    end: search.matches[search.current * 2 + 1]
  });
  return { current: search.current + 1, total };
}
