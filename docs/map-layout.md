# Document map layout

Each opened workspace keeps one `.markvibe-map.json` in its top-level folder.
The JSON stores logical card coordinates, color and icon under the document's
normalized relative path, with optional `atlas` presentation settings and saved
viewpoints. Markdown and its links remain read-only. The format is
versioned and described in `schemas/map-layout.schema.json`; C# also enforces path,
size, palette and coordinate bounds. Version **4** is the only supported format:
the root contains `schemaVersion`, `nodes` and optionally `atlas`, with no annotation collection.
Versions 1–3 and unknown fields are rejected without rewriting their original
files. There is no migration layer for these unreleased formats.

The map uses a pure black canvas in both application themes, with dark tinted
cards and illuminated connections. Each card has a nearly black base and two
subtle static radial gradients; the icon and title share one row. The selected card uses the application accent
for its border and an elevation-16 shadow with 48-pixel blur. Card appearance
colors remain independent of the selection accent. Annotation editing and its
note/ink schemas are retired. Their files are not imported or deleted.

Drag a card to arrange the graph. Connections follow the card while dragging;
releasing it commits the position. Hover gently enlarges a card; one click selects
and enlarges it further without opening the document. Double-click opens it.
Keyboard users select with Space and open with Enter. Drag the background
to pan, scroll the mouse wheel to zoom in or out around the pointer, and choose
**Fit** to frame the graph. Ctrl is not required. Zoom is held steady during an
active drag so its logical coordinates remain consistent.
Negative positions are supported. **Alt+arrow** moves a focused card by 20 logical
pixels; add Shift for 100 pixels. Escape cancels an unfinished drag.

Hover a connection to illuminate its curve and both endpoint cards. Its invisible
hit area stays 14 screen pixels wide while zooming. Connection endpoints follow
card magnification, movement and resizing.

Use the bottom-right grip to resize a desktop card. A focused grip accepts arrow
keys in 10-pixel steps, or Shift for 40 pixels; Escape cancels the preview. Width
is bounded to 180–780 and height to 120–600 logical pixels. The complete size
change commits on release. Every layout uses schema version 4; dimensions are
optional but must appear together when a card has been resized.
Compact views retain full-width cards with normal scrolling.

Right-click a card to open the frosted XAML card menu. It contains a live preview,
named color swatches with selection rings and a visible icon gallery. The active
color and icon are labeled; changes follow accepted managed map state. Its local
file actions match navigation, reader/source and AI collection menus: reveal in
Explorer, open a terminal in the containing folder, open a standalone browser
preview and delete the file after confirmation. Virtual workspaces omit local
shell/deletion actions. **Shift+F10** or the Context Menu key
also opens the picker. Escape, Close or
a click outside dismisses it. Color tints the card border/background and its icon.
On narrow or coarse-pointer devices the map remains a scrollable card list; the
saved graph positions are retained for the desktop canvas.

Deletion uses the shared frosted confirmation with the exact workspace-relative
path and a warning when a local draft will be discarded. Cancellation performs no
mutation. Approval is tied to the current UI session and the file's bytes/metadata;
Windows then receives a single-file Recycle Bin request. Folders are never deleted
from this menu. Catalog entries, retained drafts, map membership and AI collection
membership are refreshed after a completed operation. These new actions have been
compiled but their functional acceptance is reserved for the owner's manual tests.

The toolbar sits inside the black canvas, with search on the same row and a
localized **Type to search…** watermark. Captions compact before actions scroll
horizontally on narrow windows; search stays visible. The application view
selectors above it use rounded tabs with distinct selected and keyboard-focus states.

The first map opens in **Tree** when no saved layout exists. A valid saved layout
always keeps its card positions, sizes, colors and icons. Reset returns to the
initial Tree arrangement; no existing layout is silently rearranged.

Incoming and outgoing links appear in separate dark sections with direction
arrows, unique-target counts, document icons, readable titles and secondary paths.
Rows retain document navigation and the local file context menu.

The toolbar offers **Star**, **Circle**, **Grid**, **Tree** and **Folders**. Each uses the complete catalog,
independently of search, focus and the 120-card projection. The central document
has the most distinct connected documents, counting incoming and outgoing links
together. Repeated links, self-links, external URLs and missing targets do not
inflate its score. Ties prefer README/INDEX entry points, then shorter folder depth
and stable path order. Star groups cards along radial spokes; Circle surrounds
the center with a ring (multiple rings for large catalogs); Grid fills nearby rows
and columns. Star adds spokes when needed to stay within coordinate bounds.
Tree uses breadth-first levels beneath the strongest document in each connected
component. Cycles never duplicate cards; broad levels wrap into rows. Extremely
deep chains that exceed the coordinate contract fall back to a bounded grid in
breadth-first order. All arrangements leave room
between cards and preserve their colors and icons.

**Depth** is enabled initially and can be toggled independently of any arrangement.
Core calculates shortest connection distances from the selected document across
the complete catalog, before limiting visible cards. The renderer combines those
distances with logical distance from the selected card to distribute documents
across up to twelve recessed levels. Logarithmic spacing distinguishes cards with
equal graph distance and bounds far-card shrinking and opacity. Disconnected pages
start in the background and retain spatial variation. The selection uses 1.28×
magnification and stronger accent lighting. Every connection fades according to
its more distant endpoint, including connections from the selected card; hovering
a line restores full emphasis. Selection, hover and keyboard focus bring a card
forward; connections follow its rendered bounds throughout the 420 ms transition.
Clicking a card makes it the new depth reference. Completed arrangements and card
moves recompute spatial depth; pointer gestures retain stable magnification.
Depth changes neither saved positions nor the current document.
Compact list views stay full-size, and reduced-motion settings remove transitions.

**Reset all** opens a native confirmation before restoring card positions,
sizes, colors and icons. It rebuilds the active catalog in Tree layout, discards
orphaned card entries and leaves documents intact. It follows the same persistence
rules as any other layout change.

**Advanced view** shows a compact monospace strip below each card's content:
UTF-8 source size, estimated tokens, Unicode scalar characters, words, lines and
distinct linked document targets. Counts use the complete decoded Markdown,
including markup; unindexed metadata is explicitly unavailable. Words count
letter/digit groups, not language-specific linguistic segmentation. Tokens use
the app's UTF-8 byte/4 approximation, not a tokenizer or provider usage report.
Size and tokens occupy the primary row; secondary icon counters retain full
tooltips and accessible labels without clipping long captions. The appearance
selector shows each local icon beside its name, including the current selection.
No source text is sent to AI by showing these statistics.

New maps start in Tree arrangement. Existing saved coordinates remain unchanged
until an arrangement button is chosen. New pages in an established layout use
free grid slots without moving existing cards. Filtering and focusing retain
the same coordinates. Reconciliation uses the complete enumerated catalog,
including documents whose content has not been indexed, and removes entries for
absent paths. Existing cards retain their positions, colors, icons and sizes;
removed entries mark a loaded layout as changed so cleanup can be saved.
Filtered graph projections must never be used for this reconciliation. An
explicit arrangement is one managed edit and uses the existing save/download
workflow.

Cards fade in progressively from the layout center when opening or arranging the
map. The stagger is capped at one second for the last card, followed by its short
fade. Reduced-motion preferences disable entry effects. Narrow or coarse-pointer
devices retain a scrollable card list and normal scrolling instead of wheel zoom.

## Persistence

### Document atlas

The floating map chrome leaves the black canvas continuous behind its controls.
The camera reserves space beneath the toolbar when fitting the graph. Narrow
devices retain a normal card list; document preview can be opened as an overlay.

Zoom progressively changes card content without editing coordinates: below 40%
cards show their identifying header, at 40–72% their preview remains, and normal
detail includes the path and optional advanced statistics. At 105% or more, the
selected card can show up to three source headings. In the distant overview,
up to twelve non-overlapping screen-sized labels identify islands and important
documents; activating a document label selects and centers it through C#.

**Folders** retains the existing full-catalog arrangement and adds deterministic
island colors without replacing custom card colors. Each island has a count and
a keyboard-accessible expand/collapse button. Collapsing retains all document
coordinates and redirects existing external connections to the island boundary;
internal connections disappear until expansion. Island counts and frames use the
complete catalog/layout, even when not all cards fit the 120-card projection.

The exploration follow-up keeps the canvas opaque and card coordinates stable.
Resting cards use restrained surfaces and one-line excerpts; hover/selection
reveals more content, with advanced statistics restricted to the active card.

Selecting a document sends one short impulse along up to 24 adjacent links, from
source to target. Cyan dashed links enter the focal page; violet solid links
leave it. The relation lens uses hover/keyboard focus and rests on the selection.
The link button toggles emphasis. Effects finish after about one second, respect
reduced motion, and cancel when the session is suspended or the page is hidden.
They introduce no blur, SVG filters or continuous animation loop.

Ctrl/Cmd-click cards to toggle a selection of at most 12 documents. Shift-drag
empty canvas to select a rectangle; Ctrl/Cmd additionally retains the previous
selection. The checkmark tool enables selection without modifiers. Escape cancels
an unfinished rectangle. The shelf locates selected documents, compares plain
catalog previews side by side and adds the whole set to AI context. Batch reads
and the existing 16-document/4 MiB limits are validated before membership changes;
adding references sends no inference request.

Recent pages contains the last ten successful document visits. Chips center a
page without extending the trail; a subdued dotted line joins consecutive visits
only when both cards are visible. Preview selection and hover never add visits.

Attention highlights use the complete catalog before the 120-card projection:
missing Markdown targets, isolated documents and content changes observed during
this workspace session. Unindexed/failed documents suppress isolation claims until
the graph is complete. Changed means a fingerprint changed after observation or
a retained local revision is modified; it does not imply filesystem timestamps.
The counter states when indexing is incomplete. Facts are cached by metadata
revision; hover effects do not rebuild the catalog. Selection, comparison, lenses
and the visit trail are temporary workspace state and do not alter map sidecars.

**Distance** chooses all documents or one, two or three undirected links from
the selected page. Surrounding visible documents dim in place. Distances and
projection priority are calculated in C# over the complete catalog. Changing a
lens can change which documents fit the existing display limit; it never moves
their saved coordinates. Search expands a collapsed target folder before centering.

The document inspector shows the catalog introduction, up to twelve headings,
bordered icon actions and incoming/outgoing link sections. Both link sections
start collapsed and retain their expansion during inspector refreshes in the
current session. The distance selector has room for its localized captions and
shares its font size with the view-name input.

A single card click selects its preview. A second nearby click on the same
document opens it even if selection changed the underlying DOM target. Native
click counts retain the OS double-click interval; a 500 ms document/path fallback
handles changed targets. Completed drags, resizing, context menus and suspended
or replacement sessions clear the pending click. Enter remains an explicit open
action; Space selects. Compact document labels share this activation path.

Choose **Set route start**, then
select another document and choose **Find route to this document**. C# finds the
shortest route using incoming and outgoing connections and opens collapsed folders
on that route. Gold paths retain each original link's arrow direction. Unconnected
documents and routes exceeding the display limit have explicit messages; no
connection is inferred from text similarity. The inspector also lists route steps.

Hovering or focusing a connection displays its directed endpoints, source line
and a bounded source excerpt. Excerpts are plain text from the parsed source,
retained for up to 64 distinct link lines per document. Enter on a focused
connection opens its target. This does not send content to an AI provider.

The minimap represents the visible map projection and islands, with a camera
rectangle updated during pan/zoom. Click or drag to navigate; arrow keys pan and
Home fits. It is omitted from compact list views. Camera-only movement updates
existing minimap nodes rather than rebuilding the diagram.

Enter a **View name** and choose **Save view** to retain a logical camera center,
zoom, selected document, neighborhood depth and folder presentation. Up to eight
named views fit the optional `atlas.views` collection. Reusing a name replaces
that view, ignoring case; restore and delete apply to the selected saved view.
View names are bounded to 64 characters. Atlas fields receive strict validation
and use the same dirty/save/download/revision-conflict flow as card edits.
**Reset all** restores card defaults and clears the current lens/collapse state,
while preserving named viewpoints. Reduced motion disables entry, geometry and
camera transitions.

- **Windows local folders:** card moves and appearance changes autosave through
  contained, revision-checked atomic writes in `MarkVibe.Native`.
- **Connected browser folders:** folders still open with read access. Choose
  **Save layout** once to grant write access from that explicit click. Subsequent
  changes autosave while that permission remains granted. Reopening queries
  permission without requesting it during startup.
- **Imported folders, selected files, ZIPs and the sample workspace:** changes stay
  in memory until **Download layout** exports a copy. Keep it in the top-level
  folder and include it when reopening or importing. Windows uses its save dialog
  for this export.

The managed state serializes edits and the pointer adapter keeps unacknowledged
previews until C# accepts them. Navigation waits for pending edits. Unsaved changes
must be saved, downloaded or explicitly discarded before replacing a workspace.
Closing warns if edits or writes remain pending.

Opening a workspace suspends map interaction before draining the edit queue.
Provider replacement and layout loading share the managed map gate; navigation
starts after releasing it. State updates carry a layout-session identity and
generation so replies from an earlier opening cannot replace current previews.

Unsupported, oversized or malformed layouts are preserved. Externally changed
files fail the revision check rather than being overwritten. Download a separate
copy of your changes, then use **Discard changes** to reload the folder's layout.
Downloads for writable folders are backups and do not mark a failed autosave as
successful.

Revision checks detect external changes optimistically; neither provider implements
cross-process compare-and-swap. Browser downloads report that the download started,
not that the user completed the save dialog.

## Validation scope

The document inspector now uses a standard splitter whenever the canvas and
preview fit side by side. Its preferred width (220–600 px, initially 290 px) is
saved in the shell's `markvibe.layout` preferences and restored after restarting.
Window resizing temporarily constrains the visible width without overwriting the
saved preference. Narrow windows retain the explicitly opened preview overlay.

A subsequent owner-authorized Debug x64 0.1.0.16 build and local UI inspection
confirmed dragging the inspector from 290 to 400 px, saving that value and
restoring 400 px after terminating and restarting the desktop process. Broader
atlas behavior listed below remains outside that focused inspection.

Debug 0.1.0.18 also retains graph cards while sidebar navigation changes the
current document. Changed card metadata updates in place; a replacement workspace
session still receives a full presentation. Depth uses the equivalent centered
2D scale, with a consistently opaque black canvas. Local inspection with 24
synthetic linked documents confirmed retained card identity for sidebar and card
selection, with no repeated entrance animation during navigation after initial
presentation. This addresses the observed redraw path; it is not a GPU stress test.

The owner subsequently supplied a screenshot of gray native redraw corruption
after clicking cards, including shell regions. The 0.1.0.18 DOM inspection did
not establish that this was resolved. The follow-up uses a WPF-composed visible
browser, clips map painting to its opaque viewport, and removes SVG shadow
filters and the minimap's backdrop blur. Card borders, accent selection and route
strokes retain their existing roles; shared modal blur belongs to the dialog
surface. Native screen inspection must use a visible window at the actual DPI,
because a browser screenshot does not include the Windows presentation path.

The original atlas implementation received JavaScript/C# syntax, XAML/JSON
structure, localization-shape and source review. Its subsequent compilation is
covered by the Debug delivery above; automated suites, inference and publication
were not requested for this follow-up.
Suggested owner-requested acceptance: semantic zoom and readable overview labels;
island collapse/reopen with linked documents; full-catalog routes and truncation;
neighborhood stability; preview and heading navigation; minimap input; eight-view
save/replace/delete/restore across reopen; conflicting sidecar revisions; narrow
views, light/dark shell themes and reduced motion.

The 2026-10-16 visual, zoom and arrangement changes have now been included in a
Debug build; their full interaction acceptance remains open. Tests were not requested.
Suggested checks: native save/reopen and revision conflicts; pointer-anchored
wheel zoom, drag/click distinction, all three arrangements and tied/disconnected
graphs; keyboard controls, filter/focus stability, reduced motion and connected
folder write permission; narrow layouts, both application themes and touch scrolling.
