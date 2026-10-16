/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; persistent standard splitter for the document inspector; 2026-10-16
 * Human guidance: Umberto Giacobbi; resize the map inspector and restore its width after reopening
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MarkVibe.Controls;

/// <summary>Separates the inspector's preferred width from temporary responsive layout constraints.</summary>
public partial class DocumentMapView
{
    private const double MinimumCanvasWidth = 320;
    private const double MinimumInspectorWidth = 220;
    private const double MaximumInspectorWidth = 600;
    private const double DefaultInspectorWidth = 290;
    private const double InspectorDividerWidth = 6;

    private bool _inspectorOpen = true;
    private bool _inspectorExplicit;
    private double _preferredInspectorWidth = DefaultInspectorWidth;

    /// <summary>Reports completed user resizing so the shell can persist its layout without rerendering content.</summary>
    public event Func<double, Task>? InspectorWidthChanged;

    /// <summary>Applies restored shell geometry without treating restoration as a new user preference.</summary>
    public void RestoreInspectorWidth(double width)
    {
        _preferredInspectorWidth = ValidateInspectorWidth(width);
        ReflowMapChrome();
    }

    /// <summary>Uses standard splitter mouse and keyboard behavior and observes its final column width.</summary>
    private void InitializeInspectorLayout()
    {
        InspectorSplitter.LostMouseCapture += (_, _) => QueueInspectorWidthSave();
        InspectorSplitter.KeyUp += InspectorSplitterKeyUp;
    }

    /// <summary>Persists keyboard resizing after the built-in splitter has processed the arrow key.</summary>
    private void InspectorSplitterKeyUp(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key is Key.Left or Key.Right)
        {
            QueueInspectorWidthSave();
        }
    }

    /// <summary>Reads the committed or cancelled gesture after the splitter releases mouse capture.</summary>
    private void QueueInspectorWidthSave()
    {
        Dispatcher.BeginInvoke(async () => await RunActionAsync(async () =>
        {
            if (InspectorSplitter.Visibility != Visibility.Visible)
            {
                return;
            }

            var inspectorWidth = ValidateInspectorWidth(LinksColumn.Width.Value);
            if (Math.Abs(inspectorWidth - _preferredInspectorWidth) < 0.1)
            {
                return;
            }

            // Only an explicit splitter gesture replaces the preference; window resizing never does.
            _preferredInspectorWidth = inspectorWidth;
            ReflowMapChrome();
            await (InspectorWidthChanged?.Invoke(inspectorWidth) ?? Task.CompletedTask);
        }));
    }

    /// <summary>Toggles document preview, permitting an explicit overlay only when two columns cannot fit.</summary>
    private void ToggleInspector(object sender, RoutedEventArgs arguments)
    {
        _inspectorOpen = LinksPane.Visibility != Visibility.Visible;
        _inspectorExplicit = true;
        ReflowMapChrome();
    }

    /// <summary>Uses a resizable dock whenever both panes fit, retaining the preferred width when space shrinks.</summary>
    private void ReflowInspector()
    {
        var minimumDockWidth = MinimumCanvasWidth + InspectorDividerWidth + MinimumInspectorWidth;
        var canDock = ActualWidth >= minimumDockWidth;
        var isVisible = _inspectorOpen && (canDock || _inspectorExplicit);
        var isDocked = isVisible && canDock;
        var maximumDockWidth = Math.Max(
            MinimumInspectorWidth,
            Math.Min(MaximumInspectorWidth, ActualWidth - MinimumCanvasWidth - InspectorDividerWidth));
        var visibleDockWidth = Math.Min(_preferredInspectorWidth, maximumDockWidth);

        MapCanvasColumn.MinWidth = isDocked ? MinimumCanvasWidth : 0;
        LinksColumn.MinWidth = isDocked ? MinimumInspectorWidth : 0;
        LinksColumn.MaxWidth = maximumDockWidth;
        LinksColumn.Width = new GridLength(isDocked ? visibleDockWidth : 0);
        InspectorDividerColumn.Width = new GridLength(isDocked ? InspectorDividerWidth : 0);
        InspectorSplitter.Visibility = isDocked ? Visibility.Visible : Visibility.Collapsed;

        // At very narrow sizes the explicitly opened preview may overlay the canvas, but its
        // temporary width is never written back to the user's desktop splitter preference.
        Grid.SetColumn(LinksPane, canDock ? 2 : 0);
        LinksPane.Width = canDock
            ? double.NaN
            : Math.Min(_preferredInspectorWidth, Math.Max(0, ActualWidth - 32));
        LinksPane.HorizontalAlignment = canDock ? HorizontalAlignment.Stretch : HorizontalAlignment.Right;
        LinksPane.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        SetMapToggle(InspectorButton, isVisible);
    }

    /// <summary>Rejects malformed storage values and bounds widths shared with the shell persistence contract.</summary>
    private static double ValidateInspectorWidth(double width)
    {
        return double.IsFinite(width)
            ? Math.Clamp(width, MinimumInspectorWidth, MaximumInspectorWidth)
            : DefaultInspectorWidth;
    }
}
