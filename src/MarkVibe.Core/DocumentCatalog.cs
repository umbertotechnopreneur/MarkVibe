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
 * Modified with AI: OpenAI Codex; local Markdown draft catalog updates; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Modified with AI: OpenAI Codex; retain the central hub in bounded graph projections; 2026-10-16
 * Human guidance: Umberto Giacobbi; center the most-connected document in map layouts
 * Modified with AI: OpenAI Codex; source-backed document card statistics; 2026-10-16
 * Human guidance: Umberto Giacobbi; optional advanced map view with document size, word and token estimates
 * Modified with AI: OpenAI Codex; authoritative document sessions and unified catalog projections; 2026-10-16
 * Human guidance: Umberto Giacobbi; remove duplicate document state and unused search compatibility
 * Modified with AI: OpenAI Codex; source excerpts, heading previews and complete-catalog atlas routes; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement the seven approved document atlas improvements
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text;

namespace MarkVibe.Core;
public sealed record DocumentCard(string Path, string Title, string Introduction, IReadOnlyList<DocumentLink> Links, DocumentStatistics? Statistics = null)
{
    public IReadOnlyList<Heading> Headings { get; init; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyDictionary<int, string> LinkExcerpts { get; init; } = new Dictionary<int, string>();
    /// <summary>Returns the document title used by managed list controls.</summary>
    public override string ToString() => Title;
}

public sealed record MapEdge(string Source, string Target, string Anchor, int Line = 0, string Excerpt = "");
public sealed record MapSnapshot(
    IReadOnlyList<DocumentCard> Nodes,
    IReadOnlyList<MapEdge> Edges,
    IReadOnlyList<DocumentLink> Outgoing,
    IReadOnlyList<DocumentCard> Incoming,
    int TotalDocuments,
    bool Truncated,
    IReadOnlyDictionary<string, int> Depths,
    IReadOnlyList<string>? Route = null,
    string RouteMessage = "",
    IReadOnlyList<MapFolder>? Folders = null
);
public sealed class DocumentCatalog(MarkdownParser parser)
{
    private const int MaximumEditedDocuments = 16;
    private readonly Dictionary<string, DocumentCard> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _fingerprints = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MarkdownModel> _cache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DocumentSession> _sessions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _workspaceDocumentPaths = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _recency = new();
    public IReadOnlyList<DocumentCard> Cards => _cards.Values.OrderBy(c => c.Path, StringComparer.Ordinal).ToList();
    public IReadOnlyCollection<DocumentSession> Sessions => _sessions.Values;
    public List<string> Warnings { get; } = new();
    public bool IndexComplete { get; private set; }
    public int IndexedCount { get; private set; }

    /// <summary>Reseeds canonical workspace entries while retaining local revisions independently of files and parsed cache.</summary>
    public void Seed(IReadOnlyList<WorkspaceEntry> entries)
    {
        if (entries.Count > Limits.Entries)
        {
            throw new WorkspaceException("too-many-files", "This workspace exceeds the file limit.");
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (WorkspacePath.Normalize(entry.Path, imported: true) != entry.Path || !paths.Add(entry.Path))
            {
                throw new WorkspaceException("invalid-path", "The workspace contains a duplicate or noncanonical path.");
            }
        }

        var markdownEntries = entries
            .Where(entry => WorkspacePath.IsMarkdown(entry.Path) && !WorkspacePath.IsIgnored(entry.Path))
            .Take(Limits.Documents + 1)
            .ToList();
        if (markdownEntries.Count > Limits.Documents)
        {
            throw new WorkspaceException("too-many-documents", "This workspace exceeds the 1,024 document limit.");
        }

        var cards = new Dictionary<string, DocumentCard>(StringComparer.Ordinal);
        var fingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in markdownEntries)
        {
            cards.Add(entry.Path, new(entry.Path, entry.Path.Split('/').Last(), "Waiting to index this document.", []));
        }

        foreach (var session in _sessions.Values)
        {
            // A removed file must not make an unsaved local revision inaccessible during refresh.
            if (cards.ContainsKey(session.Path) || session.NeedsSave)
            {
                var model = parser.Parse(session.Path, session.CurrentSource);
                cards[session.Path] = CreateCard(model);
                fingerprints[session.Path] = model.Fingerprint;
            }
        }

        foreach (var path in _sessions.Keys.Where(path => !cards.ContainsKey(path)).ToArray())
        {
            _sessions.Remove(path);
        }

        _cards.Clear();
        foreach (var card in cards)
        {
            _cards.Add(card.Key, card.Value);
        }

        _fingerprints.Clear();
        foreach (var fingerprint in fingerprints)
        {
            _fingerprints.Add(fingerprint.Key, fingerprint.Value);
        }

        _workspaceDocumentPaths.Clear();
        _workspaceDocumentPaths.UnionWith(markdownEntries.Select(entry => entry.Path));
        _cache.Clear();
        _recency.Clear();
        Warnings.Clear();
        IndexComplete = false;
        IndexedCount = 0;
    }

    /// <summary>Indexes document content within the byte budget while reporting progress and retaining readable failures.</summary>
    public async Task IndexAsync(IWorkspaceProvider provider, Action<int, int> progress, CancellationToken cancellationToken)
    {
        var paths = _cards.Keys.ToArray();
        IndexedCount = 0;
        IndexComplete = false;
        Warnings.Clear();
        var totalBytes = 0;
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var source = await ReadSourceAsync(provider, path, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                source = EffectiveSource(path, source);
                var bytes = Encoding.UTF8.GetByteCount(source);
                if (totalBytes + bytes > Limits.IndexBytes)
                {
                    Warnings.Add("Content indexing reached its 8 MiB limit. Remaining pages can still be opened from the document list.");
                    break;
                }

                totalBytes += bytes;
                var model = parser.Parse(path, source);
                UpdateMetadata(model);
                if (_cache.ContainsKey(path))
                {
                    Remember(model);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                if (Warnings.Count < 24)
                {
                    Warnings.Add(path + ": " + error.Message);
                }

                _cards[path] = _cards[path] with
                {
                    Introduction = "This page could not be indexed. Open it for details."
                };
            }

            IndexedCount++;
            progress(IndexedCount, paths.Length);
            await Task.Yield();
        }

        IndexComplete = IndexedCount == paths.Length;
        progress(IndexedCount, paths.Length);
    }

    /// <summary>Opens and caches a catalog document, rejecting paths outside the seeded document set.</summary>
    public async Task<MarkdownModel> OpenAsync(IWorkspaceProvider provider, string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        path = WorkspacePath.Normalize(path);
        if (!_cards.ContainsKey(path))
        {
            throw new WorkspaceException("missing-document", "This page is not in the opened workspace. Add the file or reopen its folder.");
        }

        if (_cache.TryGetValue(path, out var cached))
        {
            Touch(path);
            return cached;
        }

        var source = await ReadSourceAsync(provider, path, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        source = EffectiveSource(path, source);
        var model = parser.Parse(path, source);
        UpdateDocument(model);
        return model;
    }

    /// <summary>Returns a retained editing session without creating state for ordinary document reads.</summary>
    public DocumentSession? TryGetSession(string path)
    {
        return _sessions.GetValueOrDefault(path);
    }

    /// <summary>Forgets a file and its draft only after the owner has confirmed and completed its local deletion.</summary>
    public void ForgetDeletedDocument(string path)
    {
        _sessions.Remove(path);
        _workspaceDocumentPaths.Remove(path);
        _cards.Remove(path);
        _fingerprints.Remove(path);
        _cache.Remove(path);
        _recency.Remove(path);
    }

    /// <summary>Makes room before an AI request, evicting only restored or exported sessions other than the current path.</summary>
    public void EnsureRevisionCapacity(string path)
    {
        if (!_cards.ContainsKey(path))
        {
            throw new WorkspaceException("missing-document", "This page is not in the opened workspace.");
        }

        if (_sessions.ContainsKey(path) || _sessions.Count < MaximumEditedDocuments)
        {
            return;
        }

        var removable = _sessions.Values.FirstOrDefault(session => session.Path != path && !session.NeedsSave);
        if (removable == null)
        {
            throw new WorkspaceException("markdown-drafts", "Save or restore an edited document before editing another page.");
        }

        // Export writes a separate copy. Restore the original path only if it still belongs to the workspace.
        if (_workspaceDocumentPaths.Contains(removable.Path))
        {
            var original = parser.Parse(removable.Path, removable.OriginalSource);
            UpdateDocument(original);
        }
        else
        {
            _cards.Remove(removable.Path);
            _fingerprints.Remove(removable.Path);
            _cache.Remove(removable.Path);
            _recency.Remove(removable.Path);
        }

        _sessions.Remove(removable.Path);
    }

    /// <summary>Commits parsed content, revision history, card metadata and cache together without writing the workspace file.</summary>
    public DocumentSession ApplyRevision(MarkdownModel expected, MarkdownModel revised, DocumentRevisionAction action = DocumentRevisionAction.Edit)
    {
        if (expected.Path != revised.Path || !Enum.IsDefined(action))
        {
            throw new WorkspaceException("markdown-revision", "This revision does not belong to the current document.");
        }

        if (_fingerprints.TryGetValue(expected.Path, out var fingerprint) && fingerprint != expected.Fingerprint)
        {
            throw new WorkspaceException("markdown-revision", "The document changed before this revision could be applied.");
        }

        var session = TryGetSession(expected.Path);
        if (session != null && session.CurrentSource != expected.Source)
        {
            throw new WorkspaceException("markdown-revision", "The document changed before this revision could be applied.");
        }

        if (session == null && action != DocumentRevisionAction.Edit)
        {
            throw new WorkspaceException("markdown-history", "This Markdown history action is no longer available.");
        }

        var card = CreateCard(revised);
        EnsureRevisionCapacity(expected.Path);
        session ??= new DocumentSession(expected);
        session.ApplyRevision(revised.Source, action);
        _sessions[expected.Path] = session;
        UpdateMetadata(revised, card);
        Remember(revised);
        return session;
    }

    /// <summary>Marks the exact revision exported by the caller without creating a session for an unchanged document.</summary>
    public void MarkExported(string path, string source)
    {
        TryGetSession(path)?.MarkExported(source);
    }

    /// <summary>Reads a source snapshot; callers recheck session state after awaiting it before parsing or publishing metadata.</summary>
    private async Task<string> ReadSourceAsync(IWorkspaceProvider provider, string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_sessions.TryGetValue(path, out var session))
        {
            return session.CurrentSource;
        }

        return await provider.ReadDocumentAsync(path, cancellationToken);
    }

    /// <summary>Resolves the effective source synchronously immediately before parsing, closing the asynchronous read window.</summary>
    private string EffectiveSource(string path, string source)
    {
        return _sessions.TryGetValue(path, out var session) ? session.CurrentSource : source;
    }

    /// <summary>Projects all visible document metadata from one effective parsed revision.</summary>
    private static DocumentCard CreateCard(MarkdownModel model)
    {
        var lines = model.Source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var excerpts = new Dictionary<int, string>();
        foreach (var line in model.Links.Select(link => link.Line).Distinct().Take(64))
        {
            var excerpt = line > 0 && line <= lines.Length ? lines[line - 1].Trim() : "";
            excerpts[line] = excerpt.Length <= 220 ? excerpt : excerpt[..220] + "…";
        }
        return new(model.Path, model.Title, model.Introduction, model.Links, DocumentStatistics.FromModel(model))
        {
            Headings = model.Headings.Take(12).ToArray(),
            LinkExcerpts = excerpts
        };
    }

    /// <summary>Publishes visible metadata and its source identity together, including for documents outside the parsed cache.</summary>
    private void UpdateMetadata(MarkdownModel model, DocumentCard? card = null)
    {
        _cards[model.Path] = card ?? CreateCard(model);
        _fingerprints[model.Path] = model.Fingerprint;
    }

    /// <summary>Publishes metadata and the parsed cache entry from the same effective document.</summary>
    private void UpdateDocument(MarkdownModel model)
    {
        UpdateMetadata(model);
        Remember(model);
    }

    /// <summary>Caches a parsed document and evicts the least recently used entries above the retained limit.</summary>
    private void Remember(MarkdownModel model)
    {
        _cache[model.Path] = model;
        Touch(model.Path);
        while (_cache.Count > Limits.CachedDocuments)
        {
            var oldest = _recency.First!.Value;
            _recency.RemoveFirst();
            _cache.Remove(oldest);
            // Eviction discards parsed models; catalog cards and link information remain available.
        }
    }

    /// <summary>Moves a cached document to the most-recent position in the eviction timeline.</summary>
    private void Touch(string path)
    {
        _recency.Remove(path);
        _recency.AddLast(path);
    }

    /// <summary>Projects a bounded document graph that retains its hub and selected card, optionally limited to its neighborhood.</summary>
    public MapSnapshot Map(string selected, bool neighborhood, string routeStart = "", string routeEnd = "", int focusDepth = 0)
    {
        var cards = Cards;
        var graph = new DocumentGraph(cards);
        var distances = graph.DistancesFrom(selected);
        var route = graph.ShortestPath(routeStart, routeEnd);
        var outgoing = _cards.TryGetValue(selected, out var card) ? card.Links : [];
        var incoming = cards.Where(c => c.Links.Any(l => l.Path == selected)).ToList();
        var neighbors = outgoing.Select(l => l.Path).Concat(incoming.Select(c => c.Path)).Append(selected).ToHashSet(StringComparer.Ordinal);
        var filtered = cards
            .Where(c => !neighborhood || neighbors.Contains(c.Path))
            .ToList();
        var eligiblePaths = filtered.Select(document => document.Path).ToHashSet(StringComparer.Ordinal);
        var priority = route.Concat(focusDepth > 0 ? new[] { selected } : Array.Empty<string>())
            .Where(path => eligiblePaths.Contains(path));
        var orderedPaths = focusDepth > 0
            ? graph.OrderedPaths.OrderBy(path => distances.GetValueOrDefault(path, int.MaxValue)).ToArray()
            : graph.OrderedPaths;
        var nodes = priority.Concat(orderedPaths).Distinct(StringComparer.Ordinal)
            .Where(path => eligiblePaths.Contains(path))
            .Take(Limits.MapNodes)
            .Select(path => _cards[path])
            .ToList();
        if (eligiblePaths.Contains(selected) && !nodes.Any(node => node.Path == selected))
        {
            // Keep the hub at the front while retaining an eligible selection beyond the display limit.
            nodes[^1] = _cards[selected];
        }

        var visible = nodes.Select(c => c.Path).ToHashSet(StringComparer.Ordinal);
        var routePairs = route.Zip(route.Skip(1)).ToHashSet();
        var allEdges = nodes.SelectMany(c => c.Links.Where(l => visible.Contains(l.Path))
            .Select(l => new MapEdge(c.Path, l.Path, l.Anchor, l.Line, c.LinkExcerpts.GetValueOrDefault(l.Line, ""))))
            .DistinctBy(edge => (edge.Source, edge.Target, edge.Anchor))
            .OrderByDescending(edge => routePairs.Contains((edge.Source, edge.Target)) || routePairs.Contains((edge.Target, edge.Source)))
            .ToList();
        var routeMessage = routeStart.Length == 0 || routeEnd.Length == 0 ? ""
            : route.Count == 0 ? "No connection between these documents"
            : route.Count > Limits.MapNodes ? "Route exceeds the visible document limit"
            : "Shortest route: {0} links";
        return new(
            nodes.Select(c => c with { Links = [], LinkExcerpts = new Dictionary<int, string>() }).ToList(),
            allEdges.Take(Limits.MapEdges).ToList(),
            outgoing.Take(100).ToList(),
            incoming.Take(100).Select(c => c with { Links = [] }).ToList(),
            cards.Count,
            filtered.Count > nodes.Count || allEdges.Count > Limits.MapEdges,
            nodes.ToDictionary(node => node.Path, node => distances.GetValueOrDefault(node.Path, -1), StringComparer.Ordinal),
            route,
            routeMessage,
            cards.GroupBy(document => document.Path.Contains('/') ? document.Path[..document.Path.LastIndexOf('/')] : "")
                .Select(group => new MapFolder(group.Key, group.Count())).OrderBy(folder => folder.Path, StringComparer.Ordinal).ToArray()
        );
    }
}
