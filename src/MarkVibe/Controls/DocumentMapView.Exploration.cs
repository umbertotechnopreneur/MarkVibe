/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; managed selection shelf, comparison and navigation trail; 2026-10-16
 * Modified with AI: OpenAI Codex; retain unchanged exploration previews and clarify control updates; 2026-10-16
 * Human guidance: Umberto Giacobbi; useful map actions with clear, lightweight controls
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Builds temporary exploration controls from the authoritative workspace snapshot.</summary>
public partial class DocumentMapView
{
    private bool _updatingExploration;
    private string _pickedDocumentsSignature = "";
    private string _trailDocumentsSignature = "";
    private string _comparisonDocumentsSignature = "";

    /// <summary>Publishes one exploration action through the renderer's current session identity.</summary>
    private Task ExploreAsync(string action, object? data = null)
    {
        return RunActionAsync(() => BrowserServices.CallAsync("map.command", new
        {
            hostId = HostId,
            action = "explore-" + action,
            data
        }));
    }

    /// <summary>Updates availability separately from bounded preview construction, avoiding redundant control rebuilds.</summary>
    private void UpdateExplorationControls(bool busy)
    {
        if (!_state.TryGetProperty("exploration", out var value)
            || value.Deserialize<MapExplorationSnapshot>(Wire.Options) is not { } exploration)
        {
            return;
        }

        _updatingExploration = true;
        try
        {
            SetMapToggle(RelationLensButton, exploration.LensEnabled);
            SetMapToggle(SelectionToolButton, exploration.SelectionEnabled);
            RelationLensButton.IsEnabled = !busy;
            SelectionToolButton.IsEnabled = !busy;
            AttentionMode.IsEnabled = !busy;
            AttentionMode.SelectedItem = AttentionMode.Items.Cast<ComboBoxItem>()
                .First(item => (string)item.Tag == exploration.AttentionMode);
        }
        finally
        {
            _updatingExploration = false;
        }

        AttentionSummary.Visibility = exploration.AttentionMode == "all" ? Visibility.Collapsed : Visibility.Visible;
        var attentionSummary = exploration.IndexComplete
            ? "{0} matches"
            : "{0} matches · index incomplete";
        AppLocalization.SetText(AttentionSummary, attentionSummary, exploration.AttentionCount);

        ComparePickedButton.IsEnabled = !busy && exploration.Picked.Count >= 2;
        AddPickedAiButton.IsEnabled = !busy && exploration.Picked.Count > 0 && AddToAiButton.IsEnabled;
        SelectionShelf.Visibility = exploration.Picked.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TrailShelf.Visibility = exploration.Trail.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        ExplorationShelf.Visibility = exploration.Picked.Count > 0 || exploration.Trail.Count > 1
            ? Visibility.Visible : Visibility.Collapsed;
        ComparisonSurface.Visibility = exploration.CompareOpen ? Visibility.Visible : Visibility.Collapsed;

        // Lens, busy and attention changes do not alter document previews. Keep those
        // controls mounted so a small toolbar action does not rebuild the entire shelf.
        var pickedSignature = _workspaceId + "\n" + value.GetProperty("picked").GetRawText();
        if (_pickedDocumentsSignature != pickedSignature)
        {
            AppLocalization.SetText(SelectionCount, "{0} selected", exploration.Picked.Count);
            ComparePickedButton.Content = new IconLabel("split", "Compare documents");
            AddPickedAiButton.Content = new IconLabel("sparkles", "Add to AI context");
            RebuildExplorationDocuments(PickedDocuments, exploration.Picked);
            _pickedDocumentsSignature = pickedSignature;
        }

        var trailSignature = _workspaceId + "\n" + value.GetProperty("trail").GetRawText();
        if (_trailDocumentsSignature != trailSignature)
        {
            RebuildExplorationDocuments(TrailDocuments, exploration.Trail);
            _trailDocumentsSignature = trailSignature;
        }

        if (!exploration.CompareOpen)
        {
            if (ComparisonDocuments.Children.Count > 0)
            {
                ComparisonDocuments.Children.Clear();
            }

            _comparisonDocumentsSignature = "";
        }
        else if (_comparisonDocumentsSignature != pickedSignature)
        {
            RebuildComparison(exploration.Picked);
            _comparisonDocumentsSignature = pickedSignature;
        }
    }

    /// <summary>Creates bounded document chips whose clicks locate a page without adding a history visit.</summary>
    private void RebuildExplorationDocuments(StackPanel panel, IReadOnlyList<MapExplorationDocument> documents)
    {
        panel.Children.Clear();
        var workspaceId = _workspaceId;
        foreach (var document in documents)
        {
            var caption = new TextBlock
            {
                MaxWidth = 115,
                FontSize = 10,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            AppLocalization.SetRaw(caption, document.Title);
            var button = new Button
            {
                Content = caption,
                Style = (Style)Resources["MapInspectorAction"],
                Padding = new Thickness(7, 4, 7, 4),
                MinHeight = 26,
                Margin = new Thickness(0, 0, 5, 0)
            };
            AppLocalization.SetRaw(button);
            AutomationProperties.SetName(button, document.Title + " · " + document.Path);
            ToolTipService.SetToolTip(button, document.Path);
            button.Click += async (_, _) => await RunActionAsync(async () =>
            {
                if (_workspaceId == workspaceId && CenterDocumentRequested != null)
                {
                    await CenterDocumentRequested(workspaceId, document.Path);
                }
            });
            panel.Children.Add(button);
        }
    }

    /// <summary>Shows catalog previews side by side; opening a page remains an explicit button action.</summary>
    private void RebuildComparison(IReadOnlyList<MapExplorationDocument> documents)
    {
        ComparisonDocuments.Children.Clear();
        var workspaceId = _workspaceId;
        foreach (var document in documents)
        {
            var details = new StackPanel { Width = 210 };
            var title = new TextBlock
            {
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Resources["MapChromeText"]
            };
            AppLocalization.SetRaw(title, document.Title);
            details.Children.Add(title);
            var pathText = new TextBlock
            {
                FontSize = 10,
                Foreground = LinkMutedBrush(),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 12)
            };
            AppLocalization.SetRaw(pathText, document.Path);
            details.Children.Add(pathText);
            var excerpt = new TextBlock
            {
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 19,
                Foreground = (Brush)Resources["MapChromeText"]
            };
            AppLocalization.SetRaw(excerpt, document.Introduction);
            details.Children.Add(excerpt);

            if (document.Statistics is { } statistics)
            {
                var counts = new TextBlock
                {
                    FontSize = 10,
                    Foreground = LinkMutedBrush(),
                    Margin = new Thickness(0, 12, 0, 12),
                    TextWrapping = TextWrapping.Wrap
                };
                AppLocalization.SetText(counts, "{0} words · ≈ {1} tokens", statistics.Words, statistics.EstimatedTokens);
                details.Children.Add(counts);
            }

            var openButton = new Button
            {
                Content = new IconLabel("book", "Open document"),
                Style = (Style)Resources["MapInspectorAction"],
                Margin = new Thickness(0, 12, 0, 0)
            };
            openButton.Click += async (_, _) => await RunActionAsync(async () =>
            {
                if (_workspaceId == workspaceId && NavigateRequested != null)
                {
                    await NavigateRequested(document.Path, "");
                }
            });
            details.Children.Add(openButton);
            ComparisonDocuments.Children.Add(new Border
            {
                Child = details,
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(8, 11, 16)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(53, 65, 82)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            });
        }
    }

    /// <summary>Toggles relation emphasis without changing card positions or saved map state.</summary>
    private async void ToggleRelationLens(object sender, RoutedEventArgs arguments) => await ExploreAsync("lens");

    /// <summary>Toggles the explicit multi-selection tool for users who prefer it to keyboard modifiers.</summary>
    private async void ToggleSelectionTool(object sender, RoutedEventArgs arguments) => await ExploreAsync("selection");

    /// <summary>Publishes an attention choice only after a user changes the dropdown.</summary>
    private async void AttentionChanged(object sender, SelectionChangedEventArgs arguments)
    {
        if (!_updatingExploration && AttentionMode.SelectedItem is ComboBoxItem item)
        {
            await ExploreAsync("attention", new { value = (string)item.Tag });
        }
    }

    /// <summary>Opens a comparison of the current bounded selection.</summary>
    private async void ComparePicked(object sender, RoutedEventArgs arguments) => await ExploreAsync("compare");

    /// <summary>Adds the selection as reference documents; this action sends no inference request.</summary>
    private async void AddPickedToAi(object sender, RoutedEventArgs arguments) => await ExploreAsync("add-ai");

    /// <summary>Clears temporary selection without changing any document or layout.</summary>
    private async void ClearPicked(object sender, RoutedEventArgs arguments) => await ExploreAsync("clear");

    /// <summary>Closes the comparison while retaining the picked documents.</summary>
    private async void CloseComparison(object sender, RoutedEventArgs arguments) => await ExploreAsync("close-compare");

    /// <summary>Supports keyboard dismissal of the comparison surface.</summary>
    private async void ComparisonKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Escape)
        {
            arguments.Handled = true;
            await ExploreAsync("close-compare");
        }
    }
}
