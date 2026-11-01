# Implementation status

MarkVibe provides a Windows WPF/WebView2 desktop app and a shared OpenSilver
WebAssembly browser app. Application code is MIT licensed by Umberto Giacobbi.

## Current capabilities

- Local Markdown files, folders and ZIPs, with relative links, local images and
  Mermaid diagrams; Read, read-only Source, Split and Map views.
- Workspace refresh, remembered reading position, native file-change prompts,
  themes, fonts, languages and saved shell layout.
- A bounded metadata/link catalog, document map search, arrangements, relation
  lenses, routes, previews, comparison, navigation trails and saved viewpoints.
- Schema-4 map layouts, with native atomic sidecar writes, connected-folder saves
  after explicit permission, and separate downloads for imported workspaces.
- Optional OpenAI API or Windows Codex CLI editing of a document or selection,
  explicit reference collection, cancellation, bounded local undo/redo and copy saving.
- A shared document revision for reading, source, metadata and export. AI replies
  do not overwrite workspace originals.
- Local current-page HTML/PDF export, native Windows pickers and direct PDF saving.
- Windows MSIX, MSI and portable packaging. Local delivery defaults to Release x64
  MSIX; see [packaging](windows-packaging.md).

Ink and sticky-note editing are unsupported. Existing annotation files remain
untouched. Workspace full-text search, an offline browser shell and dedicated
macOS/Linux wrappers are not implemented.

See [architecture](architecture.md) for ownership and data flow, and
[active work](../.github/tasks/todo.md) for remaining tasks. Tests and interactive
checks require a current explicit request. Microsoft Store submission and browser
publication require separate authorization.
