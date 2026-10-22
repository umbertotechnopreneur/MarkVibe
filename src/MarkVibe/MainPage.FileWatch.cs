/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; explicit current-file reload confirmation; 2026-10-23
 * Human guidance: Umberto Giacobbi; ask Yes or No when the current file changes
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows.Threading;
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;

public partial class MainPage
{
    /// <summary>Identifies one observed file revision within one exact document watch registration.</summary>
    private sealed record DiskChange(string WorkspaceId, string Path, string WatchId, string Fingerprint, bool Missing);

    private readonly DispatcherTimer _diskChangeTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private DiskChange? _pendingDiskChange;
    private string _documentWatchId = "";
    private bool _diskPromptOpen;

    /// <summary>Defers incoming file notifications until existing application operations and confirmations finish.</summary>
    private void InitializeDocumentWatch()
    {
        _diskChangeTimer.Tick += ShowDiskChangePrompt;
    }

    /// <summary>Starts a new registration from the original source, keeping AI edits distinct from disk changes.</summary>
    private async Task WatchCurrentDocumentAsync(Context context, MarkdownModel model, CancellationToken cancellationToken)
    {
        var watchId = Guid.NewGuid().ToString("N");
        _documentWatchId = watchId;
        _pendingDiskChange = null;
        _diskChangeTimer.Stop();
        var original = context.Catalog.TryGetSession(model.Path)?.OriginalSource ?? model.Source;
        try
        {
            await BrowserServices.CallAsync("document.watch", new
            {
                workspaceId = context.Provider.Info.Id,
                path = model.Path,
                watchId,
                fingerprint = MarkdownParser.Fingerprint(original)
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (_documentWatchId == watchId)
            {
                _documentWatchId = "";
                SetStatus("Automatic file watching is unavailable. Reopen the file to read changes.");
            }
        }
    }

    /// <summary>Stops the old folder's registration without changing its cached documents or drafts.</summary>
    private async Task StopDocumentWatchAsync()
    {
        _documentWatchId = "";
        _pendingDiskChange = null;
        _diskChangeTimer.Stop();
        await BrowserServices.CallAsync("document.watch.stop");
    }

    /// <summary>Retains only the newest event for the current document; stale watch IDs cannot open a dialog.</summary>
    private void QueueDiskChange(JsonElement request)
    {
        var change = new DiskChange(
            request.GetProperty("workspaceId").GetString() ?? "",
            request.GetProperty("path").GetString() ?? "",
            request.GetProperty("watchId").GetString() ?? "",
            request.GetProperty("fingerprint").GetString() ?? "",
            request.TryGetProperty("missing", out var missing) && missing.GetBoolean());
        if (!IsCurrentDiskChange(change))
        {
            return;
        }
        _pendingDiskChange = change;
        _diskChangeTimer.Start();
    }

    /// <summary>Checks both folder identity and the navigation-owned watch before publishing or reloading.</summary>
    private bool IsCurrentDiskChange(DiskChange change) => _active?.Provider.Info.Id == change.WorkspaceId
        && _document?.Path == change.Path
        && _documentWatchId == change.WatchId;

    /// <summary>Presents one Yes/No dialog, leaving the current document untouched after No or an unavailable file.</summary>
    private async void ShowDiskChangePrompt(object? sender, EventArgs arguments)
    {
        await ExecuteAsync(async () =>
        {
            if (_diskPromptOpen || _documentBusy || _workspaceTransitionBusy || _aiBusy || _exportBusy
                || _markdownEditBusy || _fileMutationBusy || _settingsDialog != null || ConfirmationWindow.IsShowing)
            {
                return;
            }
            var change = _pendingDiskChange;
            _pendingDiskChange = null;
            _diskChangeTimer.Stop();
            if (change == null || !IsCurrentDiskChange(change) || _document == null)
            {
                return;
            }
            if (change.Missing)
            {
                SetStatus("The current file is unavailable on disk. The displayed copy has been kept.");
                return;
            }
            if (_document.Fingerprint == change.Fingerprint)
            {
                return;
            }

            _diskPromptOpen = true;
            var expected = _document;
            try
            {
                var localEdits = _active!.Catalog.TryGetSession(expected.Path)?.IsModified == true;
                var accepted = await ConfirmationWindow.ConfirmAsync(
                    "File changed", "The file changed on disk. Would you like to reload it?", "Yes",
                    target: expected.Path,
                    details: localEdits ? "Reloading will discard this document's local edits and undo history." : null,
                    cancelAction: "No");
                if (accepted && IsCurrentDiskChange(change) && ReferenceEquals(_document, expected))
                {
                    await ReloadChangedDocumentAsync(expected);
                }
            }
            finally
            {
                _diskPromptOpen = false;
                if (_pendingDiskChange != null)
                {
                    _diskChangeTimer.Start();
                }
            }
        });
    }

    /// <summary>Reads and parses disk content before discarding any local state, then reuses normal navigation and rendering.</summary>
    private async Task ReloadChangedDocumentAsync(MarkdownModel expected)
    {
        AssertMarkdownEditIdle();
        CancelMarkdownAi();
        var context = _active!;
        var generation = _generation;
        ReadingPosition? position = null;
        _markdownEditBusy = true;
        UpdateExportButtons();
        try
        {
            if (_view is "read" or "source" or "split")
            {
                var sourceView = _view == "source";
                position = await BrowserServices.CallAsync<ReadingPosition>(
                    sourceView ? "source.capture" : "markdown.capture",
                    new { hostId = sourceView ? SourceHost.HostId : ReaderHost.HostId });
            }
            var source = await context.Provider.ReadDocumentAsync(expected.Path, _navigation.Token);
            var reloaded = _parser.Parse(expected.Path, source);
            if (!ReferenceEquals(context, _active) || !ReferenceEquals(expected, _document) || generation != _generation)
            {
                return;
            }
            // Parse and session checks complete before the explicit discard commit.
            context.Catalog.ReloadFromDisk(reloaded);
            _indexing.Cancel();
        }
        finally
        {
            _markdownEditBusy = false;
            UpdateExportButtons();
        }

        await NavigateAsync(expected.Path, "", push: false, position: position);
        SetStatus("Document reloaded from disk.");
    }
}
