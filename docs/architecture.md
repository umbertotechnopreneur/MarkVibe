# Architecture

## Runtime and application model

MarkVibe uses OpenSilver's DOM-based C#/XAML application model. The Browser project is a .NET 10 Blazor WebAssembly host whose root component invokes OpenSilver.WebAssembly.Runner. The OpenSilver app provides the window layout, controls, commands and state.

OpenSilver 3.3.3 is used with its matching WebAssembly runtime and Modern Theme 3.3.0. The application library and host target net10.0. The solution was compiled locally with SDK 10.0.401 on 2026-10-11 with zero warnings/errors; Edge browser checks passed at root and a development subpath.

MarkdownView, MonacoEditorView and DocumentMapView derive from OpenSilver's HtmlPresenter through DomHost. HtmlPresenter owns a stable tagged inner element; after Loaded, the adapter waits for that element to be connected before creating its scoped shadow root. Collapsed hosts attach on demand. This avoids attaching to the disconnected placeholders that GetDiv returned in the original implementation. The application uses DOM-based C#/XAML layout.

## Responsibilities

The Windows WPF/WebView2 host serves a staged copy of the same browser app through
a fixed HTTPS virtual origin. A versioned, origin-checked message bridge invokes
`MarkVibe.Native` for granted-root reads, remembered session and revision-safe ink
writes. Native OS dialogs select files/folders and export destinations; a separate
script-disabled WebView2 generates clean PDFs. No HTTP document server is involved.

Ink state, validation, anchors, reconciliation and undo/redo live in `MarkVibe.Core`.
The pointer adapter previews a bounded stroke, then submits samples to C# and renders
the validated result as SVG. Windows autosaves; browser providers download sidecars.

| C# | Browser adapters |
| --- | --- |
| Workspace contracts and capabilities | File/handle pickers and directory traversal |
| Path and URL policy | File objects, permission checks and IndexedDB handle persistence |
| One Markdig AST per document parse | Final DOMPurify sanitation and DOM insertion |
| Titles, summaries, headings and source ranges | Code highlighting, Mermaid and Monaco |
| Catalog, bounded search and actual-link map | Image decoding checks and blob URL ownership |
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
5. Render the same AST into block-wrapped HTML. Source view receives the original source.
6. Sanitize HTML in the browser and install only generated, validated link/asset metadata.
7. Resolve workspace images lazily to bounded blob URLs; render fenced Mermaid in a serialized, strict pipeline.
8. Build map cards and edges from the indexed C# link records.

The index stores bounded source text for case-insensitive content search. Its 8 MiB source-byte budget can make the index partial. Unindexed pages remain available by path; the UI reports the partial state. The map also grows as indexing completes and reports truncated projections.

Current-page exports reuse that model through a lazy adapter. The shared HTML boundary and code highlighter are also used by the reader. Export reads all supported images, including those below the fold, through the provider's image validator without duplicating reader Blob leases. It renders Mermaid in a light palette, embeds validated assets/CSS, retains same-page anchors and disables links to Markdown pages outside the exported file. HTML is downloaded locally; PDF opens a separate script-free document in the browser print dialog. See [export and native hosts](desktop-and-export.md).

## Interaction and lifecycle

The desktop shell has a resizable sidebar and Split divider. A narrow screen shows the sidebar or content at full width and offers Read, Source and Map. Split is offered only when width is at least 1,050 pixels.

Each DOM host retains its identity. Monaco imports only on demand, caches at most three source models, restores view state and uses a local module worker. Small/coarse-pointer devices prefer the lightweight source view. That view uses text nodes, line numbers, find and copy; unusually high line counts use a read-only textarea.

Reading positions use stable content-derived block IDs, a fractional offset within the block and a fractional source line. Split synchronization converts through source lines and ranges, with a short reentrancy lock. It does not equate percentages of different pane heights.

Browser history entries include a workspace id, document path, anchor and position. Positions are captured before navigation, on scroll and before the page hides. Connected handles retain a stable id and can resume automatically only with already granted read permission. Selected files, imports and ZIP contents are session selections; they must be chosen again after restart. Their stable provider/name/path-list identity restores the last page after reselection, while a saved descriptor displays the pending workspace. Absolute OS paths are not exposed by browser pickers. See [settings and resumption](settings.md).

Only two workspace contexts and three selected Markdown models per context are retained. Eviction releases providers, asset URLs and editor models. New navigation and indexing cancel their predecessors. DOM hosts dispose observers and editor instances on unload and replay bounded current-view snapshots on reattachment.

Browser ink retains up to five in-memory sessions; edits must be saved or explicitly
discarded before changing pages. Windows checks document/sidecar revisions before
each atomic write. Unsupported or unresolved annotations remain preserved originals.

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
