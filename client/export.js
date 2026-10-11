import { prepareMarkdown, highlightCode, textElement } from "./markdown.js";
import { readExportImage } from "./workspaces.js";
import { WorkspaceError, checkCancelled, limits, resourceUrl } from "./path.js";
import { appendExportInk } from "./ink.js";
import { isDesktop, nativeCall } from "./native.js";

const downloads = new Set(), printFrames = new Map();
const maximumBytes = 32 * 1048576;
const exportCss = `
html{height:auto;background:#fff;color:#20283a;--panel:#fff;--text:#20283a;--muted:#596579;--line:#dde2ea;--accent:#525ce8;--accent-soft:#eceefe;--code:#f4f5f7;--reading-scale:1;color-scheme:light}
body{margin:0;padding:24px;background:#fff}
.document-meta{padding:16px 0 0;max-width:746px}
.document-meta .eyebrow{white-space:normal;overflow-wrap:anywhere}
html .markdown{padding:24px 0 48px;max-width:746px}
.markdown pre,.mv-diagram{overflow:visible}
.markdown pre code{white-space:pre-wrap;overflow-wrap:anywhere}
.markdown table{display:table;table-layout:fixed;overflow:visible}
.markdown th,.markdown td{overflow-wrap:anywhere}
.markdown img{max-height:820px;object-fit:contain}
@page{size:A4;margin:16mm;@bottom-right{content:counter(page);font:9pt "Segoe UI",sans-serif;color:#596579}}
@media print{
 body{padding:0}
 .document-meta{padding:0;max-width:none;font-size:8pt}
 html .markdown{padding:16px 0 0;max-width:none;font-size:11pt;line-height:1.6}
 html[data-document-style=editorial] .markdown{font-size:12pt;line-height:1.65}
 html[data-document-style=compact] .markdown{font-size:10pt;line-height:1.5}
 .markdown h1{font-size:26pt}.markdown h2{font-size:18pt}.markdown h3{font-size:13pt}
 .markdown h1,.markdown h2,.markdown h3,.markdown h4{break-after:avoid}
 .markdown p{orphans:3;widows:3}
 .markdown pre,.mv-diagram,.missing-resource{break-inside:avoid}
 .markdown img,.mv-diagram svg{max-height:210mm;break-inside:avoid}
 .markdown thead{display:table-header-group}.markdown tr{break-inside:avoid}
 *{-webkit-print-color-adjust:exact;print-color-adjust:exact}
}`;

function asDataUrl(blob, signal) {
  return new Promise((resolve, reject) => {
    checkCancelled(signal);
    const reader = new FileReader();
    const abort = () => reader.abort();
    const finish = (callback, value) => { signal?.removeEventListener("abort", abort); callback(value); };
    reader.onload = () => finish(resolve, reader.result);
    reader.onerror = () => finish(reject, new WorkspaceError("image-unavailable", "This image could not be exported."));
    reader.onabort = () => finish(reject, new WorkspaceError("cancelled", "Action cancelled."));
    signal?.addEventListener("abort", abort, { once: true });
    reader.readAsDataURL(blob);
  });
}

async function prepareDocument(model, workspaceId, preferences, signal, ink) {
  checkCancelled(signal);
  const output = document.implementation.createHTMLDocument(model.title);
  output.documentElement.lang = "en";
  output.documentElement.dataset.documentStyle = ["technical", "editorial", "compact"].includes(preferences.documentStyle) ? preferences.documentStyle : "technical";
  const charset = output.createElement("meta"); charset.setAttribute("charset", "utf-8");
  const viewport = output.createElement("meta"); viewport.name = "viewport"; viewport.content = "width=device-width, initial-scale=1";
  const policy = output.createElement("meta"); policy.httpEquiv = "Content-Security-Policy";
  policy.content = "default-src 'none'; img-src data:; style-src 'unsafe-inline'; font-src data:; script-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'";
  output.head.prepend(charset, viewport, policy);
  const response = await fetch(resourceUrl("css/content.css"), { signal, credentials: "same-origin" });
  if (!response.ok) throw new WorkspaceError("export-style-unavailable", "The document style could not be loaded. Reload the app and try again.");
  const stylesheet = await response.text();
  if (stylesheet.length > 100000) throw new WorkspaceError("export-style-unavailable", "The document style exceeds the export limit.");
  const style = output.createElement("style");
  style.textContent = stylesheet.replace(/:host\(([^)]+)\)/gu, "html$1").replaceAll(":host", "html") + exportCss;
  output.head.append(style);
  const header = output.createElement("header"); header.className = "document-meta";
  header.append(textElement("span", model.path, "eyebrow"), textElement("span", "MarkVibe"));
  const article = output.createElement("article"); article.className = "markdown";
  article.append(prepareMarkdown(model)); output.body.append(header, article); highlightCode(article);
  let warnings = 0, bytes = 0, pixels = 0;
  const images = [...article.querySelectorAll("img")];
  if (images.length > 96) throw new WorkspaceError("export-too-large", "This document exceeds the export limit of 96 images.");
  for (const image of images) {
    checkCancelled(signal);
    try {
      if (!image.dataset.mvAsset) throw new WorkspaceError("blocked-image", "Remote or unsafe image not included.");
      const asset = await readExportImage(workspaceId, image.dataset.mvAsset, signal);
      bytes += asset.blob.size; pixels += asset.pixels;
      if (bytes > 20 * 1048576 || pixels > limits.imagePixelBudget)
        throw new WorkspaceError("export-too-large", "This document contains too many large images to export. Export a smaller page.");
      image.src = await asDataUrl(asset.blob, signal);
      image.loading = "eager"; image.decoding = "sync";
    } catch (error) {
      checkCancelled(signal);
      if (error.code === "export-too-large") throw error;
      warnings++;
      const notice = output.createElement("figure"); notice.className = "missing-resource";
      notice.append(textElement("strong", image.alt || "Image unavailable"), textElement("span", error.message || "This image could not be included."));
      if (image.dataset.mvAsset) notice.append(textElement("code", image.dataset.mvAsset));
      image.replaceWith(notice);
    }
  }
  const diagrams = [...article.querySelectorAll(".mv-diagram")];
  if (diagrams.length) {
    const { renderDiagrams } = await import("./diagrams.js");
    await renderDiagrams(diagrams, () => !signal?.aborted, "light");
    checkCancelled(signal);
    for (const diagram of diagrams) {
      if (diagram.querySelector("svg")) diagram.querySelector("details")?.remove();
      else warnings++;
    }
  }
  for (const link of article.querySelectorAll("a[data-mv-path]")) {
    if (link.dataset.mvPath === model.path) link.setAttribute("href", "#" + encodeURIComponent(link.dataset.mvAnchor || ""));
    else {
      link.removeAttribute("href"); link.setAttribute("aria-disabled", "true");
      link.title = "This page is not included in the exported document: " + link.dataset.mvPath;
    }
  }
  appendExportInk(article, ink);
  for (const node of article.querySelectorAll("*")) {
    for (const attribute of [...node.attributes]) if (attribute.name.startsWith("data-") || attribute.name === "tabindex") node.removeAttribute(attribute.name);
  }
  checkCancelled(signal);
  const html = "<!doctype html>\n" + output.documentElement.outerHTML;
  if (new TextEncoder().encode(html).length > maximumBytes)
    throw new WorkspaceError("export-too-large", "This document exceeds the 32 MiB export limit. Export a smaller page.");
  const stem = model.path.split("/").at(-1).replace(/\.(md|markdown|mdown)$/iu, "").replace(/[<>:"/\\|?*\u0000-\u001f]/gu, "_").slice(0, 160) || "document";
  return { html, stem, warnings };
}

function downloadHtml(html, filename) {
  const url = URL.createObjectURL(new Blob([html], { type: "text/html;charset=utf-8" })); downloads.add(url);
  const link = document.createElement("a"); link.href = url; link.download = filename;
  document.body.append(link); link.click(); link.remove();
  setTimeout(() => { URL.revokeObjectURL(url); downloads.delete(url); }, 10000);
}

async function printDocument(html, signal) {
  // A separate, script-free document keeps OpenSilver and editor chrome out of print.
  const frame = document.createElement("iframe");
  frame.className = "mv-print-document"; frame.title = "Document print preview";
  frame.setAttribute("aria-hidden", "true"); frame.tabIndex = -1;
  frame.setAttribute("sandbox", "allow-same-origin allow-modals");
  frame.style.cssText = "position:fixed;left:-100000px;top:0;width:960px;height:900px;border:0;";
  let timer;
  const cleanup = () => { clearTimeout(timer); signal?.removeEventListener("abort", cleanup); frame.remove(); printFrames.delete(frame); };
  printFrames.set(frame, cleanup); signal?.addEventListener("abort", cleanup, { once: true });
  try {
    await new Promise((resolve, reject) => {
      const abort = () => { clearTimeout(timeout); reject(new WorkspaceError("cancelled", "Action cancelled.")); };
      const timeout = setTimeout(() => { signal?.removeEventListener("abort", abort); reject(new WorkspaceError("print-unavailable", "The print preview could not be prepared. Try exporting HTML instead.")); }, 15000);
      frame.onload = () => { clearTimeout(timeout); signal?.removeEventListener("abort", abort); resolve(); };
      signal?.addEventListener("abort", abort, { once: true });
      frame.srcdoc = html; document.body.append(frame);
    });
    checkCancelled(signal);
    const printWindow = frame.contentWindow;
    await Promise.race([
      Promise.all([printWindow.document.fonts.ready, ...[...printWindow.document.images].map(image => image.decode().catch(() => {}))]),
      new Promise(resolve => setTimeout(resolve, 5000))
    ]);
    checkCancelled(signal);
    printWindow.addEventListener("afterprint", cleanup, { once: true });
    timer = setTimeout(cleanup, 60000);
    printWindow.focus(); printWindow.print();
  } catch (error) { cleanup(); throw error; }
}

export async function exportDocument({ document: model, workspaceId, preferences, format, ink }, signal) {
  if (!["html", "pdf"].includes(format)) throw new WorkspaceError("unknown-export", "Choose HTML or PDF to export this page.");
  const { html, stem, warnings } = await prepareDocument(model, workspaceId, preferences, signal, ink);
  checkCancelled(signal);
  if (isDesktop()) {
    const result = await nativeCall("export.save", { html, filename: stem + "." + format, format }, signal);
    return { ...result, warnings };
  }
  if (format === "html") downloadHtml(html, stem + ".html");
  else await printDocument(html, signal);
  return { filename: stem + "." + format, warnings };
}

export function disposeExports() {
  for (const url of downloads) URL.revokeObjectURL(url); downloads.clear();
  for (const cleanup of printFrames.values()) cleanup(); printFrames.clear();
}
