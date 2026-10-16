# Architecture

## Runtime and application model

MarkVibe uses OpenSilver's DOM-based C#/XAML application model. The Browser project is a .NET 10 Blazor WebAssembly host whose root component invokes OpenSilver.WebAssembly.Runner. The OpenSilver app provides the window layout, controls, commands and state.

OpenSilver 3.3.3 is used with its matching WebAssembly runtime and Modern Theme 3.3.0. The application library and host target net10.0; the repository pins SDK 10.0.402. Earlier builds and browser checks are historical evidence in [validation](validation.md). The 2026-10-16 model and maintainability changes received source/static review only, with no build or test execution for that work.

Application controls are declared in OpenSilver XAML: shell, navigation tree,
settings, About, source/map toolbars, AI composer, model Popup, review, usage
Expander and waiting overlay. `VectorIcons.xaml`, `Composer.xaml`, `FocusLayout.xaml`
and the other resource dictionaries own their reusable presentation.

`MarkdownView`, `SourceRenderer` and `MapRenderer` are the only HtmlPresenter
adapters. They contain specialized document/editor/graph rendering; they do not
create application toolbars, model menus, prompt inputs, note editors or dialogs.
`MonacoEditorView` and `DocumentMapView` are XAML UserControls surrounding those
adapters. No Vue framework is used.

## Responsibilities

The Windows WPF/WebView2 host serves a staged copy of the same browser app through
a fixed HTTPS virtual origin. A versioned, origin-checked message bridge invokes
`MarkVibe.Native` for granted-root reads, remembered session and revision-safe map
writes. Native OS dialogs select files/folders and export destinations; a separate
script-disabled WebView2 generates clean PDFs. No HTTP document server is involved.

Pen, ink annotation and sticky-note editing paths were removed on 2026-10-11.
The application does not enumerate, read or write their document sidecars.
Existing files remain untouched. Map schema 4 contains document cards and optional
validated atlas presentation settings (neighborhood, collapsed folders and up to
eight named camera viewpoints), without source edits or annotations;
older map versions and embedded note collections are unsupported and are not
silently migrated or overwritten.

| C# | Browser adapters |
| --- | --- |
| Workspace contracts and capabilities | File/handle pickers and directory traversal |
| Path and URL policy | File objects, permission checks and IndexedDB handle persistence |
| One Markdig AST per document parse | Final DOMPurify sanitation and DOM insertion |
| Titles, summaries, headings and source ranges | Code highlighting, Mermaid and Monaco |
| Catalog, document revisions, statistics and actual-link map | Image decoding checks and blob URL ownership |
| Navigation history and current view | Browser history events and native scroll positions |
| Versioned preferences, Modern Theme palette and shell commands | Browser persistence, CSS themes, editor theme and split-scroll events |
| Current document, export command and cancellation | Standalone HTML assembly, bounded embedded assets and document-only print frame |

IWorkspaceProvider is the stable C# abstraction. BrowserWorkspaceProvider implements it through a typed JSON bridge with operation names. The JavaScript provider registry implements connected, imported, selected-file, ZIP and demo modes behind the same enumeration, text, asset, refresh and disposal behavior.

No operation name or script is taken from Markdown or sidecar contents. Browser events call a single JSInvokable entry point and are routed to C# commands.

## Document pipeline

1. Enumerate relative paths and metadata without loading all document/image contents.
2. Open the first page immediately, then incrementally index pages with cancellation.
3. Parse bounded Markdown in Markdig with raw HTML disabled.
4. Generate duplicate-safe Unicode heading IDs, block IDs, source ranges and resolved link records from that AST.
5. Render the same AST into block-wrapped HTML. Source view receives the same effective source revision.
6. Sanitize HTML in the browser and install only generated, validated link/asset metadata.
7. Resolve workspace images lazily to bounded blob URLs; render fenced Mermaid in a serialized, strict pipeline.
8. Build map cards and edges from the indexed C# link records.

The index retains document metadata, statistics and resolved links, not a second
copy of source text for search. Its 8 MiB source-byte budget can make indexing
partial; unindexed pages remain available by path. There is no workspace-wide
full-text search control or dormant `SearchText` contract. Map search matches
catalog titles and relative paths, selects a result and animates the graph camera
to that card. It does not retain document bodies for searching. Find in the Source
view searches only its current document. Map relationships and metadata fill in as
indexing progresses, and the map reports truncated projections.

`DocumentCatalog` owns edited `DocumentSession` objects independently of its
three-model parsed cache. A session keeps original, current and last-exported
source with a monotonically increasing revision. Its state is `Original`,
`Unsaved` or `Exported`; exporting a copy does not change the workspace original.
Undo and redo each retain at most 16 entries and 4,194,304 UTF-16 code units.
Each catalog retains at most 16 edited sessions. Opening, indexing, reader/source
rendering, map metadata and export all resolve the effective revision from this
owner, so parsed-cache eviction cannot silently revert an edited document.
Refreshing a workspace retains an unsaved session even if its backing file has
disappeared, keeping that revision available for explicit copy saving.

Map layout, validation and the fixed palette/icon catalog live in `MarkVibe.Core`.
The DOM adapter previews card dragging and emits position/appearance edits to the
managed session. Each workspace stores a root-level `.markvibe-map.json`; native
writes are atomic and revision-checked, while connected browser folders enable
autosave through an explicit write-permission click. Imports export a JSON copy.
See [document map layout](map-layout.md) for interaction and recovery details.

Folder grouping is another managed layout preset, alongside Star, Circle and Grid.
The renderer draws folder frames and animates camera movement; C# owns the catalog,
search result selection and arrangement coordinates.

Each workspace context owns an `AiDocumentCollection` containing at most 16
canonical Markdown paths, without another source-text cache. The AI panel resolves
metadata for local token estimates and resolves effective source revisions when
the user sends. The current editing target is excluded from reference documents.
References are individually bounded to 1 MiB UTF-8 and collectively to 4 MiB.
They enter the request as untrusted, read-only user messages before the target;
both token-count preflight and generation use the same ordered input. Local
collection statistics require no network calls. Only the captured target can be
replaced by an accepted edit response.

Current-page exports reuse that model through a lazy adapter. The shared HTML boundary and code highlighter are also used by the reader. Export reads all supported images, including those below the fold, through the provider's image validator without duplicating reader Blob leases. It renders Mermaid in a light palette, embeds validated assets/CSS, retains same-page anchors and disables links to Markdown pages outside the exported file. HTML is downloaded locally; PDF opens a separate script-free document in the browser print dialog. See [export and native hosts](desktop-and-export.md).

## Interaction and lifecycle

The desktop shell has a resizable sidebar and Split divider. A narrow screen shows the sidebar or content at full width and offers Read, Source and Map. Split is offered only when width is at least 1,050 pixels.

Each DOM host retains its identity. Monaco imports only on demand, caches at most three source models, restores view state and uses a local module worker. Small/coarse-pointer devices prefer the lightweight source view. That view uses text nodes, line numbers, find and copy; unusually high line counts use a read-only textarea.

Reading positions use stable content-derived block IDs, a fractional offset within the block and a fractional source line. Split synchronization converts through source lines and ranges, with a short reentrancy lock. It does not equate percentages of different pane heights.

Browser history entries include a workspace id, document path, anchor and position. Positions are captured before navigation, on scroll and before the page hides. Connected handles retain a stable id and can resume automatically only with already granted read permission. Selected files, imports and ZIP contents are session selections; they must be chosen again after restart. Their stable provider/name/path-list identity restores the last page after reselection, while a saved descriptor displays the pending workspace. Absolute OS paths are not exposed by browser pickers. See [settings and resumption](settings.md).

Only two workspace contexts and three parsed Markdown models per context are retained, with edited-session retention bounded separately as described above. Workspace eviction releases providers, asset URLs and editor models. New navigation and indexing cancel their predecessors. DOM hosts dispose observers and editor instances on unload and replay bounded current-view snapshots on reattachment.

## Static assets and hosting

Client libraries are bundled locally with esbuild, with dynamic chunks for Monaco and Mermaid. Assets use document.baseURI. Bootstrap imports the client adapter, loads libs/OpenSilver.js, and starts _framework/blazor.webassembly.js.

The production runtime needs only static files. A development server is a local convenience. No production ASP.NET Core document API, document upload endpoint, analytics endpoint, database service or remote CDN is part of this source architecture.

Root and subdirectory hosting both require a trailing-slash directory URL. Hash-based document state leaves static resource URLs intact. An offline application shell/service worker is future work.

## Compatibility evidence

- [OpenSilver.WebAssembly 3.3.3 package metadata](https://www.nuget.org/packages/OpenSilver.WebAssembly/3.3.3)
- [Official OpenSilver Browser bootstrap sample](https://github.com/OpenSilver/OpenSilver.Samples.Showcase/tree/develop/src/OpenSilver.Showcase.Browser)
- [OpenSilver DOM interop API](https://doc.opensilver.net/reference/OpenSilver.Interop.html)
- [OpenSilver HtmlPresenter](https://doc.opensilver.net/documentation/in-depth-topics/html-presenter.html)
- [OpenSilver Modern Theme handling](https://doc.opensilver.net/documentation/how-to-topics/theme-handling.html)
- [Markdig 1.4.0](https://www.nuget.org/packages/Markdig/1.4.0)

The browser adapters use native file selectors to keep File objects lazy and preserve imported relative paths. OpenSilver's documented OpenFileDialog.ShowDialogAsync is a potential single-file integration point, but this project does not rely on it for directory path preservation.

## XAML and renderer boundary

Control events call managed C# owners directly. The owner performs selection,
request, cancellation, history and saving checks. Browser calls implement only
renderer operations or platform adapters. `ai.state` keeps a nonvisual close guard;
it does not create a DOM view. Prompt drafts update that guard synchronously.

`Backdrop.BlurRadius` is a XAML attached material property. Its rendering hook
sets CSS on the element OpenSilver has already created; it creates no controls.
Flyouts and dialogs reuse the system-accent elevation resource and a 16 px blur.

`UiTree` shares enumeration of declared children among typography, localization
and icon decoration. It includes OpenSilver `UserControl.Content`, popup content
and item headers. Typography does not traverse control-template internals or DOM
document content. Explicit raw-content markers keep document titles, folder names,
tooltips and accessible labels out of UI translation. The supported UI font list
is one shared JSON catalog; Windows exposes only its installed subset.
