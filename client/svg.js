/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; preserve bounded static SVG presentation; 2026-10-25
 * Human guidance: Umberto Giacobbi; fix black SVG covers while retaining resource filtering
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import DOMPurify from "dompurify";
import { WorkspaceError, limits } from "./path.js";

const presentationProperties = new Set([
  "fill", "fill-opacity", "fill-rule", "stroke", "stroke-width", "stroke-opacity",
  "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-dasharray", "stroke-dashoffset",
  "color", "opacity", "font-family", "font-size", "font-style", "font-weight", "font-variant",
  "font-stretch", "letter-spacing", "word-spacing", "text-anchor", "dominant-baseline",
  "alignment-baseline", "baseline-shift", "text-decoration", "text-transform", "paint-order",
  "shape-rendering", "text-rendering", "vector-effect", "visibility", "display",
  "stop-color", "stop-opacity", "clip-path", "marker-start", "marker-mid", "marker-end"
]);
const localReference = /^url\(\s*["']?#[a-zA-Z0-9_-]+["']?\s*\)$/iu;
const colorSchemeQuery = /^\(\s*prefers-color-scheme\s*:\s*(?:light|dark)\s*\)$/iu;

/** Accept static presentation values with fragment-only URLs and no escaped executable/resource syntax. */
function safePresentationValue(value) {
  const withoutLocalReferences = value.replace(/url\(\s*["']?#[a-zA-Z0-9_-]+["']?\s*\)/giu, "");
  return value.length <= 2048
    && !/[\\@<>]|url\s*\(|(?:https?|data|javascript|vbscript|file):|expression\s*\(/iu.test(withoutLocalReferences);
}

/** Keep drawing and typography declarations, excluding resource loading, layout overrides and animation. */
function cleanDeclarations(style) {
  for (const property of [...style]) {
    if (!presentationProperties.has(property) || !safePresentationValue(style.getPropertyValue(property))) {
      style.removeProperty(property);
    }
  }
  return style.cssText;
}

/** Retain static rules and a single level of color-scheme media rules within one shared rule budget. */
function cleanRules(rules, budget, depth = 0) {
  const retained = [];
  for (const rule of rules) {
    budget.rules--;
    if (budget.rules < 0) {
      throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
    }
    if (rule.type === CSSRule.STYLE_RULE && rule.selectorText.length <= 2000) {
      const declarations = cleanDeclarations(rule.style);
      if (declarations) {
        retained.push(rule.selectorText + " { " + declarations + " }");
      }
    } else if (rule.type === CSSRule.MEDIA_RULE && depth === 0 && colorSchemeQuery.test(rule.conditionText)) {
      const children = cleanRules(rule.cssRules, budget, depth + 1);
      if (children) {
        retained.push("@media " + rule.conditionText + " {\n" + children + "\n}");
      }
    }
  }
  return retained.join("\n");
}

/** Sanitize detached SVG styles before serialization; they remain confined to the standalone image document. */
function cleanPresentation(fragment) {
  const styleNodes = [...fragment.querySelectorAll("style")];
  const inlineNodes = [...fragment.querySelectorAll("[style]")];
  const styleLength = styleNodes.reduce((total, node) => total + node.textContent.length, 0)
    + inlineNodes.reduce((total, node) => total + node.getAttribute("style").length, 0);
  if (styleLength > 65536) {
    throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
  }

  const budget = { rules: 512 };
  for (const node of styleNodes) {
    // Constructed stylesheets parse without applying CSS or loading @import rules.
    // Only reserialized allowlisted rules ever reach the resulting image.
    const sheet = new CSSStyleSheet();
    sheet.replaceSync(node.textContent);
    const styles = cleanRules(sheet.cssRules, budget);
    if (styles) {
      node.textContent = styles;
    } else {
      node.remove();
    }
  }
  for (const node of inlineNodes) {
    const declaration = document.createElement("span").style;
    declaration.cssText = node.getAttribute("style");
    const styles = cleanDeclarations(declaration);
    if (styles) {
      node.setAttribute("style", styles);
    } else {
      node.removeAttribute("style");
    }
  }
}

/** Remove active/external SVG content, preserve static presentation and retain bounded image dimensions. */
export async function sanitizeSvg(blob) {
  if (blob.size > 256 * 1024) {
    throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
  }
  const text = new TextDecoder("utf-8", { fatal: true }).decode(await blob.arrayBuffer());
  const clean = DOMPurify.sanitize(text, {
    USE_PROFILES: { svg: true, svgFilters: false },
    FORBID_TAGS: [
      "script", "foreignObject", "image", "use", "a", "animate",
      "animateMotion", "animateTransform", "set", "filter"
    ],
    FORBID_ATTR: ["href", "xlink:href"],
    RETURN_DOM: true
  });
  const svg = clean.querySelector("svg");
  if (!svg || clean.querySelectorAll("*").length > 2048) {
    throw new WorkspaceError("invalid-image", "This SVG cannot be displayed safely.");
  }
  cleanPresentation(clean);
  for (const element of clean.querySelectorAll("*")) {
    for (const attribute of [...element.attributes]) {
      // Inline CSS has already been checked declaration by declaration.
      if (attribute.name === "style") {
        continue;
      }
      const unsafePresentation = presentationProperties.has(attribute.name)
        && !safePresentationValue(attribute.value);
      const nonlocalReference = /url\s*\(/iu.test(attribute.value)
        && !localReference.test(attribute.value);
      if (unsafePresentation || nonlocalReference) {
        element.removeAttribute(attribute.name);
      }
    }
  }

  const box = (svg.getAttribute("viewBox") || "").trim().split(/[\s,]+/u).map(Number);
  if (box.length === 4 && (!box.every(Number.isFinite)
    || box[2] <= 0 || box[3] <= 0 || box[2] > 10000 || box[3] > 10000)) {
    throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
  }
  /** Read a finite bounded SVG dimension or use the established safe fallback. */
  const dimension = (name, fallback) => {
    const value = svg.getAttribute(name);
    return value && /^\d+(?:\.\d+)?(?:px)?$/u.test(value) ? parseFloat(value) : fallback;
  };
  const width = dimension("width", box.length === 4 ? box[2] : 300);
  const height = dimension("height", box.length === 4 ? box[3] : 150);
  if (!Number.isFinite(width * height)
    || width <= 0 || height <= 0 || width > 10000 || height > 10000
    || width * height > limits.imagePixels) {
    throw new WorkspaceError("image-too-large", "This SVG exceeds the display limit.");
  }
  svg.setAttribute("width", String(width));
  svg.setAttribute("height", String(height));
  return {
    blob: new Blob([new XMLSerializer().serializeToString(svg)], { type: "image/svg+xml" }),
    pixels: Math.ceil(width * height)
  };
}
