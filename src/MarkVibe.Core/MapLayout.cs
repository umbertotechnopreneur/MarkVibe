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

/// <summary>Defines a supported card color and its localizable display label.</summary>
public sealed record MapColor(string Value, string Label);

/// <summary>Defines a supported local icon identifier and its localizable display label.</summary>
public sealed record MapIcon(string Value, string Label);

/// <summary>Stores one document card's position, appearance and optional paired dimensions.</summary>
public sealed record MapCardLayout(
    [property: JsonRequired] double X,
    [property: JsonRequired] double Y,
    [property: JsonRequired] string Color,
    [property: JsonRequired] string Icon,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Width = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Height = null
);

/// <summary>Stores the current map schema, document membership and optional atlas presentation.</summary>
public sealed record MapLayout(
    [property: JsonRequired] int SchemaVersion,
    [property: JsonRequired] IReadOnlyDictionary<string, MapCardLayout> Nodes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MapAtlasSettings? Atlas = null
);

/// <summary>Reports a provider's layout content, revision and sidecar write capabilities.</summary>
public sealed record MapFileState(string? Json, string Revision, bool CanWrite, bool CanEnableWrite = false);

/// <summary>Reads and writes the single supported map format with bounded, unambiguous JSON and geometry.</summary>
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
            // Reuse the parsed JSON instead of parsing the original string a second time.
            var value = document.RootElement.Deserialize<MapLayout>(Options) ?? throw Invalid();
            Validate(value);
            ValidateAtlasJson(document.RootElement);
            ValidateDimensionJson(document.RootElement.GetProperty("nodes"));

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
        foreach (var (path, card) in value.Nodes)
        {
            var canonicalMarkdownPath = WorkspacePath.Normalize(path, true) == path
                && WorkspacePath.IsMarkdown(path)
                && !WorkspacePath.IsIgnored(path);
            if (!canonicalMarkdownPath)
            {
                throw Invalid();
            }

            ValidateCard(card);
        }
    }

    /// <summary>Rejects invalid positions, paired dimensions outside their bounds, and unknown card styles.</summary>
    public static void ValidateCard(MapCardLayout card)
    {
        if (card == null)
        {
            throw Invalid();
        }

        var validPosition = double.IsFinite(card.X)
            && double.IsFinite(card.Y)
            && Math.Abs(card.X) <= MaximumCoordinate
            && Math.Abs(card.Y) <= MaximumCoordinate;
        var pairedDimensions = card.Width.HasValue == card.Height.HasValue;
        var validDimensions = IsValidDimension(card.Width, MinimumCardWidth, MaximumCardWidth)
            && IsValidDimension(card.Height, MinimumCardHeight, MaximumCardHeight);

        if (!validPosition || !pairedDimensions || !validDimensions)
        {
            throw Invalid();
        }

        var supportedColor = Colors.Any(color => color.Value == card.Color);
        var supportedIcon = Icons.Any(icon => icon.Value == card.Icon);
        if (!supportedColor || !supportedIcon)
        {
            throw Invalid();
        }
    }

    /// <summary>Accepts an absent dimension or a finite value within the current card bounds.</summary>
    private static bool IsValidDimension(double? dimension, double minimum, double maximum)
    {
        if (!dimension.HasValue)
        {
            return true;
        }

        var size = dimension.Value;
        return double.IsFinite(size) && size >= minimum && size <= maximum;
    }

    /// <summary>Rejects explicit nulls and non-array atlas collections that nullable deserialization can hide.</summary>
    private static void ValidateAtlasJson(JsonElement root)
    {
        if (!root.TryGetProperty("atlas", out var atlas))
        {
            return;
        }

        if (atlas.ValueKind != JsonValueKind.Object)
        {
            throw Invalid();
        }

        foreach (var collectionName in new[] { "collapsedFolders", "views" })
        {
            if (atlas.TryGetProperty(collectionName, out var collection)
                && collection.ValueKind != JsonValueKind.Array)
            {
                throw Invalid();
            }
        }
    }

    /// <summary>Distinguishes omitted card sizes from explicit null or non-number JSON members.</summary>
    private static void ValidateDimensionJson(JsonElement nodes)
    {
        // Nullable fields represent absence only; explicit nulls are not persisted defaults.
        foreach (var node in nodes.EnumerateObject())
        {
            foreach (var dimensionName in new[] { "width", "height" })
            {
                if (node.Value.TryGetProperty(dimensionName, out var dimension)
                    && dimension.ValueKind != JsonValueKind.Number)
                {
                    throw Invalid();
                }
            }
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
