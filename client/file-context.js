/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared file-context events for DOM document hosts; 2026-10-16
 * Human guidance: Umberto Giacobbi; consistent file menus in reader and source views
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { documentSelection } from "./document-selection.js";

/** Capture visible text before the managed popup takes focus, including isolated editor selections. */
export function fileSelection(host) {
  if (host.editor) {
    const model = host.editor.getModel();
    return (host.editor.getSelections() ?? [])
      .map(selection => model?.getValueInRange(selection) ?? "")
      .filter(text => text.length > 0)
      .join("\n");
  }

  if (host.textarea) {
    return host.textarea.value.slice(host.textarea.selectionStart, host.textarea.selectionEnd);
  }

  if (host.pane) {
    const selection = documentSelection(host);
    return selection && !selection.unsupported ? host.source.slice(selection.start, selection.end) : "";
  }

  const selection = host.root.getSelection?.() || document.getSelection();
  if (!selection || selection.isCollapsed || !selection.rangeCount) {
    return "";
  }

  const composed = selection.getComposedRanges?.({ shadowRoots: [host.root] })?.[0];
  const range = document.createRange();
  const boundaries = composed || selection.getRangeAt(0);
  if (!host.body.contains(boundaries.startContainer) || !host.body.contains(boundaries.endContainer)) {
    return "";
  }

  range.setStart(boundaries.startContainer, boundaries.startOffset);
  range.setEnd(boundaries.endContainer, boundaries.endOffset);
  return range.toString();
}

/** Forward document context gestures and their captured selection to XAML. */
export function attachFileContext(element, emit, targetProvider, selectionProvider) {
  /** Capture the document target and viewport point for this exact context-menu gesture. */
  const showMenu = event => {
    const target = targetProvider(event.target);
    if (!target?.workspaceId || !target.path) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    const bounds = element.getBoundingClientRect();
    const x = event.clientX || bounds.left + 24;
    const y = event.clientY || bounds.top + 24;
    emit({ type: "file-context", ...target, selectedText: selectionProvider?.() ?? "", x, y });
  };
  element.addEventListener("contextmenu", showMenu, { capture: true });
  return () => element.removeEventListener("contextmenu", showMenu, { capture: true });
}
