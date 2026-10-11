# MarkVibe local validation — 11 October 2026

Historical report before the icon/About and MSIX/portable changes. Its ignored
artifacts were removed by the later requested cleanup. Current packages carry their
own build metadata; tests were not rerun for packaging under the user's new policy.

The development app starts and passes the authorized checks. The reported missing static-assets manifest error is resolved. Modern Theme, persistent settings, document resumption, browser HTML/PDF export, Windows WPF/WebView2 with direct local PDF saving and paragraph ink are implemented. This report records local development evidence, not a release or deployment.

## Final results

| Verification | Observed outcome |
| --- | --- |
| SDK | Existing .NET SDK 10.0.401 |
| Node | Existing cached v24.19.0; system default v20.19.0 was not changed |
| Bundle | Successful local esbuild output, Monaco adapter/CSS/module worker and Mermaid chunks |
| NuGet | Restore and locked restore passed; no warnings |
| Solution build | 0 errors, 0 warnings |
| C# core checks | 7/7 passed |
| Client path/ZIP checks | 9/9 passed |
| Root browser regression | 19/19 passed at http://localhost:5080/ |
| Subpath browser regression | 19/19 passed at http://127.0.0.1:5081/markvibe/ |
| Native file/ink service checks | 10/10 passed |
| Windows WebView2 regression | 11/11 passed using installed runtime 154.0.4258.62 |
| Browser | Microsoft Edge 155.0.4283.45, headless, existing Playwright runtime |
| Standalone HTML | Actual download opened offline with scripts disabled; local images, readable diagram and anchors passed |
| PDF | Actual export action prepares a clean print document; Chromium produced 3 A4 pages; all pages visually inspected with Poppler |
| Native PDF | Actual WebView2 PrintToPdfStreamAsync output, 6 A4 pages; every page visually inspected with Poppler, with selectable Unicode, image, diagram and vector ink |
| Listening addresses | Root preview: 127.0.0.1 and ::1; temporary subpath preview: 127.0.0.1 |
| Git | Existing master checkout, no commits/remotes; all authored files remain untracked |

No SDK/workload, browser runtime or global Node installation was performed. Modern Theme, the patched XML dependency and pinned WebView2 SDK 1.0.4258.31 were restored through NuGet. Windows static assets were staged locally as build output. No branch, commit, remote, CI, installer/release packaging, publication or deployment was created.

## Concrete fixes and additions

- Corrected Monaco 0.57 package-export and Markdown-language imports; the bundled module worker calculates a real diff in the browser.
- Removed unsupported GridSplitter properties so OpenSilver XAML compiles. Restored actual map width and closed the sidebar on entering narrow layouts.
- Replaced the original Border/GetDiv DOM integration with HtmlPresenter and a connected tagged inner element. The original adapter attached to an empty disconnected placeholder; documents now render visibly.
- Corrected async source-render reuse and awaited initial layout before deciding whether the pane needs a lightweight fallback. Repeated Source/Split/Read changes and forced Monaco failure have usable outcomes.
- Made Source Find navigate to an entered match and updated native source/map button labels when their text changes.
- Allowed normal mailto recipients while still rejecting credentials on HTTP(S), fixing a failing core check. Removed a nullable Markdown-info warning.
- Pinned System.Security.Cryptography.Xml 8.0.4 to fix the vulnerable 8.0.2 transitive resolution documented in the [runtime advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-mmjf-rqrv-855v).
- Installed the official OpenSilver Modern Theme 3.3.0 without changing OpenSilver 3.3.3. Its palettes coordinate shell controls, Monaco, highlighted Markdown, map and Mermaid. Selected sidebar text follows the theme's selected foreground.
- Added a versioned local-settings boundary, legacy migration, malformed/future-version recovery, reset, storage failure handling and stale appearance suppression.
- Added workspace descriptors and stable import identities so the last page, anchor and source-based reading position resume after reselection. Already permitted connected handles can resume without a startup permission request. Remembering can be disabled and cleared.
- Added current-page HTML export with inline styles, local images and Mermaid, safe links, unavailable-resource notices and bounded output. PDF uses the same document in a script-free print frame, with light colors, page margins and numbers. Exports work from Source and Map without exporting the app shell.
- Fixed Mermaid's static styles being removed together with generated animation rules. Bounded CSS parsing retains allowlisted properties scoped to the diagram id, preserving readable node fills/labels while dropping at-rules and external references. Reader and export cases assert readable node colors.

## Browser coverage and artifacts

The 19 cases cover demo image/diagrams, document links/history, all views, source/Find, map navigation, duplicate anchors, missing pages, Split mappings, five widths, imports/ZIPs/resumption, content security, appearance/styles, settings recovery, real workers, persisted handles and both exports. Ink checks add sidecar download/reimport, vector export and preservation of unsupported originals. Every passing case asserts no unexpected page/console errors, failed HTTP assets or page-initiated external HTTP requests.

Ignored local artifacts:

- `artifacts/browser/results.json` and `artifacts/browser-subpath/results.json`: final 19/19 results, base URL and browser version.
- `artifacts/browser/modern-dark-settings.png`: dark Modern Theme with Split/Monaco and settings.
- `artifacts/browser/mobile-settings.png`: the settings panel at 320×600.
- `artifacts/browser/desktop-read.png`, `desktop-split.png`, `desktop-map.png`, `mobile-read.png`, `mobile-source.png`: synthetic UI screenshots.
- `artifacts/browser/export-fixture.html` and `artifacts/browser-subpath/export-fixture.html`: actual standalone downloads from a synthetic multi-page reading fixture, with all local assets embedded.
- `artifacts/browser/export-fixture.pdf` and `artifacts/browser-subpath/export-fixture.pdf`: 3-page A4 print-engine outputs of the actual prepared document. Both contain selectable Unicode text and no JavaScript; accessibility tagging has not been audited.
- `artifacts/browser/export-page-1.png` through `export-page-3.png`: Poppler renders of every PDF page, visually inspected after the Mermaid style correction. `export-html.png` and `export-print.png` capture the clean browser document.

The dark desktop, small settings screenshots and every final PDF page were visually inspected. PDF text extraction also retained the accented title and diagram labels. Artifacts are local and ignored by Git; earlier failure screenshots may remain as diagnostic history and are not final passing screenshots. The reproducible commands and full case list are in [validation](validation.md).

## Windows and ink coverage

Native service checks cover contained reads, canonical paths/reserved filenames, ink
roundtrip/undo/redo/erase, pressure and bounds, unsupported formats, changed/ambiguous
anchors, revision conflicts, Markdown changes/BOM, session clearing, atomic cancellation
and corrupt session recovery. The ten cases use synthetic files under `artifacts/native-core/`.

The eleven Windows cases run the actual WPF host and installed WebView2 runtime with
isolated profiles and Debug-only export destinations. They cover cold launch with local
image/Mermaid, traversal/junction/unknown-action rejection, mouse drawing/autosave/undo/redo,
rapid consecutive strokes during autosave, direct HTML/PDF, injected pen pressure/highlighter/eraser,
dark theme/native navigation, restart restoration, stale-sidecar backup/discard,
explicit changed-document review plus six-page PDF saving, and clearing stale native
session metadata when switching to a browser workspace. No page/console errors, failed assets or page external requests
were observed. Smoke bypasses OS pickers; it does not validate their interaction.

Artifacts: `artifacts/windows/results.json`, `native-reader-ink.png`, `native-dark-ink.png`,
`exports/README.html`, `exports/README.pdf`, recovery sidecar and `native-pdf-page-1.png`
through `native-pdf-page-6.png`. All native PDF pages and the dark reader were inspected.
Accented text, diagram/image labels, page numbers and section 36 survive text extraction.

## Remaining evidence limits

The connected-handle check uses a real FileSystemDirectoryHandle over synthetic origin-private files, persisted through IndexedDB. It does not establish physical OS folder-picker behavior, long-term user-folder permission retention or permission denial/revocation. Narrow widths are desktop Edge emulation, not physical Android/iOS. Other browsers, full accessibility/screen-reader behavior, sustained memory/performance, broader malicious SVG/diagram/archive cases and release static hosting remain unverified. Page request checks do not audit independent Edge/runtime traffic. No security certification is claimed.

Browser PDF automation intercepts the print dialog and prints the prepared document with Chromium. Windows exercises its real direct PDF API. OS file/folder/save picker interaction, physical printers and physical pen/palm behavior remain unverified. Other layouts, wide tables, long code/diagrams and PDF accessibility require broader acceptance. HTML/PDF cover the current page rather than an entire workspace.

Browser application-shell offline caching, macOS/Linux wrappers, true text overlays,
manual orphan reassignment, schema migrations and durable browser ink drafts remain
future work. See [settings](settings.md), [ink](ink-design.md) and [native export](desktop-and-export.md).
