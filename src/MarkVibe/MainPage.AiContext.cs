/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; workspace-scoped AI reference collection and visible input estimates; 2026-10-16
 * Modified with AI: OpenAI Codex; readable reference selection and request-boundary documentation; 2026-10-16
 * Human guidance: Umberto Giacobbi; collect documents and show their estimated token cost before sending
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using MarkVibe.Core;

namespace MarkVibe;

/// <summary>Coordinates workspace-owned AI references and their request-time source snapshots.</summary>
public partial class MainPage
{
    /// <summary>Prevents overlapping collection changes while a reference revision is being loaded.</summary>
    private bool _aiContextBusy;

    /// <summary>Contains UI metadata only; effective Markdown remains owned by the document catalog.</summary>
    private sealed record AiContextPreview(
        string Path,
        string Title,
        int? Tokens,
        int? SourceBytes,
        bool IsTarget,
        bool Missing);

    /// <summary>Returns the active workspace collection for map membership decoration.</summary>
    private string[] AiContextPaths() => _active?.AiContext.Paths.ToArray() ?? [];

    /// <summary>Toggles the selected map card's membership while checking the workspace that owns the originating control.</summary>
    private async Task ToggleAiContextAsync(string workspaceId, string path)
    {
        if (_active?.Provider.Info.Id != workspaceId)
        {
            return;
        }

        if (_active.AiContext.Paths.Contains(path, StringComparer.Ordinal))
        {
            await RemoveAiContextAsync(path);
        }
        else
        {
            await AddAiContextAsync(workspaceId, path);
        }
    }

    /// <summary>Projects collection rows from current catalog metadata without rereading files or retaining another source copy.</summary>
    private AiContextPreview[] PreviewAiContext()
    {
        if (_active == null)
        {
            return [];
        }

        var cardsByPath = _active.Catalog.Cards.ToDictionary(card => card.Path, StringComparer.Ordinal);
        return _active.AiContext.Paths.Select(path =>
        {
            cardsByPath.TryGetValue(path, out var card);
            return new AiContextPreview(
                path,
                card?.Title ?? path,
                card?.Statistics?.EstimatedTokens,
                card?.Statistics?.SourceBytes,
                IsTarget: path == _document?.Path,
                Missing: card == null);
        }).ToArray();
    }

    /// <summary>Combines the current editing target with the separately framed, deduplicated reference estimate.</summary>
    private static MarkdownAiUsage EstimateAiContext(
        string target,
        string prompt,
        IReadOnlyList<AiContextPreview> references)
    {
        var usage = MarkdownAi.Estimate(target, prompt);

        // The editing target is already counted above, even if it also belongs to the collection.
        var referenceTokens = references
            .Where(reference => !reference.IsTarget && reference.Tokens.HasValue)
            .Sum(reference => MarkdownAi.EstimateReferenceTokens(reference.Path, reference.Tokens!.Value));

        return usage with
        {
            Document = usage.Document + referenceTokens,
            Input = usage.Input + referenceTokens,
            Total = usage.Total + referenceTokens
        };
    }

    /// <summary>Adds a readable current revision without sending its contents; repeated additions keep one collection entry.</summary>
    private async Task AddAiContextAsync(string workspaceId, string path)
    {
        if (_active == null
            || _active.Provider.Info.Id != workspaceId
            || _aiBusy
            || _aiContextBusy
            || _documentBusy
            || _workspaceTransitionBusy
            || _markdownEditBusy)
        {
            return;
        }

        var context = _active;
        var normalizedPath = WorkspacePath.Normalize(path, imported: true);
        if (context.AiContext.Paths.Contains(normalizedPath, StringComparer.Ordinal))
        {
            _layout = _layout with { AiOpen = true };
            await RenderMarkdownAiAsync();
            await SaveLayoutAsync();
            return;
        }

        _aiContextBusy = true;
        try
        {
            await RenderMarkdownAiAsync();
            await context.Catalog.OpenAsync(context.Provider, normalizedPath, _navigation.Token);
            if (!ReferenceEquals(context, _active) || _workspaceTransitionBusy)
            {
                return;
            }

            // This synchronous projection sees any other completed catalog edit before committing the collection.
            var candidatePaths = context.AiContext.Paths.Append(normalizedPath).ToHashSet(StringComparer.Ordinal);
            var sourceBytes = context.Catalog.Cards
                .Where(card => candidatePaths.Contains(card.Path))
                .Sum(card => (long)(card.Statistics?.SourceBytes ?? 0));
            if (sourceBytes > MarkdownAi.ReferenceBytes)
            {
                throw new WorkspaceException(
                    "ai-context-size",
                    "The AI collection exceeds 4 MiB. Remove a document before adding another.");
            }

            context.AiContext.Add(normalizedPath);
            _layout = _layout with { AiOpen = true };
            await SaveLayoutAsync();
        }
        finally
        {
            _aiContextBusy = false;
            await RenderMarkdownAiAsync();
            if (_view == "map" && ReferenceEquals(context, _active))
            {
                await RenderMapStateAsync(context);
            }
        }
    }

    /// <summary>Removes a collection entry or clears the current workspace collection without changing any document.</summary>
    private async Task RemoveAiContextAsync(string? path)
    {
        if (_active == null || _aiBusy || _aiContextBusy || _workspaceTransitionBusy)
        {
            return;
        }

        var context = _active;
        if (path == null)
        {
            context.AiContext.Clear();
        }
        else
        {
            context.AiContext.Remove(path);
        }

        await RenderMarkdownAiAsync();
        if (_view == "map" && ReferenceEquals(context, _active))
        {
            await RenderMapStateAsync(context);
        }
    }

    /// <summary>Resolves the latest effective revisions immediately before preflight; a missing reference blocks sending.</summary>
    private static async Task<MarkdownAiReference[]> ResolveAiReferencesAsync(
        Context context,
        string targetPath,
        CancellationToken cancellationToken)
    {
        var references = new List<MarkdownAiReference>();
        var referencePaths = context.AiContext.Paths.Where(path => path != targetPath).ToArray();

        // Snapshot membership before awaiting file reads; the target is carried separately by the request.
        foreach (var path in referencePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = await context.Catalog.OpenAsync(context.Provider, path, cancellationToken);
            references.Add(new MarkdownAiReference(path, document.Source));
        }

        return references.ToArray();
    }
}
