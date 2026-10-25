/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; bind document services to shell presentation; 2026-10-25
 * Human guidance: Umberto Giacobbi; menus invoke services above the data model
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Input;
using MarkVibe.Core;
using MarkVibe.Services;

namespace MarkVibe;

/// <summary>Connects document command results to navigation, dialogs and the managed search bar.</summary>
public partial class MainPage
{
    /// <summary>Uses the application-owned search service and result model.</summary>
    private DocumentFindService _documentFind => _documentServices.Find;

    /// <summary>Presents service-validated intents using the shell's existing guarded navigation and dialogs.</summary>
    private async Task PresentDocumentCommandAsync(FileMenuTarget target, DocumentCommandResult result)
    {
        switch (result.Presentation)
        {
            case "find":
                await OpenDocumentFindAsync(target);
                break;
            case "sourceHere":
                await SetViewAsync(_view == "split" ? "split" : "source");
                if (!_documentMenus.IsCurrentDocument(target, _active?.Provider.Info.Id, _document))
                {
                    return;
                }
                await BrowserServices.CallAsync("source.restore", new
                {
                    hostId = SourceHost.HostId,
                    position = new ReadingPosition(SourceLine: result.SourceLine)
                });
                break;
            case "column":
            case "expandedColumn":
                await SetReaderWidthAsync(result.Presentation == "expandedColumn");
                break;
            case "exportPdf":
            case "exportHtml":
                await ExportAsync(result.Presentation == "exportPdf" ? "pdf" : "html");
                break;
            case "delete":
                await ConfirmDeleteFileAsync(target);
                break;
        }
    }

    /// <summary>Binds managed search controls and the document shortcut to one service.</summary>
    private void InitializeDocumentFind()
    {
        DocumentFind.SearchRequested += (query, direction) => ExecuteAsync(() => FindDocumentAsync(query, direction));
        DocumentFind.CloseRequested += () => _ = ExecuteAsync(CloseDocumentFindAsync);
        KeyDown += DocumentFindKeyDown;
    }

    /// <summary>Handles Ctrl+F when keyboard focus is in the managed shell rather than an isolated renderer.</summary>
    private async void DocumentFindKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Handled || arguments.Key != Key.F ||
            (Keyboard.Modifiers & ModifierKeys.Control) == 0 ||
            (Keyboard.Modifiers & ModifierKeys.Alt) != 0 ||
            _active == null || _document == null || _view == "map")
        {
            return;
        }
        arguments.Handled = true;
        await ExecuteAsync(() => OpenDocumentFindAsync(new(
            _active.Provider.Info.Id, _document.Path, _document.Title)));
    }

    /// <summary>Shows a managed find bar for the captured current document and prefills its selected text.</summary>
    private async Task OpenDocumentFindAsync(FileMenuTarget target)
    {
        if (_active?.Provider.Info.Id != target.WorkspaceId || _document?.Path != target.Path ||
            _workspaceTransitionBusy || _documentBusy)
        {
            return;
        }
        if (_view == "map")
        {
            await SetViewAsync("read");
        }
        await CloseDocumentFindAsync();
        if (!_documentMenus.IsCurrentDocument(target, _active?.Provider.Info.Id, _document) ||
            _workspaceTransitionBusy || _documentBusy)
        {
            return;
        }
        _documentFind.Open(target, _view);
        FileMenuPopup.IsOpen = false;
        DocumentFind.Visibility = Visibility.Visible;
        DocumentFind.Open(_documentFind.State.Query);
    }

    /// <summary>Displays only the latest search result for the still-active document snapshot.</summary>
    private async Task FindDocumentAsync(string query, int direction)
    {
        var target = _documentFind.State.Target;
        var document = _document;
        if (target == null || document?.Path != target.Path || _active?.Provider.Info.Id != target.WorkspaceId)
        {
            await CloseDocumentFindAsync();
            return;
        }
        var applied = await _documentFind.SearchAsync(query, direction);
        if (applied && ReferenceEquals(document, _document))
        {
            DocumentFind.ShowResult(_documentFind.State.Current, _documentFind.State.Total);
        }
    }

    /// <summary>Stops pending input and releases highlights before changing a document or renderer.</summary>
    private async Task CloseDocumentFindAsync()
    {
        DocumentFind.Close();
        DocumentFind.Visibility = Visibility.Collapsed;
        await _documentFind.CloseAsync();
    }
}
