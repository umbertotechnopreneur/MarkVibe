/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared connection ranking, tree levels and depth distances; 2026-10-16
 * Human guidance: Umberto Giacobbi; tree arrangements and depth around the selected document
 * Modified with AI: OpenAI Codex; stable shortest routes across complete document catalogs; 2026-10-16
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Indexes distinct in-workspace neighbors for stable arrangements and shortest connection distances.</summary>
internal sealed class DocumentGraph
{
    private readonly Dictionary<string, HashSet<string>> _neighbors;
    public IReadOnlyList<string> OrderedPaths { get; }
    public IReadOnlyList<List<string>> Levels { get; }

    /// <summary>Builds a breadth-first spanning forest, preferring the strongest document as each component's root.</summary>
    public DocumentGraph(IReadOnlyList<DocumentCard> cards)
    {
        _neighbors = cards.ToDictionary(card => card.Path, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var card in cards)
        {
            foreach (var link in card.Links)
            {
                // Anchors and reciprocal links share one neighbor; missing targets and self-links add no depth.
                if (link.Path != card.Path && _neighbors.TryGetValue(link.Path, out var targetNeighbors))
                {
                    _neighbors[card.Path].Add(link.Path);
                    targetNeighbors.Add(card.Path);
                }
            }
        }

        var ranked = cards.OrderByDescending(card => _neighbors[card.Path].Count)
            .ThenBy(card => HomePriority(card.Path))
            .ThenBy(card => card.Path.Count(character => character == '/'))
            .ThenBy(card => card.Path, StringComparer.Ordinal)
            .Select(card => card.Path).ToArray();
        var ranks = ranked.Select((path, index) => (Path: path, Rank: index))
            .ToDictionary(item => item.Path, item => item.Rank, StringComparer.Ordinal);
        var ordered = new List<string>(cards.Count);
        var levels = new List<List<string>>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<(string Path, int Depth)>();
        foreach (var root in ranked)
        {
            if (!visited.Add(root))
            {
                continue;
            }

            pending.Enqueue((root, 0));
            while (pending.TryDequeue(out var item))
            {
                ordered.Add(item.Path);
                if (levels.Count == item.Depth)
                {
                    levels.Add([]);
                }
                levels[item.Depth].Add(item.Path);
                foreach (var neighbor in _neighbors[item.Path].OrderBy(path => ranks[path]))
                {
                    if (visited.Add(neighbor))
                    {
                        pending.Enqueue((neighbor, item.Depth + 1));
                    }
                }
            }
        }
        OrderedPaths = ordered;
        Levels = levels;
    }

    /// <summary>Returns shortest undirected distances from the selected document; disconnected paths are absent.</summary>
    public IReadOnlyDictionary<string, int> DistancesFrom(string selected)
    {
        var distances = new Dictionary<string, int>(StringComparer.Ordinal);
        if (!_neighbors.ContainsKey(selected))
        {
            return distances;
        }

        var pending = new Queue<string>();
        distances[selected] = 0;
        pending.Enqueue(selected);
        while (pending.TryDequeue(out var path))
        {
            foreach (var neighbor in _neighbors[path])
            {
                if (distances.TryAdd(neighbor, distances[path] + 1))
                {
                    pending.Enqueue(neighbor);
                }
            }
        }
        return distances;
    }

    /// <summary>Finds a stable shortest undirected route across the complete catalog, retaining directed links separately.</summary>
    public IReadOnlyList<string> ShortestPath(string start, string end)
    {
        if (!_neighbors.ContainsKey(start) || !_neighbors.ContainsKey(end))
        {
            return [];
        }
        var previous = new Dictionary<string, string?>(StringComparer.Ordinal) { [start] = null };
        var pending = new Queue<string>();
        pending.Enqueue(start);
        while (pending.TryDequeue(out var path))
        {
            if (path == end)
            {
                var route = new List<string>();
                for (string? current = end; current != null; current = previous[current])
                {
                    route.Add(current);
                }
                route.Reverse();
                return route;
            }
            foreach (var neighbor in _neighbors[path].OrderBy(value => value, StringComparer.Ordinal))
            {
                if (previous.TryAdd(neighbor, path))
                {
                    pending.Enqueue(neighbor);
                }
            }
        }
        return [];
    }

    /// <summary>Breaks degree ties in favor of root README/INDEX documents before ordinary paths.</summary>
    private static int HomePriority(string path)
    {
        if (path.Contains('/'))
        {
            return 2;
        }
        var extensionStart = path.LastIndexOf('.');
        var name = extensionStart < 0 ? path : path[..extensionStart];
        if (name.Equals("README", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
        return name.Equals("index", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
    }
}
