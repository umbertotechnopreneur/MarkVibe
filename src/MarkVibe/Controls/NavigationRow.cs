/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared visible navigation hover and selection; 2026-10-16
 * Human guidance: Umberto Giacobbi; highlight backgrounds and text in Pages and Outline
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MarkVibe.Controls;

/// <summary>Keeps tree row colors consistent across mouse, keyboard, selection and theme changes.</summary>
internal static class NavigationRow
{
    private sealed class RowState
    {
        public bool Hovered;
    }

    private static readonly ConditionalWeakTable<TreeViewItem, RowState> States = new();

    /// <summary>Tracks the header only so hovering descendants never highlights their parent folders.</summary>
    public static void Attach(TreeViewItem item, FrameworkElement header)
    {
        var state = States.GetOrCreateValue(item);
        header.MouseEnter += (_, _) =>
        {
            state.Hovered = true;
            Apply(item, state);
        };
        header.MouseLeave += (_, _) =>
        {
            state.Hovered = false;
            Apply(item, state);
        };
        item.Selected += (_, _) => Apply(item, state);
        item.Unselected += (_, _) => Apply(item, state);
        item.Loaded += (_, _) => Apply(item, state);
        item.Unloaded += (_, _) => state.Hovered = false;
        Apply(item, state);
    }

    /// <summary>Makes manually composed header labels and glyphs follow the row's current foreground.</summary>
    public static void BindForeground(FrameworkElement element, DependencyProperty property, TreeViewItem item)
    {
        element.SetBinding(property, new Binding("Foreground") { Source = item });
    }

    /// <summary>Recolors existing rows after an immediate application appearance or accent change.</summary>
    public static void Refresh(ItemsControl parent)
    {
        foreach (var item in parent.Items.OfType<TreeViewItem>())
        {
            if (States.TryGetValue(item, out var state))
            {
                Apply(item, state);
            }
            Refresh(item);
        }
    }

    /// <summary>Uses an opaque selected accent and a softer hover, with readable selected text.</summary>
    private static void Apply(TreeViewItem item, RowState state)
    {
        var resources = Application.Current.Resources;
        var accent = ((SolidColorBrush)resources["SystemAccent"]).Color;
        if (item.IsSelected)
        {
            // A selected row also responds to hover without losing its persistent selected surface.
            var surface = state.Hovered
                ? Color.FromRgb(Lighten(accent.R), Lighten(accent.G), Lighten(accent.B))
                : Color.FromRgb(accent.R, accent.G, accent.B);
            item.Background = new SolidColorBrush(surface);
            var luminance = 0.2126 * Linear(surface.R) + 0.7152 * Linear(surface.G) + 0.0722 * Linear(surface.B);
            item.Foreground = new SolidColorBrush(luminance > 0.179 ? Colors.Black : Colors.White);
        }
        else
        {
            item.Background = new SolidColorBrush(state.Hovered
                ? Color.FromArgb(52, accent.R, accent.G, accent.B)
                : Colors.Transparent);
            item.Foreground = (Brush)resources[state.Hovered ? "SystemAccent" : "ShellText"];
        }
    }

    /// <summary>Raises selected hover brightness by a small fixed share of the remaining channel range.</summary>
    private static byte Lighten(byte channel) => (byte)Math.Round(channel + (255 - channel) * 0.12);

    /// <summary>Converts an sRGB channel to linear light for the black/white contrast decision.</summary>
    private static double Linear(byte channel)
    {
        var value = channel / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
