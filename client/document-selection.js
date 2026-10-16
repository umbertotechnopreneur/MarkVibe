/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; source-backed reader and editor selections; 2026-10-11
 * Human guidance: Umberto Giacobbi; replace the active Markdown selection precisely
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

/** Resolve a selection inside a shadow host, including browsers exposing composed selection ranges. */
function selectionRange(host) {
  const selection = host.root.getSelection?.() || document.getSelection();
  if (!selection || selection.isCollapsed || !selection.rangeCount) {
    return null;
  }

  const composed = selection.getComposedRanges?.({ shadowRoots: [host.root] })?.[0];
  if (!composed) {
    return selection.getRangeAt(0);
  }

  const range = document.createRange();
  range.setStart(composed.startContainer, composed.startOffset);
  range.setEnd(composed.endContainer, composed.endOffset);
  return range;
}

/** Locate a boundary text node without counting decorative line numbers or unrelated controls. */
function textBoundary(node, offset, end) {
  if (node.nodeType === Node.TEXT_NODE) {
    return { node, offset };
  }

  const children = node.childNodes;
  let child = end ? children[offset - 1] : children[offset];
  if (!child) {
    return null;
  }

  while (child.nodeType !== Node.TEXT_NODE) {
    child = end ? child.lastChild : child.firstChild;
    if (!child) {
      return null;
    }
  }

  return { node: child, offset: end ? child.textContent.length : 0 };
}

/** Translate an exact literal span or source-line boundary to a UTF-16 Markdown offset. */
function sourceOffset(host, node, offset, end, reader) {
  const boundary = textBoundary(node, offset, end);
  if (!boundary) {
    return null;
  }

  const mapped = boundary.node.parentElement?.closest(reader ? "[data-md-start]" : ".line-text");
  if (!mapped || !host.body.contains(mapped)) {
    return null;
  }

  const prefix = document.createRange();
  prefix.setStart(mapped, 0);
  prefix.setEnd(boundary.node, boundary.offset);
  const textOffset = prefix.toString().length;

  if (reader) {
    const start = Number(mapped.dataset.mdStart);
    const finish = Number(mapped.dataset.mdEnd);
    // Only literal source spans have a direct UTF-16 offset in the rendered reader.
    if (!Number.isSafeInteger(start) ||
      !Number.isSafeInteger(finish) ||
      mapped.textContent.length !== finish - start) {
      return null;
    }
    return start + textOffset;
  }

  const line = Number(mapped.closest("[data-source-line]")?.dataset.sourceLine);
  if (!Number.isSafeInteger(line) || line < 1) {
    return null;
  }

  const lines = host.source.split("\n");
  const start = lines.slice(0, line - 1).reduce((length, text) => length + text.length + 1, 0);
  return start + Math.min(textOffset, lines[line - 1]?.length || 0);
}

/** Cache exact raw line starts; Monaco normalizes mixed line endings but replacement offsets must address original text. */
function rawLineStarts(host) {
  if (host.selectionSource !== host.source) {
    host.selectionSource = host.source;
    host.selectionLineStarts = [0];

    for (const newline of host.source.matchAll(/\r\n|\r|\n/g)) {
      host.selectionLineStarts.push(newline.index + newline[0].length);
    }
  }

  return host.selectionLineStarts;
}

/** Map a textarea's LF-normalized UTF-16 selection back to the original CRLF/LF Markdown. */
function rawTextareaOffset(source, normalizedOffset) {
  let rawOffset = 0;
  let displayedOffset = 0;

  while (rawOffset < source.length && displayedOffset < normalizedOffset) {
    if (source[rawOffset] === "\r" && source[rawOffset + 1] === "\n") {
      rawOffset += 2;
    } else {
      rawOffset++;
    }
    displayedOffset++;
  }

  return rawOffset;
}

/** Capture only source-backed document selections; unsupported rendered boundaries never become whole-document edits. */
export function documentSelection(host, reader = false) {
  if (!reader && host.editor) {
    const selection = host.editor.getSelection();
    if (!selection || selection.isEmpty()) {
      return null;
    }

    const lineStarts = rawLineStarts(host);
    const start = selection.getStartPosition();
    const end = selection.getEndPosition();
    return {
      start: lineStarts[start.lineNumber - 1] + start.column - 1,
      end: lineStarts[end.lineNumber - 1] + end.column - 1
    };
  }

  if (!reader && host.textarea && host.textarea.selectionStart !== host.textarea.selectionEnd) {
    return {
      start: rawTextareaOffset(host.source, host.textarea.selectionStart),
      end: rawTextareaOffset(host.source, host.textarea.selectionEnd)
    };
  }

  const range = selectionRange(host);
  const documentBody = reader ? host.article : host.pane;
  if (!range || !documentBody) {
    return null;
  }

  const startInside = documentBody.contains(range.startContainer);
  const endInside = documentBody.contains(range.endContainer);
  if (startInside !== endInside) {
    return { unsupported: true };
  }

  if (!startInside) {
    return null;
  }

  const start = sourceOffset(host, range.startContainer, range.startOffset, false, reader);
  const end = sourceOffset(host, range.endContainer, range.endOffset, true, reader);
  return start !== null && end !== null && end > start
    ? { start, end }
    : { unsupported: true };
}

/** Observe document gestures only; moving focus into the prompt preserves the last selected Markdown range. */
export function observeDocumentSelection(host, reader = false) {
  const controller = new AbortController();
  let last = "";

  /** Publish changed ranges with the current document identity and source fingerprint. */
  const report = () => {
    if (controller.signal.aborted) {
      return;
    }

    const selection = documentSelection(host, reader);
    const documentModel = reader ? host.model : host;
    if (!documentModel?.path || !documentModel.fingerprint) {
      return;
    }

    const signature = JSON.stringify([
      host.workspaceId,
      documentModel.path,
      documentModel.fingerprint,
      selection
    ]);
    if (signature === last) {
      return;
    }

    last = signature;
    host.emit({
      type: "document-selection",
      workspaceId: host.workspaceId,
      path: documentModel.path,
      fingerprint: documentModel.fingerprint,
      selection
    });
  };

  host.container.addEventListener("pointerup", () => requestAnimationFrame(report), { signal: controller.signal });
  host.container.addEventListener("keyup", report, { signal: controller.signal });
  document.addEventListener("selectionchange", () => {
    if (host.root.activeElement || host.container.contains(document.activeElement)) {
      report();
    }
  }, { signal: controller.signal });

  host.selectionChanged = report;
  return controller;
}
