/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared file-context events for DOM document hosts; 2026-10-16
 * Human guidance: Umberto Giacobbi; consistent file menus in reader and source views
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { documentSelection } from "./document-selection.js";

/** Capture source identifiers from the displayed snapshot without parsing Markdown in the view. */
export function fileContextTarget(host, target, event, followLinks = true) {
  const currentPath = host.path ?? host.model?.path;
  const link = followLinks ? target?.closest?.("a[data-mv-path]") : null;
  const image = followLinks ? target?.closest?.("img[data-mv-asset]") : null;
  const path = link?.dataset.mvPath || image?.dataset.mvAsset || currentPath;
  const context = {
    workspaceId: host.workspaceId,
    path,
    title: path?.split("/").at(-1),
    view: host.model ? "read" : "source"
  };

  // A linked file does not own the block identifiers of the page containing its link.
  if (path !== currentPath) {
    return context;
  }

  context.fingerprint = host.fingerprint ?? host.model?.fingerprint ?? "";
  if (host.editor) {
    const pointerTarget = event?.clientX || event?.clientY
      ? host.editor.getTargetAtClientPoint(event.clientX, event.clientY)
      : null;
    context.sourceLine = pointerTarget?.position?.lineNumber ?? host.editor.getPosition()?.lineNumber ?? 1;
  } else if (host.textarea) {
    context.sourceLine = host.source.slice(0, host.textarea.selectionStart).split("\n").length;
  } else if (host.pane) {
    context.sourceLine = Number(target?.closest?.("[data-source-line]")?.dataset.sourceLine) || 1;
  } else {
    // A cell/code token resolves to its complete table/code block for copy commands.
    const element = target?.closest?.("table[data-element-id],pre[data-element-id],.mv-diagram[data-element-id]")
      ?? target?.closest?.("[data-element-id]");
    context.elementId = element?.dataset.elementId ?? "";
    context.headingId = target?.closest?.("h1,h2,h3,h4,h5,h6")?.id ?? "";
    context.sourceLine = Number(target?.closest?.("[data-source-start]")?.dataset.sourceStart) || 1;
  }

  return context;
}

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
export function attachFileContext(element, emit, targetProvider, selectionProvider, findTargetProvider = targetProvider) {
  /** Capture the document target and viewport point for this exact context-menu gesture. */
  const showMenu = event => {
    const target = targetProvider(event.target, event);
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

  /** Route the document shortcut before Monaco or the browser opens a separate search interface. */
  const requestFind = event => {
    if ((!event.ctrlKey && !event.metaKey) || event.altKey || event.key.toLowerCase() !== "f") {
      return;
    }
    const target = findTargetProvider(event.target, event);
    if (!target?.workspaceId || !target.path) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    emit({ type: "document-find", ...target, selectedText: selectionProvider?.() ?? "" });
  };

  element.addEventListener("contextmenu", showMenu, { capture: true });
  element.addEventListener("keydown", requestFind, { capture: true });
  /** Remove both document gestures when their host is detached. */
  return () => {
    element.removeEventListener("contextmenu", showMenu, { capture: true });
    element.removeEventListener("keydown", requestFind, { capture: true });
  };
}
