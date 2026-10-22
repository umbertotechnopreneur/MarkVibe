/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared file-context events for DOM document hosts; 2026-10-16
 * Human guidance: Umberto Giacobbi; consistent file menus in reader and source views
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
/** Forward document context gestures to XAML; the owner limits native file actions separately. */
export function attachFileContext(element, emit, targetProvider) {
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
    emit({ type: "file-context", ...target, x, y });
  };
  element.addEventListener("contextmenu", showMenu, { capture: true });
  return () => element.removeEventListener("contextmenu", showMenu, { capture: true });
}
