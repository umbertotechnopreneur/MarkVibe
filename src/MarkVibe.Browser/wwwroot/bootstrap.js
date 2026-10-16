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

/** Loads the runtime scripts after the locally bundled application module. */
async function script(path) {
  await new Promise((resolve, reject) => {
    const element = document.createElement("script");
    element.src = new URL(path, document.baseURI);
    element.onload = resolve;
    element.onerror = reject;
    document.body.append(element);
  });
}
try {
  await import("./js/markvibe.js");
  await script("libs/OpenSilver.js");
  await script("_framework/blazor.webassembly.js");
} catch {
  const message = document.getElementById("boot-message");
  if (message) {
    const errorMessage = "The viewer could not load. Check its local assets and reload this page.";
    message.textContent = window.markvibe?.translate?.(errorMessage) ?? errorMessage;
  }
  document.getElementById("blazor-error-ui").style.display = "block";
}
