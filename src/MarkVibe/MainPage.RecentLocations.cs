/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; alphabetic recent-path menus and measured middle abbreviation; 2026-10-23
 * Human guidance: Umberto Giacobbi; persist file and folder paths and keep both ends of long paths visible
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;

/// <summary>Offers separate recent-file and recent-folder menus without changing guarded workspace opening.</summary>
public partial class MainPage
{
    private string _recentLocationKind = "";
    private int _recentMenuRevision;

    /// <summary>Keeps the recent-path popup within the shell and dismisses it on outside gestures or resizing.</summary>
    private void InitializeRecentLocations()
    {
        Loaded += (_, _) => Application.Current.RootVisual.MouseLeftButtonDown += DismissRecentLocationsOutside;
        Unloaded += (_, _) =>
        {
            Application.Current.RootVisual.MouseLeftButtonDown -= DismissRecentLocationsOutside;
            CloseRecentLocations();
        };
        SizeChanged += (_, _) => CloseRecentLocations();
    }

    /// <summary>Adds a small dropdown indicator to desktop open actions while retaining their translated captions.</summary>
    private void ConfigureRecentLocationButtons()
    {
        if (!_desktop)
        {
            return;
        }

        foreach (var button in new[] { OpenFileButton, OpenFolderButton })
        {
            if (button.Content is IconLabel label)
            {
                label.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var indicator = new TextBlock
                {
                    Text = "▾",
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(indicator, 2);
                label.Children.Add(indicator);
            }
        }
    }

    /// <summary>Loads alphabetic native recents; browser providers retain their existing file and folder pickers.</summary>
    private async Task ShowRecentLocationsAsync(string kind, Button owner)
    {
        if (!_desktop)
        {
            await OpenWorkspaceAsync(kind);
            return;
        }

        if (RecentLocationsPopup.IsOpen && _recentLocationKind == kind)
        {
            CloseRecentLocations();
            return;
        }

        CloseRecentLocations();
        FileMenuPopup.IsOpen = false;
        _recentLocationKind = kind;
        var revision = _recentMenuRevision;
        var locations = await BrowserServices.CallAsync<RecentLocation[]>("workspace.recent", new { kind });
        if (revision != _recentMenuRevision)
        {
            return;
        }

        BrowseRecentLocationButton.Content = new IconLabel(kind == "folder" ? "folder" : "file",
            kind == "folder" ? "Browse for a folder…" : "Browse for a file…");
        AppLocalization.SetText(RecentLocationsTitle, kind == "folder" ? "Recent folders" : "Recent files");
        RecentLocationItems.Children.Clear();
        foreach (var location in locations.OrderBy(location => location.Path, StringComparer.OrdinalIgnoreCase))
        {
            RecentLocationItems.Children.Add(CreateRecentLocationButton(location));
        }

        if (locations.Length == 0)
        {
            RecentLocationItems.Children.Add(new TextBlock
            {
                Text = UiStrings.T("No recent locations yet."),
                Margin = new Thickness(10, 8, 10, 12),
                TextWrapping = TextWrapping.Wrap
            });
        }

        var root = (FrameworkElement)Application.Current.RootVisual;
        var position = owner.TransformToVisual(root).Transform(new Point(0, owner.ActualHeight + 4));
        RecentLocationsSurface.Width = Math.Max(200, Math.Min(540, root.ActualWidth - 24));
        RecentLocationsScroll.MaxHeight = Math.Max(40, root.ActualHeight - position.Y - 140);
        RecentLocationsPopup.PlacementTarget = root;
        RecentLocationsPopup.Placement = PlacementMode.Relative;
        RecentLocationsPopup.StaysWithinScreenBounds = true;
        RecentLocationsPopup.HorizontalOffset = Math.Max(8, Math.Min(position.X, root.ActualWidth - RecentLocationsSurface.Width - 12));
        RecentLocationsPopup.VerticalOffset = Math.Max(8, position.Y);
        RecentLocationsScroll.ScrollToVerticalOffset(0);
        AppLocalization.Apply(RecentLocationsSurface);
        RecentLocationsPopup.IsOpen = true;
        BrowseRecentLocationButton.Focus();
    }

    /// <summary>Builds an accessible one-line path row whose tooltip and accessible name retain the complete path.</summary>
    private Button CreateRecentLocationButton(RecentLocation location)
    {
        var caption = new TextBlock { TextWrapping = TextWrapping.NoWrap };
        AppLocalization.SetRaw(caption, location.Path);
        var button = new Button
        {
            Content = caption,
            Style = (Style)Application.Current.Resources["FileMenuButton"]
        };
        ToolTipService.SetToolTip(button, location.Path);
        AutomationProperties.SetName(button, location.Path);
        AppLocalization.SetRaw(button);
        caption.SizeChanged += (_, _) => UpdateRecentPathCaption(caption, location.Path);
        caption.Loaded += (_, _) => UpdateRecentPathCaption(caption, location.Path);
        button.Click += async (_, _) =>
        {
            CloseRecentLocations();
            await ExecuteAsync(() => OpenWorkspaceAsync("recent", recentPath: location.Path, recentKind: location.Kind));
        };
        return button;
    }

    /// <summary>Measures the active font and removes only the middle, reserving more room for the path's end.</summary>
    private static void UpdateRecentPathCaption(TextBlock caption, string path)
    {
        if (caption.ActualWidth <= 0)
        {
            return;
        }

        var measure = new TextBlock
        {
            FontFamily = caption.FontFamily,
            FontSize = caption.FontSize,
            FontWeight = caption.FontWeight
        };
        // Measure candidates without changing the live row's layout.
        double Width(string text)
        {
            measure.Text = text;
            measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return measure.DesiredSize.Width;
        }

        if (Width(path) <= caption.ActualWidth)
        {
            AppLocalization.SetRaw(caption, path);
            return;
        }

        // Reserve more space for the leaf name and immediate parent than for the drive/root prefix.
        var characters = System.Globalization.StringInfo.ParseCombiningCharacters(path);
        var minimum = 2;
        var maximum = characters.Length - 1;
        var abbreviated = "…";
        while (minimum <= maximum)
        {
            var retained = (minimum + maximum) / 2;
            var prefixLength = Math.Max(1, retained / 3);
            var suffixLength = retained - prefixLength;
            // Keep Unicode folder names intact at both abbreviation boundaries.
            var candidate = path[..characters[prefixLength]] + "…" + path[characters[^suffixLength]..];
            if (Width(candidate) <= caption.ActualWidth)
            {
                abbreviated = candidate;
                minimum = retained + 1;
            }
            else
            {
                maximum = retained - 1;
            }
        }

        AppLocalization.SetRaw(caption, abbreviated);
    }

    /// <summary>Opens the normal picker for this menu, preserving unsaved-work and transition guards.</summary>
    private async void BrowseRecentLocation(object sender, RoutedEventArgs arguments)
    {
        var kind = _recentLocationKind;
        CloseRecentLocations();
        await ExecuteAsync(() => OpenWorkspaceAsync(kind));
    }

    /// <summary>Closes the menu and invalidates outstanding asynchronous list loads.</summary>
    private void CloseRecentLocations()
    {
        _recentMenuRevision++;
        RecentLocationsPopup.IsOpen = false;
    }

    /// <summary>Lets keyboard users dismiss the dropdown without opening a location.</summary>
    private void RecentLocationsKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Escape)
        {
            CloseRecentLocations();
            (_recentLocationKind == "folder" ? OpenFolderButton : OpenFileButton).Focus();
            arguments.Handled = true;
        }
    }

    /// <summary>Keeps clicks inside the menu or on its toggle buttons from being mistaken for outside dismissal.</summary>
    private void DismissRecentLocationsOutside(object sender, MouseButtonEventArgs arguments)
    {
        var current = arguments.OriginalSource as DependencyObject;
        while (current != null)
        {
            if (ReferenceEquals(current, RecentLocationsSurface) ||
                ReferenceEquals(current, OpenFileButton) || ReferenceEquals(current, OpenFolderButton))
            {
                return;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        CloseRecentLocations();
    }
}
