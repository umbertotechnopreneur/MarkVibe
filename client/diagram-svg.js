/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; validate static diagram SVG across the frame boundary.
 * Human guidance: fix security findings affecting Linux browser users.
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import DOMPurify from "dompurify";

const properties = new Set([
  "fill", "fill-opacity", "stroke", "stroke-width", "stroke-opacity", "stroke-dasharray", "stroke-dashoffset",
  "color", "opacity", "background-color", "font", "font-family", "font-size", "font-style", "font-weight",
  "font-variant", "line-height", "letter-spacing", "word-spacing", "text-anchor", "dominant-baseline", "alignment-baseline",
  "baseline-shift", "white-space", "display", "visibility", "vertical-align", "text-decoration", "text-transform", "text-overflow",
  "overflow", "overflow-wrap", "word-wrap", "word-break", "hyphens", "max-width", "width", "height", "rx", "ry",
  "shape-rendering", "paint-order"
]);
const localUrl = /url\(\s*["']?#[a-zA-Z0-9_-]+["']?\s*\)/giu;
const colorFunctions = new Set(["rgb", "rgba", "hsl", "hsla", "hwb", "oklab", "oklch", "lab", "lch", "color"]);
const geometryAttributes = new Set([
  "id", "class", "role", "aria-label", "aria-labelledby", "aria-describedby", "aria-roledescription",
  "viewBox", "preserveAspectRatio", "d", "points", "transform", "x", "y", "x1", "y1", "x2", "y2",
  "cx", "cy", "r", "dx", "dy", "refX", "refY", "orient", "markerWidth", "markerHeight", "markerUnits",
  "gradientUnits", "gradientTransform", "offset", "patternUnits", "patternContentUnits", "patternTransform",
  "clipPathUnits", "maskUnits", "maskContentUnits", "textLength", "lengthAdjust"
]);
const paintAttributes = new Set([
  ...properties, "marker-start", "marker-mid", "marker-end", "clip-path", "clip-rule", "mask", "fill-rule",
  "stop-color", "stop-opacity", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "vector-effect"
]);

/** Permit only static CSS values, color functions and fragment-only paint references. */
function safeValue(value) {
  const remaining = value.replace(localUrl, "");
  if (/[\\@<>]/u.test(remaining) || remaining.includes("/*")) {
    return false;
  }
  return [...remaining.matchAll(/([\w-]+)\s*\(/gu)]
    .every(match => colorFunctions.has(match[1].toLowerCase()));
}

/** Remove resource-bearing shorthand, custom properties and unsupported CSS functions. */
function cleanDeclarations(style) {
  for (const property of Array.from(style)) {
    if (!properties.has(property) || !safeValue(style.getPropertyValue(property))) {
      style.removeProperty(property);
    }
  }
}

/** Keep only static rules targeting the diagram's own root and its descendants. */
function cleanStyles(source, id) {
  if (source.length > 100000) {
    throw new Error("The diagram style exceeds the display limit.");
  }
  const sheet = new CSSStyleSheet();
  sheet.replaceSync(source);
  if (sheet.cssRules.length > 512) {
    throw new Error("The diagram style exceeds the display limit.");
  }
  const scopedRules = [];
  for (const rule of sheet.cssRules) {
    if (rule.type !== CSSRule.STYLE_RULE || rule.selectorText.length > 2000 ||
        /[<\\@]/u.test(rule.selectorText) || rule.selectorText.includes("/*")) {
      continue;
    }
    const root = "#" + id;
    const scoped = rule.selectorText.split(",").every(selector => {
      const trimmed = selector.trim();
      return trimmed === root || trimmed.startsWith(root + " ") && !/^[+~]/u.test(trimmed.slice(root.length).trimStart());
    });
    if (scoped) {
      cleanDeclarations(rule.style);
      if (rule.style.length) {
        // A CSSStyleRule may contain nested rules as well as declarations.
        // Only the declarations inspected above can cross into the reader or export.
        scopedRules.push(rule.selectorText + " { " + rule.style.cssText + " }");
      }
    }
  }
  return scopedRules.join("\n");
}

/** Parse in inert template content and admit one bounded static SVG before either realm uses it. */
export function cleanDiagramSvg(markup, id) {
  if (typeof markup !== "string" || markup.length > 2 * 1024 * 1024) {
    throw new Error("The diagram exceeds the display limit.");
  }
  const template = document.createElement("template");
  template.innerHTML = markup;
  const svg = template.content.firstElementChild;
  if (svg?.localName !== "svg" || svg.namespaceURI !== "http://www.w3.org/2000/svg" ||
      svg.id !== id || svg.querySelectorAll("*").length >= 4096) {
    throw new Error("Invalid diagram SVG.");
  }
  DOMPurify.sanitize(svg, {
    IN_PLACE: true,
    USE_PROFILES: { svg: true, svgFilters: false },
    FORBID_TAGS: ["script", "foreignObject", "a", "image", "use", "animate", "animateMotion", "animateTransform", "set"],
    FORBID_ATTR: ["href", "xlink:href", "xml:base"]
  });
  for (const node of [svg, ...svg.querySelectorAll("*")]) {
    for (const attribute of [...node.attributes]) {
      if (attribute.name === "style") {
        cleanDeclarations(node.style);
      } else if (!geometryAttributes.has(attribute.name) &&
          !(paintAttributes.has(attribute.name) && safeValue(attribute.value))) {
        node.removeAttributeNode(attribute);
      }
    }
    if (node.localName === "style") {
      node.textContent = cleanStyles(node.textContent, id);
    }
  }
  return svg;
}
