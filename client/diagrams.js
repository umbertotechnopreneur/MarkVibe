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

import DOMPurify from "dompurify";
import { textElement } from "./markdown.js";
import { pause, makeId } from "./path.js";

let queue = Promise.resolve();
const supported = /^(?:flowchart|graph|sequenceDiagram|classDiagram|stateDiagram(?:-v2)?|erDiagram|timeline|mindmap|pie|gantt)\b/u;
const svgStyles = new Set(["fill", "fill-opacity", "stroke", "stroke-width", "stroke-opacity", "stroke-dasharray", "stroke-dashoffset",
  "color", "opacity", "background", "background-color", "font", "font-family", "font-size", "font-style", "font-weight",
  "font-variant", "line-height", "letter-spacing", "word-spacing", "text-anchor", "dominant-baseline", "alignment-baseline",
  "baseline-shift", "white-space", "display", "visibility", "vertical-align", "text-decoration", "text-transform", "text-overflow",
  "overflow", "overflow-wrap", "word-wrap", "word-break", "hyphens", "max-width", "width", "height", "rx", "ry", "shape-rendering", "paint-order"]);

/** Reject executable or external CSS while permitting diagram-local fragment references. */
function unsafeStyle(value) {
  const withoutLocalUrls = value.replace(/url\(\s*["']?#[a-zA-Z0-9_-]+["']?\s*\)/giu, "");
  return /[\\@<>]|url\s*\(|(?:https?|data|javascript):|expression\s*\(/iu.test(withoutLocalUrls);
}

/** Retain bounded allowlisted static CSS scoped to the current diagram identifier. */
function diagramStyles(source, id) {
  if (source.length > 100000) {
    throw new Error("The diagram style exceeds the display limit.");
  }
  // Mermaid includes animation at-rules in its generated stylesheet. Drop those
  // rules rather than removing all static node fills and text colors with them.
  const sheet = new CSSStyleSheet();
  sheet.replaceSync(source);
  if (sheet.cssRules.length > 512) {
    throw new Error("The diagram style exceeds the display limit.");
  }
  const rules = [];
  for (const rule of sheet.cssRules) {
    if (rule.type !== CSSRule.STYLE_RULE || rule.selectorText.length > 2000 ||
      !rule.selectorText.split(",").every(selector => {
        const trimmed = selector.trim();
        return trimmed === "#" + id || trimmed.startsWith("#" + id) && /^[\s.#:[>+~]/u.test(trimmed.slice(id.length + 1));
      })) {
      continue;
    }
    for (const property of [...rule.style])
      if ((!svgStyles.has(property) && !/^--mermaid-[a-z-]+$/u.test(property)) || unsafeStyle(rule.style.getPropertyValue(property))) {
        rule.style.removeProperty(property);
      }
    if (rule.style.length) {
      rules.push(rule.cssText);
    }
  }
  return rules.join("\n");
}

/** Render a bounded supported Mermaid diagram and sanitize its SVG before displaying it. */
async function renderOne(element, valid, appearance) {
  const source = element.dataset.diagramSource ?? element.querySelector("code")?.textContent ?? "";
  element.dataset.diagramSource = source;
  if (!valid()) {
    return;
  }
  try {
    if (source.length > 20000 || source.trimStart().startsWith("---") || source.includes("%%{") || /^\s*click\b/mu.test(source) || !supported.test(source.trimStart()) ||
      source.split(/\r?\n/u).length > 400) {
      throw new Error("This diagram uses unsupported configuration or exceeds the display limit.");
    }
    const { default: mermaid } = await import("mermaid");
    if (!valid()) {
      return;
    }
    const dark = appearance ? appearance === "dark" : document.documentElement.dataset.theme === "dark";
    mermaid.initialize({
      startOnLoad: false,
      securityLevel: "strict",
      theme: dark ? "dark" : "default",
      htmlLabels: false,
      maxTextSize: 20000,
      maxEdges: 160,
      suppressErrorRendering: true,
      fontFamily: "Segoe UI, system-ui, sans-serif",
      flowchart: { htmlLabels: false, useMaxWidth: true },
      secure: ["securityLevel", "startOnLoad", "maxTextSize", "maxEdges", "htmlLabels", "theme", "themeVariables", "themeCSS", "dompurifyConfig"]
    });
    const id = "diagram-" + makeId().replaceAll("-", "");
    const scratch = document.createElement("div");
    scratch.style.cssText = "position:absolute;left:-100000px;top:0;visibility:hidden;";
    document.body.append(scratch);
    let rendered;
    try {
      rendered = await mermaid.render(id, source, scratch);
    }
    finally {
      scratch.remove();
    }
    if (!valid()) {
      return;
    }
    const fragment = DOMPurify.sanitize(rendered.svg, {
      USE_PROFILES: { svg: true, svgFilters: false },
      RETURN_DOM_FRAGMENT: true,
      FORBID_TAGS: ["script", "foreignObject", "a", "image", "use", "animate", "animateMotion", "animateTransform", "set"],
      FORBID_ATTR: ["href", "xlink:href"]
    });
    for (const node of fragment.querySelectorAll("*")) {
      for (const attribute of [...node.attributes])
        if (/[\\@<>]|(?:https?|data|javascript):|expression\s*\(/iu.test(attribute.value) ||
          /url\s*\(/iu.test(attribute.value) && !/^url\(\s*["']?#[a-zA-Z0-9_-]+["']?\s*\)$/u.test(attribute.value)) {
          node.removeAttribute(attribute.name);
        }
      if (node.localName === "style") {
        const styles = diagramStyles(node.textContent, id);
        if (styles) {
          node.textContent = styles;
        } else {
          node.remove();
        }
      }
    }
    const svg = fragment.querySelector("svg");
    if (!svg || fragment.querySelectorAll("*").length > 4096) {
      throw new Error("The diagram exceeds the display limit.");
    }
    svg.setAttribute("role", "img");
    svg.setAttribute("aria-label", "Diagram from " + source.split(/\r?\n/u)[0].slice(0, 120));
    const details = document.createElement("details");
    details.append(textElement("summary", "Diagram source"), textElement("pre", source));
    element.replaceChildren(fragment, details);
  } catch (error) {
    if (!valid()) {
      return;
    }
    const details = document.createElement("details");
    details.open = true;
    details.append(textElement("summary", "Diagram unavailable"),
      textElement("p", "The rest of this page is ready to read. Check the diagram syntax in Source view.", "resource-message"),
      textElement("pre", source));
    element.replaceChildren(details);
  }
  await pause();
}
/** Serialize diagram rendering jobs so Mermaid's shared state is used by one job at a time. */
export function renderDiagrams(elements, valid, appearance) {
  /** Render this job's diagrams in order while stopping when their document generation is obsolete. */
  const job = async () => {
    for (const element of elements) {
      if (!valid()) {
        return;
      }
      await renderOne(element, valid, appearance);
    }
  };
  queue = queue.then(job, job);
  return queue;
}
