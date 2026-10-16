/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; deterministic document map arrangements; 2026-10-16
 * Human guidance: Umberto Giacobbi; center connected documents and offer star, circle and grid layouts
 * Modified with AI: OpenAI Codex; dimension-aware card spacing and centered layout cells; 2026-10-16
 * Human guidance: Umberto Giacobbi; retain resized cards when arranging the document map
 * Modified with AI: OpenAI Codex; arrange only current catalog membership; 2026-10-16
 * Human guidance: Umberto Giacobbi; remove unreleased legacy map compatibility
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Creates stable map positions from the complete catalog while preserving current card styles and sizes.</summary>
internal static class MapArrangement
{
    /// <summary>Positions the most-connected document centrally and returns a complete, uncommitted layout.</summary>
    public static MapLayout Create(MapLayout current, IReadOnlyList<DocumentCard> cards, string preset)
    {
        if (preset is not ("star" or "circle" or "grid" or "tree" or "folders"))
        {
            throw new WorkspaceException("invalid-map-arrangement", "Choose a star, circle, grid, tree or folder arrangement.");
        }

        var graph = new DocumentGraph(cards);
        var orderedPaths = graph.OrderedPaths;
        var maximumWidth = (double)MapLayoutCodec.CardWidth;
        var maximumHeight = (double)MapLayoutCodec.CardHeight;
        foreach (var path in orderedPaths)
        {
            if (current.Nodes.TryGetValue(path, out var existing))
            {
                maximumWidth = Math.Max(maximumWidth, existing.Width ?? MapLayoutCodec.CardWidth);
                maximumHeight = Math.Max(maximumHeight, existing.Height ?? MapLayoutCodec.CardHeight);
            }
        }

        // Shared padded cells preserve the non-overlap guarantee when individual cards have different sizes.
        var columnStep = maximumWidth + 28;
        var rowStep = maximumHeight + 28;
        var cardSeparation = Math.Sqrt(columnStep * columnStep + rowStep * rowStep);
        var maximumRadius = MapLayoutCodec.MaximumCoordinate - Math.Max(maximumWidth, maximumHeight);
        var positions = preset switch
        {
            "grid" => GridPositions(orderedPaths.Count, columnStep, rowStep),
            "tree" => TreePositions(graph, columnStep, rowStep),
            "folders" => FolderPositions(orderedPaths, columnStep, rowStep),
            _ => RadialPositions(orderedPaths.Count, preset, cardSeparation, maximumRadius)
        };
        var nodes = new Dictionary<string, MapCardLayout>(StringComparer.Ordinal);
        for (var index = 0; index < orderedPaths.Count; index++)
        {
            var path = orderedPaths[index];
            if (!current.Nodes.TryGetValue(path, out var card))
            {
                if (nodes.Count >= MapLayoutCodec.MaximumNodes)
                {
                    throw new WorkspaceException("map-full", "This map has reached its saved-card limit.");
                }

                card = new(0, 0, MapLayoutCodec.DefaultColor, MapLayoutCodec.DefaultIcon);
            }

            var position = positions[index];
            nodes[path] = card with
            {
                X = Math.Round(position.X + (maximumWidth - (card.Width ?? MapLayoutCodec.CardWidth)) / 2, 3),
                Y = Math.Round(position.Y + (maximumHeight - (card.Height ?? MapLayoutCodec.CardHeight)) / 2, 3)
            };
        }

        return current with { Nodes = nodes };
    }

    /// <summary>Packs immediate-parent folder grids into bounded shelves, reserving space for each folder label.</summary>
    private static IReadOnlyList<(double X, double Y)> FolderPositions(IReadOnlyList<string> paths, double columnStep, double rowStep)
    {
        var groups = paths.GroupBy(path => path.Contains('/') ? path[..path.LastIndexOf('/')] : "", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var members = group.ToArray();
                var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(members.Length * rowStep / columnStep)));
                var rows = (int)Math.Ceiling((double)members.Length / columns);
                return (Members: members, Columns: columns, Width: columns * columnStep + 48, Height: rows * rowStep + 76);
            })
            .ToArray();
        if (groups.Length == 0)
        {
            return [];
        }

        const double groupGap = 80;
        var shelfWidth = Math.Max(groups.Max(group => group.Width),
            Math.Sqrt(groups.Sum(group => (group.Width + groupGap) * (group.Height + groupGap))));
        var positions = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        var cursorX = 0d;
        var cursorY = 0d;
        var shelfHeight = 0d;
        var totalWidth = 0d;
        foreach (var group in groups)
        {
            if (cursorX > 0 && cursorX + group.Width > shelfWidth)
            {
                cursorX = 0;
                cursorY += shelfHeight + groupGap;
                shelfHeight = 0;
            }

            for (var index = 0; index < group.Members.Length; index++)
            {
                positions[group.Members[index]] = (cursorX + 24 + index % group.Columns * columnStep,
                    cursorY + 50 + index / group.Columns * rowStep);
            }

            totalWidth = Math.Max(totalWidth, cursorX + group.Width);
            shelfHeight = Math.Max(shelfHeight, group.Height);
            cursorX += group.Width + groupGap;
        }

        var totalHeight = cursorY + shelfHeight;
        return paths.Select(path => (positions[path].X - totalWidth / 2, positions[path].Y - totalHeight / 2)).ToArray();
    }

    /// <summary>Ranks unique document connections, then visits the hub's nearest neighbors before disconnected components.</summary>
    internal static IReadOnlyList<string> OrderDocuments(IReadOnlyList<DocumentCard> cards)
    {
        return new DocumentGraph(cards).OrderedPaths;
    }

    /// <summary>Places a cycle-safe spanning forest in centered levels, wrapping broad levels without overlapping cards.</summary>
    private static IReadOnlyList<(double X, double Y)> TreePositions(DocumentGraph graph, double columnStep, double rowStep)
    {
        var positions = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        var columns = Math.Clamp((int)Math.Ceiling(Math.Sqrt(graph.OrderedPaths.Count) * 1.5), 6, 64);
        var cursorY = 0d;
        foreach (var level in graph.Levels)
        {
            for (var offset = 0; offset < level.Count; offset += columns)
            {
                var rowCount = Math.Min(columns, level.Count - offset);
                for (var index = 0; index < rowCount; index++)
                {
                    positions[level[offset + index]] = ((index - (rowCount - 1) / 2d) * columnStep, cursorY);
                }
                cursorY += rowStep;
            }
            cursorY += 64;
        }
        var height = Math.Max(0, cursorY - rowStep - 64);
        if (height / 2 + rowStep > MapLayoutCodec.MaximumCoordinate)
        {
            // Pathological chains can exceed the persisted world; retain breadth-first order in bounded grid cells.
            return GridPositions(graph.OrderedPaths.Count, columnStep, rowStep);
        }
        return graph.OrderedPaths.Select(path => (positions[path].X, positions[path].Y - height / 2)).ToArray();
    }

    /// <summary>Places a hub inside bounded concentric circles or radial spokes spaced for the largest card.</summary>
    private static IReadOnlyList<(double X, double Y)> RadialPositions(int count, string preset, double cardSeparation, double maximumRadius)
    {
        var positions = new List<(double X, double Y)>(count);
        if (count == 0)
        {
            return positions;
        }

        positions.Add((0, 0));
        var surroundingCount = count - 1;
        if (surroundingCount == 0)
        {
            return positions;
        }

        // Large circles use additional rings instead of expanding beyond the persisted coordinate bounds.
        var circle = preset == "circle";
        var spokeCount = Math.Min(circle ? 256 : 8, surroundingCount);
        var firstRadius = MinimumRingRadius(spokeCount, cardSeparation);
        var outerRingIndex = (surroundingCount - 1) / spokeCount;
        while (firstRadius + outerRingIndex * cardSeparation > maximumRadius)
        {
            // Preserve eight spokes for ordinary maps; denser spokes shorten large resized-card layouts.
            var nextSpokeCount = Math.Min(surroundingCount, spokeCount * 2);
            if (nextSpokeCount == spokeCount)
            {
                throw new WorkspaceException("map-full", "This map is too large for the selected arrangement.");
            }

            spokeCount = nextSpokeCount;
            firstRadius = MinimumRingRadius(spokeCount, cardSeparation);
            outerRingIndex = (surroundingCount - 1) / spokeCount;
        }

        // Both adjacent chords and radial gaps exceed the padded diagonal, including across partial rings.
        for (var index = 0; index < surroundingCount; index++)
        {
            var spokeIndex = index % spokeCount;
            var ringIndex = index / spokeCount;
            var ringSlots = circle ? Math.Min(spokeCount, surroundingCount - ringIndex * spokeCount) : spokeCount;
            var angle = -Math.PI / 2 + 2 * Math.PI * spokeIndex / ringSlots;
            var radius = firstRadius + ringIndex * cardSeparation;
            positions.Add((Math.Round(Math.Cos(angle) * radius, 3), Math.Round(Math.Sin(angle) * radius, 3)));
        }

        return positions;
    }

    /// <summary>Chooses a radius whose adjacent card-center chords preserve the required separation.</summary>
    private static double MinimumRingRadius(int slots, double cardSeparation)
    {
        if (slots == 1)
        {
            return cardSeparation;
        }

        return Math.Max(cardSeparation, cardSeparation / (2 * Math.Sin(Math.PI / slots)));
    }

    /// <summary>Fills a compact grid outward from a real center cell, keeping the strongest connections near the hub.</summary>
    private static IReadOnlyList<(double X, double Y)> GridPositions(int count, double columnStep, double rowStep)
    {
        var columnCount = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
        if (columnCount % 2 == 0)
        {
            columnCount++;
        }

        var rowCount = Math.Max(1, (int)Math.Ceiling((double)count / columnCount));
        if (rowCount % 2 == 0)
        {
            rowCount++;
        }

        var positions = new List<(double X, double Y)>(columnCount * rowCount);
        for (var row = -rowCount / 2; row <= rowCount / 2; row++)
        {
            for (var column = -columnCount / 2; column <= columnCount / 2; column++)
            {
                positions.Add((column * columnStep, row * rowStep));
            }
        }

        return positions
            .OrderBy(position => position.X * position.X + position.Y * position.Y)
            .ThenBy(position => position.Y)
            .ThenBy(position => position.X)
            .Take(count)
            .ToList();
    }
}
