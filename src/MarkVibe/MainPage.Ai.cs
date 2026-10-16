/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; stateless Markdown AI editing and selection replacement; 2026-10-11
 * Human guidance: Umberto Giacobbi; replace the document or selection with each structured reply
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;

public partial class MainPage
{
    private bool _aiConfigured, _aiModelsReady, _aiBusy, _aiWaiting, _aiOpen = true, _markdownEditBusy;
    private bool _aiBudgetExpanded;
    private bool _aiCompact;
    private Point? _aiPanelPosition;
    private string _aiModel = "default", _aiEffort = "medium", _aiPrompt = "", _aiMessage = "";
    private MarkdownAiModel[] _aiModels = [];
    private MarkdownAiUsage? _aiUsage;
    private MarkdownAiResult? _aiResult;
    private MarkdownAiRequest? _aiPreparedRequest;
    private int[]? _aiPreparedPrefixes;
    private MarkdownModel? _aiResultPreview;
    private long _aiTotalTokens;
    private long _aiStartedAt;
    private int _aiRequestCount, _aiPromptClear, _aiKeyGeneration;
    private CancellationTokenSource? _aiRequest;
    private readonly CancellationTokenSource _aiLifetime = new();
    private MarkdownSelection? _aiSelection;
    private readonly Dictionary<string, MarkdownEdits> _markdownEdits = new(StringComparer.Ordinal);

    /// <summary>Retains a source range, including an explicit unsupported reader selection.</summary>
    private sealed record MarkdownSelection(string Fingerprint, int Start, int End, bool Unsupported);

    /// <summary>Local document undo state, never sent as conversation history or persisted beside the API key.</summary>
    private sealed class MarkdownEdits(Context context, MarkdownModel model)
    {
        public Context Context { get; } = context;
        public string Path { get; } = model.Path;
        public string Original { get; } = model.Source;
        public string Source { get; set; } = model.Source;
        public string Exported { get; set; } = model.Source;
        public List<string> Undo { get; } = [];
        public List<string> Redo { get; } = [];
        public bool Draft => Source != Original;
        public bool NeedsSave => Draft && Source != Exported;

        /// <summary>Bounds source-only local history by both entry count and UTF-16 storage, preserving the latest undo.</summary>
        public static void Remember(List<string> history, string source)
        {
            history.Add(source);
            while (history.Count > 16 || history.Sum(entry => (long)entry.Length) > 4 * 1024 * 1024)
            {
                history.RemoveAt(0);
            }
        }
    }

    /// <summary>Locates a draft only in its original provider session, avoiding stable-workspace-ID reuse.</summary>
    private MarkdownEdits? CurrentMarkdownEdits()
    {
        if (_active == null || _document == null)
        {
            return null;
        }
        _markdownEdits.TryGetValue(_active.Provider.Info.Id + "\n" + _document.Path, out var edits);
        return edits != null && ReferenceEquals(edits.Context, _active) ? edits : null;
    }

    /// <summary>Reports whether the current title should identify an in-memory Markdown revision.</summary>
    private bool IsMarkdownDraft() => CurrentMarkdownEdits()?.Draft == true;

    /// <summary>Stops the active request; only its own completion handler can clear the busy state.</summary>
    private void CancelMarkdownAi() => _aiRequest?.Cancel();

    /// <summary>Rechecks saved key availability and intersects documented text models with account access.</summary>
    private async Task RefreshMarkdownAiKeyAsync()
    {
        CancelMarkdownAi();
        var generation = ++_aiKeyGeneration;
        try
        {
            var status = await BrowserServices.CallAsync<OpenAiKeyStatus>("openai.status", cancellationToken: _aiLifetime.Token);
            if (generation != _aiKeyGeneration)
            {
                return;
            }
            if (_aiConfigured != status.Configured)
            {
                _aiOpen = true;
            }
            _aiConfigured = status.Configured;
            _aiModelsReady = false;
            _aiModels = [];
            _aiMessage = "";
            LayoutMarkdownAi();
            await RenderMarkdownAiAsync();
            if (!status.Configured)
            {
                CancelMarkdownAi();
                return;
            }
            var available = await BrowserServices.CallAsync<string[]>("openai.models", cancellationToken: _aiLifetime.Token);
            if (generation != _aiKeyGeneration)
            {
                return;
            }
            _aiModels = MarkdownAi.Models.Where(model => available.Contains(model.Id, StringComparer.Ordinal)).ToArray();
            _aiModelsReady = true;
            if (_aiModel != "default" && !_aiModels.Any(model => model.Id == _aiModel))
            {
                _aiModel = "default";
            }
            if (_aiModels.Length == 0)
            {
                _aiMessage = "No supported text models are available for this key. Check its model permissions in Settings.";
            }
        }
        catch (WorkspaceException error)
        {
            if (generation == _aiKeyGeneration)
            {
                _aiMessage = error.Message;
            }
        }
        if (generation == _aiKeyGeneration)
        {
            LayoutMarkdownAi();
            await RenderMarkdownAiAsync();
        }
    }

    /// <summary>Anchors a bounded floating composer to the reading viewport without shrinking the document.</summary>
    private void LayoutMarkdownAi()
    {
        var viewportWidth = DocumentViewport.ActualWidth > 0 ? DocumentViewport.ActualWidth : Math.Max(0, ActualWidth - 221);
        var viewportHeight = DocumentViewport.ActualHeight > 0 ? DocumentViewport.ActualHeight : Math.Max(0, ActualHeight - 108);
        var availableWidth = Math.Max(0, viewportWidth - 32);
        var availableHeight = Math.Max(0, viewportHeight - 32);
        var preferredHeight = _aiCompact ? 52 : !_aiConfigured ? 112 : _aiBudgetExpanded ? 440 : 232;
        AiHost.Width = Math.Min(380, availableWidth);
        AiHost.Height = Math.Min(preferredHeight, availableHeight);
        AiHost.Visibility = _aiOpen ? Visibility.Visible : Visibility.Collapsed;
        if (_aiPanelPosition is Point position)
        {
            var left = Math.Clamp(position.X, 8, Math.Max(8, viewportWidth - AiHost.Width - 8));
            var top = Math.Clamp(position.Y, 8, Math.Max(8, viewportHeight - AiHost.Height - 8));
            _aiPanelPosition = new Point(left, top);
            AiHost.HorizontalAlignment = HorizontalAlignment.Left;
            AiHost.VerticalAlignment = VerticalAlignment.Top;
            AiHost.Margin = new Thickness(left, top, 0, 0);
        }
        AiButton.Foreground = (Brush)Application.Current.Resources[_aiOpen ? "SystemAccent" : "ShellText"];
        if (_started)
        {
            // Reserve trailing reading space so the last paragraph can scroll above the floating dock.
            OpenSilver.Interop.ExecuteJavaScriptVoid(
                "document.documentElement.style.setProperty('--ai-dock-height', $0 + 'px')",
                _aiOpen ? AiHost.Height : 0);
        }
    }

    /// <summary>Moves the managed assistant panel while retaining its bounded position after resize or compaction.</summary>
    private void MoveMarkdownAi(Point delta)
    {
        var position = _aiPanelPosition ?? AiHost.TransformToVisual(DocumentViewport).Transform(new Point(0, 0));
        _aiPanelPosition = new Point(position.X + delta.X, position.Y + delta.Y);
        LayoutMarkdownAi();
    }

    /// <summary>Publishes reusable UI state and numeric counters; no API credential or prior prompts are included.</summary>
    private async Task RenderMarkdownAiAsync()
    {
        UpdateExportButtons();
        var selection = _aiSelection;
        var selected = _document != null && selection != null && !selection.Unsupported
            && selection.Fingerprint == _document.Fingerprint;
        var target = selected ? _document!.Source[selection!.Start..selection.End] : _document?.Source ?? "";
        var edits = CurrentMarkdownEdits();
        var resolvedModel = _aiModel == "default" ? _aiModels.FirstOrDefault()?.Id ?? MarkdownAi.DefaultModel : _aiModel;
        var preparedMatches = _aiPreparedRequest != null && _aiPreparedRequest.Model == resolvedModel
            && _aiPreparedRequest.Prompt == _aiPrompt.Trim() && _aiPreparedRequest.Document == target
            && _aiPreparedRequest.Scope == (selected ? "selection" : "document");
        var nextUsage = preparedMatches ? MarkdownAi.PreparedUsage(_aiPreparedRequest!, _aiPreparedPrefixes)
            : MarkdownAi.Estimate(target, _aiPrompt);
        var contextUsage = _aiBusy ? _aiUsage ?? nextUsage : nextUsage;
        AiWaitHost.Visibility = _aiBusy && _aiWaiting ? Visibility.Visible : Visibility.Collapsed;
        await AiWaitHost.DisplayAsync(new
        {
            waiting = _aiBusy && _aiWaiting,
            ownerHostId = AiHost.HostId,
            workspaceId = _active?.Provider.Info.Id ?? "",
            fingerprint = _document?.Fingerprint ?? "",
            startedAt = _aiStartedAt,
            timeoutSeconds = MarkdownAi.RequestTimeoutSeconds,
            stage = _aiMessage
        });
        await AiHost.DisplayAsync(new
        {
            visible = _aiOpen,
            configured = _aiConfigured,
            modelsReady = _aiModelsReady,
            models = _aiModels,
            model = _aiModel,
            resolvedModel = _aiModel == "default" ? _aiModels.FirstOrDefault()?.Id ?? "" : _aiModel,
            capacity = MarkdownAi.Capacity(resolvedModel, contextUsage),
            contextUsage,
            catalogDate = MarkdownAi.CatalogDate,
            effort = _aiEffort,
            busy = _aiBusy,
            message = _aiMessage,
            prompt = _aiPrompt,
            promptClear = _aiPromptClear,
            promptLimit = MarkdownAi.PromptCharacters,
            path = _document?.Path ?? "",
            fingerprint = _document?.Fingerprint ?? "",
            workspaceId = _active?.Provider.Info.Id ?? "",
            documentAvailable = _document != null && !_documentBusy && !_markdownEditBusy && MessageOverlay.Visibility != Visibility.Visible,
            scope = selected ? "selection" : "document",
            selectionCharacters = selected ? selection!.End - selection.Start : 0,
            unsupportedSelection = selection?.Unsupported == true,
            usage = _aiUsage ?? nextUsage,
            totalTokens = _aiTotalTokens,
            requestCount = _aiRequestCount,
            result = _aiResult,
            resultPreview = _aiResultPreview == null ? null : new { _aiResultPreview.Html, _aiResultPreview.Headings },
            canUndo = edits?.Undo.Count > 0,
            canRedo = edits?.Redo.Count > 0,
            draft = edits?.Draft == true,
            unsavedDocuments = _markdownEdits.Values.Count(draft => draft.NeedsSave)
        });
    }

    /// <summary>Retains the selected source range while focus moves to the composer; stale and invalid ranges are rejected.</summary>
    private async Task HandleMarkdownSelectionAsync(JsonElement request)
    {
        if (_document == null || _active == null || request.GetProperty("workspaceId").GetString() != _active.Provider.Info.Id
            || request.GetProperty("path").GetString() != _document.Path
            || request.GetProperty("fingerprint").GetString() != _document.Fingerprint)
        {
            return;
        }
        _aiSelection = null;
        var selection = request.GetProperty("selection");
        if (selection.ValueKind == JsonValueKind.Object)
        {
            if (selection.TryGetProperty("unsupported", out var unsupported) && unsupported.GetBoolean())
            {
                _aiSelection = new(_document.Fingerprint, 0, 0, true);
            }
            else
            {
                var start = selection.GetProperty("start").GetInt32();
                var end = selection.GetProperty("end").GetInt32();
                if (start >= 0 && end > start && end <= _document.Source.Length)
                {
                    _aiSelection = new(_document.Fingerprint, start, end, false);
                }
                else
                {
                    _aiSelection = new(_document.Fingerprint, 0, 0, true);
                }
            }
        }
        await RenderMarkdownAiAsync();
    }

    /// <summary>Routes composer actions through managed selection, parsing, request and local undo contracts.</summary>
    private async Task HandleMarkdownAiAsync(JsonElement request)
    {
        if (request.GetProperty("hostId").GetString() != AiHost.HostId)
        {
            return;
        }
        var action = request.GetProperty("action").GetString();
        switch (action)
        {
            case "layout":
                _aiBudgetExpanded = request.TryGetProperty("expanded", out var expanded) && expanded.GetBoolean();
                LayoutMarkdownAi();
                return;
            case "settings":
                ToggleSettings(this, new RoutedEventArgs());
                return;
            case "close":
                _aiOpen = false;
                LayoutMarkdownAi();
                await RenderMarkdownAiAsync();
                return;
            case "cancel":
                CancelMarkdownAi();
                return;
            case "models":
                await RefreshMarkdownAiKeyAsync();
                return;
            case "full-document":
                _aiSelection = null;
                await BrowserServices.CallAsync("document.selection.clear");
                await RenderMarkdownAiAsync();
                return;
            case "prompt":
                _aiPrompt = request.GetProperty("prompt").GetString() ?? "";
                if (_aiPrompt.Length > MarkdownAi.PromptCharacters)
                {
                    _aiPrompt = _aiPrompt[..MarkdownAi.PromptCharacters];
                }
                await RenderMarkdownAiAsync();
                return;
            case "choice":
                _aiModel = request.GetProperty("model").GetString() ?? "default";
                if (_aiModel != "default" && !_aiModels.Any(model => model.Id == _aiModel))
                {
                    _aiModel = "default";
                }
                _aiEffort = request.GetProperty("effort").GetString() ?? "medium";
                if (!MarkdownAi.Efforts.Contains(_aiEffort, StringComparer.Ordinal))
                {
                    _aiEffort = "medium";
                }
                await RenderMarkdownAiAsync();
                return;
            case "effort":
                var effort = request.GetProperty("effort").GetString();
                if (effort != null && MarkdownAi.Efforts.Contains(effort, StringComparer.Ordinal))
                {
                    // Effort does not change the model catalog or current input estimate.
                    // The child updates its caption immediately; avoid republishing the whole panel.
                    _aiEffort = effort;
                }
                return;
        }
        if (_document == null || _active == null || _documentBusy
            || request.GetProperty("fingerprint").GetString() != _document.Fingerprint
            || request.GetProperty("path").GetString() != _document.Path
            || request.GetProperty("workspaceId").GetString() != _active.Provider.Info.Id)
        {
            return;
        }
        if (action == "submit")
        {
            await SubmitMarkdownAiAsync(request);
        }
        else if (action is "undo" or "redo" or "restore")
        {
            await MoveMarkdownHistoryAsync(action);
        }
        else if (action == "save")
        {
            var document = _document;
            var filename = Regex.Replace(document.Path.Split('/').Last(), "[<>:\"/\\\\|?*\\x00-\\x1f]", "_");
            var stem = filename.Contains('.') ? filename[..filename.LastIndexOf('.')] : filename;
            filename = stem[..Math.Min(stem.Length, 120)] + ".edited.md";
            var saved = await BrowserServices.CallAsync<JsonElement>("document.saveCopy", new { document.Source, filename, workspaceId = _active.Provider.Info.Id, path = document.Path });
            if (saved.GetProperty("saved").GetBoolean())
            {
                var edits = CurrentMarkdownEdits();
                if (edits != null && _document?.Fingerprint == document.Fingerprint)
                {
                    edits.Exported = document.Source;
                }
                _aiMessage = "Markdown copy saved.";
            }
            else
            {
                _aiMessage = saved.TryGetProperty("cancelled", out var cancelled) && cancelled.GetBoolean()
                    ? "Markdown save cancelled." : "Markdown download started. Keep the copy before closing.";
            }
            await RenderMarkdownAiAsync();
        }
    }

    /// <summary>Sends one prompt and current document/selection, then replaces only the captured unchanged source range.</summary>
    private async Task SubmitMarkdownAiAsync(JsonElement eventData)
    {
        if (_aiBusy || !_aiConfigured || !_aiModelsReady || _document == null || _active == null)
        {
            return;
        }
        if (_aiSelection?.Unsupported == true)
        {
            _aiMessage = "Select this text in Source view so its Markdown range can be replaced precisely.";
            await RenderMarkdownAiAsync();
            return;
        }
        var document = _document;
        var context = _active;
        var selection = _aiSelection;
        if (selection != null && selection.Fingerprint != document.Fingerprint)
        {
            throw new WorkspaceException("markdown-selection", "Select the current Markdown again before editing it.");
        }
        var start = selection?.Start ?? 0;
        var end = selection?.End ?? document.Source.Length;
        var modelChoice = eventData.GetProperty("model").GetString() ?? "default";
        var model = modelChoice == "default" ? _aiModels.FirstOrDefault()?.Id ?? "" : modelChoice;
        if (!_aiModels.Any(available => available.Id == model))
        {
            throw new WorkspaceException("openai-choice", "Choose an available model and reasoning level.");
        }
        var request = MarkdownAi.Validate(new(model, eventData.GetProperty("effort").GetString() ?? "medium",
            eventData.GetProperty("prompt").GetString() ?? "", document.Source[start..end], selection == null ? "document" : "selection"));
        if (_markdownEdits.Count >= 16 && CurrentMarkdownEdits() == null)
        {
            MakeRoomForMarkdownDraft();
        }
        _aiPrompt = request.Prompt;
        _aiModel = modelChoice;
        _aiEffort = request.Effort;
        _aiBusy = true;
        _aiWaiting = true;
        _aiMessage = "Checking the request context…";
        _aiUsage = MarkdownAi.Estimate(request.Document, request.Prompt);
        _aiResult = null;
        _aiResultPreview = null;
        _aiStartedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(MarkdownAi.RequestTimeoutSeconds));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_navigation.Token, deadline.Token);
        _aiRequest = cancellation;
        try
        {
            await RenderMarkdownAiAsync();
            var prefixTokens = await BrowserServices.CallAsync<int[]>("openai.prepare",
                new { request, counts = MarkdownAi.CountPayloads(request) }, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            _aiUsage = MarkdownAi.PreparedUsage(request, prefixTokens);
            _aiPreparedRequest = request;
            _aiPreparedPrefixes = prefixTokens;
            MarkdownAi.EnsureFits(request, prefixTokens);
            _aiMessage = "Sending your request and waiting for OpenAI…";
            await RenderMarkdownAiAsync();
            var exchange = await BrowserServices.CallAsync<MarkdownAiExchange>("openai.edit",
                new { request, payload = MarkdownAi.Payload(request), prefixTokens }, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            var result = MarkdownAi.Read(exchange, request);
            _aiUsage = result.Usage;
            if (result.Usage.Reported)
            {
                _aiTotalTokens += result.Usage.Total;
                _aiRequestCount++;
            }
            _aiResult = result;
            if (result.Complete)
            {
                _aiResultPreview = _parser.Parse("response.md", result.Markdown);
            }
            _aiWaiting = false;
            await RenderMarkdownAiAsync();
            if (!ReferenceEquals(_active, context) || !ReferenceEquals(_document, document))
            {
                _aiMessage = "The document changed while OpenAI was working. Its response was not applied.";
                return;
            }
            if (result.Complete && result.Kind == "edit")
            {
                var source = document.Source[..start] + result.Markdown + document.Source[end..];
                var revised = _parser.Parse(document.Path, source);
                await CommitMarkdownEditAsync(revised, context, document, remember: true, cancellationToken: cancellation.Token);
                _aiMessage = selection == null ? "Document replaced · Markdown changes are unsaved" : "Selection replaced · Markdown changes are unsaved";
                _aiSelection = null;
                _aiPrompt = "";
                _aiPromptClear++;
                await BrowserServices.CallAsync("document.selection.clear");
            }
            else
            {
                _aiMessage = result.Summary;
            }
        }
        catch (OperationCanceledException)
        {
            _aiMessage = deadline.IsCancellationRequested
                || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _aiStartedAt >= MarkdownAi.RequestTimeoutSeconds * 1000L
                ? "OpenAI did not finish within one minute. The request was cancelled and your Markdown was not changed."
                : "AI request cancelled.";
        }
        catch (WorkspaceException error)
        {
            _aiMessage = error.Message;
        }
        catch (Exception)
        {
            _aiMessage = "The Markdown edit could not be completed. Your current document is preserved.";
        }
        finally
        {
            if (ReferenceEquals(_aiRequest, cancellation))
            {
                _aiRequest = null;
                _aiBusy = false;
                _aiWaiting = false;
                await RenderMarkdownAiAsync();
            }
        }
    }

    /// <summary>Commits a parsed revision while freezing renderer input and preserving original files.</summary>
    private async Task CommitMarkdownEditAsync(MarkdownModel revised, Context context, MarkdownModel expected, bool remember,
        CancellationToken cancellationToken = default)
    {
        AssertMarkdownEditIdle();
        if (_exportBusy)
        {
            throw new WorkspaceException("markdown-export", "Wait for the document export to finish before replacing Markdown.");
        }
        _markdownEditBusy = true;
        UpdateExportButtons();
        var inputSuspended = false;
        try
        {
            await BrowserServices.CallAsync("document.edit.suspend");
            inputSuspended = true;
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_active, context) || !ReferenceEquals(_document, expected))
            {
                throw new WorkspaceException("markdown-revision", "The document changed while OpenAI was working. Its response was not applied.");
            }
            var position = await ReaderHost.CaptureAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var key = context.Provider.Info.Id + "\n" + expected.Path;
            if (!_markdownEdits.TryGetValue(key, out var edits) || !ReferenceEquals(edits.Context, context))
            {
                edits = new(context, expected);
                _markdownEdits[key] = edits;
            }
            if (remember)
            {
                MarkdownEdits.Remember(edits.Undo, expected.Source);
                edits.Redo.Clear();
            }
            edits.Source = revised.Source;
            _indexing.Cancel();
            context.Catalog.UpdateDraft(revised);
            _document = revised;
            UpdateDocumentTitle(revised.Path, edits.Draft);
            RefreshOutline(revised);
            RefreshSidebar();
            try
            {
                await ReaderHost.DisplayAsync(revised, context.Provider.Info.Id, position, "", _navigation.Token);
                if (_view is "source" or "split")
                {
                    await ShowSourceAsync();
                }
                if (_view == "map")
                {
                    await ShowMapAsync();
                }
                SetStatus(edits.Draft ? "Markdown changed · save a copy or undo" : "Original Markdown restored");
            }
            catch (Exception)
            {
                // The managed edit and undo are already committed. A display failure must not claim data was rolled back.
                SetStatus("Markdown changed. Choose the view again to refresh its display.");
            }
        }
        finally
        {
            try
            {
                if (inputSuspended)
                {
                    await BrowserServices.CallAsync("document.edit.resume");
                }
            }
            finally
            {
                _markdownEditBusy = false;
                UpdateExportButtons();
            }
        }
    }

    /// <summary>Rejects navigation or competing edits during the brief managed revision commit.</summary>
    private void AssertMarkdownEditIdle()
    {
        if (_markdownEditBusy)
        {
            throw new WorkspaceException("markdown-busy", "Wait for the Markdown replacement to finish.");
        }
    }

    /// <summary>Evicts only restored or explicitly saved sessions, returning their original paths to their disk-backed catalog.</summary>
    private void MakeRoomForMarkdownDraft()
    {
        foreach (var entry in _markdownEdits.ToArray())
        {
            if (entry.Value.NeedsSave || ReferenceEquals(entry.Value, CurrentMarkdownEdits()))
            {
                continue;
            }
            if (entry.Value.Draft)
            {
                // A saved copy keeps the edit; the original file remains the source when its local session expires.
                entry.Value.Context.Catalog.UpdateDraft(_parser.Parse(entry.Value.Path, entry.Value.Original));
            }
            _markdownEdits.Remove(entry.Key);
            if (_markdownEdits.Count < 16)
            {
                return;
            }
        }
        throw new WorkspaceException("markdown-drafts", "Save or restore an edited document before editing another page.");
    }

    /// <summary>Restores local Markdown history without contacting OpenAI or adding context to the next request.</summary>
    private async Task MoveMarkdownHistoryAsync(string action)
    {
        AssertMarkdownEditIdle();
        CancelMarkdownAi();
        var edits = CurrentMarkdownEdits();
        if (edits == null || _document == null || _active == null)
        {
            return;
        }
        var history = action == "redo" ? edits.Redo : edits.Undo;
        if (action != "restore" && history.Count == 0)
        {
            return;
        }
        var source = action == "restore" ? edits.Original : history[^1];
        var revised = _parser.Parse(_document.Path, source);
        var previous = _document.Source;
        await CommitMarkdownEditAsync(revised, _active, _document, remember: false);
        if (action == "restore")
        {
            edits.Undo.Clear();
            edits.Redo.Clear();
        }
        else
        {
            history.RemoveAt(history.Count - 1);
            MarkdownEdits.Remember(action == "redo" ? edits.Undo : edits.Redo, previous);
        }
        _aiSelection = null;
        await BrowserServices.CallAsync("document.selection.clear");
        _aiMessage = "Local Markdown history updated.";
        await RenderMarkdownAiAsync();
    }

    /// <summary>Restores a retained local document draft after catalog cache eviction.</summary>
    private MarkdownModel MarkdownDraftForNavigation(Context context, MarkdownModel model)
    {
        if (_markdownEdits.TryGetValue(context.Provider.Info.Id + "\n" + model.Path, out var edits)
            && ReferenceEquals(edits.Context, context))
        {
            return _parser.Parse(model.Path, edits.Source);
        }
        return model;
    }

    /// <summary>Opens or collapses the reusable AI surface without dropping its local document drafts.</summary>
    private async void ToggleMarkdownAi(object sender, RoutedEventArgs arguments)
    {
        await ExecuteAsync(async () =>
        {
            if (_aiOpen)
            {
                var prompt = AiHost.PromptText;
                _aiPrompt = prompt[..Math.Min(prompt.Length, MarkdownAi.PromptCharacters)];
            }
            _aiOpen = !_aiOpen;
            LayoutMarkdownAi();
            await RenderMarkdownAiAsync();
        });
    }
}
