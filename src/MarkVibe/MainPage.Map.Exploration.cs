/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; guarded exploration commands and atomic AI reference batches; 2026-10-16
 * Modified with AI: OpenAI Codex; separate transient actions and validate one current AI reference snapshot; 2026-10-16
 * Human guidance: Umberto Giacobbi; useful map selection, comparison and navigation effects
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe;

/// <summary>Routes temporary map exploration independently of persisted layout edits and autosave.</summary>
public partial class MainPage
{
    /// <summary>Rejects stale renderer requests, serializes selection changes and keeps navigation outside the map gate.</summary>
    private async Task HandleMapExplorationAsync(JsonElement request)
    {
        var context = _active;
        if (context == null || !IsCurrentExplorationRequest(context, request))
        {
            return;
        }

        var action = request.GetProperty("action").GetString();
        var workspaceId = context.Provider.Info.Id;

        // These operations can acquire the map gate themselves; never await them while holding it.
        if (action == "center")
        {
            await CenterMapDocumentAsync(workspaceId, request.GetProperty("path").GetString() ?? "");
            return;
        }

        if (action == "add-ai")
        {
            await AddAiContextBatchAsync(context, context.Map.Exploration.PickedPaths);
            return;
        }

        await _mapGate.WaitAsync();
        try
        {
            if (!IsCurrentExplorationRequest(context, request))
            {
                return;
            }

            var exploration = context.Map.Exploration;
            exploration.Refresh(context.Catalog);
            ApplyMapExplorationAction(exploration, request, action);

            if (action == "attention")
            {
                await MapHost.DisplaySelectionAsync(
                    BuildAtlasMap(context, context.Map.SelectedPath),
                    context.Map.SelectedPath,
                    workspaceId,
                    MapPresentation(context.Map),
                    context.Map.Session.Value.Atlas?.FocusDepth > 0);
            }
            else
            {
                await RenderMapStateAsync(context);
            }
        }
        finally
        {
            _mapGate.Release();
        }
    }

    /// <summary>Applies one synchronous transient action after the caller validates and locks its workspace session.</summary>
    private static void ApplyMapExplorationAction(
        MapExploration exploration,
        JsonElement request,
        string? action)
    {
        switch (action)
        {
            case "toggle":
                exploration.Toggle(request.GetProperty("path").GetString() ?? "");
                break;
            case "pick":
                var paths = request.GetProperty("paths").EnumerateArray()
                    .Take(Limits.MapNodes + 1)
                    .Select(path => path.GetString() ?? "")
                    .ToArray();
                var isAdditive = request.TryGetProperty("additive", out var additive) && additive.GetBoolean();
                var pickedPaths = isAdditive ? exploration.PickedPaths.Concat(paths) : paths;
                exploration.SetPicked(pickedPaths);
                break;
            case "clear":
                exploration.SetPicked([]);
                break;
            case "lens":
                exploration.LensEnabled = !exploration.LensEnabled;
                break;
            case "selection":
                exploration.SelectionEnabled = !exploration.SelectionEnabled;
                break;
            case "compare":
                exploration.CompareOpen = exploration.PickedPaths.Count >= 2;
                break;
            case "close-compare":
                exploration.CompareOpen = false;
                break;
            case "attention":
                exploration.SetAttentionMode(request.GetProperty("value").GetString() ?? "all");
                break;
            default:
                throw new WorkspaceException("map-exploration", "This map action is unavailable.");
        }
    }

    /// <summary>Requires the active map and the exact workspace/layout session that emitted the command.</summary>
    private bool IsCurrentExplorationRequest(Context? context, JsonElement request)
    {
        return context != null
            && _view == "map"
            && ReferenceEquals(context, _active)
            && request.TryGetProperty("workspaceId", out var workspace)
            && workspace.GetString() == context.Provider.Info.Id
            && request.TryGetProperty("stateId", out var state)
            && state.GetString() == context.Map.StateId;
    }

    /// <summary>Reads and validates an entire selected batch before committing any AI collection membership.</summary>
    private async Task AddAiContextBatchAsync(Context context, IReadOnlyList<string> paths)
    {
        if (!ReferenceEquals(context, _active)
            || paths.Count == 0
            || _aiBusy
            || _aiContextBusy
            || _documentBusy
            || _workspaceTransitionBusy
            || _markdownEditBusy)
        {
            return;
        }

        var candidate = new AiDocumentCollection();
        foreach (var path in context.AiContext.Paths.Concat(paths))
        {
            candidate.Add(path);
        }

        var candidatePaths = candidate.Paths;
        var cancellation = _navigation.Token;
        _aiContextBusy = true;
        try
        {
            await RenderMarkdownAiAsync();
            await RenderMapStateAsync(context);
            foreach (var path in candidatePaths)
            {
                await context.Catalog.OpenAsync(context.Provider, path, cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (!ReferenceEquals(context, _active) || _workspaceTransitionBusy)
                {
                    return;
                }
            }

            // Validate one current metadata snapshot after the reads. Earlier documents
            // may have changed during a later await; per-read totals would mix revisions.
            ValidateAiContextBatch(context.Catalog, candidatePaths);

            // All awaits and limits precede membership changes; failed reads leave the collection intact.
            foreach (var path in candidatePaths)
            {
                context.AiContext.Add(path);
            }
            _layout = _layout with { AiOpen = true };
            await SaveLayoutAsync();
        }
        finally
        {
            _aiContextBusy = false;
            await RenderMarkdownAiAsync();
            if (ReferenceEquals(context, _active) && _view == "map")
            {
                await RenderMapStateAsync(context);
            }
        }
    }

    /// <summary>Checks readable membership and the complete batch byte limit against one current catalog snapshot.</summary>
    private static void ValidateAiContextBatch(DocumentCatalog catalog, IReadOnlyList<string> paths)
    {
        var cardsByPath = catalog.Cards.ToDictionary(card => card.Path, StringComparer.Ordinal);
        long sourceBytes = 0;
        foreach (var path in paths)
        {
            if (!cardsByPath.TryGetValue(path, out var card) || card.Statistics == null)
            {
                throw new WorkspaceException(
                    "missing-document",
                    "This page is not in the opened workspace. Add the file or reopen its folder.");
            }

            sourceBytes += card.Statistics.SourceBytes;
            if (sourceBytes > MarkdownAi.ReferenceBytes)
            {
                throw new WorkspaceException(
                    "ai-context-size",
                    "The AI collection exceeds 4 MiB. Remove a document before adding another.");
            }
        }
    }
}
