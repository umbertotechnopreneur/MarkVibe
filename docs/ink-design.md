> Historical design: pen, ink and sticky-note editing were removed from the app
> on 2026-10-11. This document describes the retired feature, not the current UI.

# Ink annotations and sidecars

Implemented for paragraph notes in the browser and Windows reader. Physical pen
hardware remains unverified; mouse and synthetic pen/pressure input were exercised
in WebView2. Markdown stays read-only.

## Draw and save

Choose **Annotate**, then **+ Handwritten note** below a paragraph. Select Pen,
Highlight, Erase, color and width. Undo/Redo restore recent edits. Dedicated writing
bands use logical coordinates and a white paper background in both themes.
Mouse and pen produce strokes; touch is ignored for drawing. While annotation mode
is enabled, writing bands capture gestures; scrolling remains available outside them.
Pressure and elapsed sample time are stored; pen width uses average stroke pressure.

Windows autosaves each completed change to a sidecar beside the document.
**Save ink** retries an unsaved write. **Export ink** saves a recovery copy through
an OS dialog. Browser providers download a sidecar with Save ink and keep at most
five session drafts in memory. Browser drafts are not durable across reload.
Save before navigating away; **Discard edits** explicitly reloads the disk sidecar.

## Contract and limits

`guide/setup.md` uses `guide/setup.md.ink.json`. Version 1 is defined by
`schemas/ink-sidecar.schema.json` and C# `InkCodec`:

- Workspace-relative document path and SHA-256 of the decoded source re-encoded as
  UTF-8, without newline normalization; the UTF-8 BOM is excluded.
- Block/heading identities, quote, prefix/suffix fields and source-line evidence.
- Logical canvas dimensions, explicit color/width/tool, coordinates, pressure and
  monotonic elapsed milliseconds.
- 1 MiB UTF-8, 128 annotations, 256 strokes per annotation and **20,000 total points**.
  Pointer capture limits a new stroke to 4,000 samples; coordinates must fit its canvas.

Unknown JSON fields, versions, invalid numbers/timing and excess budgets are rejected.
New notes use after-block placement. Imported overlay metadata is preserved and
displayed in a note band; true drawing over text is not implemented.
The synthetic demo sidecar deliberately has nonmatching anchors and remains unresolved.

## Changed documents and recovery

With an unchanged fingerprint, block identity and quote locate a note. With a changed
fingerprint, only a unique matching quote resolves an anchor; duplicate or missing
quotes remain unresolved. Prefix/suffix are retained but do not currently drive matching.
Unresolved counts are visible, and the original sidecar is preserved.

Changed-document ink is read-only. When every anchor resolves, **Use current document**
explicitly rebases metadata and saves it. If anchors remain unresolved, export the original
and reconcile it separately; no automatic migration or guessed placement is performed.

Before Windows commits, it rechecks the current Markdown fingerprint under a read lock
and compares the loaded sidecar revision. External edits cause a visible conflict without
overwriting the original. Export a backup, then explicitly discard/reload or refresh.
Temporary-file replacement protects writes; there is no cross-process compare-and-swap.

## Exports and remaining acceptance

HTML and PDF include resolved annotations as static SVG in the same paragraph bands.
Unresolved/unsupported annotations remain in their sidecar and are excluded from export.

Physical pressure devices, barrel/eraser buttons, palm rejection, multiple DPI scales,
accessibility, manual orphan reassignment, richer pressure rendering, overlay drawing,
schema migrations and durable browser drafts require further work.

