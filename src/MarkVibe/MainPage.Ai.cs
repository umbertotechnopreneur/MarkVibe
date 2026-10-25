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
 * Modified with AI: OpenAI Codex; persistent right-hand assistant dock and readable request lifecycle; 2026-10-16
 * Modified with AI: OpenAI Codex; capture the chosen AI provider and cancel superseded connection work; 2026-10-16
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

/// <summary>Owns assistant requests, captured editing ranges and catalog-backed Markdown revisions.</summary>
public partial class MainPage
{
    /// <summary>Indicates whether the selected provider is locally available and configured.</summary>
    private bool _aiConfigured;

    /// <summary>Retains a credential-free explanation of the selected connection's local status.</summary>
    private string _aiConnectionMessage = "";

    /// <summary>Identifies the provider that owns the current model catalog and last-response presentation.</summary>
    private string _aiCatalogProvider = "";

    /// <summary>Distinguishes the completed account model lookup from an empty loading catalog.</summary>
    private bool _aiModelsReady;

    /// <summary>Prevents competing AI requests until the owning request completes its cleanup.</summary>
    private bool _aiBusy;

    /// <summary>Keeps the wait overlay active until a response is available for presentation.</summary>
    private bool _aiWaiting;

    /// <summary>Blocks navigation and competing changes during an atomic managed revision commit.</summary>
    private bool _markdownEditBusy;

    /// <summary>Retains the chosen model identifier or the account-dependent default choice.</summary>
    private string _aiModel = "default";

    /// <summary>Retains the requested reasoning effort for subsequent stateless edits.</summary>
    private string _aiEffort = "medium";

    /// <summary>Retains the current bounded composer draft when the panel is hidden.</summary>
    private string _aiPrompt = "";

    /// <summary>Supplies the current request status or actionable error to the assistant view.</summary>
    private string _aiMessage = "";

    /// <summary>Contains supported text models that are available to the configured account.</summary>
    private MarkdownAiModel[] _aiModels = [];

    /// <summary>Retains estimated or provider-reported usage for the most recent request.</summary>
    private MarkdownAiUsage? _aiUsage;

    /// <summary>Retains the structured response without carrying it into the next request.</summary>
    private MarkdownAiResult? _aiResult;

    /// <summary>Contains the parsed preview of a completed response for display.</summary>
    private MarkdownModel? _aiResultPreview;

    /// <summary>Accumulates provider-reported token use for this application session.</summary>
    private long _aiTotalTokens;

    /// <summary>Records request start time in Unix milliseconds for timeout status and the wait overlay.</summary>
    private long _aiStartedAt;

    /// <summary>Counts responses whose token use was reported by the provider.</summary>
    private int _aiRequestCount;

    /// <summary>Advances when the view must replace its local prompt with the managed draft.</summary>
    private int _aiPromptClear;

    /// <summary>Rejects model lookups and edit responses superseded by a connection change.</summary>
    private int _aiConnectionGeneration;

    /// <summary>Cancels the obsolete provider's status and model lookups without ending the shell lifetime.</summary>
    private CancellationTokenSource? _aiConnectionLookup;

    /// <summary>Identifies the active request so only its completion clears shared busy state.</summary>
    private CancellationTokenSource? _aiRequest;

    /// <summary>Cancels key and model lookups when the shell lifetime ends.</summary>
    private readonly CancellationTokenSource _aiLifetime = new();

    /// <summary>Retains the document revision and source range selected before composer focus.</summary>
    private MarkdownSelection? _aiSelection;

    /// <summary>Retains a source range, including an explicit unsupported reader selection.</summary>
    private sealed record MarkdownSelection(string Fingerprint, int Start, int End, bool Unsupported);

    /// <summary>Reads the current document session from the catalog that owns its effective revision.</summary>
    private DocumentSession? CurrentDocumentSession()
    {
        if (_active == null || _document == null)
        {
            return null;
        }

        return _active.Catalog.TryGetSession(_document.Path);
    }

    /// <summary>Reports whether the current title should identify an in-memory Markdown revision.</summary>
    private bool IsMarkdownDraft() => CurrentDocumentSession()?.IsModified == true;

    /// <summary>Counts unsaved revisions across retained workspaces without duplicating document state in the shell.</summary>
    private int UnsavedDocumentCount()
    {
        return _contexts.Values.Sum(context =>
            context.Catalog.Sessions.Count(session => session.NeedsSave));
    }

    /// <summary>Stops the active request; only its own completion handler can clear the busy state.</summary>
    private void CancelMarkdownAi() => _aiRequest?.Cancel();

    /// <summary>Invalidates pending edits and catalog work before a provider or credential refresh.</summary>
    private int InvalidateMarkdownAiConnection()
    {
        CancelMarkdownAi();
        _aiConnectionLookup?.Cancel();
        return ++_aiConnectionGeneration;
    }

    /// <summary>Rejects late work after provider changes even if a transport completed concurrently with cancellation.</summary>
    private void EnsureMarkdownAiConnection(string provider, int generation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (provider != _preferences.AiProvider || generation != _aiConnectionGeneration)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    /// <summary>Loads only the selected provider's local status and supported catalog, preserving the API-key default.</summary>
    private async Task RefreshMarkdownAiKeyAsync()
    {
        var generation = InvalidateMarkdownAiConnection();
        var provider = _preferences.AiProvider;
        using var lookup = CancellationTokenSource.CreateLinkedTokenSource(_aiLifetime.Token);
        _aiConnectionLookup = lookup;

        if (_aiCatalogProvider != provider)
        {
            _aiModel = "default";
            _aiUsage = null;
            _aiResult = null;
            _aiResultPreview = null;
        }
        _aiCatalogProvider = provider;
        _aiConfigured = false;
        _aiModelsReady = false;
        _aiModels = [];
        _aiMessage = "";
        _aiConnectionMessage = "";

        try
        {
            await RenderMarkdownAiAsync();
            var status = await BrowserServices.CallAsync<AiConnectionStatus>(
                "ai.connection.status",
                new { provider },
                lookup.Token);

            EnsureMarkdownAiConnection(provider, generation, lookup.Token);

            _aiConfigured = status.Configured && status.Available;
            _aiConnectionMessage = status.Message;
            await RenderMarkdownAiAsync();

            if (!_aiConfigured)
            {
                _aiMessage = status.Message;
                return;
            }
            var availableModelIds = await BrowserServices.CallAsync<string[]>(
                "ai.models",
                new { provider },
                cancellationToken: lookup.Token);
            EnsureMarkdownAiConnection(provider, generation, lookup.Token);

            _aiModels = MarkdownAi.Models
                .Where(model => availableModelIds.Contains(model.Id, StringComparer.Ordinal))
                .ToArray();
            _aiModelsReady = true;
            if (_aiModel != "default" && !_aiModels.Any(model => model.Id == _aiModel))
            {
                _aiModel = "default";
            }

            if (_aiModels.Length == 0)
            {
                _aiMessage = provider == AiProviders.CodexCli
                    ? "No supported Codex models are available. Check the connection in Settings."
                    : "No supported text models are available for this key. Check its model permissions in Settings.";
            }
            else if (provider == AiProviders.CodexCli)
            {
                _aiMessage = "Codex CLI · Estimated input tokens";
            }
        }
        catch (OperationCanceledException)
        {
            // A newer settings choice owns the next connection status and catalog.
        }
        catch (WorkspaceException error)
        {
            if (generation == _aiConnectionGeneration)
            {
                _aiMessage = error.Message;
                _aiConnectionMessage = error.Message;
            }
        }
        finally
        {
            if (ReferenceEquals(_aiConnectionLookup, lookup))
            {
                _aiConnectionLookup = null;
            }
            if (generation == _aiConnectionGeneration)
            {
                LayoutMarkdownAi();
                await RenderMarkdownAiAsync();
            }
        }
    }

    /// <summary>Docks the assistant beside the document, using the full workspace on narrow screens.</summary>
    private void LayoutMarkdownAi()
    {
        var availableWidth = Math.Max(
            0,
            ActualWidth - SidebarColumn.Width.Value - SidebarDividerColumn.Width.Value);
        var visible = _layout.AiOpen && !(_narrow && _sidebarOpen);
        var fullWidth = visible && availableWidth < 640;
        var resizable = visible && !fullWidth;

        // Responsive constraints affect the rendered width, never the persisted preference.
        AiColumn.MinWidth = resizable ? 300 : 0;
        AiColumn.MaxWidth = resizable ? Math.Min(600, availableWidth - 246) : double.PositiveInfinity;
        AiColumn.Width = fullWidth
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(visible ? Math.Min(_layout.AiWidth, AiColumn.MaxWidth) : 0);
        AiDividerColumn.Width = new GridLength(resizable ? 6 : 0);
        AiSplitter.Visibility = resizable ? Visibility.Visible : Visibility.Collapsed;
        AiHost.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        DocumentColumn.MinWidth = resizable ? 240 : 0;
        DocumentColumn.Width = new GridLength(fullWidth ? 0 : 1, GridUnitType.Star);
        var documentHidden = fullWidth || (_narrow && _sidebarOpen);
        DocumentPanel.Visibility = documentHidden ? Visibility.Collapsed : Visibility.Visible;
        SetViewTabState(AiButton, _layout.AiOpen);
    }

    /// <summary>Publishes reusable UI state and numeric counters; no API credential or prior prompts are included.</summary>
    private async Task RenderMarkdownAiAsync()
    {
        UpdateExportButtons();
        LayoutMarkdownAi();

        var selection = _aiSelection;
        var hasCurrentSelection = _document != null
            && selection != null
            && !selection.Unsupported
            && selection.Fingerprint == _document.Fingerprint;
        var target = hasCurrentSelection
            ? _document!.Source[selection!.Start..selection.End]
            : _document?.Source ?? "";
        var edits = CurrentDocumentSession();

        // Capacity needs a known fallback before the account catalog is ready; the UI keeps its loading state.
        var capacityModel = _aiModel == "default"
            ? _aiModels.FirstOrDefault()?.Id ?? MarkdownAi.DefaultModel
            : _aiModel;
        var references = PreviewAiContext();
        var nextUsage = EstimateAiContext(target, _aiPrompt, references);
        var contextUsage = _aiBusy ? _aiUsage ?? nextUsage : nextUsage;
        var waiting = _aiBusy && _aiWaiting;

        AiWaitHost.Visibility = waiting ? Visibility.Visible : Visibility.Collapsed;
        await AiWaitHost.DisplayAsync(new
        {
            waiting,
            ownerHostId = AiHost.HostId,
            workspaceId = _active?.Provider.Info.Id ?? "",
            fingerprint = _document?.Fingerprint ?? "",
            startedAt = _aiStartedAt,
            timeoutSeconds = MarkdownAi.RequestTimeoutSeconds,
            stage = _aiMessage
        });

        var documentAvailable = _document != null
            && !_documentBusy
            && !_markdownEditBusy
            && !_workspaceTransitionBusy
            && !_aiContextBusy
            && MessageOverlay.Visibility != Visibility.Visible;

        await AiHost.DisplayAsync(new
        {
            visible = _layout.AiOpen,
            configured = _aiConfigured,
            aiProvider = _preferences.AiProvider,
            connectionMessage = _aiConnectionMessage,
            modelsReady = _aiModelsReady,
            models = _aiModels,
            model = _aiModel,
            resolvedModel = _aiModel == "default" ? _aiModels.FirstOrDefault()?.Id ?? "" : _aiModel,
            capacity = MarkdownAi.Capacity(capacityModel, contextUsage),
            contextUsage,
            catalogDate = MarkdownAi.CatalogDate,
            effort = _aiEffort,
            busy = _aiBusy,
            contextBusy = _aiContextBusy,
            references,
            referenceTokens = references.Where(reference => !reference.IsTarget).Sum(reference => reference.Tokens ?? 0),
            contextIncomplete = references.Any(reference =>
                !reference.IsTarget && (reference.Missing || !reference.Tokens.HasValue)),
            message = _aiMessage,
            prompt = _aiPrompt,
            promptClear = _aiPromptClear,
            promptLimit = MarkdownAi.PromptCharacters,
            path = _document?.Path ?? "",
            fingerprint = _document?.Fingerprint ?? "",
            workspaceId = _active?.Provider.Info.Id ?? "",
            documentAvailable,
            scope = hasCurrentSelection ? "selection" : "document",
            selectionCharacters = hasCurrentSelection ? selection!.End - selection.Start : 0,
            unsupportedSelection = selection?.Unsupported == true,
            usage = _aiUsage ?? nextUsage,
            totalTokens = _aiTotalTokens,
            requestCount = _aiRequestCount,
            result = _aiResult,
            resultPreview = _aiResultPreview == null
                ? null
                : new { _aiResultPreview.Html, _aiResultPreview.Headings },
            canUndo = edits?.CanUndo == true,
            canRedo = edits?.CanRedo == true,
            draft = edits?.IsModified == true,
            unsavedDocuments = UnsavedDocumentCount()
        });
    }

    /// <summary>Retains the selected source range while focus moves to the composer; stale and invalid ranges are rejected.</summary>
    private async Task HandleMarkdownSelectionAsync(JsonElement request)
    {
        if (_document == null
            || _active == null
            || request.GetProperty("workspaceId").GetString() != _active.Provider.Info.Id
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
                _aiSelection = new MarkdownSelection(_document.Fingerprint, 0, 0, Unsupported: true);
            }
            else
            {
                var start = selection.GetProperty("start").GetInt32();
                var end = selection.GetProperty("end").GetInt32();
                if (start >= 0 && end > start && end <= _document.Source.Length)
                {
                    _aiSelection = new MarkdownSelection(_document.Fingerprint, start, end, Unsupported: false);
                }
                else
                {
                    _aiSelection = new MarkdownSelection(_document.Fingerprint, 0, 0, Unsupported: true);
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
            case "settings":
                ToggleSettings(this, new RoutedEventArgs());
                return;
            case "close":
                _readerPresentation.SetAiOpen(false);
                LayoutMarkdownAi();
                await RenderMarkdownAiAsync();
                await SaveLayoutAsync();
                return;
            case "cancel":
                CancelMarkdownAi();
                return;
            case "models":
                await RefreshMarkdownAiKeyAsync();
                return;
            case "context-add":
                await AddAiContextAsync(
                    request.GetProperty("workspaceId").GetString() ?? "",
                    request.GetProperty("path").GetString() ?? "");
                return;
            case "context-remove":
            case "context-clear":
                if (_active?.Provider.Info.Id == request.GetProperty("workspaceId").GetString())
                {
                    var referencePath = action == "context-clear"
                        ? null
                        : request.GetProperty("referencePath").GetString();
                    await RemoveAiContextAsync(referencePath);
                }
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

        // Actions that affect Markdown must still target the exact document shown by the initiating control.
        if (_document == null
            || _active == null
            || _documentBusy
            || _workspaceTransitionBusy
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
            var context = _active;
            var filename = Regex.Replace(document.Path.Split('/').Last(), "[<>:\"/\\\\|?*\\x00-\\x1f]", "_");
            var filenameStem = filename.Contains('.') ? filename[..filename.LastIndexOf('.')] : filename;
            filename = filenameStem[..Math.Min(filenameStem.Length, 120)] + ".edited.md";

            var saved = await BrowserServices.CallAsync<JsonElement>("document.saveCopy", new
            {
                document.Source,
                filename,
                workspaceId = context.Provider.Info.Id,
                path = document.Path
            });
            if (saved.GetProperty("saved").GetBoolean())
            {
                // The copy belongs to the captured session even if navigation completed during the native save dialog.
                context.Catalog.MarkExported(document.Path, document.Source);
                _aiMessage = "Markdown copy saved.";
            }
            else
            {
                var saveCancelled = saved.TryGetProperty("cancelled", out var cancelled) && cancelled.GetBoolean();
                _aiMessage = saveCancelled
                    ? "Markdown save cancelled."
                    : "Markdown download started. Keep the copy before closing.";
            }

            await RenderMarkdownAiAsync();
        }
    }

    /// <summary>Sends one prompt and current document/selection, then replaces only the captured unchanged source range.</summary>
    private async Task SubmitMarkdownAiAsync(JsonElement eventData)
    {
        if (_aiBusy
            || _aiContextBusy
            || !_aiConfigured
            || !_aiModelsReady
            || _document == null
            || _active == null)
        {
            return;
        }

        if (_aiSelection?.Unsupported == true)
        {
            _aiMessage = "Select this text in Source view so its Markdown range can be replaced precisely.";
            await RenderMarkdownAiAsync();
            return;
        }

        // Capture the revision and range once. A later navigation must never redirect this request's edit.
        var provider = _preferences.AiProvider;
        var connectionGeneration = _aiConnectionGeneration;
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
        var request = MarkdownAi.Validate(new(
            model,
            eventData.GetProperty("effort").GetString() ?? "medium",
            eventData.GetProperty("prompt").GetString() ?? "",
            document.Source[start..end],
            selection == null ? "document" : "selection"));
        context.Catalog.EnsureRevisionCapacity(document.Path);

        _aiPrompt = request.Prompt;
        _aiModel = modelChoice;
        _aiEffort = request.Effort;
        _aiBusy = true;
        _aiWaiting = true;
        _aiMessage = "Checking the request context…";
        _aiUsage = EstimateAiContext(request.Document, request.Prompt, PreviewAiContext());
        _aiResult = null;
        _aiResultPreview = null;
        _aiStartedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Navigation and the deadline share one cancellation source, owned by this invocation's finally block.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(MarkdownAi.RequestTimeoutSeconds));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_navigation.Token, deadline.Token);
        _aiRequest = cancellation;
        try
        {
            await RenderMarkdownAiAsync();

            // Read effective catalog revisions before token preflight so estimates and payload share the same sources.
            var references = await ResolveAiReferencesAsync(context, document.Path, cancellation.Token);
            EnsureMarkdownAiConnection(provider, connectionGeneration, cancellation.Token);
            if (!ReferenceEquals(_active, context) || !ReferenceEquals(_document, document))
            {
                throw new OperationCanceledException(cancellation.Token);
            }

            request = MarkdownAi.Validate(request with { References = references });
            _aiUsage = MarkdownAi.Estimate(request.Document, request.Prompt, references);
            await RenderMarkdownAiAsync();
            if (_view == "map")
            {
                await RenderMapStateAsync(context);
            }

            var prefixTokens = await BrowserServices.CallAsync<int[]>(
                "ai.prepare",
                new { provider, request, counts = MarkdownAi.CountPayloads(request) },
                cancellation.Token);
            EnsureMarkdownAiConnection(provider, connectionGeneration, cancellation.Token);
            _aiUsage = MarkdownAi.PreparedUsage(request, prefixTokens);
            MarkdownAi.EnsureFits(request, prefixTokens);
            _aiMessage = provider == AiProviders.CodexCli
                ? "Sending your request and waiting for Codex CLI…"
                : "Sending your request and waiting for OpenAI…";
            await RenderMarkdownAiAsync();

            var exchange = await BrowserServices.CallAsync<MarkdownAiExchange>(
                "ai.edit",
                new { provider, request, payload = MarkdownAi.Payload(request), prefixTokens },
                cancellation.Token);
            EnsureMarkdownAiConnection(provider, connectionGeneration, cancellation.Token);
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
            EnsureMarkdownAiConnection(provider, connectionGeneration, cancellation.Token);

            // Present the response even if its captured document is stale, but do not commit it to a different revision.
            if (!ReferenceEquals(_active, context) || !ReferenceEquals(_document, document))
            {
                _aiMessage = "The document changed while the AI was working. Its response was not applied.";
                return;
            }

            if (result.Complete && result.Kind == "edit")
            {
                var source = document.Source[..start] + result.Markdown + document.Source[end..];
                var revised = _parser.Parse(document.Path, source);
                await CommitMarkdownEditAsync(
                    revised,
                    context,
                    document,
                    DocumentRevisionAction.Edit,
                    cancellation.Token);
                _aiMessage = selection == null
                    ? "Document replaced · Markdown changes are unsaved"
                    : "Selection replaced · Markdown changes are unsaved";
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
            if (connectionGeneration == _aiConnectionGeneration)
            {
                var elapsedMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _aiStartedAt;
                var deadlineExpired = deadline.IsCancellationRequested
                    || elapsedMilliseconds >= MarkdownAi.RequestTimeoutSeconds * 1000L;
                _aiMessage = deadlineExpired
                    ? "The AI did not finish within one minute. The request was cancelled and your Markdown was not changed."
                    : "AI request cancelled.";
            }
        }
        catch (WorkspaceException error)
        {
            if (connectionGeneration == _aiConnectionGeneration)
            {
                _aiMessage = error.Message;
            }
        }
        catch (Exception)
        {
            if (connectionGeneration == _aiConnectionGeneration)
            {
                _aiMessage = "The Markdown edit could not be completed. Your current document is preserved.";
            }
        }
        finally
        {
            if (ReferenceEquals(_aiRequest, cancellation))
            {
                _aiRequest = null;
                _aiBusy = false;
                _aiWaiting = false;
                await RenderMarkdownAiAsync();
                if (_view == "map" && ReferenceEquals(context, _active))
                {
                    await RenderMapStateAsync(context);
                }
            }
        }
    }

    /// <summary>Commits a parsed revision while freezing renderer input and preserving original files.</summary>
    private async Task CommitMarkdownEditAsync(
        MarkdownModel revised,
        Context context,
        MarkdownModel expected,
        DocumentRevisionAction action,
        CancellationToken cancellationToken = default)
    {
        AssertMarkdownEditIdle();
        if (_workspaceTransitionBusy || _documentBusy)
        {
            throw new WorkspaceException("markdown-busy", "Wait for the document to finish opening before replacing Markdown.");
        }

        if (_exportBusy)
        {
            throw new WorkspaceException("markdown-export", "Wait for the document export to finish before replacing Markdown.");
        }

        _markdownEditBusy = true;
        UpdateExportButtons();
        var inputSuspended = false;
        try
        {
            await CloseDocumentFindAsync();
            await BrowserServices.CallAsync("document.edit.suspend");
            inputSuspended = true;
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_active, context) || !ReferenceEquals(_document, expected))
            {
                throw new WorkspaceException("markdown-revision", "The document changed while the AI was working. Its response was not applied.");
            }

            var position = await ReaderHost.CaptureAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_active, context) || !ReferenceEquals(_document, expected))
            {
                throw new WorkspaceException("markdown-revision", "The document changed before this revision could be applied.");
            }

            // Commit the catalog before refreshing views; renderer failures cannot undo the managed revision.
            _indexing.Cancel();
            var edits = context.Catalog.ApplyRevision(expected, revised, action);
            _document = revised;
            UpdateDocumentTitle(revised.Path, edits.IsModified);
            RefreshOutline(revised);
            RefreshSidebar();

            try
            {
                if (_view is "read" or "split")
                {
                    await ReaderHost.DisplayAsync(revised, context.Provider.Info.Id, position, "", _navigation.Token);
                }
                if (_view is "source" or "split")
                {
                    await ShowSourceAsync();
                }
                if (_view == "map")
                {
                    await ShowMapAsync();
                }
                SetStatus(edits.IsModified ? "Markdown changed · save a copy or undo" : "Original Markdown restored");
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
        if (_fileMutationBusy)
        {
            throw new WorkspaceException("file-busy", "Wait for the file operation to finish.");
        }

        if (_markdownEditBusy)
        {
            throw new WorkspaceException("markdown-busy", "Wait for the Markdown replacement to finish.");
        }
    }

    /// <summary>Restores local Markdown history without contacting OpenAI or adding context to the next request.</summary>
    private async Task MoveMarkdownHistoryAsync(string action)
    {
        AssertMarkdownEditIdle();
        CancelMarkdownAi();
        var edits = CurrentDocumentSession();
        if (edits == null || _document == null || _active == null)
        {
            return;
        }

        var revisionAction = action switch
        {
            "undo" => DocumentRevisionAction.Undo,
            "redo" => DocumentRevisionAction.Redo,
            "restore" => DocumentRevisionAction.Restore,
            _ => throw new WorkspaceException("markdown-action", "This document action is unavailable.")
        };
        var historyUnavailable = (revisionAction == DocumentRevisionAction.Undo && !edits.CanUndo)
            || (revisionAction == DocumentRevisionAction.Redo && !edits.CanRedo);
        if (historyUnavailable)
        {
            return;
        }

        var source = edits.PreviewRevision(revisionAction);
        var revised = _parser.Parse(_document.Path, source);
        await CommitMarkdownEditAsync(revised, _active, _document, revisionAction);
        _aiSelection = null;
        await BrowserServices.CallAsync("document.selection.clear");
        _aiMessage = "Local Markdown history updated.";
        await RenderMarkdownAiAsync();
    }

    /// <summary>Opens or collapses the reusable AI surface without dropping its local document drafts.</summary>
    private async void ToggleMarkdownAi(object sender, RoutedEventArgs arguments)
    {
        await ExecuteAsync(async () =>
        {
            if (_layout.AiOpen)
            {
                var prompt = AiHost.PromptText;
                _aiPrompt = prompt[..Math.Min(prompt.Length, MarkdownAi.PromptCharacters)];
            }

            _readerPresentation.SetAiOpen(!_layout.AiOpen);
            LayoutMarkdownAi();
            await RenderMarkdownAiAsync();
            await SaveLayoutAsync();
        });
    }
}
