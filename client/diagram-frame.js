/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; isolated Mermaid rendering from untrusted Markdown.
 * Human guidance: fix security findings affecting Linux browser users.
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import mermaid from "mermaid";
import { cleanDiagramSvg } from "./diagram-svg.js";

mermaid.startOnLoad = false;

/** Render in this frame's realm so image decoders, CSS and generated DOM all obey its CSP. */
async function renderRequest(event) {
  const port = event.ports[0];
  if (!port) {
    return;
  }

  const scratch = document.createElement("div");
  try {
    const { id, source, dark } = event.data;
    if (!/^diagram-[a-f0-9]{32}$/u.test(id) || typeof source !== "string" || source.length > 20000) {
      throw new Error("Invalid diagram request.");
    }
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
    document.body.append(scratch);
    const rendered = await mermaid.render(id, source, scratch);
    port.postMessage(cleanDiagramSvg(rendered.svg, id).outerHTML);
  } catch {
    port.postMessage(null);
  } finally {
    port.close();
    scratch.remove();
  }
}

/** Bind a private channel once. Navigating the frame invalidates the channel. */
function connect(event) {
  if (parent === window || event.source !== parent || event.origin !== new URL(location.href).origin ||
      event.data !== "markvibe-diagrams" || !event.ports[0]) {
    return;
  }
  window.removeEventListener("message", connect);
  event.ports[0].onmessage = renderRequest;
  event.ports[0].postMessage("ready");
}

window.addEventListener("message", connect);
