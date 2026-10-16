/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; XAML atlas controls, preview and route navigation; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement all seven document atlas improvements
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;

public partial class DocumentMapView
{
    private bool _updatingAtlas;
    private bool _inspectorOpen = true;
    private bool _inspectorExplicit;
    private string _viewSignature = "";
    private Button? _inspectorAiButton;
    private Button? _routeEndButton;
    private Button? _routeClearButton;

    /// <summary>Forwards typed control data to the renderer, which adds the current workspace and layout session.</summary>
    private Task AtlasCommandAsync(string action, object? data = null)
    {
        return RunActionAsync(() => BrowserServices.CallAsync("map.command", new { hostId = HostId, action, data }));
    }

    /// <summary>Refreshes managed controls without overwriting a partially typed viewpoint name.</summary>
    private void UpdateAtlasControls(bool busy)
    {
        _updatingAtlas = true;
        NeighborhoodDepth.SelectedIndex = _state.TryGetProperty("focusDepth", out var depth) ? depth.GetInt32() : 0;
        NeighborhoodDepth.IsEnabled = !busy;
        SetMapToggle(FocusButton, NeighborhoodDepth.SelectedIndex > 0);
        var layout = _state.GetProperty("layout").Deserialize<MapLayout>(Wire.Options);
        var views = layout?.Atlas?.Views ?? [];
        var signature = _workspaceId + "\n" + string.Join("\n", views.Select(view => view.Name));
        if (signature != _viewSignature)
        {
            var selectedName = (SavedViews.SelectedItem as ComboBoxItem)?.Tag as string;
            SavedViews.Items.Clear();
            foreach (var view in views)
            {
                var item = new ComboBoxItem { Content = view.Name, Tag = view.Name };
                AppLocalization.SetRaw(item);
                SavedViews.Items.Add(item);
                if (view.Name == selectedName)
                {
                    SavedViews.SelectedItem = item;
                }
            }
            if (SavedViews.SelectedIndex < 0 && SavedViews.Items.Count > 0)
            {
                SavedViews.SelectedIndex = 0;
            }
            _viewSignature = signature;
        }
        SaveViewButton.IsEnabled = !busy;
        SavedViews.IsEnabled = !busy && views.Count > 0;
        RestoreViewButton.IsEnabled = !busy && SavedViews.SelectedIndex >= 0;
        DeleteViewButton.IsEnabled = RestoreViewButton.IsEnabled;
        var hasRouteStart = _state.TryGetProperty("routeStart", out var routeStart) && !string.IsNullOrEmpty(routeStart.GetString());
        if (_routeEndButton != null)
        {
            _routeEndButton.IsEnabled = !busy && hasRouteStart;
        }
        if (_routeClearButton != null)
        {
            _routeClearButton.IsEnabled = !busy && hasRouteStart;
        }
        if (_inspectorAiButton?.Content is TextBlock aiCaption)
        {
            var included = _state.TryGetProperty("aiContextPaths", out var paths)
                && paths.EnumerateArray().Any(path => path.GetString() == _selectedPath);
            AppLocalization.SetText(aiCaption, included ? "Remove selected document from AI context" : "Add selected document to AI context");
            _inspectorAiButton.IsEnabled = AddToAiButton.IsEnabled;
        }
        _updatingAtlas = false;
    }

    /// <summary>Changes neighborhood distance only in response to a user selection.</summary>
    private async void NeighborhoodChanged(object sender, SelectionChangedEventArgs arguments)
    {
        if (!_updatingAtlas && _state.ValueKind == JsonValueKind.Object && NeighborhoodDepth.SelectedIndex >= 0)
        {
            await AtlasCommandAsync("atlas-focus", new { value = NeighborhoodDepth.SelectedIndex });
        }
    }

    /// <summary>Saves or replaces the named viewpoint after capturing logical camera coordinates.</summary>
    private async void SaveView(object sender, RoutedEventArgs arguments)
    {
        var name = ViewName.Text.Trim();
        if (name.Length == 0)
        {
            ViewName.Focus();
            return;
        }
        await AtlasCommandAsync("atlas-save-view", new { name });
    }

    /// <summary>Shows a localized name hint while keeping the actual saved name entirely user-owned.</summary>
    private void ViewNameChanged(object sender, TextChangedEventArgs arguments)
    {
        if (ViewNameWatermark != null)
        {
            ViewNameWatermark.Visibility = ViewName.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>Provides keyboard access to saving the current named view.</summary>
    private void ViewNameKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Enter)
        {
            arguments.Handled = true;
            SaveView(sender, arguments);
        }
    }

    /// <summary>Updates restore/delete availability when the user chooses a saved view.</summary>
    private void SavedViewChanged(object sender, SelectionChangedEventArgs arguments)
    {
        if (!_updatingAtlas && RestoreViewButton != null && DeleteViewButton != null)
        {
            RestoreViewButton.IsEnabled = SavedViews.SelectedIndex >= 0;
            DeleteViewButton.IsEnabled = RestoreViewButton.IsEnabled;
        }
    }

    /// <summary>Restores the selected view's camera and lens without rearranging documents.</summary>
    private async void RestoreView(object sender, RoutedEventArgs arguments)
    {
        if (SavedViews.SelectedItem is ComboBoxItem { Tag: string name })
        {
            await AtlasCommandAsync("atlas-restore-view", new { name });
        }
    }

    /// <summary>Removes only the selected saved viewpoint from the map sidecar.</summary>
    private async void DeleteView(object sender, RoutedEventArgs arguments)
    {
        if (SavedViews.SelectedItem is ComboBoxItem { Tag: string name })
        {
            await AtlasCommandAsync("atlas-delete-view", new { name });
        }
    }

    /// <summary>Toggles document preview, including an explicitly opened overlay on narrow screens.</summary>
    private void ToggleInspector(object sender, RoutedEventArgs arguments)
    {
        _inspectorOpen = LinksPane.Visibility != Visibility.Visible;
        _inspectorExplicit = true;
        ReflowMapChrome();
    }

    /// <summary>Uses a side column on desktop and an optional overlay where the map needs its full width.</summary>
    private void ReflowInspector()
    {
        var wide = ActualWidth > 1000;
        var visible = _inspectorOpen && (wide || _inspectorExplicit);
        LinksColumn.Width = new GridLength(visible && wide ? 290 : 0);
        LinksPane.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(LinksPane, wide ? 1 : 0);
        LinksPane.Width = wide ? double.NaN : Math.Max(180, Math.Min(290, ActualWidth - 32));
        LinksPane.HorizontalAlignment = wide ? HorizontalAlignment.Stretch : HorizontalAlignment.Right;
        SetMapToggle(InspectorButton, visible);
    }

    /// <summary>Builds a source-backed preview and section navigation for the selected catalog card.</summary>
    private void PopulateInspector(MapSnapshot map)
    {
        Links.Children.Clear();
        _inspectorAiButton = null;
        _routeEndButton = null;
        _routeClearButton = null;
        var card = map.Nodes.FirstOrDefault(node => node.Path == _selectedPath);
        if (card == null)
        {
            return;
        }
        var title = new TextBlock { FontSize = 19, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        AppLocalization.SetRaw(title, card.Title);
        var path = new TextBlock { FontSize = 10, Foreground = (Brush)Resources["MapChromeText"], TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 16) };
        AppLocalization.SetRaw(path, card.Path);
        var preview = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, LineHeight = 20, Margin = new Thickness(0, 0, 0, 14) };
        AppLocalization.SetRaw(preview, card.Introduction);
        Links.Children.Add(title);
        Links.Children.Add(path);
        Links.Children.Add(preview);
        var workspaceId = _workspaceId;
        var documentPath = card.Path;
        AddInspectorAction("Open document", async () =>
        {
            if (_workspaceId == workspaceId && NavigateRequested != null)
            {
                await NavigateRequested(documentPath, "");
            }
        });
        _inspectorAiButton = AddInspectorAction("Add selected document to AI context", async () =>
        {
            if (_workspaceId == workspaceId && AddToAiRequested != null)
            {
                await AddToAiRequested(workspaceId, documentPath);
            }
        });
        foreach (var heading in card.Headings)
        {
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11,
                Margin = new Thickness(Math.Max(0, heading.Level - 1) * 8, 2, 0, 2) };
            AppLocalization.SetRaw(caption, heading.Text);
            var button = new HyperlinkButton { Content = caption, HorizontalContentAlignment = HorizontalAlignment.Left };
            button.Click += async (_, _) => await RunActionAsync(async () =>
            {
                if (_workspaceId == workspaceId && NavigateRequested != null)
                {
                    await NavigateRequested(documentPath, heading.Id);
                }
            });
            Links.Children.Add(button);
        }
        AddInspectorAction("Set route start", () => AtlasCommandAsync("atlas-route-start"));
        _routeEndButton = AddInspectorAction("Find route to this document", () => AtlasCommandAsync("atlas-route-end"));
        _routeClearButton = AddInspectorAction("Clear route", () => AtlasCommandAsync("atlas-route-clear"));
        if (_state.ValueKind == JsonValueKind.Object && _state.TryGetProperty("routeStart", out var start) && start.GetString() is { Length: > 0 } startPath)
        {
            var startLabel = new TextBlock { FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            AppLocalization.SetText(startLabel, "Route start: {0}", startPath);
            Links.Children.Add(startLabel);
        }
        if (map.RouteMessage.Length > 0)
        {
            var routeLabel = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
            AppLocalization.SetText(routeLabel, map.RouteMessage, Math.Max(0, (map.Route?.Count ?? 0) - 1));
            Links.Children.Add(routeLabel);
            var direction = new TextBlock { FontSize = 10, Foreground = (Brush)Resources["MapChromeText"], TextWrapping = TextWrapping.Wrap };
            AppLocalization.SetText(direction, "Connections in either direction");
            Links.Children.Add(direction);
            foreach (var routePath in map.Route ?? [])
            {
                var routeCard = map.Nodes.FirstOrDefault(node => node.Path == routePath);
                var caption = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11 };
                AppLocalization.SetRaw(caption, routeCard?.Title ?? routePath);
                var button = new HyperlinkButton { Content = caption, HorizontalContentAlignment = HorizontalAlignment.Left };
                button.Click += async (_, _) => await RunActionAsync(async () =>
                {
                    if (_workspaceId == workspaceId && CenterDocumentRequested != null)
                    {
                        await CenterDocumentRequested(workspaceId, routePath);
                    }
                });
                Links.Children.Add(button);
            }
        }
    }

    /// <summary>Creates a localized inspector action whose failures use the existing managed notice.</summary>
    private Button AddInspectorAction(string label, Func<Task> action)
    {
        var caption = new TextBlock();
        AppLocalization.SetText(caption, label);
        var button = new Button { Content = caption, Style = (Style)Resources["MapToolbarButton"],
            HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 3, 0, 3) };
        button.Click += async (_, _) => await RunActionAsync(action);
        Links.Children.Add(button);
        return button;
    }
}
