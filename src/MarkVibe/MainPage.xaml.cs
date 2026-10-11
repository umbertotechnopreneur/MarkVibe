using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MarkVibe.Core;
using MarkVibe.Workspace;
using OpenSilver.Themes.Modern;
using MarkVibe.Controls;

namespace MarkVibe;

public partial class MainPage : UserControl
{
    private sealed record Context(BrowserWorkspaceProvider Provider, DocumentCatalog Catalog);
    private sealed record ResumeInfo(string Mode, string Name, string WorkspaceId, string Path, string Anchor, ReadingPosition Position, bool CanResume);
    private sealed record InitialState(Preferences Preferences, bool Dark, bool RememberedFolder, string Path, string Anchor, ReadingPosition? Position, ResumeInfo? Resume, bool Desktop = false, bool Startup = false);
    private readonly Dictionary<string, Context> _contexts = new(StringComparer.Ordinal);
    private readonly NavigationHistory _history = new();
    private readonly MarkdownParser _parser = new();
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(220) };
    private Context? _active;
    private MarkdownModel? _document;
    private CancellationTokenSource _navigation = new(), _indexing = new(), _opening = new(), _exporting = new();
    private bool _exportBusy;
    private Preferences _preferences = new();
    private ResumeInfo? _resume;
    private string _view = "read", _missingPath = "";
    private bool _sidebarOpen = true, _updating, _started, _narrow, _mapFocused;
    private int _generation, _preferenceGeneration;
    private InkSession? _ink;
    private string _inkRevision = "", _inkOriginal = "", _inkMessage = "";
    private bool _inkInvalid, _inkBusy;
    private readonly SemaphoreSlim _inkGate = new(1, 1);
    private sealed record SidecarState(string? Json, string Revision);

    public MainPage()
    {
        InitializeComponent();
        Loaded += Start;
        Unloaded += Stop;
        SizeChanged += (_, _) => LayoutViews();
        _searchTimer.Tick += async (_, _) => { _searchTimer.Stop(); RefreshSidebar(); if (_view == "map") await ExecuteAsync(ShowMapAsync); };
    }

    private async void Start(object sender, RoutedEventArgs arguments)
    {
        if (_started) return;
        _started = true;
        ButtonIcons.Decorate(this);
        LayoutViews();
        BrowserServices.Runtime.OnEvent(HandleBrowserEventAsync);
        await ExecuteAsync(async () =>
        {
            var initial = await BrowserServices.CallAsync<InitialState>("initialize");
            _preferences = initial.Preferences.Validated();
            SynchronizeSettings();
            await ApplyPreferencesAsync();
            if (initial.Desktop) { ExportHelp.Text = "Save HTML or PDF directly to a local file. Images, diagrams and resolved ink are included."; WorkspaceHelp.Text = "Local files stay on this device. Handwritten notes are saved separately beside the Markdown."; }
            if (initial.Startup) { await OpenWorkspaceAsync("startup"); return; }
            _resume = initial.Resume;
            if (_resume != null)
            {
                if (_resume.CanResume)
                {
                    try { await OpenWorkspaceAsync("resume", initial.Path, initial.Anchor, initial.Position); return; }
                    catch (WorkspaceException) { /* The folder may have lost permission since initialization. */ }
                }
                ShowResume();
            }
            else await OpenWorkspaceAsync("demo", initial.Path, initial.Anchor, initial.Position);
        });
    }

    private async void Stop(object sender, RoutedEventArgs arguments)
    {
        _generation++; _navigation.Cancel(); _indexing.Cancel(); _opening.Cancel(); _exporting.Cancel(); _searchTimer.Stop();
        foreach (var context in _contexts.Values)
            try { await context.Provider.DisposeAsync(); } catch (Exception) { }
        _contexts.Clear();
        await BrowserServices.Runtime.DisposeAsync();
        _navigation.Dispose(); _indexing.Dispose(); _opening.Dispose(); _exporting.Dispose();
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        try { await action(); }
        catch (OperationCanceledException) { StatusText.Text = "Action cancelled. Your current workspace is still available."; }
        catch (WorkspaceException error) { StatusText.Text = error.Message; }
        catch (Exception) { StatusText.Text = "This action could not be completed. Try again or reopen the folder."; }
    }

    private async Task OpenWorkspaceAsync(string mode, string initialPath = "", string anchor = "", ReadingPosition? position = null)
    {
        await FlushInkAsync();
        _exporting.Cancel(); ExportHtmlButton.IsEnabled = ExportPdfButton.IsEnabled = false;
        _opening.Cancel(); _opening.Dispose(); _opening = new();
        var cancellation = _opening.Token;
        CancelButton.Visibility = Visibility.Visible;
        StatusText.Text = "Opening your workspace…";
        await CaptureHistoryAsync();
        try
        {
            var info = await BrowserServices.CallAsync<WorkspaceInfo>("workspace.open", new { mode }, cancellation);
            if (initialPath.Length == 0 && mode != "demo")
            {
                var saved = await BrowserServices.CallAsync<JsonElement>("history.saved", new { workspaceId = info.Id }, cancellation);
                if (saved.ValueKind == JsonValueKind.Object)
                {
                    var entry = Wire.Decode<HistoryEntry>(saved.GetRawText());
                    initialPath = entry.Path; anchor = entry.Anchor; position = entry.Position;
                }
            }
            var provider = new BrowserWorkspaceProvider(info);
            var catalog = new DocumentCatalog(_parser);
            try { catalog.Seed(await provider.EnumerateAsync(cancellation)); }
            catch { await provider.DisposeAsync(); throw; }
            // The browser owns replacement of a provider with the same stable workspace id.
            _contexts[info.Id] = new(provider, catalog);
            _active = _contexts[info.Id];
            while (_contexts.Count > 2)
            {
                var oldest = _contexts.First(c => c.Key != info.Id);
                await oldest.Value.Provider.DisposeAsync(); _contexts.Remove(oldest.Key);
            }
            _indexing.Cancel();
            SettingsPanel.Visibility = Visibility.Collapsed;
            ResumeWorkspaceButton.Visibility = Visibility.Collapsed;
            _resume = null;
            SearchBox.Text = "";
            RefreshSidebar(); UpdateWorkspaceLabels();
            var first = catalog.Cards.FirstOrDefault(c => c.Path == initialPath)?.Path
                ?? catalog.Cards.FirstOrDefault(c => c.Path.Equals("README.md", StringComparison.OrdinalIgnoreCase))?.Path
                ?? catalog.Cards.FirstOrDefault()?.Path;
            if (first == null)
            {
                _document = null;
                ShowMessage("No Markdown pages yet", "Choose Markdown files or a folder containing documentation.");
            }
            else await NavigateAsync(first, first == initialPath ? anchor : "", true, first == initialPath ? position : null);
            _ = IndexAsync(_active);
        }
        finally { CancelButton.Visibility = Visibility.Collapsed; UpdateExportButtons(); }
    }

    private async Task IndexAsync(Context context)
    {
        _indexing.Dispose(); _indexing = new();
        var cancellation = _indexing.Token;
        try
        {
            await context.Catalog.IndexAsync(context.Provider, (done, total) =>
            {
                if (context != _active) return;
                IndexLabel.Text = $"Indexed {done} of {total} pages";
                if (done % 10 == 0 || done == total) RefreshSidebar();
            }, cancellation);
            if (context == _active)
            {
                RefreshSidebar();
                IndexLabel.Text = context.Catalog.IndexComplete ? $"{context.Catalog.Cards.Count} pages · on this device" : "Partial content index · paths remain searchable";
                if (context.Catalog.Warnings.Count > 0) StatusText.Text = context.Catalog.Warnings[0];
                if (_view == "map") await ShowMapAsync();
            }
        }
        catch (OperationCanceledException) { if (context == _active) IndexLabel.Text = "Index paused · pages can still be opened"; }
        catch (Exception) { if (context == _active) IndexLabel.Text = "Index unavailable · reopen the folder"; }
    }

    private async Task NavigateAsync(string path, string anchor = "", bool push = true, ReadingPosition? position = null)
    {
        if (_active == null) return;
        await FlushInkAsync();
        _exporting.Cancel(); ExportHtmlButton.IsEnabled = ExportPdfButton.IsEnabled = false;
        _navigation.Cancel(); _navigation.Dispose(); _navigation = new();
        var cancellation = _navigation.Token;
        var generation = ++_generation;
        var context = _active;
        path = WorkspacePath.Normalize(path);
        if (push) await CaptureHistoryAsync();
        StatusText.Text = "Opening page…";
        try
        {
            var model = await context.Catalog.OpenAsync(context.Provider, path, cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (generation != _generation || context != _active) return;
            _document = model;
            await LoadInkAsync(model, context, cancellation);
            _missingPath = "";
            MessageOverlay.Visibility = Visibility.Collapsed;
            UpdateExportButtons();
            DocumentTitle.Text = model.Title; Breadcrumb.Text = model.Path;
            OutlineList.ItemsSource = model.Headings;
            RefreshSidebar(); UpdateWorkspaceLabels();
            if (push)
            {
                var entry = _history.Push(context.Provider.Info.Id, model.Path, anchor);
                await BrowserServices.CallAsync("history.push", entry, cancellation);
            }
            if (_narrow) _sidebarOpen = false;
            LayoutViews();
            if (_view == "source" && position == null && anchor.Length > 0)
            {
                var heading = model.Headings.FirstOrDefault(h => h.Id == anchor);
                if (heading != null) position = new("", 0, heading.Line);
            }
            await ReaderHost.DisplayAsync(model, context.Provider.Info.Id, position, position == null ? anchor : "", cancellation);
            await RenderInkAsync();
            if (_view is "source" or "split") await ShowSourceAsync(cancellation);
            if (_view == "map") await ShowMapAsync();
            if (position != null) await BrowserServices.CallAsync("source.restore", new { hostId = SourceHost.HostId, position }, cancellation);
            BackButton.IsEnabled = _history.CanBack; ForwardButton.IsEnabled = _history.CanForward;
            StatusText.Text = model.LineCount.ToString() + " source lines · " + (_inkMessage.Length > 0 ? _inkMessage : "Markdown stays unchanged · annotate paragraphs below");
            if (_narrow) { _sidebarOpen = false; LayoutViews(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (generation != _generation) return;
            _missingPath = path;
            ShowMessage("This page could not be opened", error is WorkspaceException ? error.Message : "Reopen the folder or choose this page again.");
            StatusText.Text = "Choose the missing file or reopen its folder.";
        }
    }

    private async Task CaptureHistoryAsync()
    {
        if (_document == null || _history.Current == null || _view == "map" || (_narrow && _sidebarOpen)) return;
        var position = await ReaderHost.CaptureAsync();
        if (_view == "source") position = await BrowserServices.CallAsync<ReadingPosition>("source.capture", new { hostId = SourceHost.HostId });
        _history.Capture(position);
        await BrowserServices.CallAsync("history.replace", _history.Current);
    }

    private Task ShowSourceAsync(CancellationToken cancellationToken = default) =>
        _document == null || _active == null ? Task.CompletedTask : SourceHost.DisplayAsync(_document, _active.Provider.Info.Id, _preferences, cancellationToken);

    private Task ShowMapAsync()
    {
        if (_active == null) return Task.CompletedTask;
        return MapHost.DisplayAsync(_active.Catalog.Map(_document?.Path ?? "", SearchBox.Text, _mapFocused), _document?.Path ?? "", _mapFocused);
    }

    private async Task SetViewAsync(string view)
    {
        await CaptureHistoryAsync();
        var position = _history.Current?.Position;
        _view = view == "split" && ActualWidth < 1050 ? "source" : view;
        _sidebarOpen = !_narrow && _sidebarOpen;
        LayoutViews();
        if (_document != null && _active != null && (_view is "read" or "split"))
            await ReaderHost.DisplayAsync(_document, _active.Provider.Info.Id, position, "", _navigation.Token);
        if (_view is "source" or "split")
        {
            await ShowSourceAsync();
            if (position != null) await BrowserServices.CallAsync("source.restore", new { hostId = SourceHost.HostId, position });
        }
        if (_view == "map") await ShowMapAsync();
    }

    private void LayoutViews()
    {
        var wasNarrow = _narrow;
        _narrow = ActualWidth > 0 && ActualWidth < 760;
        if (_narrow && !wasNarrow) _sidebarOpen = false;
        if (_narrow && _view == "split") _view = "source";
        var sidebarVisible = _sidebarOpen;
        Sidebar.Visibility = sidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumnSpan(Sidebar, _narrow ? 3 : 1);
        DocumentPanel.Visibility = _narrow && sidebarVisible ? Visibility.Collapsed : Visibility.Visible;
        SidebarColumn.Width = new GridLength(sidebarVisible && !_narrow ? Math.Clamp(SidebarColumn.ActualWidth, 190, 360) : 0);
        SettingsPanel.Width = Math.Max(240, Math.Min(310, ActualWidth - 36));
        SettingsPanel.MaxHeight = Math.Max(120, ActualHeight - 112);
        SidebarDividerColumn.Width = new GridLength(sidebarVisible && !_narrow ? 8 : 0);
        SidebarSplitter.Visibility = sidebarVisible && !_narrow ? Visibility.Visible : Visibility.Collapsed;
        BrandName.Visibility = ActualWidth < 1100 ? Visibility.Collapsed : Visibility.Visible;
        BrandBadge.Visibility = _narrow ? Visibility.Collapsed : Visibility.Visible;
        TitlePanel.Visibility = ActualWidth < 900 ? Visibility.Collapsed : Visibility.Visible;
        OpenFolderButton.Visibility = _narrow ? Visibility.Collapsed : Visibility.Visible;
        ButtonIcons.SetText(OpenFileButton, _narrow ? "Open" : "Open file");
        foreach (var button in new[] { ReadButton, SourceButton, SplitButton, MapButton }) ButtonIcons.SetCompact(button, _narrow);
        SplitButton.Visibility = ActualWidth < 1050 ? Visibility.Collapsed : Visibility.Visible;
        SearchBox.Width = _narrow ? Math.Clamp(ActualWidth - 230, 88, 190) : 220;
        ZoomOutButton.Visibility = ZoomInButton.Visibility = ZoomLabel.Visibility = _narrow ? Visibility.Collapsed : Visibility.Visible;
        var source = _view is "source" or "split";
        var read = _view is "read" or "split";
        SourceHost.Visibility = source ? Visibility.Visible : Visibility.Collapsed;
        ReaderHost.Visibility = read ? Visibility.Visible : Visibility.Collapsed;
        MapHost.Visibility = _view == "map" ? Visibility.Visible : Visibility.Collapsed;
        SourceColumn.Width = source ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        ReadColumn.Width = read || _view == "map" ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        SplitDividerColumn.Width = new GridLength(_view == "split" ? 6 : 0);
        SplitDivider.Visibility = _view == "split" ? Visibility.Visible : Visibility.Collapsed;
        foreach (var (button, name) in new[] { (ReadButton, "read"), (SourceButton, "source"), (SplitButton, "split"), (MapButton, "map") })
            button.Background = (Brush)Application.Current.Resources[name == _view ? "AccentSoft" : "ShellBackground"];
        if (_started) OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.sync($0,$1,$2)", ReaderHost.HostId, SourceHost.HostId, _view == "split");
    }

    private void RefreshSidebar()
    {
        if (_active == null) return;
        _updating = true;
        var cards = _active.Catalog.Search(SearchBox.Text).Select(r => r.Document with { Introduction = r.Snippet }).ToList();
        DocumentList.ItemsSource = cards;
        DocumentList.SelectedItem = cards.FirstOrDefault(c => c.Path == _document?.Path);
        _updating = false;
    }

    private void UpdateWorkspaceLabels()
    {
        if (_active == null) return;
        var info = _active.Provider.Info;
        WorkspaceName.Text = info.Name;
        WorkspaceMode.Text = info.Mode switch
        {
            "native" => "Local folder · ink saved beside Markdown",
            "connected" => info.Capabilities.HandleRemembered ? "Connected folder · reopen after a restart" : "Connected folder · this session",
            "imported" => "Imported folder · select again to refresh",
            "files" => "Selected files · add related pages and images",
            "zip" => "Imported bundle · select again to refresh",
            _ => "Sample workspace · explore freely"
        };
        RefreshButton.IsEnabled = info.Capabilities.CanRefresh;
        RelatedFilesButton.IsEnabled = info.Capabilities.CanAddFiles;
    }

    private void ShowMessage(string title, string message)
    {
        MessageTitle.Text = title; MessageText.Text = message;
        MessageOverlay.Visibility = Visibility.Visible;
        UpdateExportButtons();
        ChooseMissingButton.Visibility = _missingPath.Length > 0 && _active?.Provider.Info.Capabilities.CanAddFiles == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task ApplyPreferencesAsync()
    {
        var generation = ++_preferenceGeneration;
        var result = await BrowserServices.CallAsync<JsonElement>("preferences.apply", new { preferences = _preferences, readerId = ReaderHost.HostId, sourceId = SourceHost.HostId });
        if (generation != _preferenceGeneration) return;
        var dark = result.GetProperty("dark").GetBoolean();
        ((ModernTheme)Application.Current.Theme).CurrentPalette = dark ? ModernTheme.Palettes.Dark : ModernTheme.Palettes.Light;
        var palette = dark
            ? new[] { "#111621", "#181F2D", "#E5EAF4", "#A4B0C4", "#303C52", "#929AFF", "#2A3155" }
            : new[] { "#F4F5F7", "#FFFFFF", "#20283A", "#596579", "#DDE2EA", "#525CE8", "#ECEEFE" };
        var keys = new[] { "ShellBackground", "PanelBackground", "ShellText", "MutedText", "ShellBorder", "Accent", "AccentSoft" };
        for (var index = 0; index < keys.Length; index++)
        {
            var hex = palette[index];
            ((SolidColorBrush)Application.Current.Resources[keys[index]]).Color = Color.FromArgb(255, Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16));
        }
        ZoomLabel.Text = _preferences.Zoom + "%";
        if (!result.GetProperty("persisted").GetBoolean()) StatusText.Text = "Preferences are available for this session. Browser storage is unavailable.";
    }

    private void SynchronizeSettings()
    {
        _updating = true;
        AppearanceCombo.SelectedIndex = Array.IndexOf(new[] { "system", "light", "dark" }, _preferences.Appearance);
        StyleCombo.SelectedIndex = Array.IndexOf(new[] { "technical", "editorial", "compact" }, _preferences.DocumentStyle);
        SimpleSourceCheck.IsChecked = _preferences.SimpleSource;
        RememberDocumentCheck.IsChecked = _preferences.RememberLastDocument;
        _updating = false;
    }

    private async Task HandleBrowserEventAsync(string json) => await ExecuteAsync(async () =>
    {
        using var data = JsonDocument.Parse(json);
        var root = data.RootElement;
        switch (root.GetProperty("type").GetString())
        {
            case "ink": await HandleInkAsync(root); break;
            case "navigate":
                if (_view == "map") await SetViewAsync("read");
                await NavigateAsync(root.GetProperty("path").GetString() ?? "", root.TryGetProperty("anchor", out var anchor) ? anchor.GetString() ?? "" : "");
                break;
            case "history":
                var entry = Wire.Decode<HistoryEntry>(root.GetProperty("entry").GetRawText());
                if (!_contexts.TryGetValue(entry.WorkspaceId, out var context)) { ShowMessage("Reopen this workspace", "Open its folder, files or bundle again to continue from this page."); return; }
                _active = context;
                _history.Restore(entry);
                await NavigateAsync(entry.Path, entry.Anchor, false, entry.Position);
                if (!context.Catalog.IndexComplete) { _indexing.Cancel(); _ = IndexAsync(context); }
                break;
            case "search": SearchBox.Focus(); break;
            case "status": StatusText.Text = root.GetProperty("message").GetString() ?? ""; break;
            case "appearance": await ApplyPreferencesAsync(); break;
            case "source-preference":
                _preferences = _preferences with { SimpleSource = root.GetProperty("simple").GetBoolean() };
                SynchronizeSettings(); await ApplyPreferencesAsync(); await ShowSourceAsync(); break;
            case "map-focus":
                _mapFocused = root.GetProperty("focused").GetBoolean();
                await ShowMapAsync();
                break;
            case "resource-added": await ReloadCatalogAsync(root.GetProperty("path").GetString() ?? _document?.Path ?? ""); break;
        }
    });

    private async Task ReloadCatalogAsync(string path)
    {
        if (_active == null) return;
        await CaptureHistoryAsync();
        var samePage = path == _document?.Path;
        var position = samePage ? _history.Current?.Position : null;
        _indexing.Cancel();
        _active.Catalog.Seed(await _active.Provider.EnumerateAsync(_opening.Token));
        await NavigateAsync(path, "", !samePage, position);
        _ = IndexAsync(_active);
    }

    private async void DocumentSelected(object sender, SelectionChangedEventArgs arguments)
    {
        if (!_updating && DocumentList.SelectedItem is DocumentCard card && card.Path != _document?.Path)
            await ExecuteAsync(() => NavigateAsync(card.Path));
    }
    private async void HeadingSelected(object sender, SelectionChangedEventArgs arguments)
    {
        if (OutlineList.SelectedItem is Heading heading && _document != null) await ExecuteAsync(() => NavigateAsync(_document.Path, heading.Id));
    }
    private void SearchChanged(object sender, TextChangedEventArgs arguments) { if (!_started) return; _searchTimer.Stop(); _searchTimer.Start(); }
    private async void ToggleSidebar(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        await CaptureHistoryAsync(); _sidebarOpen = !_sidebarOpen; LayoutViews();
    });
    private void ToggleSettings(object sender, RoutedEventArgs arguments) => SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    private void ShowAbout(object sender, RoutedEventArgs arguments) => new AboutWindow().Show();
    private void ShowResume()
    {
        if (_resume == null) return;
        WorkspaceName.Text = _resume.Name;
        WorkspaceMode.Text = "Last workspace · reopen to continue";
        DocumentTitle.Text = "Continue reading";
        Breadcrumb.Text = _resume.Path;
        ShowMessage("Continue where you left off", $"Reopen {_resume.Name} to continue reading {_resume.Path}. Your last reading position is remembered on this device.");
        ResumeWorkspaceButton.Visibility = Visibility.Visible;
        StatusText.Text = "Choose the last workspace again to allow access to its documents.";
    }
    private async void ResumeWorkspace(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        if (_resume == null) return;
        var mode = _resume.Mode == "connected" ? "reopen" : _resume.Mode;
        await OpenWorkspaceAsync(mode);
    });
    private void ShowPages(object sender, RoutedEventArgs arguments) { DocumentList.Visibility = Visibility.Visible; OutlineList.Visibility = Visibility.Collapsed; }
    private void ShowOutline(object sender, RoutedEventArgs arguments) { DocumentList.Visibility = Visibility.Collapsed; OutlineList.Visibility = Visibility.Visible; }
    private void CancelWork(object sender, RoutedEventArgs arguments) { _opening.Cancel(); _indexing.Cancel(); _navigation.Cancel(); _exporting.Cancel(); }
    private void UpdateExportButtons() => ExportHtmlButton.IsEnabled = ExportPdfButton.IsEnabled = !_exportBusy && _document != null && MessageOverlay.Visibility != Visibility.Visible;
    private async Task ExportAsync(string format)
    {
        if (_document == null || _active == null || _exportBusy) return;
        var model = _document; var context = _active;
        _exporting.Dispose(); _exporting = new();
        var cancellation = _exporting.Token;
        _exportBusy = true; UpdateExportButtons();
        CancelButton.Visibility = Visibility.Visible;
        StatusText.Text = "Preparing document export…";
        try
        {
            await BrowserServices.CallAsync("ink.flush", cancellationToken: cancellation);
            var result = await BrowserServices.CallAsync<JsonElement>("document.export", new { document = model, workspaceId = context.Provider.Info.Id, preferences = _preferences, format, ink = _ink?.Resolved }, cancellation);
            cancellation.ThrowIfCancellationRequested();
            var warnings = result.GetProperty("warnings").GetInt32();
            StatusText.Text = (result.TryGetProperty("cancelled", out var cancelled) && cancelled.GetBoolean() ? "Export cancelled." : result.TryGetProperty("saved", out var savedExport) && savedExport.GetBoolean() ? "Saved " + result.GetProperty("filename").GetString() : format == "html" ? "HTML download started: " + result.GetProperty("filename").GetString() : "Print dialog opened. Choose Save as PDF to save the document.")
                + (warnings > 0 ? $" · {warnings} unavailable resources marked in the document" : "");
        }
        catch (OperationCanceledException) { if (_document == model && _active == context) StatusText.Text = "Document export cancelled."; }
        finally { _exportBusy = false; UpdateExportButtons(); if (_document == model && _active == context) CancelButton.Visibility = Visibility.Collapsed; }
    }
    private async void ExportHtml(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => ExportAsync("html"));
    private async void ExportPdf(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => ExportAsync("pdf"));
    private async void Back(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () => { await CaptureHistoryAsync(); await BrowserServices.CallAsync("history.go", new { delta = -1 }); });
    private async void Forward(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () => { await CaptureHistoryAsync(); await BrowserServices.CallAsync("history.go", new { delta = 1 }); });
    private async void OpenFiles(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => OpenWorkspaceAsync("files"));
    private async void OpenFolder(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => OpenWorkspaceAsync("folder"));
    private async void ImportFolder(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => OpenWorkspaceAsync("imported"));
    private async void OpenZip(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => OpenWorkspaceAsync("zip"));
    private async void ReopenFolder(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => OpenWorkspaceAsync("reopen"));
    private async void OpenDemo(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => OpenWorkspaceAsync("demo"));
    private async void ReadView(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("read"));
    private async void SourceViewClick(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("source"));
    private async void SplitView(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("split"));
    private async void MapViewClick(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("map"));
    private async void AddFiles(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        if (_active == null) return;
        await BrowserServices.CallAsync("workspace.addFiles", new { workspaceId = _active.Provider.Info.Id });
        await ReloadCatalogAsync(_document?.Path ?? "");
    });
    private async void ChooseMissing(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        if (_active == null) return;
        await BrowserServices.CallAsync("workspace.addMissing", new { workspaceId = _active.Provider.Info.Id, path = _missingPath });
        await ReloadCatalogAsync(_missingPath);
    });
    private async void RefreshWorkspace(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        if (_active == null) return;
        await _active.Provider.RefreshAsync(_opening.Token);
        await ReloadCatalogAsync(_document?.Path ?? "");
    });
    private async void PreferenceChanged(object sender, SelectionChangedEventArgs arguments)
    {
        if (_updating || !_started || AppearanceCombo.SelectedIndex < 0 || StyleCombo.SelectedIndex < 0) return;
        _preferences = _preferences with { Appearance = new[] { "system", "light", "dark" }[AppearanceCombo.SelectedIndex], DocumentStyle = new[] { "technical", "editorial", "compact" }[StyleCombo.SelectedIndex] };
        await ExecuteAsync(ApplyPreferencesAsync);
    }
    private async void SourcePreferenceChanged(object sender, RoutedEventArgs arguments)
    {
        if (_updating || !_started) return;
        _preferences = _preferences with { SimpleSource = SimpleSourceCheck.IsChecked == true };
        await ExecuteAsync(async () => { await ApplyPreferencesAsync(); if (_view is "source" or "split") await ShowSourceAsync(); });
    }
    private async void RememberPreferenceChanged(object sender, RoutedEventArgs arguments)
    {
        if (_updating || !_started) return;
        _preferences = _preferences with { RememberLastDocument = RememberDocumentCheck.IsChecked == true };
        await ExecuteAsync(ApplyPreferencesAsync);
    }
    private async void ResetPreferences(object sender, RoutedEventArgs arguments)
    {
        _preferences = new();
        SynchronizeSettings();
        await ExecuteAsync(async () => { await ApplyPreferencesAsync(); if (_view is "source" or "split") await ShowSourceAsync(); });
    }
    private async void ZoomOut(object sender, RoutedEventArgs arguments) { _preferences = (_preferences with { Zoom = _preferences.Zoom - 10 }).Validated(); await ExecuteAsync(ApplyPreferencesAsync); }
    private async void ZoomIn(object sender, RoutedEventArgs arguments) { _preferences = (_preferences with { Zoom = _preferences.Zoom + 10 }).Validated(); await ExecuteAsync(ApplyPreferencesAsync); }
}
