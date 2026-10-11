// Original, local SVG symbols shared with the OpenSilver shell.
const namespace = "http://www.w3.org/2000/svg";
export function iconName(label) {
  if (/zoom.*out|^−$/iu.test(label)) return "zoom-out";
  if (/zoom.*in|^\+$/iu.test(label)) return "zoom-in";
  if (/annotate|handwritten|^pen$/iu.test(label)) return "pen";
  if (/highlight/iu.test(label)) return "highlight";
  if (/erase/iu.test(label)) return "erase";
  if (/undo/iu.test(label)) return "undo";
  if (/redo/iu.test(label)) return "redo";
  if (/discard|close|cancel/iu.test(label)) return "close";
  if (/save/iu.test(label)) return "save";
  if (/export/iu.test(label)) return "download";
  if (/current document/iu.test(label)) return "review";
  if (/simple/iu.test(label)) return "simple";
  if (/editor/iu.test(label)) return "code";
  if (/copy/iu.test(label)) return "copy";
  if (/focus|all pages/iu.test(label)) return "focus";
  if (/fit/iu.test(label)) return "fit";
  return "file";
}
export function iconSvg(name) {
  const svg = document.createElementNS(namespace, "svg");
  svg.setAttribute("viewBox", "0 0 24 24"); svg.setAttribute("width", "18"); svg.setAttribute("height", "18");
  svg.setAttribute("aria-hidden", "true"); svg.setAttribute("focusable", "false"); svg.classList.add("mv-icon");
  svg.style.cssText = "display:inline-block;flex:none;vertical-align:middle;pointer-events:none;";
  const use = document.createElementNS(namespace, "use");
  use.setAttribute("href", new URL("icons.svg", document.baseURI).href + "#" + name);
  svg.append(use); return svg;
}
export function setButtonText(button, text, label = text) {
  const caption = document.createElement("span"); caption.textContent = text;
  button.replaceChildren(iconSvg(iconName(label)), caption);
  button.style.display = "inline-flex"; button.style.alignItems = "center"; button.style.gap = "6px";
  button.setAttribute("aria-label", label); button.title = label;
}
