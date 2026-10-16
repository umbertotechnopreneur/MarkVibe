# Document export and native hosts

Current-page HTML/PDF export is implemented in browsers and the Windows WPF/WebView2 host.

## Export the current page

Choose **Settings → Export current page** in Read, Source, Split or Map.
Both formats include the title/path, reading styles, highlighted code, supported
local images, sanitized Mermaid and resolved vector ink on a light background.
Only the current document is included. Same-document anchors and safe external links
remain usable; links to neighboring Markdown pages are marked unavailable.

Browser HTML downloads a standalone UTF-8 file with inline CSS and embedded assets.
It works offline without scripts or the app runtime. Browser PDF opens a clean,
script-free print document; choose Save as PDF in the browser/OS dialog. Browser
code cannot establish whether that dialog saved or cancelled.

Windows uses an OS save dialog for either format. PDF is generated directly with
`PrintToPdfStreamAsync` in a separate WebView2 with page scripts/web messaging
disabled. The host waits for local assets/fonts and commits via temporary-file
replacement. It reports the saved filename or cancellation; no browser print dialog
is involved. A4 styling supplies margins and page numbers. Cancellation before commit
preserves the prior destination; a completed commit cannot be undone by cancellation.

Missing/blocked images and failed diagrams become notices. Remote images are not
fetched. Limits: 96 images, 8 MiB per input, 20 MiB embedded image bytes, 32 megapixels
aggregate and 32 MiB final UTF-8 HTML. No document upload is used.

## Run Windows

Build the solution after bundling; `scripts/stage-windows-assets.mjs` materializes
the Browser runtime manifest under the host's `bin/.../app/`, including OpenSilver,
WASM and client assets. It requires neither an HTTP server nor MAUI.

```powershell
dotnet run --project src/MarkVibe.Windows --no-build
dotnet run --project src/MarkVibe.Windows --no-build -- --open 'C:\Docs\README.md'
```

Use Open file/Open folder to grant a local documentation root. Adjacent Markdown and
images are available through relative paths. Refresh rereads the bounded catalog.
Native access excludes UNC roots, reparse points, junctions, alternate streams and
reserved Windows filenames. The host exposes specific typed operations rather than
unrestricted filesystem or shell access.

Settings live in the persistent WebView2 profile; the native session records the
selected root and document/anchor/position under `%LOCALAPPDATA%\MarkVibe\Windows`.
It automatically reopens on restart. Disabling remembering clears the saved session;
`--open` overrides it. Ink autosaves separately; see [ink](ink-design.md). Document
map positions, colors and icons autosave in the selected root's
`.markvibe-map.json`; see [map layout](map-layout.md). Text notes use that same map
JSON or `<document path>.notes.json` in the reader, with shared styling, formatted
bodies and source element anchors; see [sticky notes](notes-design.md). HTML/PDF
exports render colored note boxes after their owning source block, including an
unresolved-notes section. These sidecars leave Markdown unchanged. The new map
and text-note routes have not yet been built or runtime-verified.

Source builds need the .NET 10 desktop runtime and Microsoft Edge WebView2 Runtime.
The MSIX and portable distributions include .NET; WebView2 Runtime remains required.
Portable profiles live in a sibling `data/` directory. See [distribution and installation](windows-packaging.md).

## Other platforms and evidence

| Platform | Current route |
| --- | --- |
| Windows | Implemented WPF/WebView2 wrapper; synthetic native checks pass |
| macOS, Android, iOS | Possible [OpenSilver MAUI Hybrid launcher](https://doc.opensilver.net/documentation/general/maui-hybrid-launcher.html); no wrapper implemented |
| Linux | Possible [OpenSilver Photino launcher](https://doc.opensilver.net/documentation/general/photino-launcher.html); no wrapper implemented |
| Browsers | Existing WebAssembly project; browser permissions and printing apply |

Greater filesystem access comes from the explicitly implemented native service,
not automatically from embedding a web page. The bridge checks sender URL, version
and per-launch capability, blocks external page resources and opens user-initiated
external links in the system browser. Follow Microsoft's
[WebView2 security guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security)
and [printing documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/print).

Local automation exercises the real WebView2 runtime and PDF API using isolated
profiles, synthetic documents and Debug-only save destinations. Native picker interaction,
physical pen/printers and clean-machine installation remain unverified. Packaging runs
no tests; the user must explicitly request installation acceptance.
See [validation](validation.md).

