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
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import * as monaco from "monaco-editor/editor/editor.api.js";
import { conf, language } from "monaco-editor/languages/definitions/markdown/markdown.js";
import "monaco-editor/editor/contrib/find/browser/findController.js";
import "monaco-editor/editor/contrib/clipboard/browser/clipboard.js";

monaco.languages.register({ id: "markdown", extensions: [".md", ".markdown"] });
monaco.languages.setLanguageConfiguration("markdown", conf);
monaco.languages.setMonarchTokensProvider("markdown", language);

globalThis.MonacoEnvironment = {
  /** Create the local Monaco module worker using the application's existing base path. */
  getWorker() {
    return new Worker(new URL("js/editor.worker.js", document.baseURI), { type: "module", name: "markvibe-editor" });
  }
};
export { monaco };
