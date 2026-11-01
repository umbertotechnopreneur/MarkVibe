# Local diagnostics

Windows builds write structured JSON Lines logs under
`%LOCALAPPDATA%\MarkVibe\Windows\logs`. Portable builds use the adjacent
`data\logs` directory. An explicit `--data-dir` places `logs` below that directory.

Open the default folder from PowerShell:

```powershell
explorer "$env:LOCALAPPDATA\MarkVibe\Windows\logs"
```

Each launch gets its own timestamped file. Files rotate at approximately 1 MiB;
the logger retains up to eight files and never deletes other file patterns.
Logs remain local. No debug port or additional package is required.

Records cover native startup/shutdown, WebView2 initialization, navigation,
renderer failures, unhandled JavaScript errors and Blazor console errors.
Native operation failures record exception types, HRESULTs and method names.
Console diagnostics extract exception identifiers and missing XAML resource
names; they do not dump console arguments, exception messages, source paths,
documents, credentials or request/response bodies. Browser-only builds retain
their browser console diagnostics and do not write Windows log files.

A successful `webview.navigation` record means that the entry page loaded; it
does not establish that OpenSilver rendered a document. Inspect the visible
application before declaring startup successful. Disk permission failures in
logging never prevent the application from starting.

## Shared XAML resources

`App.xaml.cs` loads `Styles/AppStyles.xaml` into the application's existing
`Resources` dictionary. Its merge order is palette, typography, buttons,
navigation and dialogs. The container must already belong to the application
before loading the children: OpenSilver resolves eager sibling `StaticResource`
references through application resources. Loading an unattached aggregator
causes startup failures such as `Cannot find resource named 'MutedText'`.
Keep one shared instance of each palette brush so live theme updates reach all
controls.

## Graph profiling

Run this only when profiling is explicitly requested. Use existing dependencies
and a Debug browser build with source maps:

```powershell
npm run bundle:debug
dotnet build src/MarkVibe.Browser/MarkVibe.Browser.csproj -c Debug --no-restore
dotnet run --project src/MarkVibe.Browser -c Debug --no-build --no-restore --launch-profile MarkVibe
```

In another terminal:

```powershell
node scripts/profile-graph.mjs --folder 'E:\YourMarkdownWorkspace\docs' --output artifacts/graph-profile/local-run
```

The profiler imports documents read-only into an isolated browser, waits for
indexing and records DevTools traces. It rejects non-loopback URLs and Release
bundles. Run profiles sequentially; `--url` selects another local port and
`--headed` shows its browser. Keep private documents and traces out of Git.

For interactive inspection, open `http://localhost:5080` in a separate Edge
profile, open the folder and switch to Map. After indexing, use F12 **Performance**
to record a gesture. **Sources** resolves the linked source maps. Import
`*.trace.json` in Performance or `*.cpuprofile` in the JavaScript Profiler.
A manual browser session does not inherit the script's external-request blocking.

Compare the same graph and gestures using p95 frame time, long tasks, layout,
paint and compositor work. Browser timings do not establish native WebView2 behavior.
Stop the development server afterwards. Run `npm run bundle` before a subsequent
Release build to restore optimized client assets. Packaging clears `artifacts/`;
keep only the latest useful local run.
