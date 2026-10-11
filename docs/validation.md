# Local validation

Historical checks from 2026-10-11, before the icon/About and distribution changes. They ran under the earlier explicit authorization using synthetic documents. Current policy: run tests only on a current explicit user request. New packages record build-only evidence in [Windows distribution](windows-packaging.md); prior fixtures were removed by the requested artifact cleanup.

## Observed results

| Stage | Result |
| --- | --- |
| Client bundle | Pass using existing Node 24.19.0; local chunks, CSS, worker and notices generated |
| NuGet restore | Pass, including locked mode; no restore warnings |
| Solution build | Pass, 0 warnings and 0 errors |
| Core checks | 7/7 pass |
| Client path/ZIP checks | 9/9 pass |
| Edge browser checks at root | 19/19 pass, Edge 155.0.4283.45 headless |
| Edge browser checks at /markvibe/ | 19/19 pass against the same build and browser |
| Native file/ink service checks | 10/10 pass |
| Windows WebView2 checks | 11/11 pass, installed runtime 154.0.4258.62 |
| Native PDF | Direct API output, 6 A4 pages visually inspected |
| Root startup | Pass; static-assets endpoint manifest exists; preview listens on loopback port 5080 |

See [the validation report](validation-results-2026-10-11.md) for fixes, artifacts, subpath results and evidence limits. Earlier reports that compilation and dependencies were unverified are historical and have been superseded by these local results.

## Reproducing the current local build

Run only the stages explicitly requested for the current task. The examples below include optional test commands; do not run those merely because a build was requested. Stop at a failed stage; never proceed to --no-build after a failed Browser build. Use an existing Node >=22.12:

~~~powershell
node --version
~~~

The historical run used v24.19.0 through a process-local PATH change. No global Node configuration was changed. Do not create output in Codex directories.

~~~powershell
npm run bundle
if ($LASTEXITCODE -ne 0) { throw 'Client bundle failed.' }
dotnet restore MarkVibe.slnx --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
dotnet build MarkVibe.slnx --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
# Run the following checks only on an explicit current test request:
dotnet run --project tests/MarkVibe.Core.Checks/MarkVibe.Core.Checks.csproj --no-build
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
dotnet run --project tests/MarkVibe.Native.Checks/MarkVibe.Native.Checks.csproj --no-build
if ($LASTEXITCODE -ne 0) { throw 'Native checks failed.' }
npm run test:client
if ($LASTEXITCODE -ne 0) { throw 'Client checks failed.' }
dotnet run --project src/MarkVibe.Browser/MarkVibe.Browser.csproj --no-build --launch-profile MarkVibe
~~~

Open [http://localhost:5080/](http://localhost:5080/). Stop an existing preview first if the port is occupied. A fresh authorized dependency reproduction uses npm ci --ignore-scripts --no-audit --no-fund with the existing npm lock. Changing dependencies requires updating the relevant lock through an authorized restore. If an SDK/workload is missing, do not install it implicitly.

## Browser regression harness

With the preview running, the local harness uses the already available Playwright package and installed Microsoft Edge. No browser download was required:

~~~powershell
$env:MARKVIBE_PLAYWRIGHT_PATH = 'C:\Tools\playwright\node_modules\playwright'
npm run test:browser
~~~

The package path is machine-specific. Alternatively supply another existing Playwright package, and choose an installed browser channel through MARKVIBE_BROWSER_CHANNEL. This validation did not change package dependencies to install Playwright.

The 19 browser checks cover:

1. Visible demo reader, local image and Mermaid.
2. Real document links and browser Back/Forward.
3. Repeated Source/Split/Read switching, read-only Monaco and a far-away source Find match.
4. Real map cards/connections, focus, zoom and navigation.
5. Selected files, duplicate anchors, a missing page and source-location Split synchronization.
6. Readable layouts at 320/390/768/1024/1440px and narrow source fallback.
7. Folder-prefix normalization and nested ZIP pages, including restart/reselection of both providers.
8. Synthetic hostile Markdown: scripts, executable links and remote images blocked.
9. Modern Theme, Monaco, Mermaid, all reading styles, zoom, Light/Dark/System, reload, rapid theme changes, reset and small settings layout.
10. Last document/anchor/source position after restart, import resumption and opt-out.
11. A real Monaco module worker calculating a diff from local assets.
12. Forced Monaco module failure with a usable read-only fallback and Find.
13. Legacy settings migration, malformed JSON and unknown future versions.
14. Unavailable localStorage with usable session preferences.
15. Genuine persisted directory-handle resumption using synthetic origin-private files.
16. Standalone HTML download, below-fold embedded images, readable Mermaid, local anchors, neighboring-page notices, offline loading and script/remote-resource boundaries.
17. Actual PDF action from Map while the app is dark, print-frame cleanup, clean light output and Chromium PDF generation.
18. Ink sidecar download, reimport and vector inclusion in standalone HTML.
19. Unsupported ink preserved as a read-only original with an exact backup download.

The native print dialog is intercepted during automation. The prepared document is printed with the same browser engine to `export-fixture.pdf`, then all pages are rendered with Poppler and visually inspected. This checks layout, selectable text, Unicode, page numbers, code, tables, images and diagram labels; it does not verify an OS dialog choice or physical printer. No PDF accessibility tagging audit is claimed.

Each isolated test rejects unexpected page/console errors, failed HTTP assets and external HTTP requests originating from the page. That assertion does not audit unrelated browser/runtime network traffic. Screenshots and results.json are written under ignored artifacts/browser/.

## Development subpath

Start a separate loopback host from the current build:

~~~powershell
dotnet run --project src/MarkVibe.Browser/MarkVibe.Browser.csproj --no-build --no-launch-profile -- --urls http://127.0.0.1:5081 --pathbase=/markvibe
~~~

In another PowerShell session using the same Node/Playwright environment:

~~~powershell
$env:MARKVIBE_BASE_URL = 'http://127.0.0.1:5081/markvibe/'
$env:MARKVIBE_ARTIFACTS_DIR = 'artifacts/browser-subpath'
npm run test:browser
~~~

The worker check explicitly asserts that its URL includes the configured base path. This exercises local development hosting, not release publishing or a deployed static site.

## Remaining acceptance

Windows packaging is described in [distribution](windows-packaging.md). Installer acceptance is a requested manual installation. Other browsers/devices, OS pickers, physical pen/palm input, printers, accessibility, large workspaces and production hosting remain possible acceptance work; suggest relevant checks and wait for a request before running tests. Touch-size and injected pen checks are not physical-device evidence. macOS/Linux wrappers and text overlays remain future work.

## Windows regression harness

After the solution build, with existing Playwright and WebView2 Runtime:

~~~powershell
dotnet run --project src/MarkVibe.Windows --no-build
npm run test:windows
~~~

Close any manually launched desktop instance before running automation. The harness
launches a Debug host offscreen with isolated `--data-dir`, `--export-dir` and a
loopback debugging port; Release rejects smoke options. It exercises the real
WebView2 runtime, native file/sidecar operations and direct PDF API. OS dialogs are
bypassed only by explicit smoke destinations. Results and synthetic fixtures live
under ignored `artifacts/windows/`; native service checks use `artifacts/native-core/`.

The 11 Windows cases include rapid consecutive stroke autosaves and cold restart
after switching from a native folder to a browser workspace. Navigation and export
wait for completed stroke events before reading the ink model.
