/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; persistent dock and standard splitter layout; 2026-10-16
 * Human guidance: Umberto Giacobbi; remember pane sizes and keep the assistant docked
 * Modified with AI: OpenAI Codex; persist reader column width without rerendering; 2026-10-24
 * Human guidance: Umberto Giacobbi; use the available horizontal reading space
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;

/// <summary>Retains user-selected pane sizes independently of temporary responsive layout constraints.</summary>
public partial class MainPage
{
    /// <summary>Projects the layout owned by the shared application model.</summary>
    private ShellLayout _layout => _documentServices.State.Reader.Layout;
    /// <summary>Reads the service's layout-restoration state.</summary>
    private bool _layoutReady => _documentServices.State.Reader.LayoutReady;

    /// <summary>Fits toolbar controls to the measured document pane, including sidebar and assistant resizing.</summary>
    private void LayoutDocumentToolbar()
    {
        var toolbarWidth = Math.Max(0, DocumentPanel.ActualWidth - 24);
        var readerVisible = _view is "read" or "split";
        var compact = toolbarWidth < (readerVisible ? 940 : 570);
        var stacked = readerVisible && toolbarWidth < 520;

        DocumentToolbarRow.Height = new GridLength(stacked ? 100 : 56);
        Grid.SetRow(DocumentActions, stacked ? 1 : 0);
        Grid.SetColumn(DocumentActions, stacked ? 0 : 1);
        Grid.SetColumnSpan(DocumentActions, stacked ? 2 : 1);
        ReaderWidthControls.Visibility = readerVisible ? Visibility.Visible : Visibility.Collapsed;

        foreach (var button in new[]
        {
            ReadButton, SourceButton, SplitButton, MapButton, AiButton,
            ColumnButton, ExpandedColumnButton
        })
        {
            ButtonIcons.SetCompact(button, compact);
        }

        ButtonIcons.SetCompact(ExportHtmlButton, true);
        ButtonIcons.SetCompact(ExportPdfButton, true);
        UpdateReaderWidthButtons();
    }

    /// <summary>Switches reader width without refreshing document themes, assets or diagrams.</summary>
    private async void ChangeReaderWidth(object sender, RoutedEventArgs arguments)
    {
        await ExecuteAsync(() => SetReaderWidthAsync(ReferenceEquals(sender, ExpandedColumnButton)));
    }

    /// <summary>Binds the same service-backed width choice for toolbar and document menu.</summary>
    private async Task SetReaderWidthAsync(bool expanded)
    {
        if (!_layoutReady || _workspaceTransitionBusy || _layout.ReaderExpanded == expanded)
        {
            return;
        }
        _readerPresentation.SetWidth(expanded);
        UpdateReaderWidthButtons();
        await SaveLayoutAsync();
    }

    /// <summary>Shows the active width visually and in each button's accessible name.</summary>
    private void UpdateReaderWidthButtons()
    {
        SetViewTabState(ColumnButton, !_layout.ReaderExpanded);
        SetViewTabState(ExpandedColumnButton, _layout.ReaderExpanded);
        System.Windows.Automation.AutomationProperties.SetName(
            ColumnButton, UiStrings.T(_layout.ReaderExpanded ? "Column" : "Column (selected)"));
        System.Windows.Automation.AutomationProperties.SetName(
            ExpandedColumnButton, UiStrings.T(_layout.ReaderExpanded ? "Expanded column (selected)" : "Expanded column"));
    }

    /// <summary>Observes standard splitter completion without implementing custom dragging.</summary>
    private void InitializeLayoutPersistence()
    {
        MapHost.InspectorWidthChanged += SaveMapInspectorWidthAsync;
        DocumentPanel.SizeChanged += (_, _) => LayoutDocumentToolbar();

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

        _readerPresentation.SetMapInspectorWidth(width);
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
                _readerPresentation.SetSidebarWidth(SidebarColumn.Width.Value);
            }
            else if (splitter == AiSplitter && AiSplitter.Visibility == Visibility.Visible)
            {
                _readerPresentation.SetAiWidth(AiColumn.Width.Value);
            }
            else if (splitter == SplitDivider && _view == "split")
            {
                var totalWidth = SourceColumn.Width.Value + ReadColumn.Width.Value;
                if (totalWidth > 0)
                {
                    _readerPresentation.SetSourceRatio(SourceColumn.Width.Value / totalWidth);
                }
            }


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

        var persisted = await _readerPresentation.SaveLayoutAsync();
        if (!persisted)
        {
            SetStatus("Preferences are available for this session. Browser storage is unavailable.");
        }
    }
}
