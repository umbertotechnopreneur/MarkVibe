/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; persistent dock and standard splitter layout; 2026-10-16
 * Human guidance: Umberto Giacobbi; remember pane sizes and keep the assistant docked
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MarkVibe;

/// <summary>Retains user-selected pane sizes independently of temporary responsive layout constraints.</summary>
public partial class MainPage
{
    /// <summary>Stores shell geometry only; missing preferences start with the assistant closed.</summary>
    private sealed record ShellLayout(
        bool AiOpen = false,
        double AiWidth = 360,
        bool SidebarOpen = true,
        double SidebarWidth = 220,
        double SourceRatio = 0.5,
        double MapInspectorWidth = 290)
    {
        /// <summary>Rejects nonfinite sizes and bounds restored geometry before applying it to XAML.</summary>
        public ShellLayout Validated() => this with
        {
            AiWidth = double.IsFinite(AiWidth) ? Math.Clamp(AiWidth, 300, 600) : 360,
            SidebarWidth = double.IsFinite(SidebarWidth) ? Math.Clamp(SidebarWidth, 180, 520) : 220,
            SourceRatio = double.IsFinite(SourceRatio) ? Math.Clamp(SourceRatio, 0.2, 0.8) : 0.5,
            MapInspectorWidth = double.IsFinite(MapInspectorWidth) ? Math.Clamp(MapInspectorWidth, 220, 600) : 290
        };
    }

    private ShellLayout _layout = new();
    private bool _layoutReady;

    /// <summary>Observes standard splitter completion without implementing custom dragging.</summary>
    private void InitializeLayoutPersistence()
    {
        MapHost.InspectorWidthChanged += SaveMapInspectorWidthAsync;

        foreach (var splitter in new[] { SidebarSplitter, SplitDivider, AiSplitter })
        {
            splitter.LostMouseCapture += (_, _) => QueueSplitterSave(splitter);
            splitter.KeyUp += (_, arguments) =>
            {
                if (arguments.Key is Key.Left or Key.Right)
                {
                    QueueSplitterSave(splitter);
                }
            };
        }
    }

    /// <summary>Saves only deliberate inspector resizing while retaining all other shell preferences.</summary>
    private async Task SaveMapInspectorWidthAsync(double width)
    {
        if (!_layoutReady)
        {
            return;
        }

        _layout = (_layout with { MapInspectorWidth = width }).Validated();
        await SaveLayoutAsync();
    }

    /// <summary>Reads final column definitions after the splitter has committed or cancelled its gesture.</summary>
    private void QueueSplitterSave(GridSplitter splitter)
    {
        Dispatcher.BeginInvoke(async () => await ExecuteAsync(async () =>
        {
            if (!_layoutReady)
            {
                return;
            }

            // Window resizing and automatic pane hiding never overwrite the user's preferred sizes.
            if (splitter == SidebarSplitter && SidebarSplitter.Visibility == Visibility.Visible)
            {
                _layout = _layout with { SidebarWidth = SidebarColumn.Width.Value };
            }
            else if (splitter == AiSplitter && AiSplitter.Visibility == Visibility.Visible)
            {
                _layout = _layout with { AiWidth = AiColumn.Width.Value };
            }
            else if (splitter == SplitDivider && _view == "split")
            {
                var totalWidth = SourceColumn.Width.Value + ReadColumn.Width.Value;
                if (totalWidth > 0)
                {
                    _layout = _layout with { SourceRatio = SourceColumn.Width.Value / totalWidth };
                }
            }

            _layout = _layout.Validated();
            LayoutViews();
            await SaveLayoutAsync();
        }));
    }

    /// <summary>Saves small layout changes without reapplying themes or rerendering document content.</summary>
    private async Task SaveLayoutAsync()
    {
        if (!_layoutReady)
        {
            return;
        }

        var persisted = await BrowserServices.CallAsync<bool>("layout.save", _layout);
        if (!persisted)
        {
            SetStatus("Preferences are available for this session. Browser storage is unavailable.");
        }
    }
}
