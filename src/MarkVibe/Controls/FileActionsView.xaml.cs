/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared document file action controls; 2026-10-16
 * Human guidance: Umberto Giacobbi; consistent file context menus and explicit delete confirmation
 * Modified with AI: OpenAI Codex; render service-owned context command models; 2026-10-25
 * Human guidance: Umberto Giacobbi; keep command policy in services and presentation in controls
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Core;
using MarkVibe.Services;

namespace MarkVibe.Controls;

/// <summary>Displays service-provided commands without deciding document capabilities or executing business actions.</summary>
public partial class FileActionsView : UserControl
{
    /// <summary>Publishes the selected command identifier to the owner's service adapter.</summary>
    public event Func<string, Task>? ActionRequested;

    /// <summary>Initializes the shared command list used by document and graph menus.</summary>
    public FileActionsView()
    {
        InitializeComponent();
    }

    /// <summary>Adapts graph-menu capabilities to the same service-owned file command policy.</summary>
    public void Configure(bool localFiles, bool markdown, bool folder, bool hasSelection = false)
    {
        Configure(DocumentMenuService.CreateFileActions(localFiles, markdown, folder, hasSelection));
    }

    /// <summary>Renders commands in their supplied order, grouping related choices into collapsed sections.</summary>
    public void Configure(IReadOnlyList<DocumentMenuItem> items)
    {
        CommandItems.Children.Clear();
        var groups = new Dictionary<string, StackPanel>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var destination = CommandItems;
            if (item.Group.Length > 0)
            {
                if (!groups.TryGetValue(item.Group, out destination))
                {
                    destination = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
                    groups.Add(item.Group, destination);
                    CommandItems.Children.Add(CreateGroup(item.Group, destination));
                }
            }

            if (item.Action == "delete")
            {
                destination.Children.Add(new Border
                {
                    Height = 1,
                    Margin = new Thickness(6, 7, 6, 7),
                    Background = (Brush)Application.Current.Resources["ShellBorder"]
                });
            }

            destination.Children.Add(CreateButton(item));
        }

        AppLocalization.Apply(this);
    }

    /// <summary>Creates an accessible expandable menu section with a localizable heading.</summary>
    private static Expander CreateGroup(string label, StackPanel content)
    {
        var heading = new TextBlock { Margin = new Thickness(6, 5, 6, 5) };
        AppLocalization.SetText(heading, label);
        var group = new Expander
        {
            Style = (Style)Application.Current.Resources["MinimalExpander"],
            Header = heading,
            Content = content,
            IsExpanded = false,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(group, label);
        return group;
    }

    /// <summary>Maps one command descriptor to an icon, caption, active marker and routed click.</summary>
    private Button CreateButton(DocumentMenuItem item)
    {
        var caption = new Grid();
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        caption.Children.Add(new IconLabel(item.Icon, item.Label));
        if (item.Checked)
        {
            var marker = new TextBlock
            {
                Text = "✓",
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            AppLocalization.SetRaw(marker);
            Grid.SetColumn(marker, 1);
            caption.Children.Add(marker);
        }

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["FileMenuButton"],
            Content = caption,
            IsEnabled = item.Enabled,
            Tag = item.Action
        };
        if (item.Action == "delete")
        {
            button.Foreground = new SolidColorBrush(Color.FromRgb(248, 120, 132));
        }

        var accessibleLabel = item.Checked ? UiStrings.T("{0} (selected)", UiStrings.T(item.Label)) : item.Label;
        AutomationProperties.SetName(button, accessibleLabel);
        ToolTipService.SetToolTip(button, item.Label);
        button.Click += CommandClicked;
        return button;
    }

    /// <summary>Forwards a selected command; the service remains responsible for revalidating its target.</summary>
    private async void CommandClicked(object sender, RoutedEventArgs arguments)
    {
        if (sender is Button { Tag: string action } && ActionRequested != null)
        {
            await ActionRequested(action);
        }
    }
}
