/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; card preview, selected color swatches and icon gallery; 2026-10-16
 * Human guidance: Umberto Giacobbi; improve the appearance popup and include file context actions
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using MarkVibe.Core;

namespace MarkVibe.Controls;

public partial class DocumentMapView
{
    private readonly Dictionary<string, Button> _colorChoices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _iconChoices = new(StringComparer.Ordinal);
    private int _appearanceColumns = 8;
    public event Func<FileMenuTarget, string, Task>? FileActionRequested;
    public event Action<FileMenuTarget, Point>? FileMenuRequested;

    /// <summary>Opens a bounded appearance gallery for the current authenticated map session.</summary>
    private void ShowAppearance(JsonElement request)
    {
        if (request.GetProperty("workspaceId").GetString() != _workspaceId ||
            request.GetProperty("stateId").GetString() != _state.GetProperty("stateId").GetString())
        {
            return;
        }
        _appearancePath = request.GetProperty("path").GetString() ?? "";
        AppLocalization.SetRaw(AppearanceTitle, request.GetProperty("title").GetString() ?? _appearancePath);
        AppLocalization.SetRaw(AppearancePath, _appearancePath);
        var local = _workspaceId.StartsWith("native:", StringComparison.Ordinal);
        AppearanceFileActions.Configure(local, true, false);
        AppearanceFileDivider.Visibility = local ? Visibility.Visible : Visibility.Collapsed;
        var root = (FrameworkElement)Application.Current.RootVisual;
        MapFlyoutSurface.Width = Math.Max(240, Math.Min(360, root.ActualWidth - 24));
        _appearanceColumns = Math.Clamp((int)((MapFlyoutSurface.Width - 48) / 38), 4, 8);
        BuildAppearanceChoices();
        AppearanceScroll.MaxHeight = Math.Max(180, root.ActualHeight - 60);
        AppearancePopup.PlacementTarget = root;
        AppearancePopup.Placement = PlacementMode.Relative;
        AppearancePopup.StaysWithinScreenBounds = true;
        AppearancePopup.HorizontalOffset = Math.Max(8, Math.Min(request.GetProperty("x").GetDouble(), root.ActualWidth - MapFlyoutSurface.Width - 16));
        AppearancePopup.VerticalOffset = Math.Max(8, Math.Min(request.GetProperty("y").GetDouble(), root.ActualHeight - 660));
        AppearancePopup.IsOpen = true;
        RefreshAppearanceFromState();
    }

    /// <summary>Creates compact swatches and visible icon choices with named tooltips and accessible selection states.</summary>
    private void BuildAppearanceChoices()
    {
        _colorChoices.Clear();
        _iconChoices.Clear();
        PrepareChoiceGrid(Colors, _appearanceColumns, MapLayoutCodec.Colors.Count);
        PrepareChoiceGrid(Icons, _appearanceColumns, MapLayoutCodec.Icons.Count);
        for (var index = 0; index < MapLayoutCodec.Colors.Count; index++)
        {
            var choice = MapLayoutCodec.Colors[index];
            var fill = ColorBrush(choice.Value);
            var content = new Grid();
            content.Children.Add(new Ellipse { Width = 22, Height = 22, Fill = fill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            content.Children.Add(new VectorIcon { Symbol = "review", Width = 14, Height = 14, Foreground = new SolidColorBrush(ColorsWhite), Visibility = Visibility.Collapsed });
            var button = CreateAppearanceChoice(choice.Label, choice.Value, content);
            button.Click += async (_, _) => await ChangeAppearanceAsync(choice.Value, null);
            PlaceChoice(Colors, button, index);
            _colorChoices.Add(choice.Value, button);
        }
        for (var index = 0; index < MapLayoutCodec.Icons.Count; index++)
        {
            var choice = MapLayoutCodec.Icons[index];
            var button = CreateAppearanceChoice(choice.Label, choice.Value, new VectorIcon { Symbol = "map-" + choice.Value, Width = 20, Height = 20 });
            button.Click += async (_, _) => await ChangeAppearanceAsync(null, choice.Value);
            PlaceChoice(Icons, button, index);
            _iconChoices.Add(choice.Value, button);
        }
    }

    private static Color ColorsWhite => Color.FromRgb(255, 255, 255);

    /// <summary>Resets the gallery grid before populating equally spaced choices.</summary>
    private static void PrepareChoiceGrid(Grid grid, int columns, int count)
    {
        grid.Children.Clear();
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();
        for (var index = 0; index < columns; index++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        }
        for (var index = 0; index < (count + columns - 1) / columns; index++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
        }
    }

    /// <summary>Creates an appearance action while retaining its untranslated catalog label for language changes.</summary>
    private static Button CreateAppearanceChoice(string label, string value, FrameworkElement content)
    {
        var button = new Button { Content = content, Tag = value, Style = (Style)Application.Current.Resources["AppearanceChoiceButton"] };
        ToolTipService.SetToolTip(button, UiStrings.T(label));
        AutomationProperties.SetName(button, UiStrings.T(label));
        return button;
    }

    /// <summary>Places one swatch or icon in the eight-column gallery.</summary>
    private void PlaceChoice(Grid grid, Button button, int index)
    {
        Grid.SetColumn(button, index % _appearanceColumns);
        Grid.SetRow(button, index / _appearanceColumns);
        grid.Children.Add(button);
    }

    /// <summary>Reflects accepted managed appearance state in the preview and selection rings.</summary>
    private void RefreshAppearanceFromState()
    {
        if (!AppearancePopup.IsOpen || !_state.GetProperty("layout").GetProperty("nodes").TryGetProperty(_appearancePath, out var value))
        {
            return;
        }
        var color = value.GetProperty("color").GetString()!;
        var icon = value.GetProperty("icon").GetString()!;
        var brush = ColorBrush(color);
        AppearancePreview.BorderBrush = brush;
        AppearanceIcon.Foreground = brush;
        AppearanceIcon.Symbol = "map-" + icon;
        AppLocalization.SetText(ColorCaption, MapLayoutCodec.Colors.First(choice => choice.Value == color).Label);
        AppLocalization.SetText(IconCaption, MapLayoutCodec.Icons.First(choice => choice.Value == icon).Label);
        foreach (var choice in _colorChoices)
        {
            var selected = choice.Key == color;
            SetAppearanceSelection(choice.Value, selected);
            if (choice.Value.Content is Grid content && content.Children[1] is VectorIcon check)
            {
                check.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        foreach (var choice in _iconChoices)
        {
            SetAppearanceSelection(choice.Value, choice.Key == icon);
        }
    }

    /// <summary>Marks the active choice with a visible accent ring and an accessible selected description.</summary>
    private static void SetAppearanceSelection(Button button, bool selected)
    {
        button.BorderThickness = new Thickness(selected ? 2 : 0);
        button.BorderBrush = (Brush)Application.Current.Resources["SystemAccent"];
        button.Background = (Brush)Application.Current.Resources[selected ? "AccentSoft" : "PanelBackground"];
        AutomationProperties.SetHelpText(button, selected ? UiStrings.T("Selected") : "");
    }

    /// <summary>Converts the validated map palette into a native solid brush.</summary>
    private static SolidColorBrush ColorBrush(string color) => new(Color.FromRgb(Convert.ToByte(color[1..3], 16), Convert.ToByte(color[3..5], 16), Convert.ToByte(color[5..7], 16)));

    /// <summary>Closes the picker before routing an explicit action for the file that opened it.</summary>
    private Task RequestAppearanceFileActionAsync(string action)
    {
        var target = new FileMenuTarget(_workspaceId, _appearancePath, AppearanceTitle.Text);
        AppearancePopup.IsOpen = false;
        return RunActionAsync(() => FileActionRequested?.Invoke(target, action) ?? Task.CompletedTask);
    }
}
