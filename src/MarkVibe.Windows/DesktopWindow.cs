/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; readability and OpenAI key settings; 2026-10-11
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Modified with AI: OpenAI Codex; typed AI transport and explicit Markdown copy saving; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Modified with AI: OpenAI Codex; read-only installed UI font discovery; 2026-10-16
 * Human guidance: Umberto Giacobbi; offer installed monospace fonts without installing or changing document fonts
 * Modified with AI: OpenAI Codex; protect original Markdown files from aliased export destinations; 2026-10-16
 * Human guidance: Umberto Giacobbi; preserve originals while saving edited copies, including after the original is removed
 * Modified with AI: OpenAI Codex; compose the visible browser through WPF; 2026-10-16
 * Human guidance: Umberto Giacobbi; correct gray redraw corruption during map selection
 * Modified with AI: OpenAI Codex; selectable native API and Codex CLI connection routing; 2026-10-16
 * Human guidance: Umberto Giacobbi; independent provider tests and explicit document-only sends
 * Modified with AI: OpenAI Codex; opaque direct browser presentation; 2026-10-16
 * Human guidance: Umberto Giacobbi; prevent missing window regions while panning the document map
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
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
using Microsoft.Win32.SafeHandles;
using static MarkVibe.Core.UiStrings;

namespace MarkVibe.Windows;
/// <summary>Owns the opaque desktop browser, isolated print host and authenticated native operations.</summary>
internal sealed partial class DesktopWindow : Window
{
    private const string Host = "markvibe.local";
    private const string Origin = "https://" + Host;
    private readonly DesktopOptions _options;
    private readonly NativeFiles _files;
    private readonly CurrentDocumentWatcher _documentWatcher;
    private readonly AiConnectionService _aiConnections;
    private readonly WebViewDiagnostics _diagnostics = new();
    // All application overlays live inside the browser. Direct HWND presentation avoids
    // the capture-to-WPF texture path, which exposed missing regions during map movement.
    private readonly WebView2 _view = new()
    {
        DefaultBackgroundColor = System.Drawing.Color.FromArgb(244, 245, 247)
    };
    private readonly WebView2 _print = new()
    {
        Width = 960,
        Height = 900,
        Visibility = Visibility.Hidden,
        IsHitTestVisible = false
    };
    private readonly SemaphoreSlim _operations = new(1, 1);
    private readonly string _capability = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<string, byte[]> _printDocuments = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _requests = new(StringComparer.Ordinal);
    private string? _startupDocument;
    private bool _closing, _initialized;
    private int _pending;
    private CoreWebView2Environment? _environment;
    private readonly string _systemLanguage = CultureInfo.CurrentUICulture.Name;
    private bool _dark;
    /// <summary>Creates the visible and print WebViews, native file service and desktop lifetime handlers.</summary>
    public DesktopWindow(DesktopOptions options)
    {
        _options = options;
        _files = new(options.DataDirectory);
        _documentWatcher = new CurrentDocumentWatcher(_files, Dispatcher, NotifyDocumentChange);
        _aiConnections = new(options.DataDirectory);
        _startupDocument = options.Document;
        UiStrings.SetLanguage("system", _systemLanguage);
        Title = "MarkVibe";
        Background = System.Windows.Media.Brushes.WhiteSmoke;
        Width = 1250;
        Height = 820;
        MinWidth = 480;
        MinHeight = 400;
        var grid = new Grid
        {
            ClipToBounds = true
        };
        grid.Children.Add(_print);
        grid.Children.Add(_view);
        Content = grid;
        if (options.SmokeTest)
        {
            ShowInTaskbar = false;
            Left = -16000;
            Top = -16000;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        Loaded += Initialize;
        SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            _documentWatcher.Dispose();
            SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;
            _view.Dispose();
            _print.Dispose();
        };
    }

    /// <summary>Initializes the isolated WebView environment, local asset host and authenticated native bridge.</summary>
    private async void Initialize(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(Path.Combine(_options.AssetsDirectory, "index.html")))
            {
                throw new InvalidOperationException("Desktop assets are missing. Run npm run bundle, then dotnet build MarkVibe.slnx.");
            }

            var arguments = "--no-first-run";
            if (_options.DebugPort != 0)
            {
                arguments += " --remote-debugging-address=127.0.0.1 --remote-debugging-port=" + _options.DebugPort;
            }

            _environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(_options.DataDirectory, "WebView2"), new() { AdditionalBrowserArguments = arguments });
            await _view.EnsureCoreWebView2Async(_environment);
            WindowsBackdrop.Apply(this, _view, _dark);
            var core = _view.CoreWebView2;
            core.SetVirtualHostNameToFolderMapping(Host, _options.AssetsDirectory, CoreWebView2HostResourceAccessKind.Deny);
            Secure(core, true);
            await _diagnostics.AttachAsync(core);
            core.WebMessageReceived += MessageReceived;
            core.NavigationStarting += (_, args) =>
            {
                if (MainPage(args.Uri))
                {
                    // Virtual host folder mapping has no implicit directory index.
                    // Normalize a root reload to the actual bundled entry file.
                    var entryUri = new Uri(args.Uri);
                    if (entryUri.AbsolutePath == "/")
                    {
                        args.Cancel = true;
                        core.Navigate(Origin + "/index.html" + entryUri.Query + entryUri.Fragment);
                    }
                    return;
                }

                args.Cancel = true;
                if (args.IsUserInitiated)
                {
                    OpenExternal(args.Uri);
                }
            };
            core.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                if (args.IsUserInitiated)
                {
                    OpenExternal(args.Uri);
                }
            };
            core.FrameNavigationStarting += (_, args) =>
            {
                if (args.Uri != "about:blank")
                {
                    args.Cancel = true;
                }
            };
            await core.AddScriptToExecuteOnDocumentCreatedAsync("if(location.origin === '" + Origin + "' && window === window.top) Object.defineProperty(window,'markvibeDesktop',{value:Object.freeze({version:1,capability:'" + _capability + "'})});");
            _initialized = true;
            core.Navigate(Origin + "/index.html");
        }
        catch (WebView2RuntimeNotFoundException error)
        {
            DesktopLog.Error("webview.runtimeMissing", error);
            ShowFailure("Install the Microsoft Edge WebView2 Runtime, then reopen MarkVibe. No runtime was installed automatically.");
        }
        catch (Exception error)
        {
            DesktopLog.Error("webview.startupFailure", error);
            ShowFailure(error.Message);
        }
    }

    /// <summary>Displays a startup failure in the desktop host when the application cannot load.</summary>
    private void ShowFailure(string message)
    {
        Content = new TextBlock
        {
            Text = "MarkVibe could not start.\n\n" + message,
            Margin = new Thickness(32),
            TextWrapping = TextWrapping.Wrap
        };
        if (_options.SmokeTest)
        {
            File.WriteAllText(Path.Combine(_options.DataDirectory, "startup-error.txt"), message);
        }
    }

    /// <summary>Configures WebView capabilities and resource boundaries for the visible or print view.</summary>
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
            if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host == Host)
            {
                return;
            }

            if (args.Request.Uri.StartsWith("data:", StringComparison.Ordinal) ||
                args.Request.Uri.StartsWith("blob:" + Origin + "/", StringComparison.Ordinal))
            {
                return;
            }

            args.Response = _environment!.CreateWebResourceResponse(Stream.Null, 403, "External resources blocked", "Content-Type: text/plain");
        };
    }

    /// <summary>Checks that a navigation targets the local application entry page.</summary>
    private static bool MainPage(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == "https" &&
        uri.Host == Host &&
        uri.IsDefaultPort &&
        uri.AbsolutePath is "/" or "/index.html";
    /// <summary>Opens an allowed external URL with the operating system's registered handler.</summary>
    private static void OpenExternal(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https" or "mailto") ||
            value.Any(char.IsControl))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
        }
    }

    /// <summary>Authenticates and bounds browser requests, handles cancellation and returns structured native responses.</summary>
    private async void MessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!MainPage(e.Source) || !MainPage(_view.CoreWebView2.Source) || e.WebMessageAsJson.Length > 45 * 1048576)
        {
            return;
        }

        string? id = null;
        try
        {
            using var message = JsonDocument.Parse(e.WebMessageAsJson, new() { MaxDepth = 20 });
            var root = message.RootElement;
            if (root.GetProperty("version").GetInt32() != 1 || root.GetProperty("capability").GetString() != _capability)
            {
                return;
            }

            id = root.GetProperty("id").GetString();
            if (id == null || id.Length > 80)
            {
                return;
            }

            var operation = root.GetProperty("operation").GetString();
            var args = root.GetProperty("args").Clone();
            if (operation == "cancel")
            {
                if (_requests.TryGetValue(args.GetProperty("requestId").GetString() ?? "", out var active))
                {
                    active.Cancel();
                }

                return;
            }

            if (_requests.ContainsKey(id) || _requests.Count >= 64)
            {
                throw new WorkspaceException("native-busy", "Too many local requests. Wait and retry.");
            }

            using var cancellation = new CancellationTokenSource();
            _requests[id] = cancellation;
            _pending++;
            var gateHeld = false;
            try
            {
                // Provider work must not hold the file/export gate for minutes.
                // Its independent cancellation still participates in close protection.
                if (!IsAiOperation(operation))
                {
                    await _operations.WaitAsync(cancellation.Token);
                    gateHeld = true;
                }
                cancellation.Token.ThrowIfCancellationRequested();
                var result = await Dispatch(operation, args, cancellation.Token);
                if (!_closing && MainPage(_view.CoreWebView2.Source))
                {
                    _view.CoreWebView2.PostWebMessageAsJson(Wire.Encode(new { version = 1, id, ok = true, value = result }));
                }
            }
            finally
            {
                if (gateHeld)
                {
                    _operations.Release();
                }
                _pending--;
                _requests.Remove(id);
            }
        }
        catch (Exception error)
        {
            if (error is not OperationCanceledException)
            {
                DesktopLog.Error("native.requestFailure", error);
            }
            if (id != null && !_closing && MainPage(_view.CoreWebView2.Source))
            {
                var issue = error as WorkspaceException;
                _view.CoreWebView2.PostWebMessageAsJson(Wire.Encode(new { version = 1, id, ok = false, code = error is OperationCanceledException ? "cancelled" : issue?.Code ?? "native-failed", message = error is OperationCanceledException ? "Action cancelled." : issue?.Message ?? "The local operation failed. Check file access and retry; existing files were preserved." }));
            }
        }
    }

    /// <summary>Identifies independent AI requests so they never hold the local file/export gate.</summary>
    private static bool IsAiOperation(string? operation)
    {
        return operation?.StartsWith("ai.", StringComparison.Ordinal) == true;
    }

    /// <summary>Routes validated bridge operations to local workspace, sidecar and export services.</summary>
    private async Task<object?> Dispatch(string? operation, JsonElement args, CancellationToken token)
    {
        if (_options.SmokeTest && operation?.StartsWith("file.", StringComparison.Ordinal) == true)
        {
            throw new WorkspaceException("test-shell-disabled", "External file actions are unavailable in the isolated verification profile.");
        }
        // Isolated checks never inspect saved credentials or start an AI connection.
        if (_options.SmokeTest && IsAiOperation(operation))
        {
            if (operation == "ai.connection.status")
            {
                var provider = args.TryGetProperty("provider", out var requestedProvider)
                    && requestedProvider.ValueKind == JsonValueKind.String
                    && requestedProvider.GetString() == AiProviders.CodexCli
                        ? AiProviders.CodexCli
                        : AiProviders.ApiKey;
                return new AiConnectionStatus(provider, false, false,
                    "AI connections are unavailable in the isolated verification profile.");
            }
            throw new WorkspaceException("test-api-disabled",
                "AI connections are unavailable in the isolated verification profile.");
        }
        if (IsAiOperation(operation))
        {
            return await DispatchAiAsync(operation!, args, token);
        }

        // Reads a required string argument from the authenticated request payload.
        string Text(string key) => args.GetProperty(key).GetString() ?? throw new WorkspaceException("invalid-request", "Missing native argument.");
        switch (operation)
        {
            case "initialize":
                return new
                {
                    session = _files.Session,
                    startup = _startupDocument != null,
                    systemLanguage = _systemLanguage,
                    acrylicAvailable = WindowsBackdrop.Supported
                };
            case "ui.fonts":
                return UiFontCatalog.Installed(System.Windows.Media.Fonts.SystemFontFamilies
                    .SelectMany(family => family.FamilyNames.Values.Append(family.Source)));
            case "window.appearance":
                _dark = args.GetProperty("dark").GetBoolean();
                UiStrings.SetLanguage(Text("language"), _systemLanguage);
                return new
                {
                    acrylic = WindowsBackdrop.Apply(this, _view, _dark),
                    acrylicAvailable = WindowsBackdrop.Supported,
                    accent = WindowsBackdrop.Accent
                };
            case "document.saveCopy":
                return await SaveMarkdownCopyAsync(Text("source"), Text("filename"), Text("workspaceId"), Text("path"), token);
            case "file.reveal":
            case "file.terminal":
                DesktopFileActions.OpenLocation(_files, Text("workspaceId"), Text("path"), operation == "file.terminal");
                return true;
            case "file.deleteRevision":
                return DesktopRecycleBin.Revision(_files, Text("workspaceId"), Text("path"));
            case "file.delete":
                DesktopRecycleBin.Delete(_files, Text("workspaceId"), Text("path"), Text("revision"));
                return true;
            case "file.browser":
                return await OpenBrowserPreviewAsync(Text("html"), token);
            case "workspace.open":
            {
                var mode = Text("mode");
                string? root = null;
                string? selected = null;
                if (mode == "resume")
                {
                    return _files.Resume();
                }

                if (_startupDocument != null)
                {
                    selected = _startupDocument;
                    _startupDocument = null;
                }
                else if (mode == "files")
                {
                    var picker = new OpenFileDialog
                    {
                        Title = T("Open Markdown document"),
                        Filter = T("Markdown documents") + "|*.md;*.markdown;*.mdown",
                        CheckFileExists = true
                    };
                    if (picker.ShowDialog(this) == true)
                    {
                        selected = picker.FileName;
                    }
                }
                else if (mode is "folder" or "reopen" or "connected")
                {
                    var picker = new OpenFolderDialog
                    {
                        Title = T("Open documentation folder"),
                        Multiselect = false
                    };
                    if (picker.ShowDialog(this) == true)
                    {
                        root = picker.FolderName;
                    }
                }
                else
                {
                    throw new WorkspaceException("unknown-action", "This native workspace action is unavailable.");
                }

                if (selected != null)
                {
                    if (!WorkspacePath.IsMarkdown(selected))
                    {
                        throw new WorkspaceException("unsupported-file", "Choose a Markdown document.");
                    }

                    root = Path.GetDirectoryName(selected);
                }

                if (root == null)
                {
                    throw new WorkspaceException("cancelled", "No folder or document was selected.");
                }

                var info = _files.Open(root);
                if (selected != null)
                {
                    return new
                    {
                        info.Id,
                        info.Name,
                        info.Mode,
                        info.Capabilities,
                        selectedPath = Path.GetFileName(selected)
                    };
                }

                return info;
            }

            case "workspace.entries":
                return await Task.Run(() => _files.Entries(Text("workspaceId")));
            case "workspace.read":
                return Convert.ToBase64String(await _files.ReadAsync(Text("workspaceId"), Text("path"), token));
            case "document.watch":
                _documentWatcher.Start(Text("workspaceId"), Text("path"), Text("watchId"), Text("fingerprint"));
                return true;
            case "document.watch.stop":
                _documentWatcher.Stop();
                return true;
            case "map.read":
                return await _files.ReadMapAsync(Text("workspaceId"), token);
            case "map.write":
                return await _files.WriteMapAsync(Text("workspaceId"), Text("revision"), Text("json"), token);
            case "session.remember":
                await _files.RememberAsync(
                    args.TryGetProperty("entry", out var entry) && entry.ValueKind == JsonValueKind.Object ? Wire.Decode<HistoryEntry>(entry.GetRawText()) : null,
                    args.GetProperty("enabled").GetBoolean(),
                    token
                );
                return true;
            case "export.save":
                return await SaveExport(Text("format"), Text("filename"), Text("html"), token);
            case "map.export":
            {
                var json = Text("json");
                var filename = Text("filename");
                if (Encoding.UTF8.GetByteCount(json) > MapLayoutCodec.MaximumBytes ||
                    Path.GetFileName(filename) != filename ||
                    filename != MapLayoutCodec.FileName)
                {
                    throw new WorkspaceException("invalid-export", "Invalid sidecar backup.");
                }

                _ = MapLayoutCodec.Read(json);

                string destination;
                if (_options.SmokeTest)
                {
                    Directory.CreateDirectory(_options.ExportDirectory!);
                    destination = Path.Combine(_options.ExportDirectory!, filename);
                }
                else
                {
                    var picker = new SaveFileDialog
                    {
                        Title = T("Save layout"),
                        FileName = filename,
                        Filter = T("Map layout JSON") + "|*.json",
                        OverwritePrompt = true
                    };
                    if (picker.ShowDialog(this) != true)
                    {
                        throw new WorkspaceException("cancelled", "Sidecar backup cancelled.");
                    }

                    destination = picker.FileName;
                }

                token.ThrowIfCancellationRequested();
                await AtomicFile.WriteTextAsync(destination, json, token);
                return true;
            }

            default:
                throw new WorkspaceException("unknown-action", "This native operation is unavailable.");
        }
    }

    /// <summary>Opens a bounded script-free rendered preview through the user's HTML browser association.</summary>
    private async Task<object> OpenBrowserPreviewAsync(string html, CancellationToken token)
    {
        if (Encoding.UTF8.GetByteCount(html) > 32 * 1048576 || !html.StartsWith("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkspaceException("invalid-export", "The browser preview exceeds its document limit.");
        }
        var previewDirectory = Path.Combine(_options.DataDirectory, "Previews");
        Directory.CreateDirectory(previewDirectory);
        var previewPath = Path.Combine(previewDirectory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(html))) + ".html");
        await AtomicFile.WriteTextAsync(previewPath, html, token);
        token.ThrowIfCancellationRequested();
        Process.Start(new ProcessStartInfo(previewPath) { UseShellExecute = true });
        // Retain a small cache for open tabs, deleting only this feature's hash-named preview files.
        foreach (var previous in new DirectoryInfo(previewDirectory).EnumerateFiles("*.html")
            .Where(file => file.Name.Length == 69 && file.Name[..64].All(Uri.IsHexDigit))
            .OrderByDescending(file => file.LastWriteTimeUtc).Skip(8))
        {
            try { previous.Delete(); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return new { opened = true };
    }

    /// <summary>Saves an edited Markdown copy through an explicit picker; opened workspace files remain protected.</summary>
    private async Task<object> SaveMarkdownCopyAsync(string source, string filename, string workspaceId, string path, CancellationToken token)
    {
        if (Path.GetFileName(filename) != filename || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || !filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || Encoding.UTF8.GetByteCount(source) > Limits.DocumentBytes)
        {
            throw new WorkspaceException("invalid-export", "Choose a valid Markdown filename within the document size limit.");
        }
        var picker = new SaveFileDialog
        {
            Title = T("Save Markdown copy"),
            FileName = filename,
            DefaultExt = ".md",
            Filter = T("Markdown documents") + "|*.md",
            OverwritePrompt = true,
            AddExtension = true
        };
        if (picker.ShowDialog(this) != true)
        {
            return new { saved = false, cancelled = true };
        }
        var destination = Path.GetFullPath(picker.FileName);
        if (!destination.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkspaceException("invalid-export", "Choose a filename ending in .md.");
        }
        using var originalGuard = ProtectMarkdownOriginal(workspaceId, path, destination);
        token.ThrowIfCancellationRequested();
        await AtomicFile.WriteTextAsync(destination, source, token);
        return new { saved = true, cancelled = false };
    }

    /// <summary>Rejects original-path aliases and temporarily prevents replacing an existing original during the copy write.</summary>
    private SafeFileHandle? ProtectMarkdownOriginal(string workspaceId, string path, string destination)
    {
        if (!workspaceId.StartsWith("native:", StringComparison.Ordinal))
        {
            return null;
        }

        var relativePath = WorkspacePath.Normalize(path, imported: true).Replace('/', Path.DirectorySeparatorChar);
        var originalPath = Path.GetFullPath(Path.Combine(_files.Root(workspaceId), relativePath));
        if (string.Equals(originalPath, destination, StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkspaceException("markdown-copy", "Choose a new filename for the edited copy, outside the opened Markdown files.");
        }

        // Metadata-only access follows junctions without requiring content access. Excluding delete sharing
        // also prevents a destination alias from replacing the original between validation and atomic rename.
        var original = OpenFileMetadata(originalPath, 0, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
        if (original.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            original.Dispose();
            if (error is 2 or 3)
            {
                // Removed originals must not prevent exporting an otherwise valid in-memory revision.
                return null;
            }
            throw new Win32Exception(error);
        }

        try
        {
            if (File.Exists(destination))
            {
                using var target = OpenFileMetadata(destination, 0, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
                if (target.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                if (string.Equals(FinalFilePath(original), FinalFilePath(target), StringComparison.OrdinalIgnoreCase))
                {
                    throw new WorkspaceException("markdown-copy", "Choose a new filename for the edited copy, outside the opened Markdown files.");
                }
            }
            return original;
        }
        catch
        {
            original.Dispose();
            throw;
        }
    }

    /// <summary>Obtains the normalized Windows path behind an opened file, resolving directory junctions and short-name aliases.</summary>
    private static string FinalFilePath(SafeFileHandle handle)
    {
        var path = new StringBuilder(512);
        var length = GetFinalPathNameByHandle(handle, path, (uint)path.Capacity, 0);
        if (length >= path.Capacity)
        {
            path.Capacity = checked((int)length + 1);
            length = GetFinalPathNameByHandle(handle, path, (uint)path.Capacity, 0);
        }
        if (length == 0 || length >= path.Capacity)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        return path.ToString();
    }

    /// <summary>Opens a metadata handle with explicit sharing while leaving file content unchanged.</summary>
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern SafeFileHandle OpenFileMetadata(string filename, uint access, FileShare share, IntPtr security,
        FileMode creation, uint flags, IntPtr template);

    /// <summary>Reads the normalized final path for a metadata handle using the Windows file API.</summary>
    [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle handle, StringBuilder path, uint capacity, uint flags);

    /// <summary>Saves the prepared HTML or PDF through the native destination and print pipeline.</summary>
    private async Task<object> SaveExport(string format, string filename, string html, CancellationToken token)
    {
        if (format is not ("html" or "pdf") ||
            Path.GetFileName(filename) != filename ||
            filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !filename.EndsWith("." + format, StringComparison.Ordinal) ||
            Encoding.UTF8.GetByteCount(html) > 32 * 1048576)
        {
            throw new WorkspaceException("invalid-export", "The export exceeds its size or filename limit.");
        }

        string destination;
        if (_options.SmokeTest)
        {
            Directory.CreateDirectory(_options.ExportDirectory!);
            destination = Path.Combine(_options.ExportDirectory!, filename);
        }
        else
        {
            var picker = new SaveFileDialog
            {
                Title = T("Save document export"),
                FileName = filename,
                DefaultExt = "." + format,
                Filter = format == "pdf" ? T("PDF document") + "|*.pdf" : T("HTML document") + "|*.html",
                OverwritePrompt = true,
                AddExtension = true
            };
            if (picker.ShowDialog(this) != true)
            {
                return new
                {
                    filename,
                    saved = false,
                    cancelled = true
                };
            }

            destination = picker.FileName;
        }

        if (!destination.EndsWith("." + format, StringComparison.OrdinalIgnoreCase))
        {
            throw new WorkspaceException("invalid-export", "Choose a filename ending in ." + format + ".");
        }

        token.ThrowIfCancellationRequested();
        if (format == "html")
        {
            await AtomicFile.WriteTextAsync(destination, html, token);
        }
        else
        {
            await _print.EnsureCoreWebView2Async(_environment);
            var core = _print.CoreWebView2;
            if (!_printConfigured)
            {
                _printConfigured = true;
                Secure(core, false);
                core.NavigationStarting += (_, args) =>
                {
                    if (!args.Uri.StartsWith(Origin + "/export/", StringComparison.Ordinal))
                    {
                        args.Cancel = true;
                    }
                };
                core.NewWindowRequested += (_, args) => args.Handled = true;
                core.WebResourceRequested += (_, args) =>
                {
                    if (_printDocuments.TryGetValue(args.Request.Uri, out var bytes))
                    {
                        args.Response = _environment!.CreateWebResourceResponse(
                            new MemoryStream(bytes, false),
                            200,
                            "OK",
                            "Content-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nContent-Security-Policy: default-src 'none'; img-src data:; style-src 'unsafe-inline'; font-src data:; script-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'"
                        );
                    }
                };
            }

            var address = Origin + "/export/" + Guid.NewGuid().ToString("N") + ".html";
            _printDocuments[address] = Encoding.UTF8.GetBytes(html);
            var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            // Waits for the hidden print WebView navigation to complete before PDF generation.
            void Completed(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                if (e.IsSuccess)
                {
                    loaded.TrySetResult();
                }
                else
                {
                    loaded.TrySetException(new IOException("The PDF document could not be loaded."));
                }
            }

            core.NavigationCompleted += Completed;
            try
            {
                core.Navigate(address);
                await loaded.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
                // Load completion includes embedded images. ExecuteScript is host initiated
                // and remains available with page scripts disabled.
                var ready = false;
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    if (await core.ExecuteScriptAsync("document.fonts.status==='loaded' && Array.from(document.images).every(i=>i.complete)") == "true")
                    {
                        ready = true;
                        break;
                    }

                    await Task.Delay(100, token);
                }

                if (!ready)
                {
                    throw new IOException("The PDF images or fonts did not finish loading.");
                }

                var settings = _environment!.CreatePrintSettings();
                settings.ShouldPrintBackgrounds = true;
                settings.ShouldPrintHeaderAndFooter = false;
                settings.PageWidth = 8.2677;
                settings.PageHeight = 11.6929;
                settings.MarginTop = settings.MarginBottom = settings.MarginLeft = settings.MarginRight = 0;
                using var pdf = await core.PrintToPdfStreamAsync(settings);
                if (pdf == null)
                {
                    throw new IOException("WebView2 did not return a PDF.");
                }

                await AtomicFile.WriteAsync(destination, pdf, token);
            }
            finally
            {
                core.NavigationCompleted -= Completed;
                _printDocuments.Remove(address);
            }
        }

        return new
        {
            filename = Path.GetFileName(destination),
            saved = true,
            cancelled = false
        };
    }

    private bool _printConfigured;
    /// <summary>Defers closing until browser edits and native requests are safe to finish.</summary>
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closing || !_initialized)
        {
            return;
        }

        e.Cancel = true;
        try
        {
            if (_pending > 0)
            {
                await _operations.WaitAsync();
                _operations.Release();
            }

            var result = await _view.CoreWebView2.ExecuteScriptAsync("window.markvibe?.canClose?.() ?? true");
            if (result != "true" &&
                MessageBox.Show(
                this,
                T("Some Markdown, annotations or map changes have not been saved, or an AI request is running. Return to save or stop it. Close and discard unfinished work?"),
                T("Unsaved changes"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            ) != MessageBoxResult.Yes)
            {
                return;
            }

            _closing = true;
            foreach (var request in _requests.Values.ToArray())
            {
                request.Cancel();
            }
            Close();
        }
        catch (Exception)
        {
            if (MessageBox.Show(this, T("Close MarkVibe? Unfinished work may not be saved."), "MarkVibe", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _closing = true;
                Close();
            }
        }
    }

    /// <summary>Reapplies the desktop backdrop after Windows appearance preferences change.</summary>
    private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Dispatcher.BeginInvoke(() =>
    {
        if (!_initialized || _closing)
        {
            return;
        }

        WindowsBackdrop.Apply(this, _view, _dark);
        _view.CoreWebView2.PostWebMessageAsJson(Wire.Encode(new { version = 1, notification = "appearance" }));
    });
}
