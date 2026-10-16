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

const svgNamespace = "http://www.w3.org/2000/svg";
/** Render a decorative SVG symbol inside the specialized graph canvas while keeping decorative icons hidden from assistive technology. */
export function iconSvg(name) {
  const svg = document.createElementNS(svgNamespace, "svg");
  svg.setAttribute("viewBox", "0 0 24 24");
  svg.setAttribute("width", "18");
  svg.setAttribute("height", "18");
  svg.setAttribute("aria-hidden", "true");
  svg.setAttribute("focusable", "false");
  svg.classList.add("mv-icon");
  svg.style.cssText = "display:inline-block;flex:none;vertical-align:middle;pointer-events:none;";
  const use = document.createElementNS(svgNamespace, "use");
  use.setAttribute("href", new URL("icons.svg", document.baseURI).href + "#" + name);
  svg.append(use);
  return svg;
}
