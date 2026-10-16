/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; managed map search and AI collection action; 2026-10-16
 * Human guidance: Umberto Giacobbi; find and center documents, collect AI context explicitly
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;

public partial class DocumentMapView
{
    private string _selectedPath = "";
    private bool _updatingSearch;
    public event Func<string, string, IReadOnlyList<DocumentCard>>? FindDocumentsRequested;
    public event Func<string, string, Task>? CenterDocumentRequested;
    public event Func<string, string, Task>? AddToAiRequested;

    /// <summary>Finds titles and paths in the complete managed catalog without changing the current graph or document.</summary>
    private void MapSearchChanged(object sender, TextChangedEventArgs arguments)
    {
        if (MapSearchResults == null || MapSearchEmpty == null || MapSearchWatermark == null || SearchFeedback == null)
        {
            return;
        }

        _updatingSearch = true;
        MapSearchResults.Items.Clear();
        var query = MapSearchBox.Text.Trim();
        MapSearchWatermark.Visibility = MapSearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearSearchButton.Visibility = MapSearchBox.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (query.Length > 0 && FindDocumentsRequested != null)
        {
            foreach (var document in FindDocumentsRequested(_workspaceId, query))
            {
                var caption = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, Foreground = (Brush)Resources["MapChromeText"] };
                AppLocalization.SetRaw(caption, document.Title + " · " + document.Path);
                var item = new ListBoxItem { Content = caption, Tag = document.Path };
                AppLocalization.SetRaw(item);
                AutomationProperties.SetName(item, document.Title + " · " + document.Path);
                ToolTipService.SetToolTip(item, document.Path);
                MapSearchResults.Items.Add(item);
            }
        }

        MapSearchResults.Visibility = MapSearchResults.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        MapSearchEmpty.Visibility = query.Length > 0 && MapSearchResults.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SearchFeedback.Visibility = query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        _updatingSearch = false;
    }

    /// <summary>Centers a chosen result while retaining the query and its input control across graph refreshes.</summary>
    private async void MapSearchSelected(object sender, SelectionChangedEventArgs arguments)
    {
        if (_updatingSearch || MapSearchResults.SelectedItem is not ListBoxItem { Tag: string path })
        {
            return;
        }

        HideMapSearchResults();
        if (CenterDocumentRequested != null)
        {
            var workspaceId = _workspaceId;
            await RunActionAsync(() => CenterDocumentRequested(workspaceId, path));
            if (_workspaceId == workspaceId)
            {
                MapSearchBox.Focus();
            }
        }
    }

    /// <summary>Provides keyboard access to the first search result and clears the query with Escape.</summary>
    private void MapSearchKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Escape)
        {
            MapSearchBox.Text = "";
            arguments.Handled = true;
        }
        else if ((arguments.Key == Key.Enter || arguments.Key == Key.Down) && MapSearchResults.Items.Count > 0)
        {
            MapSearchResults.SelectedIndex = 0;
            arguments.Handled = true;
        }
    }

    /// <summary>Clears map search without changing the selected card or opening its document.</summary>
    private void ClearMapSearch(object sender, RoutedEventArgs arguments)
    {
        MapSearchBox.Text = "";
        MapSearchBox.Focus();
    }

    /// <summary>Closes results without erasing the user's query.</summary>
    private void HideMapSearchResults()
    {
        var wasUpdating = _updatingSearch;
        _updatingSearch = true;
        MapSearchResults.SelectedIndex = -1;
        MapSearchResults.Visibility = Visibility.Collapsed;
        MapSearchEmpty.Visibility = Visibility.Collapsed;
        SearchFeedback.Visibility = Visibility.Collapsed;
        _updatingSearch = wasUpdating;
    }

    /// <summary>Reflects collection membership and disables additions during an outstanding context read.</summary>
    private void UpdateAiContextButton(bool busy)
    {
        var included = _state.TryGetProperty("aiContextPaths", out var paths)
            && paths.EnumerateArray().Any(path => path.GetString() == _selectedPath);
        var contextBusy = _state.TryGetProperty("aiContextBusy", out var loading) && loading.GetBoolean();
        AddToAiButton.IsEnabled = !busy && !contextBusy && _selectedPath.Length > 0;
        SetMapToggle(AddToAiButton, included);
        var label = included ? "Remove selected document from AI context" : "Add selected document to AI context";
        ToolTipService.SetToolTip(AddToAiButton, UiStrings.T(label));
        AutomationProperties.SetName(AddToAiButton, UiStrings.T(label));
    }

    /// <summary>Requests explicit collection changes; it never sends a provider request itself.</summary>
    private async void ToggleAiContext(object sender, RoutedEventArgs arguments)
    {
        if (AddToAiRequested != null && AddToAiButton.IsEnabled)
        {
            var workspaceId = _workspaceId;
            var path = _selectedPath;
            await RunActionAsync(() => AddToAiRequested(workspaceId, path));
        }
    }
}
