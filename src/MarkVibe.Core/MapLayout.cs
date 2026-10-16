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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarkVibe.Core;
public sealed record MapColor(string Value, string Label);
public sealed record MapIcon(string Value, string Label);
public sealed record MapCardLayout(
    [property: JsonRequired] double X,
    [property: JsonRequired] double Y,
    [property: JsonRequired] string Color,
    [property: JsonRequired] string Icon,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Width = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Height = null
);
public sealed record MapLayout(
    [property: JsonRequired] int SchemaVersion,
    [property: JsonRequired] IReadOnlyDictionary<string, MapCardLayout> Nodes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MapAtlasSettings? Atlas = null
);
public sealed record MapFileState(string? Json, string Revision, bool CanWrite, bool CanEnableWrite = false);
public static class MapLayoutCodec
{
    public const string FileName = ".markvibe-map.json";
    public const int SchemaVersion = 4;
    public const int MaximumBytes = 1024 * 1024;
    public const int MaximumNodes = Limits.Entries;
    public const double MaximumCoordinate = 100000;
    public const int CardWidth = 260, CardHeight = 166, ColumnStep = 288, RowStep = 194;
    public const int MinimumCardWidth = 180, MinimumCardHeight = 120;
    public const int MaximumCardWidth = 780, MaximumCardHeight = 600;
    public const string DefaultColor = "#0085ff", DefaultIcon = "file";
    public static IReadOnlyList<MapColor> Colors { get; } = [
        new("#5b557f", "Slate"),
        new("#ff4141", "Red"),
        new("#ff7020", "Orange"),
        new("#ffc400", "Amber"),
        new("#00bd55", "Green"),
        new(DefaultColor, "Blue"),
        new("#914bff", "Purple"),
        new("#ff59ad", "Pink"),
        new("#748094", "Gray"),
        new("#d64578", "Rose"),
        new("#e88263", "Coral"),
        new("#84ad18", "Lime"),
        new("#008f83", "Teal"),
        new("#009fbd", "Cyan"),
        new("#5865d9", "Indigo"),
        new("#a56d46", "Brown")
    ];
    public static IReadOnlyList<MapIcon> Icons { get; } = [
        new("file", "Document"),
        new("folder", "Folder"),
        new("coin", "Money"),
        new("book", "Book"),
        new("graduation", "Learning"),
        new("pencil", "Pencil"),
        new("pen-tool", "Design"),
        new("code", "Code"),
        new("terminal", "Terminal"),
        new("music", "Music"),
        new("flower", "Flower"),
        new("palette", "Art"),
        new("stethoscope", "Health"),
        new("sparkles", "Ideas"),
        new("briefcase", "Work"),
        new("chart", "Chart"),
        new("kettlebell", "Fitness"),
        new("dumbbell", "Training"),
        new("notebook", "Notes"),
        new("balance", "Balance"),
        new("moon", "Moon"),
        new("plane", "Travel"),
        new("globe", "World"),
        new("heart", "Heart")
    ];

    private static readonly JsonSerializerOptions Options = new(Wire.Options)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 8,
        WriteIndented = true
    };
    /// <summary>Reads only the current strict card layout; unsupported originals remain untouched by the caller.</summary>
    public static MapLayout Read(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes)
        {
            throw Invalid();
        }

        if (json.StartsWith('\uFEFF'))
        {
            json = json[1..];
        }

        try
        {
            using var document = JsonDocument.Parse(json, new() { MaxDepth = 8 });
            RejectDuplicateProperties(document.RootElement);
            var value = JsonSerializer.Deserialize<MapLayout>(json, Options) ?? throw Invalid();
            Validate(value);
            if (document.RootElement.TryGetProperty("atlas", out var atlas))
            {
                if (atlas.ValueKind != JsonValueKind.Object)
                {
                    throw Invalid();
                }
                foreach (var collection in new[] { "collapsedFolders", "views" })
                {
                    if (atlas.TryGetProperty(collection, out var items) && items.ValueKind != JsonValueKind.Array)
                    {
                        throw Invalid();
                    }
                }
            }

            // Nullable fields represent absence only; explicit nulls are not valid persisted dimensions.
            foreach (var node in document.RootElement.GetProperty("nodes").EnumerateObject())
            {
                foreach (var dimension in new[] { "width", "height" })
                {
                    if (node.Value.TryGetProperty(dimension, out var size) && size.ValueKind != JsonValueKind.Number)
                    {
                        throw Invalid();
                    }
                }
            }

            return value;
        }
        catch (JsonException)
        {
            throw Invalid();
        }
    }

    /// <summary>Validates a map layout and serializes it within the persisted byte budget.</summary>
    public static string Write(MapLayout value)
    {
        Validate(value);
        var json = JsonSerializer.Serialize(value, Options) + "\n";
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes)
        {
            throw Invalid();
        }

        return json;
    }

    /// <summary>Validates the single supported schema, canonical document paths and bounded card geometry.</summary>
    public static void Validate(MapLayout value)
    {
        if (value.SchemaVersion != SchemaVersion ||
            value.Nodes == null ||
            value.Nodes.Count > MaximumNodes)
        {
            throw Invalid();
        }

        MapAtlas.Validate(value.Atlas);
        foreach (var(path, card)in value.Nodes)
        {
            if (WorkspacePath.Normalize(path, true) != path || !WorkspacePath.IsMarkdown(path) || WorkspacePath.IsIgnored(path))
            {
                throw Invalid();
            }

            ValidateCard(card);
        }
    }

    /// <summary>Rejects invalid positions, paired dimensions outside their bounds, and unknown card styles.</summary>
    public static void ValidateCard(MapCardLayout card)
    {
        if (card == null ||
            !double.IsFinite(card.X) ||
            !double.IsFinite(card.Y) ||
            Math.Abs(card.X) > MaximumCoordinate ||
            Math.Abs(card.Y) > MaximumCoordinate ||
            card.Width.HasValue != card.Height.HasValue ||
            card.Width is { } width && (!double.IsFinite(width) || width < MinimumCardWidth || width > MaximumCardWidth) ||
            card.Height is { } height && (!double.IsFinite(height) || height < MinimumCardHeight || height > MaximumCardHeight) ||
            !Colors.Any(color => color.Value == card.Color) ||
            !Icons.Any(icon => icon.Value == card.Icon))
        {
            throw Invalid();
        }
    }

    /// <summary>Rejects ambiguous duplicate members before deserialization can overwrite a previous value.</summary>
    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicateProperties(item);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Add(property.Name))
            {
                throw Invalid();
            }

            RejectDuplicateProperties(property.Value);
        }
    }

    /// <summary>Creates the standard invalid-map error while preserving the original file.</summary>
    private static WorkspaceException Invalid() => new("invalid-map", "This map layout is unsupported or invalid. The original file is preserved; download your layout to keep a separate copy.");
}

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
        Value = loaded ?? new(MapLayoutCodec.SchemaVersion, new Dictionary<string, MapCardLayout>(StringComparer.Ordinal));
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
            while (nodes.Values.Any(node => candidate.X < node.X + (node.Width ?? MapLayoutCodec.CardWidth) + 12 &&
                candidate.X + MapLayoutCodec.CardWidth + 12 > node.X &&
                candidate.Y < node.Y + (node.Height ?? MapLayoutCodec.CardHeight) + 12 &&
                candidate.Y + MapLayoutCodec.CardHeight + 12 > node.Y));
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
    public void Move(string path, double x, double y) => Update(path, Card(path)with { X = x, Y = y });
    /// <summary>Updates a card's color and icon through the validated layout change path.</summary>
    public void Style(string path, string color, string icon) => Update(path, Card(path)with { Color = color, Icon = icon });
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
    private MapCardLayout Card(string path) => Value.Nodes.TryGetValue(path, out var card) ? card : throw new WorkspaceException("missing-map-card", "This card is no longer in the map.");
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
