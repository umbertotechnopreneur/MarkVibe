# Settings and workspace resumption

Settings are managed by the C# `Preferences` record and the OpenSilver dialog.
JavaScript validates the persistence boundary and adapts browser storage, media
queries and library APIs. Earlier runtime evidence is recorded separately in
[validation](validation.md). The 2026-10-16 font, dialog and maintainability changes
were included in the Debug x64 build; interaction checks and test execution remain
separate. See the task archive for the current build and launch evidence.

## Available preferences

| Preference | Default | Behavior |
| --- | --- | --- |
| Appearance | System | Light/Dark override, or live system color-scheme changes |
| Language | System | Apply the system language or an available translation immediately |
| UI font | System default | Use a supported monospace family installed on Windows, without changing document/editor fonts |
| Acrylic | On | Request the native backdrop when available |
| Document presentation | Technical | Technical, Editorial or Compact reading |
| Light/dark document themes | MarkVibe | Select independent reader themes for the two appearances |
| Light/dark code themes | Automatic | Follow the selected reader theme or choose a code theme |
| Reading size | 100% | Toolbar zoom, clamped to 80–150% |
| Lightweight source | Off | Prefer the selectable read-only fallback over Monaco |
| Remember last workspace | On | Retain workspace identity, relative document path, anchor and source-based position |
| AI connection | OpenAI API key | Persist the radio choice between API key and Windows Codex CLI; no automatic fallback |

Reset preferences restores these defaults. The panel fits small viewports and scrolls internally. Preferences survive reload when browser storage is available. Storage failure keeps the app usable for that session and reports a message after a preference change.

Each AI connection has its own **Test** action and an explicit success/failure
dialog. API testing checks model-list access with the entered or saved key; a
successful candidate replaces the saved key. Codex CLI testing makes a short
synthetic request using the existing CLI login on Windows. Neither test sends an
open document. The CLI probe can consume account usage; its presence/login status
alone is not a remote test. See [AI connection details](ai-editing.md).

## Coordinated theme application

`App.xaml` installs the official `OpenSilver.Themes.Modern` theme through `Application.Theme`, as shown in the [OpenSilver documentation](https://doc.opensilver.net/documentation/how-to-topics/theme-handling.html). Its LightPalette and DarkPalette use MarkVibe's shell colors. Changing appearance selects `ModernTheme.CurrentPalette`, updates shell brushes, publishes CSS variables to DOM hosts, selects Monaco's `vs`/`vs-dark`, and renders Mermaid using the matching palette. The map and highlighted code inherit the same appearance. Existing source mappings preserve the reading location during presentation changes.

Control templates remain supplied by Modern Theme. App-specific styles adjust control properties. Selected sidebar text inherits the theme's selected foreground so contrast follows both palettes. Generation checks prevent older asynchronous appearance work from replacing newer preferences.

ComboBox, CheckBox and PasswordBox captions use the same mutable brushes as the
shell, including popup entries and validation errors. Changing appearance updates
already-open settings controls without recreating the dialog.

## Versioned local persistence

`localStorage["markvibe.preferences"]` stores `{ "version": 1, "values": { ... } }`. Only this envelope is supported; the earlier flat object is no longer migrated. Unknown values are discarded or normalized, and malformed JSON or unsupported versions recover to defaults. C# validates allowed appearance/style values and zoom limits. Browser validation remains a separate untrusted-storage boundary.

`markvibe.lastWorkspace` stores version, provider mode, display name and workspace identity. `markvibe.lastDocument` stores the history entry and reading position. Markdown content and assets are not stored in these entries. Connected directory handles use IndexedDB's existing `markvibe-folders` store.

`markvibe.layout` separately stores the AI pane's open state and width, folder-pane
visibility and width, source/preview ratio and map-inspector width. The AI pane
starts closed when no layout is saved. Standard splitter gestures save their
completed geometry; temporary window constraints do not replace the preferred
sizes. C# and the storage boundary validate the versioned layout envelope.

These settings are scoped to the browser origin/profile. Clearing site data, private browsing, quota pressure or using a different origin can remove them. Browser pickers do not reveal an absolute OS path; the remembered path is relative to the workspace. Windows uses a persistent WebView2 profile and a separate native session under `%LOCALAPPDATA%\MarkVibe\Windows`. Its session stores the user-selected root plus relative document/anchor/position, without Markdown content.

## Restart behavior

- The sample workspace reopens its last page, anchor and reading position automatically.
- Windows automatically reopens the last granted local folder/document. Missing or inaccessible paths present a reopening action. `--open` takes precedence over the remembered document.
- A remembered connected folder reopens automatically only when `queryPermission({mode:"read"})` is already granted. Startup does not call `requestPermission`. If permission is unavailable, the app shows the last page/path and an explicit reopening action.
- Selected files, imported folders and ZIPs show “Continue where you left off.” Choose the workspace again to regain access. A stable identity based on provider mode, name and normalized entry paths restores the selected document and position after that selection.
- A different selected workspace opens its own initial page. Imported identities cannot distinguish two different folders with the same name and identical entry paths; browsers do not provide their absolute paths.
- Disabling remembering removes saved document/workspace entries and the persisted directory handle. The current open workspace remains readable. Navigation no longer saves it; restarting returns to the sample. Resetting preferences enables remembering again.

The reader stores block identity, fraction and source line, rather than a percentage of total page height. Scroll, navigation, browser history, pagehide and hidden-page transitions retain the useful position. Imported document contents must still be reselected after a restart.

## Extending settings

Add a defaulted field to `Preferences`, validate it, bind a control in the existing dialog, and explicitly apply it to the relevant adapter. Extend the storage boundary's whitelist; an incompatible prerelease change can reject the older envelope rather than adding a migration layer. Keep persistence behind adapters. Current-page export buttons are actions, not persisted preferences: they preserve the selected reading style and use a light export palette. Native destinations are selected with OS save dialogs.

The supported UI families are defined once in
`src/MarkVibe.Core/Presentation/ui-fonts.json`, consumed by Core and the browser
boundary. Windows filters this catalog against installed families; no fonts are
installed or downloaded. Other browser hosts retain the system UI font. `UiTree`
shares traversal of declared controls, including UserControl content, while
typography leaves document DOM and theme glyph internals alone. Explicit raw
markers prevent document titles and folder names from being treated as UI labels
when the language changes.

Interface body text uses Semilight (350); headings and selected controls use
Medium (500). Applying preferences repeatedly retains these weights. The declared
UI traversal preserves caption bindings and leaves document/editor font weights
under their own presentation settings.

## OpenAI connection

The key card displays saved/unavailable status and accepts a masked replacement.
Leaving the field blank keeps the saved key. **Validate and save** verifies a
typed candidate against the model-list endpoint before replacing the stored key;
the check sends no documents and generates no text. Windows stores the accepted
key in the user environment; the browser discloses its unencrypted localStorage.
Normal startup automatically loads the model list when a saved key is configured.
See [security boundaries](security.md) for that network behavior and the isolated
native smoke profile, which does not read credentials or call OpenAI.

OpenAI uses an ordinary flat settings section, with a masked key field, plain
connection status and inline validation feedback. It has no enclosing card or
status badge. The fields and text align with the other preference sections.

Settings, About and confirmation dialogs use a shared frosted surface with a
300-millisecond blur transition on the full-window modal backdrop. Styling targets
the stable `DialogBackdrop` template element because OpenSilver replaces classes
assigned during initial Loaded callbacks. Its nested native `Overlay` provides
the tint; the portal stays opaque and unfiltered. The content surface animates
separately and remains sharp. Reduced-motion preferences skip the delayed close.
Local inspection of Debug x64 0.1.0.16 confirmed 16 px blur while Settings remained
open and live caption colors through light/dark/light changes. This is focused UI
inspection, not full application acceptance or an automated test-suite result.
