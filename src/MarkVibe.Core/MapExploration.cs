/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; bounded exploration and catalog-backed attention; 2026-10-16
 * Modified with AI: OpenAI Codex; document exploration state and clarify catalog refresh phases; 2026-10-16
 * Human guidance: Umberto Giacobbi; useful futuristic map navigation and finite link effects
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Describes verified missing Markdown targets, isolation and changes observed during this session.</summary>
public sealed record MapAttention(int MissingLinks, bool Isolated, bool Changed);

/// <summary>Carries plain catalog text for a selected document or a successfully opened page.</summary>
public sealed record MapExplorationDocument(
    string Path,
    string Title,
    string Introduction,
    DocumentStatistics? Statistics);

/// <summary>Publishes temporary exploration controls independently of the persisted map layout.</summary>
public sealed record MapExplorationSnapshot(
    bool LensEnabled,
    bool SelectionEnabled,
    string AttentionMode,
    bool CompareOpen,
    int AttentionCount,
    bool IndexComplete,
    IReadOnlyList<MapExplorationDocument> Picked,
    IReadOnlyList<MapExplorationDocument> Trail);

/// <summary>Owns workspace exploration without changing source files, coordinates or sidecar compatibility.</summary>
public sealed class MapExploration
{
    /// <summary>Limits comparison previews and selection actions to a small, readable batch.</summary>
    public const int MaximumPicked = 12;
    private const int MaximumTrail = 10;
    private readonly List<string> _picked = [];
    private readonly List<string> _trail = [];
    private readonly Dictionary<string, string> _fingerprints = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changed = new(StringComparer.Ordinal);
    private Dictionary<string, DocumentCard> _cards = new(StringComparer.Ordinal);
    private long _catalogRevision = -1;
    private bool _catalogIndexComplete;

    /// <summary>Enables temporary emphasis of a hovered document's relationships.</summary>
    public bool LensEnabled { get; set; } = true;

    /// <summary>Enables card picking without requiring a keyboard modifier.</summary>
    public bool SelectionEnabled { get; set; }

    /// <summary>Reports whether the picked documents are shown in a comparison surface.</summary>
    public bool CompareOpen { get; set; }

    /// <summary>Names the validated attention filter applied to catalog facts.</summary>
    public string AttentionMode { get; private set; } = "all";

    /// <summary>Reports whether every document has observed metadata for reliable isolation checks.</summary>
    public bool IndexComplete { get; private set; }

    /// <summary>Returns a selection snapshot so callers cannot mutate the owned path order.</summary>
    public IReadOnlyList<string> PickedPaths => _picked.ToArray();

    /// <summary>Exposes derived attention facts keyed by canonical workspace paths.</summary>
    public IReadOnlyDictionary<string, MapAttention> Attention { get; private set; }
        = new Dictionary<string, MapAttention>();

    /// <summary>Refreshes full-catalog facts; isolation is asserted only after indexing finishes.</summary>
    public void Refresh(DocumentCatalog catalog)
    {
        if (_catalogRevision == catalog.MetadataRevision && _catalogIndexComplete == catalog.IndexComplete)
        {
            return;
        }

        _cards = catalog.Cards.ToDictionary(card => card.Path, StringComparer.Ordinal);
        var connected = new HashSet<string>(StringComparer.Ordinal);
        var missing = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var card in _cards.Values)
        {
            var absentTargets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var link in card.Links)
            {
                if (_cards.ContainsKey(link.Path))
                {
                    if (link.Path != card.Path)
                    {
                        connected.Add(card.Path);
                        connected.Add(link.Path);
                    }
                }
                else if (WorkspacePath.IsMarkdown(link.Path))
                {
                    absentTargets.Add(link.Path);
                }
            }
            missing[card.Path] = absentTargets.Count;

            ObserveDocumentChange(catalog, card.Path);
        }

        // Failed or budget-limited indexing must not manufacture isolated-document warnings.
        IndexComplete = catalog.IndexComplete && _cards.Keys.All(path => catalog.FingerprintFor(path) != null);
        RemoveUnavailableDocuments();
        CompareOpen &= _picked.Count >= 2;
        Attention = _cards.Keys.ToDictionary(
            path => path,
            path => new MapAttention(
                missing[path],
                IndexComplete && !connected.Contains(path),
                _changed.Contains(path)),
            StringComparer.Ordinal);

        _catalogRevision = catalog.MetadataRevision;
        _catalogIndexComplete = catalog.IndexComplete;
    }

    /// <summary>Records observed source changes and local edits without treating an initial read as a modification.</summary>
    private void ObserveDocumentChange(DocumentCatalog catalog, string path)
    {
        var fingerprint = catalog.FingerprintFor(path);
        if (fingerprint != null)
        {
            var observedBefore = _fingerprints.TryGetValue(path, out var previous);
            if (observedBefore && previous != fingerprint)
            {
                _changed.Add(path);
            }

            _fingerprints[path] = fingerprint;
        }

        if (catalog.TryGetSession(path)?.IsModified == true)
        {
            _changed.Add(path);
        }
    }

    /// <summary>Prunes transient references after catalog membership changes, retaining the order of surviving paths.</summary>
    private void RemoveUnavailableDocuments()
    {
        _picked.RemoveAll(path => !_cards.ContainsKey(path));
        _trail.RemoveAll(path => !_cards.ContainsKey(path));
        foreach (var path in _fingerprints.Keys.Where(path => !_cards.ContainsKey(path)).ToArray())
        {
            _fingerprints.Remove(path);
            _changed.Remove(path);
        }
    }

    /// <summary>Returns matches to the attention lens in stable catalog order for the bounded graph projection.</summary>
    public IReadOnlyList<string> PriorityPaths()
    {
        return Attention
            .Where(item => Matches(item.Value))
            .Select(item => item.Key)
            .ToArray();
    }

    /// <summary>Checks a fact against the chosen attention mode without interpreting presentation colors.</summary>
    private bool Matches(MapAttention attention) => AttentionMode switch
    {
        "attention" => attention.MissingLinks > 0 || attention.Isolated || attention.Changed,
        "isolated" => attention.Isolated,
        "missing" => attention.MissingLinks > 0,
        "changed" => attention.Changed,
        _ => false
    };

    /// <summary>Rejects unsupported attention modes before publishing browser-supplied control values.</summary>
    public void SetAttentionMode(string mode)
    {
        if (mode is not ("all" or "attention" or "isolated" or "missing" or "changed"))
        {
            throw new WorkspaceException("map-attention", "This map action is unavailable.");
        }

        AttentionMode = mode;
    }

    /// <summary>Toggles one known document while preserving selection order and the comparison size limit.</summary>
    public void Toggle(string path)
    {
        if (!_cards.ContainsKey(path))
        {
            return;
        }

        if (!_picked.Remove(path))
        {
            SetPicked(_picked.Append(path));
        }

        CompareOpen &= _picked.Count >= 2;
    }

    /// <summary>Atomically accepts a bounded set of known paths from card clicks or a rectangle gesture.</summary>
    public void SetPicked(IEnumerable<string> paths)
    {
        var accepted = paths
            .Where(_cards.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .Take(MaximumPicked + 1)
            .ToArray();
        if (accepted.Length > MaximumPicked)
        {
            throw new WorkspaceException("map-selection-limit", "Select at most 12 documents at a time.");
        }

        _picked.Clear();
        _picked.AddRange(accepted);
        CompareOpen &= _picked.Count >= 2;
    }

    /// <summary>Records successful navigation only; hover, selection and camera centering are excluded.</summary>
    public void RecordVisit(string path)
    {
        if (_trail.LastOrDefault() == path)
        {
            return;
        }

        _trail.Add(path);
        if (_trail.Count > MaximumTrail)
        {
            _trail.RemoveAt(0);
        }
    }

    /// <summary>Returns copied UI state and bounded catalog previews without sending source text.</summary>
    public MapExplorationSnapshot Snapshot()
    {
        // Count matches directly: publishing controls does not need an allocated
        // priority-path array, which is used only when building the graph projection.
        var attentionCount = Attention.Values.Count(Matches);
        var pickedDocuments = _picked.Where(_cards.ContainsKey).Select(Preview).ToArray();
        var trailDocuments = _trail.Where(_cards.ContainsKey).Select(Preview).ToArray();

        return new(
            LensEnabled,
            SelectionEnabled,
            AttentionMode,
            CompareOpen,
            attentionCount,
            IndexComplete,
            pickedDocuments,
            trailDocuments);
    }

    /// <summary>Projects one known catalog card without copying its link graph into control state.</summary>
    private MapExplorationDocument Preview(string path)
    {
        var card = _cards[path];
        return new(path, card.Title, card.Introduction, card.Statistics);
    }
}
