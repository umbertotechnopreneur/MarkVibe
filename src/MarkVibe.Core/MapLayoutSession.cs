/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; readability revision; 2026-10-11
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Modified with AI: OpenAI Codex; atomic map arrangements and connected defaults; 2026-10-16
 * Human guidance: Umberto Giacobbi; star, circle and grid layouts around the most-connected document
 * Modified with AI: OpenAI Codex; validated persistent card dimensions and atomic map reset; 2026-10-16
 * Human guidance: Umberto Giacobbi; resize cards and restore map defaults without changing documents
 * Modified with AI: OpenAI Codex; current-only map schema and catalog-owned card membership; 2026-10-16
 * Human guidance: Umberto Giacobbi; simplify the unreleased model without backward compatibility
 * Modified with AI: OpenAI Codex; optional validated atlas settings and eight saved viewpoints; 2026-10-16
 * Human guidance: Umberto Giacobbi; persist map views without changing document positions or source
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Owns current map edits and automatic placement without reading or writing workspace files.</summary>
public sealed class MapLayoutSession
{
    private bool _automaticArrangement;
    public MapLayout Value { get; private set; }
    public bool Dirty { get; private set; }

    /// <summary>Commits validated presentation settings through the same revision-checked save flow as card edits.</summary>
    public void SetAtlas(MapAtlasSettings atlas)
    {
        var updated = Value with { Atlas = atlas };
        MapLayoutCodec.Write(updated);
        Value = updated;
        Dirty = true;
        _automaticArrangement = false;
    }

    /// <summary>Preserves a loaded layout; only an absent layout opts into the automatic initial tree.</summary>
    public MapLayoutSession(MapLayout? loaded = null)
    {
        Value = loaded ?? new MapLayout(
            MapLayoutCodec.SchemaVersion,
            new Dictionary<string, MapCardLayout>(StringComparer.Ordinal));
        MapLayoutCodec.Validate(Value);
        _automaticArrangement = loaded == null;
    }

    /// <summary>Reconciles the complete enumerated catalog, removing absent paths while preserving established cards.</summary>
    public void EnsureDocuments(IReadOnlyList<DocumentCard> cards)
    {
        // The argument contains every catalog path from Seed, including documents whose contents are not indexed yet.
        // A filtered graph projection must never be passed here: only a complete enumeration can establish absence.
        var currentPaths = cards.Select(card => card.Path).ToHashSet(StringComparer.Ordinal);
        var currentNodes = Value.Nodes
            .Where(pair => currentPaths.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var removedCards = currentNodes.Count != Value.Nodes.Count;
        var currentLayout = Value with { Nodes = currentNodes };
        if (_automaticArrangement)
        {
            // The catalog is seeded before indexing; recompute defaults as links become available until the first edit or save.
            var automatic = MapArrangement.Create(currentLayout, cards, "tree");
            MapLayoutCodec.Write(automatic);
            Value = automatic;
            Dirty |= removedCards;
            return;
        }

        var nodes = currentNodes;
        var columnCount = Math.Max(1, Math.Min(4, (int)Math.Ceiling(Math.Sqrt(cards.Count))));
        var gridSlot = 0;
        foreach (var card in cards)
        {
            if (nodes.ContainsKey(card.Path))
            {
                continue;
            }

            if (nodes.Count >= MapLayoutCodec.MaximumNodes)
            {
                throw new WorkspaceException("map-full", "This map has reached its saved-card limit.");
            }

            MapCardLayout candidate;
            do
            {
                candidate = new(
                    gridSlot % columnCount * MapLayoutCodec.ColumnStep,
                    gridSlot / columnCount * MapLayoutCodec.RowStep,
                    MapLayoutCodec.DefaultColor,
                    MapLayoutCodec.DefaultIcon
                );
                gridSlot++;
                if (candidate.Y > MapLayoutCodec.MaximumCoordinate)
                {
                    throw new WorkspaceException("map-full", "There is no free space for new cards in the default grid.");
                }
            }
            while (nodes.Values.Any(node => OverlapsWithSpacing(candidate, node)));
            nodes.Add(card.Path, candidate);
        }

        var reconciled = Value with
        {
            Nodes = nodes
        };
        // Publish membership changes only after the complete sidecar has passed its size and geometry checks.
        MapLayoutCodec.Write(reconciled);
        Value = reconciled;
        Dirty |= removedCards;
    }

    /// <summary>Commits one complete arrangement after validating its coordinates and persisted byte budget.</summary>
    public void Arrange(IReadOnlyList<DocumentCard> cards, string preset)
    {
        var arranged = MapArrangement.Create(Value, cards, preset);
        MapLayoutCodec.Write(arranged);
        var wasAutomatic = _automaticArrangement;
        _automaticArrangement = false;
        if (arranged.Nodes.Count == Value.Nodes.Count &&
            arranged.Nodes.All(pair => Value.Nodes.TryGetValue(pair.Key, out var previous) && previous == pair.Value))
        {
            // Explicitly accepting a generated layout must persist it even when no coordinate needs to change.
            Dirty = Dirty || wasAutomatic && cards.Count > 0;
            return;
        }

        // Publish every new position together while preserving the current cards' styles and dimensions.
        Value = arranged;
        Dirty = true;
    }

    /// <summary>Updates a card's coordinates through the validated layout change path.</summary>
    public void Move(string path, double x, double y)
    {
        var currentCard = Card(path);
        Update(path, currentCard with { X = x, Y = y });
    }

    /// <summary>Updates a card's color and icon through the validated layout change path.</summary>
    public void Style(string path, string color, string icon)
    {
        var currentCard = Card(path);
        Update(path, currentCard with { Color = color, Icon = icon });
    }

    /// <summary>Persists a bounded pair of card dimensions in the current layout.</summary>
    public void Resize(string path, double width, double height)
    {
        Update(path, Card(path) with { Width = width, Height = height });
    }

    /// <summary>Rebuilds the complete catalog's cards with default styles, dimensions and the initial tree arrangement.</summary>
    public void Reset(IReadOnlyList<DocumentCard> cards)
    {
        // Starting from an empty layout also removes orphaned paths; Markdown files are never modified.
        var atlas = Value.Atlas is { } settings
            ? settings with { FocusDepth = 0, FolderGroups = false, CollapsedFolders = [] }
            : null;
        var defaults = new MapLayout(MapLayoutCodec.SchemaVersion, new Dictionary<string, MapCardLayout>(StringComparer.Ordinal), atlas);
        var reset = MapArrangement.Create(defaults, cards, "tree");
        MapLayoutCodec.Write(reset);
        Value = reset;
        _automaticArrangement = false;
        Dirty = true;
    }

    /// <summary>Marks the current layout as saved after a completed persistence operation.</summary>
    public void Saved()
    {
        Dirty = false;
        _automaticArrangement = false;
    }
    /// <summary>Returns the current saved card layout or reports that the card is unavailable.</summary>
    private MapCardLayout Card(string path)
    {
        if (!Value.Nodes.TryGetValue(path, out var card))
        {
            throw new WorkspaceException("missing-map-card", "This card is no longer in the map.");
        }

        return card;
    }

    /// <summary>Keeps newly placed cards at least twelve map units away from existing card bounds.</summary>
    private static bool OverlapsWithSpacing(MapCardLayout candidate, MapCardLayout existing)
    {
        const double spacing = 12;
        var candidateRight = candidate.X + (candidate.Width ?? MapLayoutCodec.CardWidth) + spacing;
        var candidateBottom = candidate.Y + (candidate.Height ?? MapLayoutCodec.CardHeight) + spacing;
        var existingRight = existing.X + (existing.Width ?? MapLayoutCodec.CardWidth) + spacing;
        var existingBottom = existing.Y + (existing.Height ?? MapLayoutCodec.CardHeight) + spacing;

        return candidate.X < existingRight
            && candidateRight > existing.X
            && candidate.Y < existingBottom
            && candidateBottom > existing.Y;
    }

    /// <summary>Commits a validated changed card layout and marks the session dirty.</summary>
    private void Update(string path, MapCardLayout card)
    {
        MapLayoutCodec.ValidateCard(card);
        if (Card(path) == card)
        {
            _automaticArrangement = false;
            return;
        }

        var nodes = Value.Nodes.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        nodes[path] = card;
        var updated = Value with
        {
            Nodes = nodes
        };
        // Publish coordinates and dimensions together only after the whole sidecar fits its byte budget.
        MapLayoutCodec.Write(updated);
        Value = updated;
        _automaticArrangement = false;
        Dirty = true;
    }
}
