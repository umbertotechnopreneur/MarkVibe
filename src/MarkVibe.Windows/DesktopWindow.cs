using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MarkVibe.Core;
using MarkVibe.Native;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

namespace MarkVibe.Windows;

internal sealed class DesktopWindow : Window
{
    private const string Host = "markvibe.local";
    private const string Origin = "https://" + Host;
    private readonly DesktopOptions _options;
    private readonly NativeFiles _files;
    private readonly WebView2 _view = new(), _print = new() { Width = 960, Height = 900, Visibility = Visibility.Hidden, IsHitTestVisible = false };
    private readonly SemaphoreSlim _operations = new(1, 1);
    private readonly string _capability = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<string, byte[]> _printDocuments = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _requests = new(StringComparer.Ordinal);
    private string? _startupDocument;
    private bool _closing, _initialized;
    private int _pending;
    private CoreWebView2Environment? _environment;

    public DesktopWindow(DesktopOptions options)
    {
        _options = options; _files = new(options.DataDirectory); _startupDocument = options.Document;
        Title = "MarkVibe"; Width = 1250; Height = 820; MinWidth = 480; MinHeight = 400;
        var grid = new Grid { ClipToBounds = true }; grid.Children.Add(_print); grid.Children.Add(_view); Content = grid;
        if (options.SmokeTest) { ShowInTaskbar = false; Left = -16000; Top = -16000; WindowStartupLocation = WindowStartupLocation.Manual; }
        Loaded += Initialize;
        Closing += OnClosing;
        Closed += (_, _) => { _view.Dispose(); _print.Dispose(); };
    }
    private async void Initialize(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(Path.Combine(_options.AssetsDirectory, "index.html"))) throw new InvalidOperationException("Desktop assets are missing. Run npm run bundle, then dotnet build MarkVibe.slnx.");
            var arguments = "--no-first-run";
            if (_options.DebugPort != 0) arguments += " --remote-debugging-address=127.0.0.1 --remote-debugging-port=" + _options.DebugPort;
            _environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(_options.DataDirectory, "WebView2"), new() { AdditionalBrowserArguments = arguments });
            await _view.EnsureCoreWebView2Async(_environment);
            var core = _view.CoreWebView2;
            core.SetVirtualHostNameToFolderMapping(Host, _options.AssetsDirectory, CoreWebView2HostResourceAccessKind.Deny);
            Secure(core, true);
            core.WebMessageReceived += MessageReceived;
            core.NavigationStarting += (_, args) =>
            {
                if (MainPage(args.Uri)) return;
                args.Cancel = true;
                if (args.IsUserInitiated) OpenExternal(args.Uri);
            };
            core.NewWindowRequested += (_, args) => { args.Handled = true; if (args.IsUserInitiated) OpenExternal(args.Uri); };
            core.FrameNavigationStarting += (_, args) => { if (args.Uri != "about:blank") args.Cancel = true; };
            await core.AddScriptToExecuteOnDocumentCreatedAsync("if(location.origin === '" + Origin + "' && window === window.top) Object.defineProperty(window,'markvibeDesktop',{value:Object.freeze({version:1,capability:'" + _capability + "'})});");
            _initialized = true; core.Navigate(Origin + "/index.html");
        }
        catch (WebView2RuntimeNotFoundException)
        { ShowFailure("Install the Microsoft Edge WebView2 Runtime, then reopen MarkVibe. No runtime was installed automatically."); }
        catch (Exception error) { ShowFailure(error.Message); }
    }
    private void ShowFailure(string message)
    {
        Content = new TextBlock { Text = "MarkVibe could not start.\n\n" + message, Margin = new Thickness(32), TextWrapping = TextWrapping.Wrap };
        if (_options.SmokeTest) File.WriteAllText(Path.Combine(_options.DataDirectory, "startup-error.txt"), message);
    }
    private void Secure(CoreWebView2 core, bool bridge)
    {
        core.Settings.AreHostObjectsAllowed = false;
        core.Settings.IsWebMessageEnabled = bridge;
        core.Settings.IsScriptEnabled = bridge;
        core.Settings.AreDevToolsEnabled = bridge && _options.SmokeTest;
        core.Settings.AreDefaultContextMenusEnabled = bridge;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
        core.DownloadStarting += (_, args) => args.Cancel = true;
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += (_, args) =>
        {
            if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host == Host) return;
            if (args.Request.Uri.StartsWith("data:", StringComparison.Ordinal) || args.Request.Uri.StartsWith("blob:" + Origin + "/", StringComparison.Ordinal)) return;
            args.Response = _environment!.CreateWebResourceResponse(Stream.Null, 403, "External resources blocked", "Content-Type: text/plain");
        };
    }
    private static bool MainPage(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host == Host && uri.IsDefaultPort && uri.AbsolutePath is "/" or "/index.html";
    private static void OpenExternal(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https" or "mailto") || value.Any(char.IsControl)) return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); } catch (Win32Exception) { }
    }
    private async void MessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!MainPage(e.Source) || !MainPage(_view.CoreWebView2.Source) || e.WebMessageAsJson.Length > 45 * 1048576) return;
        string? id = null;
        try
        {
            using var message = JsonDocument.Parse(e.WebMessageAsJson, new() { MaxDepth = 20 }); var root = message.RootElement;
            if (root.GetProperty("version").GetInt32() != 1 || root.GetProperty("capability").GetString() != _capability) return;
            id = root.GetProperty("id").GetString(); if (id == null || id.Length > 80) return;
            var operation = root.GetProperty("operation").GetString(); var args = root.GetProperty("args").Clone();
            if (operation == "cancel") { if (_requests.TryGetValue(args.GetProperty("requestId").GetString() ?? "", out var active)) active.Cancel(); return; }
            if (_requests.ContainsKey(id) || _requests.Count >= 64) throw new WorkspaceException("native-busy", "Too many local requests. Wait and retry.");
            using var cancellation = new CancellationTokenSource(); _requests[id] = cancellation;
            _pending++; await _operations.WaitAsync();
            try
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var result = await Dispatch(operation, args, cancellation.Token);
                if (MainPage(_view.CoreWebView2.Source)) _view.CoreWebView2.PostWebMessageAsJson(Wire.Encode(new { version = 1, id, ok = true, value = result }));
            }
            finally { _operations.Release(); _pending--; _requests.Remove(id); }
        }
        catch (Exception error)
        {
            if (id != null && MainPage(_view.CoreWebView2.Source))
            {
                var issue = error as WorkspaceException;
                _view.CoreWebView2.PostWebMessageAsJson(Wire.Encode(new { version = 1, id, ok = false, code = error is OperationCanceledException ? "cancelled" : issue?.Code ?? "native-failed", message = error is OperationCanceledException ? "Action cancelled." : issue?.Message ?? "The local operation failed. Check file access and retry; existing files were preserved." }));
            }
        }
    }
    private async Task<object?> Dispatch(string? operation, JsonElement args, CancellationToken token)
    {
        string Text(string key) => args.GetProperty(key).GetString() ?? throw new WorkspaceException("invalid-request", "Missing native argument.");
        switch (operation)
        {
            case "initialize": return new { session = _files.Session, startup = _startupDocument != null };
            case "workspace.open":
            {
                var mode = Text("mode"); string? root = null; string? selected = null;
                if (mode == "resume") return _files.Resume();
                if (_startupDocument != null) { selected = _startupDocument; _startupDocument = null; }
                else if (mode == "files")
                {
                    var picker = new OpenFileDialog { Filter = "Markdown documents|*.md;*.markdown;*.mdown", CheckFileExists = true };
                    if (picker.ShowDialog(this) == true) selected = picker.FileName;
                }
                else if (mode is "folder" or "reopen" or "connected")
                {
                    var picker = new OpenFolderDialog { Title = "Open documentation folder", Multiselect = false };
                    if (picker.ShowDialog(this) == true) root = picker.FolderName;
                }
                else throw new WorkspaceException("unknown-action", "This native workspace action is unavailable.");
                if (selected != null)
                {
                    if (!WorkspacePath.IsMarkdown(selected)) throw new WorkspaceException("unsupported-file", "Choose a Markdown document.");
                    root = Path.GetDirectoryName(selected);
                }
                if (root == null) throw new WorkspaceException("cancelled", "No folder or document was selected.");
                var info = _files.Open(root);
                if (selected != null) return new { info.Id, info.Name, info.Mode, info.Capabilities, selectedPath = Path.GetFileName(selected) };
                return info;
            }
            case "workspace.entries": return await Task.Run(() => _files.Entries(Text("workspaceId")));
            case "workspace.read": return Convert.ToBase64String(await _files.ReadAsync(Text("workspaceId"), Text("path"), token));
            case "ink.read": return await _files.ReadSidecarAsync(Text("workspaceId"), Text("path"), token);
            case "ink.write": return await _files.WriteSidecarAsync(Text("workspaceId"), Text("path"), Text("documentHash"), Text("revision"), Text("json"), token);
            case "session.remember":
                await _files.RememberAsync(args.TryGetProperty("entry", out var entry) && entry.ValueKind == JsonValueKind.Object ? Wire.Decode<HistoryEntry>(entry.GetRawText()) : null, args.GetProperty("enabled").GetBoolean(), token); return true;
            case "export.save": return await SaveExport(Text("format"), Text("filename"), Text("html"), token);
            case "export.sidecar":
            {
                var json = Text("json"); var filename = Text("filename");
                if (Encoding.UTF8.GetByteCount(json) > InkCodec.MaximumBytes || Path.GetFileName(filename) != filename || !filename.EndsWith(".ink.json", StringComparison.Ordinal)) throw new WorkspaceException("invalid-export", "Invalid sidecar backup.");
                string destination;
                if (_options.SmokeTest) { Directory.CreateDirectory(_options.ExportDirectory!); destination = Path.Combine(_options.ExportDirectory!, filename); }
                else
                {
                    var picker = new SaveFileDialog { FileName = filename, Filter = "Ink annotation JSON|*.ink.json", OverwritePrompt = true };
                    if (picker.ShowDialog(this) != true) throw new WorkspaceException("cancelled", "Annotation backup cancelled."); destination = picker.FileName;
                }
                token.ThrowIfCancellationRequested(); await AtomicFile.WriteTextAsync(destination, json, token); return true;
            }
            default: throw new WorkspaceException("unknown-action", "This native operation is unavailable.");
        }
    }
    private async Task<object> SaveExport(string format, string filename, string html, CancellationToken token)
    {
        if (format is not ("html" or "pdf") || Path.GetFileName(filename) != filename || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !filename.EndsWith("." + format, StringComparison.Ordinal) || Encoding.UTF8.GetByteCount(html) > 32 * 1048576)
            throw new WorkspaceException("invalid-export", "The export exceeds its size or filename limit.");
        string destination;
        if (_options.SmokeTest)
        {
            Directory.CreateDirectory(_options.ExportDirectory!); destination = Path.Combine(_options.ExportDirectory!, filename);
        }
        else
        {
            var picker = new SaveFileDialog { FileName = filename, DefaultExt = "." + format, Filter = format == "pdf" ? "PDF document|*.pdf" : "HTML document|*.html", OverwritePrompt = true, AddExtension = true };
            if (picker.ShowDialog(this) != true) return new { filename, saved = false, cancelled = true };
            destination = picker.FileName;
        }
        if (!destination.EndsWith("." + format, StringComparison.OrdinalIgnoreCase)) throw new WorkspaceException("invalid-export", "Choose a filename ending in ." + format + ".");
        token.ThrowIfCancellationRequested();
        if (format == "html") await AtomicFile.WriteTextAsync(destination, html, token);
        else
        {
            await _print.EnsureCoreWebView2Async(_environment);
            var core = _print.CoreWebView2;
            if (!_printConfigured)
            {
                _printConfigured = true; Secure(core, false);
                core.NavigationStarting += (_, args) => { if (!args.Uri.StartsWith(Origin + "/export/", StringComparison.Ordinal)) args.Cancel = true; };
                core.NewWindowRequested += (_, args) => args.Handled = true;
                core.WebResourceRequested += (_, args) =>
                {
                    if (_printDocuments.TryGetValue(args.Request.Uri, out var bytes))
                        args.Response = _environment!.CreateWebResourceResponse(new MemoryStream(bytes, false), 200, "OK", "Content-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nContent-Security-Policy: default-src 'none'; img-src data:; style-src 'unsafe-inline'; font-src data:; script-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'");
                };
            }
            var address = Origin + "/export/" + Guid.NewGuid().ToString("N") + ".html";
            _printDocuments[address] = Encoding.UTF8.GetBytes(html);
            var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Completed(object? sender, CoreWebView2NavigationCompletedEventArgs e) { if (e.IsSuccess) loaded.TrySetResult(); else loaded.TrySetException(new IOException("The PDF document could not be loaded.")); }
            core.NavigationCompleted += Completed;
            try
            {
                core.Navigate(address); await loaded.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
                // Load completion includes embedded images. ExecuteScript is host initiated
                // and remains available with page scripts disabled.
                var ready = false;
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    if (await core.ExecuteScriptAsync("document.fonts.status==='loaded' && Array.from(document.images).every(i=>i.complete)") == "true") { ready = true; break; }
                    await Task.Delay(100, token);
                }
                if (!ready) throw new IOException("The PDF images or fonts did not finish loading.");
                var settings = _environment!.CreatePrintSettings(); settings.ShouldPrintBackgrounds = true; settings.ShouldPrintHeaderAndFooter = false;
                settings.PageWidth = 8.2677; settings.PageHeight = 11.6929;
                settings.MarginTop = settings.MarginBottom = settings.MarginLeft = settings.MarginRight = 0;
                using var pdf = await core.PrintToPdfStreamAsync(settings);
                if (pdf == null) throw new IOException("WebView2 did not return a PDF.");
                await AtomicFile.WriteAsync(destination, pdf, token);
            }
            finally { core.NavigationCompleted -= Completed; _printDocuments.Remove(address); }
        }
        return new { filename = Path.GetFileName(destination), saved = true, cancelled = false };
    }
    private bool _printConfigured;
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closing || !_initialized) return;
        e.Cancel = true;
        try
        {
            if (_pending > 0) { await _operations.WaitAsync(); _operations.Release(); }
            var result = await _view.CoreWebView2.ExecuteScriptAsync("window.markvibe?.canClose?.() ?? true");
            if (result != "true" && MessageBox.Show(this, "Some ink has not been saved. Return to the document to save or export it. Close and discard the unsaved ink?", "Unsaved annotations", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            _closing = true; Close();
        }
        catch (Exception) { if (MessageBox.Show(this, "Close MarkVibe? Unfinished work may not be saved.", "MarkVibe", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { _closing = true; Close(); } }
    }
}
