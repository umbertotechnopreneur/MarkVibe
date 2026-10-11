using System.Text;

namespace MarkVibe.Core;

public sealed record DocumentCard(string Path, string Title, string Introduction, IReadOnlyList<DocumentLink> Links, bool HasSidecar, string SearchText = "")
{
    public override string ToString() => Title;
}
public sealed record MapEdge(string Source, string Target, string Anchor);
public sealed record MapSnapshot(IReadOnlyList<DocumentCard> Nodes, IReadOnlyList<MapEdge> Edges, IReadOnlyList<DocumentLink> Outgoing,
    IReadOnlyList<DocumentCard> Incoming, int TotalDocuments, bool Truncated);
public sealed record SearchResult(DocumentCard Document, string Snippet);

public sealed class DocumentCatalog(MarkdownParser parser)
{
    private readonly Dictionary<string, DocumentCard> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MarkdownModel> _cache = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _recency = new();
    private HashSet<string> _paths = new(StringComparer.Ordinal);
    public IReadOnlyList<DocumentCard> Cards => _cards.Values.OrderBy(c => c.Path, StringComparer.Ordinal).ToList();
    public List<string> Warnings { get; } = new();
    public bool IndexComplete { get; private set; }
    public int IndexedCount { get; private set; }

    public void Seed(IReadOnlyList<WorkspaceEntry> entries)
    {
        _cards.Clear(); _cache.Clear(); _recency.Clear(); Warnings.Clear(); IndexComplete = false; IndexedCount = 0;
        if (entries.Count > Limits.Entries) throw new WorkspaceException("too-many-files", "This workspace exceeds the file limit.");
        _paths = new(entries.Select(e => WorkspacePath.Normalize(e.Path, imported: true)), StringComparer.Ordinal);
        var docs = entries.Where(e => WorkspacePath.IsMarkdown(e.Path) && !WorkspacePath.IsIgnored(e.Path)).Take(Limits.Documents + 1).ToList();
        if (docs.Count > Limits.Documents) throw new WorkspaceException("too-many-documents", "This workspace exceeds the 1,024 document limit.");
        foreach (var entry in docs)
            _cards.Add(entry.Path, new(entry.Path, entry.Path.Split('/').Last(), "Waiting to index this document.", [], _paths.Contains(entry.Path + ".ink.json")));
    }

    public async Task IndexAsync(IWorkspaceProvider provider, Action<int, int> progress, CancellationToken cancellationToken)
    {
        var paths = _cards.Keys.ToArray();
        IndexedCount = 0; IndexComplete = false; Warnings.Clear();
        var totalBytes = 0;
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var source = await provider.ReadDocumentAsync(path, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var bytes = Encoding.UTF8.GetByteCount(source);
                if (totalBytes + bytes > Limits.IndexBytes)
                {
                    Warnings.Add("Content indexing reached its 8 MiB limit. Remaining pages can still be opened and found by path.");
                    break;
                }
                totalBytes += bytes;
                var model = parser.Parse(path, source);
                _cards[path] = new(path, model.Title, model.Introduction, model.Links, _paths.Contains(path + ".ink.json"), source);
                if (_cache.ContainsKey(path)) Remember(model);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error)
            {
                if (Warnings.Count < 24) Warnings.Add(path + ": " + error.Message);
                _cards[path] = _cards[path] with { Introduction = "This page could not be indexed. Open it for details." };
            }
            IndexedCount++;
            progress(IndexedCount, paths.Length);
            await Task.Yield();
        }
        IndexComplete = IndexedCount == paths.Length;
        progress(IndexedCount, paths.Length);
    }

    public async Task<MarkdownModel> OpenAsync(IWorkspaceProvider provider, string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        path = WorkspacePath.Normalize(path);
        if (!_cards.ContainsKey(path)) throw new WorkspaceException("missing-document", "This page is not in the opened workspace. Add the file or reopen its folder.");
        if (_cache.TryGetValue(path, out var cached)) { Touch(path); return cached; }
        var source = await provider.ReadDocumentAsync(path, cancellationToken);
        var model = parser.Parse(path, source);
        Remember(model);
        var previous = _cards[path];
        _cards[path] = previous with { Title = model.Title, Introduction = model.Introduction, Links = model.Links };
        return model;
    }

    private void Remember(MarkdownModel model)
    {
        _cache[model.Path] = model; Touch(model.Path);
        while (_cache.Count > Limits.CachedDocuments)
        {
            var oldest = _recency.First!.Value;
            _recency.RemoveFirst(); _cache.Remove(oldest);
        }
    }
    private void Touch(string path)
    {
        _recency.Remove(path); _recency.AddLast(path);
    }

    public IReadOnlyList<SearchResult> Search(string query)
    {
        query = query.Trim();
        var results = new List<SearchResult>();
        foreach (var card in Cards)
        {
            var index = card.SearchText.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (query.Length > 0 && index < 0 && card.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 && card.Path.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
            var snippet = card.Introduction;
            if (index >= 0 && query.Length > 0)
            {
                var start = Math.Max(0, index - 50);
                snippet = card.SearchText.Substring(start, Math.Min(180, card.SearchText.Length - start)).Replace('\n', ' ');
            }
            results.Add(new(card, snippet));
            if (results.Count >= 200) break;
        }
        return results;
    }

    public MapSnapshot Map(string selected, string query, bool neighborhood)
    {
        var cards = Cards;
        var outgoing = _cards.TryGetValue(selected, out var card) ? card.Links : [];
        var incoming = cards.Where(c => c.Links.Any(l => l.Path == selected)).ToList();
        var neighbors = outgoing.Select(l => l.Path).Concat(incoming.Select(c => c.Path)).Append(selected).ToHashSet(StringComparer.Ordinal);
        var filtered = cards.Where(c => !neighborhood || neighbors.Contains(c.Path))
            .Where(c => string.IsNullOrWhiteSpace(query) || (c.Title + " " + c.Path + " " + c.Introduction).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        var nodes = filtered.Take(Limits.MapNodes).Select(c => c with { SearchText = "" }).ToList();
        var visible = nodes.Select(c => c.Path).ToHashSet(StringComparer.Ordinal);
        var allEdges = nodes.SelectMany(c => c.Links.Where(l => visible.Contains(l.Path)).Select(l => new MapEdge(c.Path, l.Path, l.Anchor))).Distinct().ToList();
        return new(nodes.Select(c => c with { Links = [] }).ToList(), allEdges.Take(Limits.MapEdges).ToList(), outgoing.Take(100).ToList(), incoming.Take(100).Select(c => c with { SearchText = "", Links = [] }).ToList(), cards.Count,
            filtered.Count > nodes.Count || allEdges.Count > Limits.MapEdges);
    }
}
