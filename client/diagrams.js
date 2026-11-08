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

import { textElement } from "./markdown.js";
import { pause, makeId, resourceUrl } from "./path.js";
import { cleanDiagramSvg } from "./diagram-svg.js";

let queue = Promise.resolve();
const supported = /^(?:flowchart|graph|sequenceDiagram|classDiagram|stateDiagram(?:-v2)?|erDiagram|timeline|mindmap|pie|gantt)\b/u;

/** Await a single reply and release the response port even if the isolated runtime fails to load. */
async function readReply(port, close = true) {
  let timer;
  try {
    return await new Promise((resolve, reject) => {
      timer = setTimeout(() => reject(new Error("The diagram renderer did not respond.")), 30000);
      port.onmessage = event => resolve(event.data);
      port.onmessageerror = () => reject(new Error("Invalid diagram response."));
    });
  } finally {
    clearTimeout(timer);
    port.onmessage = null;
    port.onmessageerror = null;
    if (close) {
      port.close();
    }
  }
}

/** Open one opaque, network-blocked renderer for a serial batch of reader or export diagrams. */
function createRenderer() {
  const frame = document.createElement("iframe");
  frame.setAttribute("sandbox", "allow-scripts");
  frame.setAttribute("aria-hidden", "true");
  frame.tabIndex = -1;
  frame.referrerPolicy = "no-referrer";
  frame.style.cssText = "position:absolute;left:-100000px;top:0;width:1024px;height:768px;visibility:hidden;";
  frame.src = resourceUrl("js/diagram-frame.html");

  const channel = new MessageChannel();
  const ready = readReply(channel.port1, false);
  frame.addEventListener("load", () => {
    // A sandboxed origin has no targetable origin string. Send only the setup port here.
    frame.contentWindow.postMessage("markvibe-diagrams", "*", [channel.port2]);
  }, { once: true });
  document.body.append(frame);

  return {
    /** Send document text only through the channel bound to the loaded frame. */
    async render(id, source, dark) {
      if (await ready !== "ready") {
        throw new Error("The isolated diagram renderer is unavailable.");
      }
      const response = new MessageChannel();
      const result = readReply(response.port1);
      channel.port1.postMessage({ id, source, dark }, [response.port2]);
      return await result;
    },
    /** Release the runtime and its communication channel when this batch finishes. */
    dispose() {
      channel.port1.close();
      frame.remove();
    }
  };
}

/** Render a bounded supported Mermaid diagram and sanitize its SVG before displaying it. */
async function renderOne(element, valid, appearance, renderer) {
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
    const dark = appearance ? appearance === "dark" : document.documentElement.dataset.theme === "dark";
    const id = "diagram-" + makeId().replaceAll("-", "");
    const markup = await renderer().render(id, source, dark);
    if (!valid()) {
      return;
    }
    const svg = cleanDiagramSvg(markup, id);
    svg.setAttribute("role", "img");
    svg.setAttribute("aria-label", "Diagram from " + source.split(/\r?\n/u)[0].slice(0, 120));
    const details = document.createElement("details");
    details.append(textElement("summary", "Diagram source"), textElement("pre", source));
    element.replaceChildren(svg, details);
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
    let renderer;
    /** Load the isolated runtime once, after the first supported fence passes its limits. */
    const getRenderer = () => renderer ??= createRenderer();
    try {
      for (const element of elements) {
        if (!valid()) {
          return;
        }
        await renderOne(element, valid, appearance, getRenderer);
      }
    } finally {
      renderer?.dispose();
    }
  };
  queue = queue.then(job, job);
  return queue;
}
