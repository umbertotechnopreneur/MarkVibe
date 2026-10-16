/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; managed graph toolbar and appearance controls; 2026-10-11
 * Modified with AI: OpenAI Codex; map details, responsive icons and confirmed reset; 2026-10-16
 * Modified with AI: OpenAI Codex; directional link sections with document titles and paths; 2026-10-16
 * Human guidance: Umberto Giacobbi; all application controls in XAML
 * Modified with AI: OpenAI Codex; manage atlas preview, responsive chrome and neighborhood controls; 2026-10-16
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Keeps graph drawing and gestures in the adapter while owning its toolbar, links and picker in XAML.</summary>
public partial class DocumentMapView : UserControl
{
    private bool _focused;
    private bool _advanced;
    private bool _depth = true;
    private string _workspaceId = "";
    private bool _confirmingReset;
    private string _appearancePath = "";
    private JsonElement _state;
    public string HostId => Renderer.HostId;
    public event Func<string, string, Task>? NavigateRequested;
    public event Func<Func<Task>, Task>? ActionRequested;
    /// <summary>Loads map controls and reflows the link pane at narrow widths.</summary>
    public DocumentMapView()
    {
        InitializeComponent();
        FoldersButton.Content = new IconLabel("folder", "Folders");
        TreeButton.Content = new IconLabel("outline", "Tree");
        DepthButton.Content = new IconLabel("pages", "Depth");
        SaveViewButton.Content = new IconLabel("save", "Save view");
        RestoreViewButton.Content = new IconLabel("back", "Restore view");
        AppearanceFileActions.ActionRequested += RequestAppearanceFileActionAsync;
        SetMapToggle(DepthButton, _depth);
        ButtonIcons.Decorate(this);
        SizeChanged += ReflowControls;
        Unloaded += (_, _) => AppearancePopup.IsOpen = false;
        Loaded += (_, _) => Application.Current.RootVisual.MouseLeftButtonDown += OutsideAppearance;
        Unloaded += (_, _) => Application.Current.RootVisual.MouseLeftButtonDown -= OutsideAppearance;
    }
    /// <summary>Keeps search on the toolbar row and compacts captions before allowing horizontal action scrolling.</summary>
    private void ReflowControls(object sender, SizeChangedEventArgs arguments)
    {
        ReflowMapChrome();
    }

    /// <summary>Shares toolbar sizing between window resizes and explicit preview-panel changes.</summary>
    private void ReflowMapChrome()
    {
        ReflowInspector();
        var inspectorWidth = LinksColumn.Width.Value;

        var canvasWidth = ActualWidth - inspectorWidth;
        Summary.Visibility = canvasWidth < 1450 ? Visibility.Collapsed : Visibility.Visible;
        SearchField.Width = Math.Clamp(canvasWidth * 0.23, 160, 270);
        SearchFeedback.MaxWidth = Math.Max(160, Math.Min(440, canvasWidth - 24));
        var iconOnly = canvasWidth < 1250;
        ButtonIcons.SetCompact(StarButton, iconOnly);
        ButtonIcons.SetCompact(CircleButton, iconOnly);
        ButtonIcons.SetCompact(GridButton, iconOnly);
        ButtonIcons.SetCompact(TreeButton, iconOnly);
        ButtonIcons.SetCompact(FoldersButton, iconOnly);
        ButtonIcons.SetCompact(AdvancedButton, iconOnly);
        ButtonIcons.SetCompact(DepthButton, iconOnly);
        ButtonIcons.SetCompact(SaveViewButton, canvasWidth < 1100);
        ButtonIcons.SetCompact(RestoreViewButton, canvasWidth < 1100);
        AppearancePopup.IsOpen = false;
    }
    /// <summary>Publishes graph data to the specialized renderer and populates managed navigation links.</summary>
    public Task DisplayAsync(MapSnapshot map, string selected, string workspaceId, object state, bool focused = false)
    {
        PreparePresentation(map, selected, workspaceId, state, focused);
        return BrowserServices.CallAsync("map.render", new { hostId = HostId, map, selected, workspaceId, state, focused, advanced = _advanced, depth = _depth });
    }
    /// <summary>Refreshes the common managed graph chrome for both full rendering and selection changes.</summary>
    private void PreparePresentation(MapSnapshot map, string selected, string workspaceId, object state, bool focused)
    {
        _focused = focused;
        if (_workspaceId != workspaceId)
        {
            AppearancePopup.IsOpen = false;
            MapSearchBox.Text = "";
            HideMapSearchResults();
        }
        _workspaceId = workspaceId;
        _selectedPath = selected;
        _state = JsonSerializer.SerializeToElement(state, Wire.Options);
        Summary.Text = map.Nodes.Count + " / " + map.TotalDocuments + " · " + map.Edges.Count;
        PopulateInspector(map);
        var documentTitles = map.Nodes.ToDictionary(node => node.Path, node => node.Title, StringComparer.Ordinal);
        AddLinks("Links from this page", "forward", map.Outgoing.Select(link =>
            (link.Path, link.Anchor, documentTitles.GetValueOrDefault(link.Path, System.IO.Path.GetFileName(link.Path)))));
        AddLinks("Links to this page", "back", map.Incoming.Select(node => (node.Path, "", node.Title)));
        UpdateControls(state);
    }
    /// <summary>Updates managed action availability before forwarding a layout acknowledgment.</summary>
    public Task DisplayStateAsync(string workspaceId, object state)
    {
        UpdateControls(state);
        return BrowserServices.CallAsync("map.state", new { hostId = HostId, workspaceId, state });
    }
    /// <summary>Applies managed layout capabilities without exposing legacy annotations.</summary>
    private void UpdateControls(object state)
    {
        _state = JsonSerializer.SerializeToElement(state, Wire.Options);
        var hasError = _state.TryGetProperty("hasError", out var errorValue) && errorValue.GetBoolean();
        MapNotice.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
        AppLocalization.SetText(MapNoticeText, _state.GetProperty("message").GetString() ?? "");
        var busy = _state.GetProperty("busy").GetBoolean();
        var readOnly = _state.TryGetProperty("readOnly", out var readOnlyValue) && readOnlyValue.GetBoolean();
        var canArrange = !busy && !readOnly;
        StarButton.IsEnabled = canArrange;
        CircleButton.IsEnabled = canArrange;
        GridButton.IsEnabled = canArrange;
        TreeButton.IsEnabled = canArrange;
        FoldersButton.IsEnabled = canArrange;
        SetMapToggle(FoldersButton, _state.TryGetProperty("folderGroups", out var grouped) && grouped.GetBoolean());
        UpdateAiContextButton(busy);
        ResetButton.IsEnabled = canArrange && !_confirmingReset;
        SaveButton.IsEnabled = !busy && !_state.GetProperty("invalid").GetBoolean();
        SaveButton.Visibility = _state.GetProperty("canWrite").GetBoolean() || _state.GetProperty("canEnableWrite").GetBoolean() ? Visibility.Visible : Visibility.Collapsed;
        DownloadButton.IsEnabled = !busy;
        DiscardButton.Visibility = _state.GetProperty("dirty").GetBoolean() ? Visibility.Visible : Visibility.Collapsed;
        DiscardButton.IsEnabled = !busy;
        RefreshAppearanceFromState();
        UpdateAtlasControls(busy);
    }
    /// <summary>Opens a native OpenSilver Popup at renderer-reported viewport coordinates.</summary>
    public void OpenAppearance(JsonElement request) => ShowAppearance(request);
    /// <summary>Forwards appearance changes to the graph's validated managed mutation path.</summary>
    private Task ChangeAppearanceAsync(string? color, string? icon) => RunActionAsync(() => BrowserServices.CallAsync("map.appearance", new { hostId = HostId, path = _appearancePath, color, icon }));
    /// <summary>Dismisses the appearance flyout without modifying graph state.</summary>
    private void CloseAppearance(object sender, RoutedEventArgs arguments)
    {
        AppearancePopup.IsOpen = false;
    }
    /// <summary>Cycles the persisted neighborhood lens while retaining surrounding document positions.</summary>
    private async void FocusMap(object sender, RoutedEventArgs arguments)
    {
        var depth = _state.TryGetProperty("focusDepth", out var value) ? value.GetInt32() : 0;
        await AtlasCommandAsync("atlas-focus", new { value = (depth + 1) % 4 });
    }
    /// <summary>Fits the renderer's camera to its current nodes.</summary>
    private async void FitMap(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("fit");
    }
    /// <summary>Centers the most connected page and distributes the remaining cards around it.</summary>
    private async void ArrangeStar(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("arrange-star");
    }
    /// <summary>Arranges visible cards around a circular perimeter.</summary>
    private async void ArrangeCircle(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("arrange-circle");
    }
    /// <summary>Arranges visible cards in a regular grid.</summary>
    private async void ArrangeGrid(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("arrange-grid");
    }
    /// <summary>Places connected documents in successive levels beneath their strongest component roots.</summary>
    private async void ArrangeTree(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("arrange-tree");
    }
    /// <summary>Groups the complete workspace into separate folder grids while preserving card sizes and styles.</summary>
    private async void ArrangeFolders(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("arrange-folders");
    }
    /// <summary>Toggles per-card metadata while keeping the setting across map refreshes.</summary>
    private async void ToggleAdvanced(object sender, RoutedEventArgs arguments)
    {
        _advanced = !_advanced;
        SetMapToggle(AdvancedButton, _advanced);
        await RunActionAsync(() => BrowserServices.CallAsync("map.details", new { hostId = HostId, advanced = _advanced }));
    }
    /// <summary>Changes visual depth without modifying saved coordinates or the current document.</summary>
    private async void ToggleDepth(object sender, RoutedEventArgs arguments)
    {
        _depth = !_depth;
        SetMapToggle(DepthButton, _depth);
        await RunActionAsync(() => BrowserServices.CallAsync("map.depth", new { hostId = HostId, depth = _depth }));
    }
    /// <summary>Uses readable dark-canvas colors regardless of the application's light or dark theme.</summary>
    private void SetMapToggle(Button button, bool active)
    {
        button.Foreground = active ? (Brush)Application.Current.Resources["SystemAccent"] : (Brush)Resources["MapChromeText"];
    }
    /// <summary>Confirms a destructive layout reset and rejects approval after the displayed session changes.</summary>
    private async void ResetMap(object sender, RoutedEventArgs arguments)
    {
        if (_confirmingReset || !ResetButton.IsEnabled)
        {
            return;
        }

        var workspaceId = _workspaceId;
        var stateId = _state.GetProperty("stateId").GetString();
        var layout = _state.GetProperty("layout").GetRawText();
        _confirmingReset = true;
        ResetButton.IsEnabled = false;
        try
        {
            var accepted = await ConfirmationWindow.ConfirmAsync("Are you sure?", "Reset all map positions, colors, icons and card sizes. Your documents will not change.", "Reset");
            // A modal approval belongs only to the unchanged map that the user reviewed.
            if (!accepted || !IsLoaded || _workspaceId != workspaceId ||
                _state.GetProperty("stateId").GetString() != stateId ||
                _state.GetProperty("layout").GetRawText() != layout ||
                _state.GetProperty("busy").GetBoolean() || _state.GetProperty("readOnly").GetBoolean())
            {
                return;
            }

            await CommandAsync("map-reset");
        }
        finally
        {
            _confirmingReset = false;
            UpdateControls(_state);
        }
    }
    /// <summary>Saves map layout using the existing permission-aware adapter.</summary>
    private async void SaveMap(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("save");
    }
    /// <summary>Exports a map layout copy through the provider.</summary>
    private async void DownloadMap(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("download");
    }
    /// <summary>Reloads the persisted map layout through the owner.</summary>
    private async void DiscardMap(object sender, RoutedEventArgs arguments)
    {
        await CommandAsync("discard");
    }
    /// <summary>Calls renderer operations without creating any browser controls.</summary>
    private Task CommandAsync(string action) => RunActionAsync(() => BrowserServices.CallAsync("map.command", new { hostId = HostId, action }));

    /// <summary>Routes map permission and clipboard failures through the owner's visible notice.</summary>
    private Task RunActionAsync(Func<Task> action)
    {
        return ActionRequested?.Invoke(action) ?? Task.CompletedTask;
    }

    /// <summary>Dismisses the native appearance popup when the user clicks outside its controls.</summary>
    private void OutsideAppearance(object sender, MouseButtonEventArgs arguments)
    {
        if (!AppearancePopup.IsOpen)
        {
            return;
        }
        var current = arguments.OriginalSource as DependencyObject;
        while (current != null)
        {
            if (ReferenceEquals(current, AppearancePopup.Child))
            {
                return;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        AppearancePopup.IsOpen = false;
    }

    /// <summary>Allows keyboard users to dismiss the appearance popup without changing graph state.</summary>
    private void AppearanceKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Escape)
        {
            AppearancePopup.IsOpen = false;
            arguments.Handled = true;
        }
    }
}
