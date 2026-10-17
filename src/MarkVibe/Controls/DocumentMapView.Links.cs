/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; directional map link sections and document rows; 2026-10-16
 * Human guidance: Umberto Giacobbi; elegant incoming/outgoing navigation with document icons
 * Modified with AI: OpenAI Codex; expandable link sections with retained session expansion; 2026-10-16
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Builds directional document links in the map inspector.</summary>
public partial class DocumentMapView
{
    private readonly Dictionary<string, bool> _linkSectionExpansion = new(StringComparer.Ordinal);

    /// <summary>Groups unique targets in an initially collapsed section and remembers expansion during inspector refreshes.</summary>
    private void AddLinks(string title, string direction, IEnumerable<(string Path, string Anchor, string Title)> links)
    {
        var targets = links.DistinctBy(link => (link.Path, link.Anchor)).ToArray();
        var rows = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };

        if (targets.Length == 0)
        {
            var empty = new TextBlock
            {
                FontSize = 11,
                Foreground = LinkMutedBrush(),
                Margin = new Thickness(4, 4, 4, 6)
            };
            AppLocalization.SetText(empty, "No links yet");
            rows.Children.Add(empty);
        }
        foreach (var link in targets)
        {
            rows.Children.Add(CreateLinkRow(link.Path, link.Anchor, link.Title));
        }

        var section = new Expander
        {
            Style = (Style)Application.Current.Resources["MinimalExpander"],
            Foreground = (Brush)Resources["MapChromeText"],
            Header = CreateLinkHeading(title, direction, targets.Length),
            Content = rows,
            IsExpanded = _linkSectionExpansion.TryGetValue(title, out var expanded) && expanded,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(section, UiStrings.T(title));
        section.Expanded += (_, _) => _linkSectionExpansion[title] = true;
        section.Collapsed += (_, _) => _linkSectionExpansion[title] = false;

        Links.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(11, 13, 18)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(35, 41, 52)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 4, 0, 12),
            Child = section
        });
    }

    /// <summary>Builds a directional section heading with its localized label and distinct-target count.</summary>
    private Grid CreateLinkHeading(string title, string direction, int targetCount)
    {
        var heading = new Grid { Margin = new Thickness(4, 2, 4, 2) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heading.Children.Add(new VectorIcon
        {
            Symbol = direction,
            Width = 16,
            Height = 16,
            Foreground = (Brush)Application.Current.Resources["SystemAccent"],
            VerticalAlignment = VerticalAlignment.Center
        });

        var label = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeights.Medium,
            Foreground = (Brush)Resources["MapChromeText"],
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        AppLocalization.SetText(label, title);
        Grid.SetColumn(label, 1);
        heading.Children.Add(label);

        var count = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Foreground = LinkMutedBrush(),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        AppLocalization.SetRaw(count, targetCount.ToString(CultureInfo.CurrentCulture));
        Grid.SetColumn(count, 2);
        heading.Children.Add(count);
        return heading;
    }

    /// <summary>Shows a readable document title and its path while retaining navigation and native context actions.</summary>
    private Button CreateLinkRow(string path, string anchor, string title)
    {
        var button = new Button { Style = (Style)Resources["MapLinkButton"] };
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.Children.Add(new VectorIcon
        {
            Symbol = "file", Width = 16, Height = 16,
            VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 0, 0)
        });
        var details = new StackPanel();
        var caption = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap };
        caption.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") { Source = button });
        AppLocalization.SetRaw(caption, title);
        details.Children.Add(caption);
        var location = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"), FontSize = 10,
            Foreground = LinkMutedBrush(), TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 4, 0, 0)
        };
        AppLocalization.SetRaw(location, path + (anchor.Length > 0 ? "#" + anchor : ""));
        details.Children.Add(location);
        Grid.SetColumn(details, 1);
        content.Children.Add(details);
        button.Content = content;
        AppLocalization.SetRaw(button);
        AutomationProperties.SetName(button, title + " · " + path);
        ToolTipService.SetToolTip(button, path + (anchor.Length > 0 ? "#" + anchor : ""));
        button.MouseEnter += (_, _) => button.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
        button.MouseLeave += (_, _) => button.Foreground = (Brush)Resources["MapChromeText"];
        var workspaceId = _workspaceId;
        var target = new FileMenuTarget(workspaceId, path, title);
        button.MouseRightButtonDown += (_, arguments) =>
        {
            if (target.WorkspaceId.StartsWith("native:", StringComparison.Ordinal))
            {
                arguments.Handled = true;
                FileMenuRequested?.Invoke(target, arguments.GetPosition(Application.Current.RootVisual));
            }
        };
        button.Click += async (_, _) => await RunActionAsync(async () =>
        {
            if (_workspaceId == workspaceId && NavigateRequested != null)
            {
                await NavigateRequested(path, anchor);
            }
        });
        return button;
    }

    /// <summary>Uses a restrained secondary text color with readable contrast on the black map chrome.</summary>
    private static SolidColorBrush LinkMutedBrush() => new(Color.FromRgb(142, 154, 174));
}
