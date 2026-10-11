import * as monaco from "monaco-editor/editor/editor.api.js";
import { conf, language } from "monaco-editor/languages/definitions/markdown/markdown.js";
import "monaco-editor/editor/contrib/find/browser/findController.js";
import "monaco-editor/editor/contrib/clipboard/browser/clipboard.js";

monaco.languages.register({ id: "markdown", extensions: [".md", ".markdown"] });
monaco.languages.setLanguageConfiguration("markdown", conf);
monaco.languages.setMonarchTokensProvider("markdown", language);

globalThis.MonacoEnvironment = {
  getWorker() { return new Worker(new URL("js/editor.worker.js", document.baseURI), { type: "module", name: "markvibe-editor" }); }
};
export { monaco };
