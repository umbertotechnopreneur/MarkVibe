/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared document context routing and confirmed local deletion; 2026-10-16
 * Human guidance: Umberto Giacobbi; Explorer, terminal, browser and delete in file context menus
 * Modified with AI: OpenAI Codex; forward sidebar refresh through the application service; 2026-10-25
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MarkVibe.Controls;
using MarkVibe.Core;
using MarkVibe.Services;

namespace MarkVibe;

/// <summary>Presents service-provided file commands and the existing confirmation dialog.</summary>
public partial class MainPage
{
    private bool _confirmingFileDelete;
    private bool _fileMutationBusy;

    /// <summary>Obtains application-owned models and services from the composition root.</summary>
    private DocumentApplicationServices _documentServices => ((App)Application.Current).Documents;
    /// <summary>Routes menu operations through the shared command service.</summary>
    private DocumentMenuService _documentMenus => _documentServices.Menus;
    /// <summary>Routes reader presentation changes through its model service.</summary>
    private ReaderPresentationService _readerPresentation => _documentServices.Reader;

    /// <summary>Resolves folder-specific reading choices over the global Settings defaults.</summary>
    private Preferences ReaderPreferences => _readerPresentation.ResolvePreferences();

    /// <summary>Restores this folder's own choices before its first document is rendered.</summary>
    private async Task LoadReaderAppearanceAsync(CancellationToken cancellationToken = default)
    {
        var context = _active;
        if (context != null)
        {
            await _readerPresentation.ReadAppearanceAsync(context.Provider.Info.Id, cancellationToken);
        }
    }

    /// <summary>Wires the shared context popup and current-document/workspace entry points once.</summary>
    private void InitializeFileMenus()
    {
        Loaded += (_, _) => _documentServices.WorkspaceRefresh.StateChanged += UpdateSidebarRefreshAction;
        Unloaded += (_, _) => _documentServices.WorkspaceRefresh.StateChanged -= UpdateSidebarRefreshAction;
        DocumentAppearance.PreferenceChanged += (key, value) => ExecuteAsync(async () =>
        {
            if (_active == null || _workspaceTransitionBusy)
            {
                return;
            }
            var context = _active;
            var result = await _readerPresentation.ChangeAppearanceAsync(
                context.Provider.Info.Id, key, value);
            if (ReferenceEquals(context, _active))
            {
                await ApplyPreferencesAsync();
                if (!result.Persisted)
                {
                    SetStatus("Preferences are available for this session. Browser storage is unavailable.");
                }
            }
        });
        DocumentFileActions.ActionRequested += action => ExecuteAsync(async () =>
        {
            var target = _documentServices.State.MenuTarget;
            FileMenuPopup.IsOpen = false;
            if (target != null)
            {
                await OpenFileActionAsync(target, action);
            }
        });
        MapHost.FileActionRequested += OpenFileActionAsync;
        MapHost.FileMenuRequested += ShowFileMenu;
        InitializeDocumentFind();
        AttachFileMenu(DocumentTitle, () => _active == null || _document == null ? null : new(_active.Provider.Info.Id, _document.Path, _document.Title));
        AttachFileMenu(WorkspaceName, () => _active == null ? null : new(_active.Provider.Info.Id, "", _active.Provider.Info.Name, true));
        AttachFileMenu(OutlineTree, () => _active == null || _document == null ? null : new(_active.Provider.Info.Id, _document.Path, _document.Title));
        AiHost.FileTargetCreated += (element, path, title) =>
        {
            var context = _active;
            if (context != null)
            {
                AttachFileMenu(element, () => new(context.Provider.Info.Id, path, title));
            }
        };
        Loaded += (_, _) => Application.Current.RootVisual.MouseLeftButtonDown += DismissFileMenuOutside;
        Unloaded += (_, _) =>
        {
            Application.Current.RootVisual.MouseLeftButtonDown -= DismissFileMenuOutside;
            FileMenuPopup.IsOpen = false;
        };
        SizeChanged += (_, _) => FileMenuPopup.IsOpen = false;
    }

    /// <summary>Displays the refresh service's availability without retaining operation state in the sidebar.</summary>
    private void UpdateSidebarRefreshAction()
    {
        SidebarRefreshButton.IsEnabled = _documentServices.WorkspaceRefresh.CanExecute(
            _active?.Provider.Info,
            _workspaceTransitionBusy || _documentBusy || _markdownEditBusy || _fileMutationBusy);
    }

    /// <summary>Forwards sidebar activation to the shared workspace service and existing view projection.</summary>
    private async void RefreshWorkspace(object sender, RoutedEventArgs arguments) => await ExecuteAsync(async () =>
    {
        var context = _active;
        if (context == null)
        {
            return;
        }

        await _documentServices.WorkspaceRefresh.ExecuteAsync(
            context.Provider,
            () => ReferenceEquals(context, _active)
                && !_workspaceTransitionBusy && !_documentBusy && !_markdownEditBusy && !_fileMutationBusy,
            async () =>
            {
                await ReloadCatalogAsync(_document?.Path ?? _missingPath);
                RefreshSidebar();
            },
            _opening.Token);
    });

    /// <summary>Adds right-click and Shift+F10 routing while preserving the target's normal navigation behavior.</summary>
    private void AttachFileMenu(FrameworkElement element, Func<FileMenuTarget?> targetProvider)
    {
        element.MouseRightButtonDown += (_, arguments) =>
        {
            if (!arguments.Handled && targetProvider() is { } target)
            {
                arguments.Handled = true;
                ShowFileMenu(target, arguments.GetPosition(Application.Current.RootVisual));
            }
        };
        element.KeyDown += (_, arguments) =>
        {
            if (!arguments.Handled && arguments.Key == Key.F10 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0 &&
                targetProvider() is { } target)
            {
                arguments.Handled = true;
                var position = element.TransformToVisual(Application.Current.RootVisual).Transform(new Point(16, Math.Min(32, element.ActualHeight)));
                ShowFileMenu(target, position);
            }
        };
    }

    /// <summary>Exposes local shell actions only for the currently active native Windows workspace.</summary>
    private bool CanUseLocalFileMenu(FileMenuTarget target) => _desktop && _active?.Provider.Info.Id == target.WorkspaceId && _active.Provider.Info.Mode == "native";

    /// <summary>Shows document appearance in every workspace and native file actions where supported.</summary>
    private void ShowFileMenu(FileMenuTarget target, Point position)
    {
        if (_active?.Provider.Info.Id != target.WorkspaceId)
        {
            return;
        }
        _documentMenus.Open(target, CanUseLocalFileMenu(target), _document, _layout.ReaderExpanded);
        AppLocalization.SetRaw(FileMenuTitle, target.Title);
        AppLocalization.SetRaw(FileMenuPath, target.Path.Length == 0 ? _active!.Provider.Info.Name : target.Path);
        DocumentFileActions.Configure(_documentServices.State.MenuItems);
        ReaderAppearanceGroup.IsExpanded = false;
        ReaderAppearanceGroup.Header = UiStrings.T("Appearance");
        System.Windows.Automation.AutomationProperties.SetName(ReaderAppearanceGroup, UiStrings.T("Appearance"));
        DocumentAppearance.Update(ReaderPreferences);
        _ = LoadReaderFontsAsync();
        var root = (FrameworkElement)Application.Current.RootVisual;
        FileMenuSurface.Width = Math.Max(220, Math.Min(310, root.ActualWidth - 24));
        FileMenuSurface.MaxHeight = Math.Max(80, Math.Min(680, root.ActualHeight - 32));
        FileMenuPopup.PlacementTarget = root;
        FileMenuPopup.Placement = PlacementMode.Relative;
        FileMenuPopup.StaysWithinScreenBounds = true;
        FileMenuPopup.HorizontalOffset = Math.Max(8, Math.Min(position.X, root.ActualWidth - FileMenuSurface.Width - 16));
        FileMenuPopup.VerticalOffset = Math.Max(8, Math.Min(position.Y, root.ActualHeight - FileMenuSurface.MaxHeight - 16));
        FileMenuScroll.ScrollToVerticalOffset(0);
        FileMenuPopup.IsOpen = true;
        AppLocalization.Apply(DocumentFileActions);
        AppLocalization.Apply(DocumentAppearance);
    }

    /// <summary>Populates only the verified installed monospace families already exposed by the desktop host.</summary>
    private async Task LoadReaderFontsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var fonts = await BrowserServices.CallAsync<UiFontOption[]>("ui.fonts");
            DocumentAppearance.SetFonts(fonts, ReaderPreferences.DocumentFont);
            AppLocalization.Apply(DocumentAppearance);
        });
    }

    /// <summary>Routes file actions for the captured target without changing map selection or opening another document.</summary>
    private async Task OpenFileActionAsync(FileMenuTarget target, string action)
    {
        if (_active?.Provider.Info.Id != target.WorkspaceId || _workspaceTransitionBusy)
        {
            return;
        }

        var context = _active!;
        var document = _document;
        var session = new DocumentCommandSession(context.Provider.Info, context.Catalog,
            context.Provider, document, ReaderPreferences, CanUseLocalFileMenu(target),
            () => ReferenceEquals(context, _active) && ReferenceEquals(document, _document)
                && !_workspaceTransitionBusy);
        var result = await _documentMenus.ExecuteAsync(session, target, action, _opening.Token);
        if (session.IsCurrent())
        {
            await PresentDocumentCommandAsync(target, result);
        }
    }
    /// <summary>Requires the app's blurred confirmation and revalidates both the file revision and UI session before recycling.</summary>
    private async Task ConfirmDeleteFileAsync(FileMenuTarget target)
    {
        if (_confirmingFileDelete || _documentBusy || _exportBusy)
        {
            return;
        }
        AssertMarkdownEditIdle();
        var context = _active!;
        var generation = _generation;
        var draft = context.Catalog.TryGetSession(target.Path)?.CurrentSource;
        _confirmingFileDelete = true;
        try
        {
            if (_aiBusy)
            {
                throw new WorkspaceException("file-busy", "Stop the AI request before deleting a file.");
            }
            var revision = await BrowserServices.CallAsync<string>("file.deleteRevision", new { workspaceId = target.WorkspaceId, path = target.Path });
            if (!ReferenceEquals(context, _active) || generation != _generation)
            {
                return;
            }
            var hasDraft = context.Catalog.TryGetSession(target.Path)?.NeedsSave == true;
            var accepted = await ConfirmationWindow.ConfirmAsync("Delete file?", "Are you sure you want to delete the selected file?", "Move to Recycle Bin",
                target.Path, hasDraft ? "The file will move to the Recycle Bin. Unsaved edits to this document will be discarded." : "The file will move to the Recycle Bin. You can restore it from Windows.", destructive: true);
            if (!accepted || !ReferenceEquals(context, _active) || generation != _generation || _workspaceTransitionBusy ||
                context.Catalog.TryGetSession(target.Path)?.CurrentSource != draft)
            {
                return;
            }
            AssertMarkdownEditIdle();
            _fileMutationBusy = true;
            try
            {
                await BrowserServices.CallAsync("map.flush");
                if (!ReferenceEquals(context, _active) || generation != _generation || _workspaceTransitionBusy)
                {
                    return;
                }
                await BrowserServices.CallAsync("file.delete", new { workspaceId = target.WorkspaceId, path = target.Path, revision });
            }
            finally
            {
                _fileMutationBusy = false;
            }
            context.Catalog.ForgetDeletedDocument(target.Path);
            context.AiContext.Remove(target.Path);
            await RefreshAfterFileDeletionAsync(context, target.Path);
            SetStatus("File moved to the Recycle Bin.");
        }
        finally
        {
            _confirmingFileDelete = false;
        }
    }

    /// <summary>Removes the deleted file from navigation, map and AI context, choosing an available document if necessary.</summary>
    private async Task RefreshAfterFileDeletionAsync(Context context, string deletedPath)
    {
        if (ReferenceEquals(context, _active))
        {
            _indexing.Cancel();
        }
        await context.Provider.RefreshAsync(CancellationToken.None);
        var entries = await context.Provider.EnumerateAsync(CancellationToken.None);
        context.Catalog.Seed(entries);
        context.Map.Session.EnsureDocuments(context.Catalog.Cards);
        if (context.Map.SelectedPath == deletedPath)
        {
            context.Map.SelectedPath = "";
        }
        if (!ReferenceEquals(context, _active))
        {
            return;
        }
        RefreshSidebar();
        if (_document?.Path == deletedPath)
        {
            await CloseDocumentFindAsync();
            var next = context.Catalog.Cards.FirstOrDefault(card => card.Path.Equals("README.md", StringComparison.OrdinalIgnoreCase)) ?? context.Catalog.Cards.FirstOrDefault();
            _document = null;
            if (next == null)
            {
                UpdateDocumentTitle("");
                ShowMessage("No Markdown pages yet", "Choose Markdown files or a folder containing documentation.");
                UpdateExportButtons();
            }
            else
            {
                await NavigateAsync(next.Path);
            }
        }
        else if (_view == "map")
        {
            await ShowMapAsync();
        }
        else if (_document != null && (_view is "read" or "split"))
        {
            await ReaderHost.DisplayAsync(_document, context.Provider.Info.Id, null, "", _opening.Token);
        }
        await RenderMarkdownAiAsync();
        _ = IndexAsync(context);
    }

    /// <summary>Closes the file menu on Escape without invoking an action.</summary>
    private void FileMenuKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Escape)
        {
            FileMenuPopup.IsOpen = false;
            arguments.Handled = true;
        }
    }

    /// <summary>Dismisses the file menu when the pointer lands outside its own surface.</summary>
    private void DismissFileMenuOutside(object sender, MouseButtonEventArgs arguments)
    {
        // Theme items live in a ComboBox popup outside the menu's visual ancestry.
        if (DocumentAppearance.IsThemeListOpen)
        {
            return;
        }
        var current = arguments.OriginalSource as DependencyObject;
        while (current != null)
        {
            if (ReferenceEquals(current, FileMenuSurface))
            {
                return;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        FileMenuPopup.IsOpen = false;
    }
}
