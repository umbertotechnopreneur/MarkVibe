/* VBWR B
 * Project: MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Modified with AI: OpenAI Codex; single-row sidebar tabs; 2026-10-11
 * Human guidance: Umberto Giacobbi; adjacent left-aligned tabs without backgrounds
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls.Primitives;

namespace MarkVibe.Controls;

/// <summary>Keeps navigation tabs in one ordered row instead of TabPanel's selected-row wrapping.</summary>
public sealed class SingleRowTabPanel : TabPanel
{
    /// <summary>Measures each visible header within its share of the available strip.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleChildren = Children.Where(child => child.Visibility == Visibility.Visible).ToArray();
        var slotWidth = availableSize.Width / Math.Max(1, visibleChildren.Length);
        var height = 0d;
        var width = 0d;
        foreach (var child in visibleChildren)
        {
            child.Measure(new Size(slotWidth, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
            width += child.DesiredSize.Width;
        }
        return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, height);
    }

    /// <summary>Keeps natural-width tabs adjacent at the left without wrapping or spreading across the strip.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visibleChildren = Children.Where(child => child.Visibility == Visibility.Visible).ToArray();
        var slotWidth = finalSize.Width / Math.Max(1, visibleChildren.Length);
        var left = 0d;
        foreach (var child in visibleChildren)
        {
            var width = Math.Min(slotWidth, child.DesiredSize.Width);
            child.Arrange(new Rect(left, 0, width, finalSize.Height));
            left += width;
        }
        return finalSize;
    }
}
