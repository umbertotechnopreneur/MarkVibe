/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; readability revision; 2026-10-11
 * Modified with AI: OpenAI Codex; retain browser history navigation during workspace opening; 2026-10-23
 * Modified with AI: OpenAI Codex; adapt toolbar spacing for reader width controls; 2026-10-24
 * Human guidance: Umberto Giacobbi; use the available horizontal reading space
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Modified with AI: OpenAI Codex; Markdown AI lifecycle and revision integration; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Modified with AI: OpenAI Codex; independent map card selection; 2026-10-16
 * Human guidance: Umberto Giacobbi; one click selects and double-click opens a document
 * Modified with AI: OpenAI Codex; host-resolved application font preference; 2026-10-16
 * Human guidance: Umberto Giacobbi; one installed UI font selector that leaves document typography separate
 * Modified with AI: OpenAI Codex; project managed atlas lenses and remove the retired binary focus event; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement the approved document atlas navigation
 * Modified with AI: OpenAI Codex; live theme brushes, readable palette updates and restored inspector width; 2026-10-16
 * Human guidance: Umberto Giacobbi; consistent theme changes and persistent standard splitters
 * Modified with AI: OpenAI Codex; track successful navigation and route guarded exploration commands; 2026-10-16
 * Modified with AI: OpenAI Codex; host the shared publication footer and canonical initiative badge; 2026-10-16
 * Modified with AI: OpenAI Codex; refresh AI connections only when provider or validated credentials change; 2026-10-16
 * Modified with AI: OpenAI Codex; notify the startup service after initialization; 2026-10-25
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Core;
using MarkVibe.Workspace;
using OpenSilver.Themes.Modern;
using MarkVibe.Controls;

namespace MarkVibe;

/// <summary>Coordinates workspace navigation, document views and the persistent application shell.</summary>
public partial class MainPage : UserControl
{
    /// <summary>Owns the provider, authoritative document catalog and map state for one opened workspace.</summary>
    private sealed record Context(BrowserWorkspaceProvider Provider, DocumentCatalog Catalog, MapWorkspaceState Map)
    {
        public AiDocumentCollection AiContext { get; } = new();
    }

    /// <summary>Describes a recoverable workspace and its last saved document position.</summary>
    private sealed record ResumeInfo(string Mode, string Name, string WorkspaceId, string Path, string Anchor, ReadingPosition Position, bool CanResume);

    /// <summary>Carries the host's startup capabilities, preferences and resumable workspace snapshot.</summary>
    private sealed record InitialState(
        Preferences Preferences,
        bool Dark,
        bool RememberedFolder,
        string Path,
        string Anchor,
        ReadingPosition? Position,
        ResumeInfo? Resume,
        bool Desktop = false,
        bool Startup = false,
        ShellLayout? Layout = null
    );
    private readonly Dictionary<string, Context> _contexts = new(StringComparer.Ordinal);
    private readonly NavigationHistory _history = new();
    private readonly MarkdownParser _parser = new();
    private Context? _active;
    private MarkdownModel? _document;
    private CancellationTokenSource _navigation = new(), _indexing = new(), _opening = new(), _exporting = new();
    private bool _exportBusy, _documentBusy, _desktop, _acrylicAvailable;
    /// <summary>Projects the persistence result owned by the reader preference service.</summary>
    private bool _preferencesPersisted => _readerPresentation.State.PreferencesPersisted;
    private bool _workspaceTransitionBusy;
    /// <summary>The latest browser Back/Forward destination received while providers are being replaced.</summary>
    private HistoryEntry? _pendingHistory;
    private SettingsWindow? _settingsDialog;
    /// <summary>Reads the application's authoritative preference model.</summary>
    private Preferences _preferences => _readerPresentation.State.Preferences;
    private ResumeInfo? _resume;
    private string _view = "read", _missingPath = "";
    private string _missingAsset = "", _missingAssetWorkspace = "";
    private bool _sidebarOpen = true, _updating, _started, _narrow;
    private int _generation;
    /// <summary>Initializes the shell controls and responsive layout handlers.</summary>
    public MainPage()
    {
        InitializeComponent();
        InitializeLayoutPersistence();
        InitializeFileMenus();
        InitializeRecentLocations();
        InitializeDocumentWatch();
        PublicationFooter.AboutRequested += ShowAbout;
        Loaded += Start;
        Unloaded += Stop;
        SizeChanged += (_, _) => LayoutViews();
        AiHost.ActionRequested += request => ExecuteAsync(() => HandleMarkdownAiAsync(request));
        AiWaitHost.CancelRequested += CancelMarkdownAi;
        SourceHost.ActionRequested += ExecuteAsync;
        MapHost.ActionRequested += ExecuteAsync;
        MapHost.NavigateRequested += (path, anchor) => ExecuteAsync(async () => { await SetViewAsync("read"); await NavigateAsync(path, anchor); });
        MapHost.AddToAiRequested += (workspaceId, path) => ExecuteAsync(() => ToggleAiContextAsync(workspaceId, path));
        MapHost.FindDocumentsRequested += FindMapDocuments;
        MapHost.CenterDocumentRequested += (workspaceId, path) => ExecuteAsync(() => CenterMapDocumentAsync(workspaceId, path));
        SourceHost.SimpleRequested += simple => ExecuteAsync(async () =>
        {
            _readerPresentation.SetSimpleSource(simple);
            SynchronizeSettings();
            await ApplyPreferencesAsync();
            await ShowSourceAsync();
        });
    }

    /// <summary>Initializes browser state and restores the last permitted workspace or opens the local demo.</summary>
    private async void Start(object sender, RoutedEventArgs arguments)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        VibeWareHeaderLogo.Source = IconLabel.Asset("images/vibeware-logo.png");
        AppLocalization.Apply(this);
        LayoutViews();
        BrowserServices.Runtime.OnEvent(HandleBrowserEventAsync);
        await ExecuteAsync(async () =>
        {
            var initial = await BrowserServices.CallAsync<InitialState>("initialize");
            _desktop = initial.Desktop;
            ConfigureRecentLocationButtons();
            _readerPresentation.SetPreferences(initial.Preferences);
            _readerPresentation.RestoreLayout(initial.Layout ?? new ShellLayout());
            MapHost.RestoreInspectorWidth(_layout.MapInspectorWidth);
            _sidebarOpen = _layout.SidebarOpen && !_narrow;

            LayoutViews();
            SynchronizeSettings();
            await ApplyPreferencesAsync();
            SynchronizeSettings();
            _ = ExecuteAsync(RefreshMarkdownAiKeyAsync);
            if (initial.Startup)
            {
                await OpenWorkspaceAsync("startup");
                return;
            }

            _resume = initial.Resume;
            if (_resume != null)
            {
                if (_resume.CanResume)
                {
                    try
                    {
                        await OpenWorkspaceAsync("resume", initial.Path, initial.Anchor, initial.Position);
                        return;
                    }
                    catch (WorkspaceException)
                    { /* The folder may have lost permission since initialization. */
                    }
                }

                ShowResume();
            }
            else
            {
                await OpenWorkspaceAsync("demo", initial.Path, initial.Anchor, initial.Position);
            }
        });
        await ExecuteAsync(() => BrowserServices.CallAsync("startup.ready"));
    }

    /// <summary>Cancels outstanding work and releases workspace providers and browser resources on unload.</summary>
    private async void Stop(object sender, RoutedEventArgs arguments)
    {
        _documentWatchId = "";
        _pendingDiskChange = null;
        _diskChangeTimer.Stop();
        _generation++;
        _aiConnectionGeneration++;
        _aiLifetime.Cancel();
        CancelMarkdownAi();
        _navigation.Cancel();
        _indexing.Cancel();
        _opening.Cancel();
        _exporting.Cancel();
        foreach (var context in _contexts.Values)
        {
            try
            {
                await context.Provider.DisposeAsync();
            }
            catch (Exception)
            {
            }
        }

        _contexts.Clear();
        await BrowserServices.Runtime.DisposeAsync();
        _navigation.Dispose();
        _indexing.Dispose();
        _opening.Dispose();
        _exporting.Dispose();
    }

    /// <summary>Runs a UI action and converts cancellation or provider failures into user-facing status text.</summary>
    private async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            SetStatus("Action cancelled. Your current workspace is still available.");
        }
        catch (WorkspaceException error)
        {
            ShowActionNotice(error.Message);
        }
        catch (Exception)
        {
            ShowActionNotice("This action could not be completed. Try again or reopen the folder.");
        }
    }

    /// <summary>Freezes graph input around provider replacement, resuming it when opening fails.</summary>
    private async Task OpenWorkspaceAsync(string mode, string initialPath = "", string anchor = "", ReadingPosition? position = null,
        string recentPath = "", string recentKind = "")
    {
        AssertMarkdownEditIdle();
        if (_workspaceTransitionBusy)
        {
            throw new WorkspaceException("workspace-busy", "Wait for the workspace to finish opening.");
        }
        _workspaceTransitionBusy = true;
        OpenFileButton.IsEnabled = false;
        OpenFolderButton.IsEnabled = false;
        UpdateParentFolderActions();
        var mapSuspended = false;
        try
        {
            await BrowserServices.CallAsync("map.suspend");
            mapSuspended = true;
            await OpenWorkspaceCoreAsync(mode, initialPath, anchor, position, recentPath, recentKind);
        }
        finally
        {
            try
            {
                if (mapSuspended)
                {
                    await BrowserServices.CallAsync("map.resume");
                }
            }
            finally
            {
                _workspaceTransitionBusy = false;
                OpenFileButton.IsEnabled = true;
                OpenFolderButton.IsEnabled = true;
                UpdateParentFolderActions();
                if (_pendingHistory is { } pendingHistory)
                {
                    _pendingHistory = null;
                    await RestoreHistoryAsync(pendingHistory);
                }
            }
        }
    }

    /// <summary>Flushes pending edits, opens and seeds a provider, restores its page and starts bounded indexing.</summary>
    private async Task OpenWorkspaceCoreAsync(string mode, string initialPath, string anchor, ReadingPosition? position,
        string recentPath, string recentKind)
    {
        if (UnsavedDocumentCount() > 0)
        {
            throw new WorkspaceException("markdown-unsaved", "Save a Markdown copy or restore edited documents before opening another workspace.");
        }
        await CloseDocumentFindAsync();
        CancelMarkdownAi();
        await FlushMapAsync(changingWorkspace: true);
        _exporting.Cancel();
        _documentBusy = true;
        UpdateExportButtons();
        _opening.Cancel();
        _opening.Dispose();
        _opening = new();
        var cancellation = _opening.Token;
        CancelButton.Visibility = Visibility.Visible;
        ShowActionNotice("Opening your workspace…");
        await CaptureHistoryAsync();
        var mapGateHeld = false;
        try
        {
            await _mapGate.WaitAsync(cancellation);
            mapGateHeld = true;
            // Revalidate immediately before a provider with the same stable ID can be replaced.
            AssertMarkdownEditIdle();
            if (UnsavedDocumentCount() > 0)
            {
                throw new WorkspaceException("markdown-unsaved", "Save a Markdown copy or restore edited documents before opening another workspace.");
            }
            var info = await BrowserServices.CallAsync<WorkspaceInfo>("workspace.open", new
            {
                mode,
                recentPath,
                recentKind,
                workspaceId = _active?.Provider.Info.Id ?? ""
            }, cancellation);
            if (initialPath.Length == 0 && mode != "demo")
            {
                var saved = await BrowserServices.CallAsync<JsonElement>("history.saved", new { workspaceId = info.Id }, cancellation);
                if (saved.ValueKind == JsonValueKind.Object)
                {
                    var entry = Wire.Decode<HistoryEntry>(saved.GetRawText());
                    initialPath = entry.Path;
                    anchor = entry.Anchor;
                    position = entry.Position;
                }
            }

            var provider = new BrowserWorkspaceProvider(info);
            var catalog = new DocumentCatalog(_parser);
            try
            {
                catalog.Seed(await provider.EnumerateAsync(cancellation));
            }
            catch
            {
                await provider.DisposeAsync();
                throw;
            }

            // The browser owns replacement of a provider with the same stable workspace id.
            var map = await LoadMapAsync(provider, catalog, cancellation);
            _contexts[info.Id] = new(provider, catalog, map);
            _active = _contexts[info.Id];
            await StopDocumentWatchAsync();
            await LoadReaderAppearanceAsync(cancellation);
            await ApplyPreferencesAsync();
            while (_contexts.Count > 2)
            {
                var oldest = _contexts.First(c => c.Key != info.Id);
                await oldest.Value.Provider.DisposeAsync();
                _contexts.Remove(oldest.Key);
            }

            // Navigation flushes the map again, so release the gate before navigating.
            _mapGate.Release();
            mapGateHeld = false;
            _indexing.Cancel();
            _settingsDialog?.Close();
            ResumeWorkspaceButton.Visibility = Visibility.Collapsed;
            _resume = null;
            RefreshSidebar();
            UpdateWorkspaceLabels();
            var initialDocumentPath = catalog.Cards.FirstOrDefault(c => c.Path == initialPath)?.Path ?? catalog.Cards.FirstOrDefault(c => c.Path.Equals("README.md", StringComparison.OrdinalIgnoreCase))?.Path ?? catalog.Cards.FirstOrDefault()?.Path;
            if (initialDocumentPath == null)
            {
                _document = null;
                ShowMessage("No Markdown pages yet", "Choose Markdown files or a folder containing documentation.");
            }
            else
            {
                await NavigateAsync(initialDocumentPath, initialDocumentPath == initialPath ? anchor : "", true, initialDocumentPath == initialPath ? position : null);
            }

            _ = IndexAsync(_active);
        }
        finally
        {
            if (mapGateHeld)
            {
                _mapGate.Release();
            }

            _documentBusy = false;
            CancelButton.Visibility = Visibility.Collapsed;
            StatusNotice.Visibility = Visibility.Collapsed;
            UpdateExportButtons();
            await RenderMarkdownAiAsync();
        }
    }

    /// <summary>Indexes a context in the background and updates progress only while it is still active.</summary>
    private async Task IndexAsync(Context context)
    {
        _indexing.Dispose();
        _indexing = new();
        var cancellation = _indexing.Token;
        try
        {
            await context.Catalog.IndexAsync(context.Provider, (indexedDocuments, totalDocuments) =>
            {
                if (context != _active)
                {
                    return;
                }

                if (indexedDocuments % 10 == 0 || indexedDocuments == totalDocuments)
                {
                    RefreshSidebar();
                }
            }, cancellation);
            if (context == _active)
            {
                RefreshSidebar();
                await RenderMarkdownAiAsync();
                if (context.Catalog.Warnings.Count > 0)
                {
                    ShowActionNotice(context.Catalog.Warnings[0]);
                }

                if (_view == "map")
                {
                    await ShowMapAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation cancels obsolete background indexing without interrupting reading.
        }
        catch (Exception)
        {
            if (context == _active)
            {
                ShowActionNotice("Index unavailable · reopen the folder");
            }
        }
    }

    /// <summary>Navigates through the managed document lifecycle.</summary>
    private async Task NavigateAsync(string path, string anchor = "", bool push = true, ReadingPosition? position = null)
    {
        AssertMarkdownEditIdle();
        await NavigateCoreAsync(path, anchor, push, position);
    }

    /// <summary>Loads Markdown while rejecting obsolete navigation results.</summary>
    private async Task NavigateCoreAsync(string path, string anchor, bool push, ReadingPosition? position)
    {
        if (_active == null)
        {
            return;
        }

        await CloseDocumentFindAsync();
        CancelMarkdownAi();
        await FlushMapAsync();
        _exporting.Cancel();
        _documentBusy = true;
        UpdateExportButtons();
        _navigation.Cancel();
        _navigation.Dispose();
        _navigation = new();
        var cancellation = _navigation.Token;
        var generation = ++_generation;
        var context = _active;
        path = WorkspacePath.Normalize(path);
        if (push)
        {
            await CaptureHistoryAsync();
        }

        SetStatus("Opening page…");
        try
        {
            var model = await context.Catalog.OpenAsync(context.Provider, path, cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (generation != _generation || context != _active)
            {
                return;
            }

            _document = model;
            context.Map.SelectedPath = model.Path;
            context.Map.Exploration.RecordVisit(model.Path);
            _aiSelection = null;
            _aiResult = null;
            _aiResultPreview = null;
            _aiUsage = null;
            _aiMessage = "";
            _missingPath = "";
            MessageOverlay.Visibility = Visibility.Collapsed;
            UpdateExportButtons();
            UpdateDocumentTitle(model.Path, IsMarkdownDraft());
            RefreshOutline(model);
            RefreshSidebar();
            UpdateWorkspaceLabels();
            if (push)
            {
                var entry = _history.Push(context.Provider.Info.Id, model.Path, anchor);
                await BrowserServices.CallAsync("history.push", entry, cancellation);
            }

            if (_narrow)
            {
                _sidebarOpen = false;
            }

            LayoutViews();
            if (_view == "source" && position == null && anchor.Length > 0)
            {
                var heading = model.Headings.FirstOrDefault(h => h.Id == anchor);
                if (heading != null)
                {
                    position = new("", 0, heading.Line);
                }
            }

            if (_view is "read" or "split")
            {
                await ReaderHost.DisplayAsync(model, context.Provider.Info.Id, position, position == null ? anchor : "", cancellation);
            }
            if (_view is "source" or "split")
            {
                await ShowSourceAsync(cancellation);
            }

            if (_view == "map")
            {
                // Keep existing graph elements while sidebar navigation changes the active document.
                await ShowMapAsync(updateSelection: true);
            }

            if (position != null && (_view is "source" or "split"))
            {
                // A collapsed source presenter is not mounted during a reader-only workspace restore.
                await BrowserServices.CallAsync("source.restore", new { hostId = SourceHost.HostId, position }, cancellation);
            }

            await WatchCurrentDocumentAsync(context, model, cancellation);
            BackButton.IsEnabled = _history.CanBack;
            ForwardButton.IsEnabled = _history.CanForward;
            if (_narrow)
            {
                _sidebarOpen = false;
                LayoutViews();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception error)
        {
            if (generation != _generation)
            {
                return;
            }

            _missingPath = path;
            ShowMessage("This page could not be opened", error is WorkspaceException ? error.Message : "Reopen the folder or choose this page again.");
            SetStatus("Choose the missing file or reopen its folder.");
        }
        finally
        {
            _documentBusy = false;
            UpdateExportButtons();
            await RenderMarkdownAiAsync();
        }
    }

    /// <summary>Captures the visible reader or source position before replacing the current history entry.</summary>
    private async Task CaptureHistoryAsync()
    {
        if (_document == null || _history.Current == null || _view == "map" || (_narrow && _sidebarOpen))
        {
            return;
        }

        var position = await ReaderHost.CaptureAsync();
        if (_view == "source")
        {
            position = await BrowserServices.CallAsync<ReadingPosition>("source.capture", new { hostId = SourceHost.HostId });
        }

        _history.Capture(position);
        await BrowserServices.CallAsync("history.replace", _history.Current);
    }

    /// <summary>Displays the active document's source when both document and workspace are available.</summary>
    private Task ShowSourceAsync(CancellationToken cancellationToken = default) => _document == null || _active == null ? Task.CompletedTask : SourceHost.DisplayAsync(_document, _active.Provider.Info.Id, _preferences, cancellationToken);
    /// <summary>Displays the graph, retaining its visual elements when navigation only updates the selected document.</summary>
    private Task ShowMapAsync(bool updateSelection = false)
    {
        if (_active == null)
        {
            return Task.CompletedTask;
        }

        _active.Map.Session.EnsureDocuments(_active.Catalog.Cards);
        var selected = _active.Map.SelectedPath;
        var map = BuildAtlasMap(_active, selected);
        var presentation = MapPresentation(_active.Map);
        var focused = _active.Map.Session.Value.Atlas?.FocusDepth > 0;

        if (updateSelection)
        {
            return MapHost.DisplaySelectionAsync(
                map, selected, _active.Provider.Info.Id, presentation, focused, center: true);
        }

        return MapHost.DisplayAsync(
            map,
            selected,
            _active.Provider.Info.Id,
            presentation,
            focused
        );
    }

    /// <summary>Changes the managed reader, source or graph view.</summary>
    private async Task SetViewAsync(string view)
    {
        AssertMarkdownEditIdle();
        await SetViewCoreAsync(view);
    }

    /// <summary>Captures position and restores the selected browser host.</summary>
    private async Task SetViewCoreAsync(string view)
    {
        AssertMarkdownEditIdle();
        await CloseDocumentFindAsync();
        await BrowserServices.CallAsync("map.flush");
        await CaptureHistoryAsync();
        var position = _history.Current?.Position;
        _view = view == "split" && ActualWidth < 1050 ? "source" : view;
        _sidebarOpen = !_narrow && _sidebarOpen;
        LayoutViews();
        if (_document != null && _active != null && (_view is "read" or "split"))
        {
            await ReaderHost.DisplayAsync(_document, _active.Provider.Info.Id, position, "", _navigation.Token);
        }

        if (_view is "source" or "split")
        {
            await ShowSourceAsync();
            if (position != null)
            {
                await BrowserServices.CallAsync("source.restore", new { hostId = SourceHost.HostId, position });
            }
        }

        if (_view == "map")
        {
            await ShowMapAsync();
        }
    }

    /// <summary>Applies the shell's wide or narrow layout and updates compact toolbar controls.</summary>
    private void LayoutViews()
    {
        var wasNarrow = _narrow;
        _narrow = ActualWidth > 0 && ActualWidth < 760;
        if (_narrow && !wasNarrow)
        {
            _sidebarOpen = false;
        }
        else if (!_narrow && wasNarrow)
        {
            _sidebarOpen = _layout.SidebarOpen;
        }

        if (_narrow && _view == "split")
        {
            _view = "source";
        }

        var sidebarVisible = _sidebarOpen;
        Sidebar.Visibility = sidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumnSpan(Sidebar, _narrow ? 5 : 1);
        DocumentPanel.Visibility = _narrow && sidebarVisible ? Visibility.Collapsed : Visibility.Visible;
        SidebarColumn.MinWidth = sidebarVisible && !_narrow ? 180 : 0;
        SidebarColumn.MaxWidth = Math.Max(180, Math.Min(520, ActualWidth * 0.45));
        SidebarColumn.Width = new GridLength(sidebarVisible && !_narrow
            ? Math.Min(_layout.SidebarWidth, SidebarColumn.MaxWidth)
            : 0);
        SidebarDividerColumn.Width = new GridLength(sidebarVisible && !_narrow ? 6 : 0);
        SidebarSplitter.Visibility = sidebarVisible && !_narrow ? Visibility.Visible : Visibility.Collapsed;
        BrandBadge.Visibility = _narrow ? Visibility.Collapsed : Visibility.Visible;
        TitlePanel.Visibility = ActualWidth < 900 ? Visibility.Collapsed : Visibility.Visible;
        OpenFolderButton.Visibility = _narrow ? Visibility.Collapsed : Visibility.Visible;
        ButtonIcons.SetText(OpenFileButton, _narrow ? "Open" : "Open file");
        ButtonIcons.SetCompact(SettingsButton, true);
        LayoutDocumentToolbar();

        SplitButton.Visibility = ActualWidth < 1050 ? Visibility.Collapsed : Visibility.Visible;
        var source = _view is "source" or "split";
        var read = _view is "read" or "split";
        SourceHost.Visibility = source ? Visibility.Visible : Visibility.Collapsed;
        ReaderHost.Visibility = read ? Visibility.Visible : Visibility.Collapsed;
        MapHost.Visibility = _view == "map" ? Visibility.Visible : Visibility.Collapsed;
        var split = _view == "split";
        SourceColumn.Width = source
            ? new GridLength(split ? _layout.SourceRatio : 1, GridUnitType.Star)
            : new GridLength(0);
        ReadColumn.Width = read || _view == "map"
            ? new GridLength(split ? 1 - _layout.SourceRatio : 1, GridUnitType.Star)
            : new GridLength(0);
        SplitDividerColumn.Width = new GridLength(_view == "split" ? 6 : 0);
        SplitDivider.Visibility = _view == "split" ? Visibility.Visible : Visibility.Collapsed;
        foreach (var (button, name) in new[]
        {
            (ReadButton, "read"),
            (SourceButton, "source"),
            (SplitButton, "split"),
            (MapButton, "map")
        }

        )
        {
            SetViewTabState(button, name == _view);
        }
        if (_started)
        {
            OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.sync($0,$1,$2)", ReaderHost.HostId, SourceHost.HostId, _view == "split");
        }
        LayoutMarkdownAi();
    }

    /// <summary>Gives view selectors and the assistant a shared rounded-tab selection treatment.</summary>
    private static void SetViewTabState(Button button, bool selected)
    {
        var resources = Application.Current.Resources;
        button.Background = (Brush)resources[selected ? "AccentSoft" : "PanelBackground"];
        button.Foreground = (Brush)resources[selected ? "SystemAccent" : "ShellText"];
        button.BorderBrush = (Brush)resources[selected ? "SystemAccent" : "ShellBorder"];
        button.FontWeight = selected ? FontWeights.Medium : FontWeights.SemiLight;
    }

    /// <summary>Synchronizes the native document tree while preserving expanded folders.</summary>
    private void RefreshSidebar()
    {
        if (_active == null)
        {
            return;
        }

        _updating = true;
        RefreshDocumentTree(_active.Catalog.Cards, _active.Provider.Info.Id, _document?.Path);
        _updating = false;
    }

    /// <summary>Updates workspace labels and available provider actions for the active context.</summary>
    private void UpdateWorkspaceLabels()
    {
        if (_active == null)
        {
            return;
        }

        var info = _active.Provider.Info;
        WorkspaceName.Text = info.Name;
        UpdateParentFolderActions();
        SynchronizeSettings();
    }

    /// <summary>Shows both parent links only for native folders and disables them during transitions or at a drive root.</summary>
    private void UpdateParentFolderActions()
    {
        var info = _active?.Provider.Info;
        var visibility = info?.Mode == "native" ? Visibility.Visible : Visibility.Collapsed;
        var canOpenParent = info?.Capabilities.CanOpenParent == true && !_workspaceTransitionBusy;
        foreach (var button in new[] { HeaderParentFolderButton, SidebarParentFolderButton })
        {
            button.Visibility = visibility;
            button.IsEnabled = canOpenParent;
        }
    }

    /// <summary>Opens one native parent folder using the same draft, map and history guards as other workspace changes.</summary>
    private async void OpenParentFolder(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        if (_active?.Provider.Info.Capabilities.CanOpenParent != true || _workspaceTransitionBusy)
        {
            return;
        }

        await OpenWorkspaceAsync("parent");
    });

    /// <summary>Shows a single workspace-relative path without repeating a document heading.</summary>
    private void UpdateDocumentTitle(string path, bool draft = false, string? workspaceName = null)
    {
        var name = workspaceName ?? _active?.Provider.Info.Name;
        var title = string.IsNullOrEmpty(name) ? path : name + " / " + path;
        AppLocalization.SetRaw(DocumentTitle, title + (draft ? " *" : ""));
        ToolTipService.SetToolTip(DocumentTitle, title);
    }

    /// <summary>Displays a shell message and optional recovery action in the document area.</summary>
    private void ShowMessage(string title, string message)
    {
        AppLocalization.SetText(MessageTitle, title);
        AppLocalization.SetText(MessageText, message);
        MessageOverlay.Visibility = Visibility.Visible;
        UpdateExportButtons();
        ChooseMissingButton.Visibility = _missingPath.Length > 0 && _active?.Provider.Info.Capabilities.CanAddFiles == true ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Applies validated preferences to browser and managed appearance while rejecting stale results.</summary>
    private async Task ApplyPreferencesAsync()
    {
        var response = await _readerPresentation.ApplyAsync(ReaderHost.HostId, SourceHost.HostId);
        if (response == null)
        {
            return;
        }
        var result = response.Value;

        var dark = result.GetProperty("dark").GetBoolean();
        var acrylic = result.TryGetProperty("acrylic", out var backdrop) && backdrop.GetBoolean();
        _acrylicAvailable = result.TryGetProperty("acrylicAvailable", out var supported) && supported.GetBoolean();
        if (result.TryGetProperty("locale", out var locale))
        {
            UiStrings.SetLanguage(locale.GetString() ?? "en");
        }

        var uiFont = result.TryGetProperty("uiFont", out var resolvedFont) ? resolvedFont.GetString() : "system";
        AppTypography.SetFont(uiFont);

        var systemAccent = result.TryGetProperty("accent", out var accent)
            ? accent.GetString()
            : null;
        ApplyManagedAppearance(dark, acrylic, systemAccent);

        NavigationRow.Refresh(DocumentTree);
        NavigationRow.Refresh(OutlineTree);
        AppLocalization.Apply(this);
        UpdateWorkspaceLabels();
        SynchronizeSettings();
        DocumentAppearance.Update(ReaderPreferences);
        LayoutViews();
        if (!_preferencesPersisted)
        {
            SetStatus("Preferences are available for this session. Browser storage is unavailable.");
        }
    }

    /// <summary>Updates shared brush instances so already-open controls keep their live palette references.</summary>
    private static void ApplyManagedAppearance(bool dark, bool acrylic, string? systemAccent)
    {
        var theme = (ModernTheme)Application.Current.Theme;
        theme.CurrentPalette = dark ? ModernTheme.Palettes.Dark : ModernTheme.Palettes.Light;

        SetBrush("ShellBackground", ParseColor(dark ? "#14171C" : "#F4F5F7"));
        SetBrush("PanelBackground", ParseColor(dark ? "#191D23" : "#FFFFFF"));
        SetBrush("ShellText", ParseColor(dark ? "#EDF0F5" : "#20283A"));
        SetBrush("MutedText", ParseColor(dark ? "#AFB5C1" : "#596579"));
        SetBrush("ShellBorder", ParseColor(dark ? "#30353F" : "#DDE2EA"));
        SetBrush("Accent", ParseColor(dark ? "#A894F9" : "#525CE8"));
        SetBrush("AccentSoft", ParseColor(dark ? "#252333" : "#ECEEFE"));
        SetBrush("ErrorText", ParseColor(dark ? "#FFA6B1" : "#A31F34"));
        SetBrush("IconBlue", ParseColor(dark ? "#75B8FA" : "#2169B5"));
        SetBrush("IconTeal", ParseColor(dark ? "#59D3C8" : "#087E78"));
        SetBrush("IconGold", ParseColor(dark ? "#EAC16B" : "#91620C"));
        SetBrush("IconCoral", ParseColor(dark ? "#F78E9D" : "#BE3849"));
        SetBrush("IconGreen", ParseColor(dark ? "#83D79C" : "#2C804B"));
        SetBrush("IconPurple", ParseColor(dark ? "#BAA0F5" : "#7654BD"));

        var panelColor = ParseColor(dark ? "#191D23" : "#FFFFFF");
        panelColor.A = acrylic ? (byte)(dark ? 218 : 185) : (byte)255;
        SetBrush("AcrylicPanel", panelColor);

        // Acrylic keeps the chosen neutral graphite tint; opaque hosts use the normal shell surface.
        var backgroundColor = acrylic
            ? Color.FromArgb((byte)(dark ? 200 : 95), 17, 20, 26)
            : ParseColor(dark ? "#14171C" : "#F4F5F7");
        SetBrush("AcrylicBackground", backgroundColor);
        theme.LightPalette.ContainerBackgroundColor = acrylic
            ? Color.FromArgb(190, 255, 255, 255)
            : Colors.White;
        theme.DarkPalette.ContainerBackgroundColor = acrylic
            ? Color.FromArgb(225, 25, 29, 35)
            : Color.FromRgb(25, 29, 35);

        if (systemAccent is { Length: 7 })
        {
            SetBrush("SystemAccent", ParseColor(systemAccent));
        }
    }

    /// <summary>Parses a validated six-digit RGB appearance color from the browser or application palette.</summary>
    private static Color ParseColor(string hex)
    {
        var red = Convert.ToByte(hex.Substring(1, 2), 16);
        var green = Convert.ToByte(hex.Substring(3, 2), 16);
        var blue = Convert.ToByte(hex.Substring(5, 2), 16);
        return Color.FromArgb(255, red, green, blue);
    }

    /// <summary>Mutates a shared brush without replacing references retained by existing controls and templates.</summary>
    private static void SetBrush(string key, Color color)
    {
        var brush = (SolidColorBrush)Application.Current.Resources[key];
        brush.Color = color;
    }

    /// <summary>Retains routine status without occupying a permanent footer or replacing an actionable notice.</summary>
    private void SetStatus(string key, params object? [] arguments)
    {
        if (StatusNotice.Visibility != Visibility.Visible)
        {
            AppLocalization.SetText(StatusText, key, arguments);
        }
    }

    /// <summary>Shows a dismissible notice for an explicit operation or recoverable failure.</summary>
    private void ShowActionNotice(string key, params object? [] arguments)
    {
        AppLocalization.SetText(StatusText, key, arguments);
        StatusNotice.Visibility = Visibility.Visible;
    }

    /// <summary>Dismisses transient feedback without cancelling a running operation.</summary>
    private void DismissStatus(object sender, RoutedEventArgs arguments)
    {
        StatusNotice.Visibility = Visibility.Collapsed;
    }
    /// <summary>Opens the existing resource picker from a XAML action and rejects stale workspace invitations.</summary>
    private async void ChooseMissingResource(object sender, RoutedEventArgs arguments)
    {
        await ExecuteAsync(async () =>
        {
            if (_active?.Provider.Info.Id != _missingAssetWorkspace || _missingAsset.Length == 0) return;
            var workspaceId = _missingAssetWorkspace;
            await BrowserServices.CallAsync("workspace.addMissing", new { workspaceId, path = _missingAsset });
            MissingResourceButton.Visibility = Visibility.Collapsed;
            await ReloadCatalogAsync(_document?.Path ?? "");
        });
    }
    /// <summary>Builds the settings dialog state from current preferences, workspace and export capabilities.</summary>
    private SettingsAvailability SettingsState() => new(_desktop, _acrylicAvailable);
    /// <summary>Updates an open settings dialog from the current shell state.</summary>
    private void SynchronizeSettings() => _settingsDialog?.Update(_preferences, SettingsState());
    /// <summary>Validates changed settings, applies them and synchronizes their persisted status.</summary>
    private async Task<bool> ApplySettingsAsync(Preferences preferences)
    {
        var validated = preferences.Validated();
        var sourceChanged = validated.SimpleSource != _preferences.SimpleSource;
        var providerChanged = validated.AiProvider != _preferences.AiProvider;
        if (providerChanged)
        {
            // Reject the previous provider's completion before any asynchronous preference update.
            InvalidateMarkdownAiConnection();
            _aiConfigured = false;
            _aiModelsReady = false;
        }

        _readerPresentation.SetPreferences(validated);
        await ApplyPreferencesAsync();
        if (providerChanged)
        {
            await RefreshMarkdownAiKeyAsync();
        }
        if (sourceChanged && (_view is "source" or "split"))
        {
            await ShowSourceAsync();
        }

        return _preferencesPersisted;
    }

    /// <summary>Routes renderer navigation, position, appearance and selection events to managed state.</summary>
    private async Task HandleBrowserEventAsync(string json) => await ExecuteAsync(async () =>
    {
        using var data = JsonDocument.Parse(json);
        var root = data.RootElement;
        switch (root.GetProperty("type").GetString())
        {
            case "missing-resource":
                if (_active is { } assetContext && assetContext.Provider.Info.Id == root.GetProperty("workspaceId").GetString()
                    && _document?.Path == root.GetProperty("path").GetString())
                {
                    _missingAsset = root.GetProperty("assetPath").GetString() ?? "";
                    _missingAssetWorkspace = assetContext.Provider.Info.Id;
                    ShowActionNotice("Choose missing image");
                    MissingResourceButton.Visibility = Visibility.Visible;
                }
                break;
            case "markdown-ai":
                await HandleMarkdownAiAsync(root);
                break;
            case "document-selection":
                await HandleMarkdownSelectionAsync(root);
                break;
            case "map-appearance":
                if (_view == "map" && _active?.Provider.Info.Id == root.GetProperty("workspaceId").GetString())
                {
                    FileMenuPopup.IsOpen = false;
                    MapHost.OpenAppearance(root);
                }
                break;
            case "file-context":
                ShowFileMenu(Wire.Decode<FileMenuTarget>(root.GetRawText()),
                    new Point(root.GetProperty("x").GetDouble(), root.GetProperty("y").GetDouble()));
                break;
            case "document-find":
                await OpenDocumentFindAsync(Wire.Decode<FileMenuTarget>(root.GetRawText()));
                break;
            case "document-file-changed":
                QueueDiskChange(root);
                break;
            case "map":
                await HandleMapAsync(root);
                break;
            case "map-select":
                await HandleMapSelectionAsync(root);
                break;
            case "map-explore":
                await HandleMapExplorationAsync(root);
                break;
            case "navigate":
                if (_view == "map")
                {
                    await SetViewAsync("read");
                }

                await NavigateAsync(root.GetProperty("path").GetString() ?? "", root.TryGetProperty("anchor", out var anchor) ? anchor.GetString() ?? "" : "");
                break;
            case "history":
                var entry = Wire.Decode<HistoryEntry>(root.GetProperty("entry").GetRawText());
                await RestoreHistoryAsync(entry);
                break;
            case "status":
                if (root.TryGetProperty("severity", out var severity) && severity.GetString() == "error")
                {
                    ShowActionNotice(root.GetProperty("message").GetString() ?? "");
                }
                else
                {
                    SetStatus(root.GetProperty("message").GetString() ?? "");
                }
                break;
            case "appearance":
                await ApplyPreferencesAsync();
                break;
            case "desktop-activated":
                if (_settingsDialog != null)
                {
                    await _settingsDialog.RefreshFileAssociationsAsync();
                }
                break;
            case "source-preference":
                _readerPresentation.SetSimpleSource(root.GetProperty("simple").GetBoolean());
                SynchronizeSettings();
                await ApplyPreferencesAsync();
                await ShowSourceAsync();
                break;
            case "resource-added":
                await ReloadCatalogAsync(root.GetProperty("path").GetString() ?? _document?.Path ?? "");
                break;
        }
    });

    /// <summary>Restores browser history after any provider replacement, retaining the latest pending destination.</summary>
    private async Task RestoreHistoryAsync(HistoryEntry entry)
    {
        AssertMarkdownEditIdle();
        if (_workspaceTransitionBusy)
        {
            // The first document becomes interactive before opening finishes. Browser Back
            // already changes its URL, so retain that destination instead of discarding it.
            _pendingHistory = entry;
            return;
        }

        if (!_contexts.TryGetValue(entry.WorkspaceId, out var context))
        {
            ShowMessage("Reopen this workspace", "Open its folder, files or bundle again to continue from this page.");
            return;
        }

        _active = context;
        await LoadReaderAppearanceAsync();
        await ApplyPreferencesAsync();
        _history.Restore(entry);
        await NavigateAsync(entry.Path, entry.Anchor, false, entry.Position);
        if (!context.Catalog.IndexComplete)
        {
            _indexing.Cancel();
            _ = IndexAsync(context);
        }
    }

    /// <summary>Refreshes the current catalog and view while preserving pending edits and available history.</summary>
    private async Task ReloadCatalogAsync(string path)
    {
        if (_active == null || _workspaceTransitionBusy)
        {
            return;
        }

        AssertMarkdownEditIdle();
        var context = _active;
        var generation = _generation;
        await CaptureHistoryAsync();
        var samePage = path == _document?.Path;
        var position = samePage ? _history.Current?.Position : null;
        _indexing.Cancel();
        var entries = await context.Provider.EnumerateAsync(_opening.Token);
        if (!ReferenceEquals(context, _active) || generation != _generation || _workspaceTransitionBusy)
        {
            return;
        }
        AssertMarkdownEditIdle();
        context.Catalog.Seed(entries);
        context.Map.Session.EnsureDocuments(context.Catalog.Cards);
        await NavigateAsync(path, "", !samePage, position);
        if (ReferenceEquals(context, _active))
        {
            _ = IndexAsync(context);
        }
    }

    /// <summary>Navigates to the selected document unless sidebar selection is being synchronized.</summary>
    private async void DocumentSelected(object sender, RoutedPropertyChangedEventArgs<object> arguments)
    {
        if (!_updating && arguments.NewValue is TreeViewItem { Tag: DocumentCard card } && card.Path != _document?.Path)
        {
            await ExecuteAsync(() => NavigateAsync(card.Path));
        }
    }

    /// <summary>Toggles the document sidebar and reapplies responsive layout.</summary>
    private async void ToggleSidebar(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        await CaptureHistoryAsync();
        _sidebarOpen = !_sidebarOpen;
        _readerPresentation.SetSidebarOpen(_sidebarOpen);
        LayoutViews();
        await SaveLayoutAsync();
    });
    /// <summary>Opens or activates the settings dialog with the current shell state.</summary>
    private void ToggleSettings(object sender, RoutedEventArgs arguments)
    {
        if (_settingsDialog != null)
        {
            return;
        }

        var dialog = new SettingsWindow(_preferences, SettingsState(), ApplySettingsAsync);
        _settingsDialog = dialog;
        dialog.Closed += async (_, _) =>
        {
            if (_settingsDialog == dialog)
            {
                _settingsDialog = null;
                if (dialog.AiConnectionChanged)
                {
                    await ExecuteAsync(RefreshMarkdownAiKeyAsync);
                }
            }
        };
        dialog.Show();
    }

    /// <summary>Opens the canonical VibeWare manifesto when the header badge is activated.</summary>
    private void OpenManifesto(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Manifesto);
    /// <summary>Opens the About dialog.</summary>
    private void ShowAbout(object sender, RoutedEventArgs arguments) => new AboutWindow().Show();
    /// <summary>Displays the last-document recovery action when its workspace needs reopening.</summary>
    private void ShowResume()
    {
        if (_resume == null)
        {
            return;
        }

        WorkspaceName.Text = _resume.Name;
        UpdateDocumentTitle(_resume.Path, workspaceName: _resume.Name);
        AppLocalization.SetText(MessageTitle, "Continue where you left off");
        AppLocalization.SetText(MessageText, "Reopen {0} to continue reading {1}. Your last reading position is remembered on this device.", _resume.Name, _resume.Path);
        MessageOverlay.Visibility = Visibility.Visible;
        UpdateExportButtons();
        ResumeWorkspaceButton.Visibility = Visibility.Visible;
        SetStatus("Choose the last workspace again to allow access to its documents.");
    }

    /// <summary>Reopens or reconnects the remembered workspace and restores its document position.</summary>
    private async void ResumeWorkspace(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        if (_resume == null)
        {
            return;
        }

        var mode = _resume.Mode == "connected" ? "reopen" : _resume.Mode;
        await OpenWorkspaceAsync(mode);
    });
    /// <summary>Cancels the current opening, navigation, indexing or export operations.</summary>
    private void CancelWork(object sender, RoutedEventArgs arguments)
    {
        _opening.Cancel();
        _indexing.Cancel();
        _navigation.Cancel();
        _exporting.Cancel();
    }

    /// <summary>Synchronizes export availability with the active document and outstanding operations.</summary>
    private void UpdateExportButtons()
    {
        var available = !_exportBusy && !_documentBusy && !_markdownEditBusy && !_aiBusy
            && _document != null && MessageOverlay.Visibility != Visibility.Visible;
        ExportHtmlButton.IsEnabled = available;
        ExportPdfButton.IsEnabled = available;
        SynchronizeSettings();
    }
    /// <summary>Exports the current Markdown document through the specialized rendering adapter.</summary>
    private async Task ExportAsync(string format)
    {
        if (_document == null || _active == null || _exportBusy || _markdownEditBusy)
        {
            return;
        }

        var model = _document;
        var context = _active;
        _exporting.Dispose();
        _exporting = new();
        var cancellation = _exporting.Token;
        _exportBusy = true;
        UpdateExportButtons();
        CancelButton.Visibility = Visibility.Visible;
        ShowActionNotice("Preparing document export…");
        try
        {
            var result = await _documentMenus.ExportAsync(
                model, context.Provider.Info.Id, ReaderPreferences, format, cancellation);
            cancellation.ThrowIfCancellationRequested();
            var warnings = result.GetProperty("warnings").GetInt32();
            var filename = result.GetProperty("filename").GetString() ?? "";
            var exportMessage = result.TryGetProperty("cancelled", out var cancelled) && cancelled.GetBoolean() ? UiStrings.T("Export cancelled.") : result.TryGetProperty("saved", out var savedExport) && savedExport.GetBoolean() ? UiStrings.T("Saved {0}", filename) : format == "html" ? UiStrings.T("HTML download started: {0}", filename) : UiStrings.T("Print dialog opened. Choose Save as PDF to save the document.");
            if (warnings > 0)
            {
                exportMessage += UiStrings.T(" · {0} unavailable resources marked in the document", warnings);
            }

            ShowActionNotice(exportMessage);
        }
        catch (OperationCanceledException)
        {
            if (_document == model && _active == context)
            {
                ShowActionNotice("Document export cancelled.");
            }
        }
        finally
        {
            _exportBusy = false;
            UpdateExportButtons();
            if (_document == model && _active == context)
            {
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>Exports the current page as standalone HTML through the guarded UI action runner.</summary>
    private async void ExportHtml(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => ExportAsync("html"));
    /// <summary>Exports the current page as PDF through the guarded UI action runner.</summary>
    private async void ExportPdf(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => ExportAsync("pdf"));
    /// <summary>Restores the previous document history entry and reading position.</summary>
    private async void Back(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        await CaptureHistoryAsync();
        await BrowserServices.CallAsync("history.go", new { delta = -1 });
    });
    /// <summary>Restores the next document history entry and reading position.</summary>
    private async void Forward(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        await CaptureHistoryAsync();
        await BrowserServices.CallAsync("history.go", new { delta = 1 });
    });
    /// <summary>Opens selected Markdown files as a workspace.</summary>
    private async void OpenFiles(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => ShowRecentLocationsAsync("files", OpenFileButton));
    /// <summary>Opens a connected documentation folder with its provider capabilities.</summary>
    private async void OpenFolder(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => ShowRecentLocationsAsync("folder", OpenFolderButton));
    /// <summary>Opens the application's local demo workspace.</summary>
    private async void OpenDemo(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => OpenWorkspaceAsync("demo"));
    /// <summary>Switches the shell to the Markdown reader.</summary>
    private async void ReadView(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("read"));
    /// <summary>Switches the shell to read-only source.</summary>
    private async void SourceViewClick(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("source"));
    /// <summary>Switches the shell to synchronized reader and source views.</summary>
    private async void SplitView(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("split"));
    /// <summary>Switches the shell to the document graph.</summary>
    private async void MapViewClick(object sender, RoutedEventArgs arguments) => await ExecuteAsync(() => SetViewAsync("map"));
    /// <summary>Selects a missing document file and retries the requested navigation.</summary>
    private async void ChooseMissing(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        if (_active == null)
        {
            return;
        }

        await BrowserServices.CallAsync("workspace.addMissing", new { workspaceId = _active.Provider.Info.Id, path = _missingPath });
        await ReloadCatalogAsync(_missingPath);
    });
}
