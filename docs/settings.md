# Settings and workspace resumption

Implemented and locally exercised on 2026-10-11. Settings are managed by the C# `Preferences` record and the OpenSilver panel. JavaScript adapts browser storage, media queries and library APIs; it does not own a second application model.

## Available preferences

| Preference | Default | Behavior |
| --- | --- | --- |
| Appearance | System | Light/Dark override, or live system color-scheme changes |
| Document presentation | Technical | Technical, Editorial or Compact reading |
| Reading size | 100% | Toolbar zoom, clamped to 80–150% |
| Lightweight source | Off | Prefer the selectable read-only fallback over Monaco |
| Remember last workspace | On | Retain workspace identity, relative document path, anchor and source-based position |

Reset preferences restores these defaults. The panel fits small viewports and scrolls internally. Preferences survive reload when browser storage is available. Storage failure keeps the app usable for that session and reports a message after a preference change.

## Coordinated theme application

`App.xaml` installs the official `OpenSilver.Themes.Modern` theme through `Application.Theme`, as shown in the [OpenSilver documentation](https://doc.opensilver.net/documentation/how-to-topics/theme-handling.html). Its LightPalette and DarkPalette use MarkVibe's shell colors. Changing appearance selects `ModernTheme.CurrentPalette`, updates shell brushes, publishes CSS variables to DOM hosts, selects Monaco's `vs`/`vs-dark`, and renders Mermaid using the matching palette. The map and highlighted code inherit the same appearance. Existing source mappings preserve the reading location during presentation changes.

Control templates remain supplied by Modern Theme. App-specific styles adjust control properties. Selected sidebar text inherits the theme's selected foreground so contrast follows both palettes. Generation checks prevent older asynchronous appearance work from replacing newer preferences.

## Versioned local persistence

`localStorage["markvibe.preferences"]` stores `{ "version": 1, "values": { ... } }`. The browser boundary accepts the earlier flat preference object and writes the versioned form. Unknown fields are discarded; malformed JSON and unsupported future versions recover to defaults. C# validates allowed appearance/style values and zoom limits.

`markvibe.lastWorkspace` stores version, provider mode, display name and workspace identity. `markvibe.lastDocument` stores the history entry and reading position. Markdown content and assets are not stored in these entries. Connected directory handles use IndexedDB's existing `markvibe-folders` store.

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

Add a defaulted field to `Preferences`, validate it, bind a control in the existing panel, and explicitly apply it to the relevant adapter. Extend the storage boundary's whitelist and migrate when the saved shape changes. Keep persistence behind adapters. Current-page export buttons are actions, not persisted preferences: they preserve the selected reading style and use a light export palette. Native destinations are selected with OS save dialogs.
