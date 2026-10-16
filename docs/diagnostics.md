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
