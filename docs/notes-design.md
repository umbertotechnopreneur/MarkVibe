> Historical design: pen, ink and sticky-note editing were removed from the app
> on 2026-10-11. This document describes the retired feature, not the current UI.
> The remaining codec/schema and provider enumeration were removed on 2026-10-16.
> Map schema 4 has no notes collection. Names and schema paths below are historical;
> no legacy files are migrated or deleted.

# Reusable sticky notes

Status: implemented in source on 2026-10-11, with Astra review and static syntax,
schema and whitespace checks. No build, bundle, tests or running-app validation
were requested or performed for this change.

The graph and Markdown reader share `NotesView` and the existing appearance picker:
16 named colors, 24 local SVG icons, search and reset. A note has a plain-text title
and a separate Markdown body. Compact Bold, Italic, List and Link actions insert
Markdown; Preview renders through the managed parser and the existing HTML sanitizer.

## Using notes

In Map, choose **Add note**, then drag its **Move** handle to any logical position.
Cards and notes share zoom/pan coordinates, and **Fit** includes the notes. Dragging
a note does not pan the map or open a card. Narrow/coarse-pointer mode displays a
scrollable list while retaining desktop coordinates.

In Read, choose **Add note**, then click the source element to anchor it. Headings
at every level, paragraphs, images, lists/items, quotes, tables/cells, code and
diagrams use the same contract. Generated highlight spans and Mermaid SVG internals
resolve to their owning source block. The document itself is a target, including
when its Markdown is empty.

Use **Appearance** or right-click the note frame to choose color and icon. Text
fields keep their normal editing menu. Drag the
handle, or focus it and use arrows to move by 2 pixels, Shift+arrows by 20. Escape
cancels an unfinished drag or target selection. **Delete** removes a note; the
toolbar offers undo/redo with twenty retained edit snapshots. Title/body fields
remain dedicated to typing. Notes can coexist with paragraph ink without creating
strokes or selecting underlying text.

## Persistence and recovery

| Host | Coordinates | Sidecar |
| --- | --- | --- |
| Map | Absolute logical coordinates | `notes` in the root `.markvibe-map.json` |
| Markdown reader | Source element plus local offsets | `<full document path>.notes.json`, e.g. `guide/document.md.notes.json` |

Keeping the Markdown extension avoids collisions between similarly named `.md`
and `.markdown` documents. Source Markdown remains read-only.

Map version 1 remains supported and unchanged until a note mutation requires
version 2. Reader notes use their own version 1 schema. Both formats allow up to
128 notes and 1 MiB of UTF-8 JSON. Titles allow 128 characters, bodies 16 KiB UTF-8;
coordinates, dimensions, colors, icons and anchors are validated in C#. Rendered
HTML is derived, bounded state and never appears in a saved sidecar.

Native folders autosave with contained, atomic, revision-checked writes. Reader
writes also verify the current Markdown fingerprint while holding a document read
lock. Connected browser folders query existing permissions on opening; the direct
**Save notes** or **Save layout** click can request read/write access. Subsequent
changes autosave while granted. Browser note writers resolve existing nested
directories and recheck both document fingerprint and sidecar revision before
closing the temporary stream.

Imported folders/files/ZIPs and the demo retain changes in memory. **Save notes**
or **Download notes** exports the document sidecar; **Download layout** includes
map notes. Keep copies beside their Markdown document or in the workspace root,
respectively. Browser download initiation cannot establish that a save completed.
For writable folders a downloaded backup does not clear a failed autosave.

Invalid or unsupported sidecars are preserved. Externally modified files cause a
conflict and leave edits available for a separate copy. Explicit discard reloads
the file without first sending unsent text drafts to autosave. Navigation drains
drafts and acknowledgments; changing documents/workspaces requires persistence or
explicit discard. Closing warns about unsaved notes and pending operations.
Detached DOM editors remain retained until drained or their drafts are transferred
to a replacement editor for the same session. Clean inactive reader sessions are
capped at sixteen; provider replacement fences events with new session identities.

Revision checks are optimistic; there is no cross-process compare-and-swap. A new
browser leaf created before an aborted first write can remain empty; it is not
deleted automatically because another process may have changed it.

## Anchors

`MarkdownNoteTargets` derives target IDs, kind, source span, fragment hash, context,
short quote and canonical image path from the C# syntax tree. Only
`data-note-target-id` crosses into the rendered DOM. Existing ink block identities
remain unchanged.

Unchanged documents resolve exact identity and source evidence. Changed documents
require a unique exact fragment/type match, with context to disambiguate repeated
elements. Each anchor retains its own original document fingerprint when another
note is saved. Missing or ambiguous anchors remain visible in the recovery area;
**Reattach** lets the user choose a new target. No fuzzy or nearest-position match
silently relocates a note.

Image anchors can include normalized `u/v` attachment coordinates, so resizing
keeps the attachment point. DOM observers reposition notes after images, diagrams,
theme changes and responsive reflow. Raw page coordinates, CSS selectors,
temporary asset URLs and generated SVG IDs are never persisted as anchors.

## Export and validation

HTML and PDF use the same reconstruction from managed Markdown, ink and notes.
Notes render as colored boxes after their containing source block; document-level
notes follow the article, and unresolved notes have an explicit section. Titles
use text nodes, formatted bodies use the sanitizer, and icons become inert inline
SVG geometry. Export does not attempt to reproduce overlapping screen positions
across PDF page breaks.

Suggested requested validation: native/browser save and reopen, external changes,
permission denial/revocation, zoomed dragging, undo/redo, IME drafts and view changes,
responsive image anchors, duplicate fragments, explicit reattachment, ink isolation
and HTML/PDF output. These behaviors have source review, not runtime evidence.
