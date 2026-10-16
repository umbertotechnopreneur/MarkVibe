/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; sticky-note model and persistence validation; 2026-10-11
 * Human guidance: Umberto Giacobbi; graph and Markdown sticky notes with formatted content
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MarkVibe.Core;
public sealed record NoteAnchor(
    [property: JsonRequired] string TargetId,
    [property: JsonRequired] string Kind,
    [property: JsonRequired] string FragmentHash,
    [property: JsonRequired] string Quote,
    [property: JsonRequired] string Context,
    string AssetPath = "",
    double? U = null,
    double? V = null,
    string DocumentHash = ""
);
public sealed record StickyNote(
    [property: JsonRequired] string Id,
    [property: JsonRequired] string Title,
    [property: JsonRequired] string Body,
    [property: JsonRequired] string Color,
    [property: JsonRequired] string Icon,
    [property: JsonRequired] double X,
    [property: JsonRequired] double Y,
    double Width = 240,
    double Height = 220,
    int ZIndex = 0,
    NoteAnchor? Anchor = null
);
public static class NotesCodec
{
    public const string SidecarSuffix = ".notes.json";
    public const int MaximumBytes = 1024 * 1024;
    public const int MaximumNotes = 128;
    public const int MaximumTitleLength = 128;
    public const int MaximumBodyBytes = 16 * 1024;
    private static readonly JsonSerializerOptions Options = new(Wire.Options)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 12,
        WriteIndented = true
    };
    /// <summary>Checks unique note ids, per-note bounds and the collection's serialized byte budget.</summary>
    public static void ValidateNotes(IReadOnlyList<StickyNote> notes)
    {
        if (notes == null || notes.Count > MaximumNotes)
        {
            throw Invalid("A page or map can contain at most 128 sticky notes.");
        }

        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var note in notes)
        {
            ValidateNote(note);
            if (!identifiers.Add(note.Id))
            {
                throw Invalid("Sticky-note ids must be unique.");
            }
        }

        if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(notes, Options)) > MaximumBytes)
        {
            throw Invalid("This sticky-note collection exceeds 1 MiB.");
        }
    }

    /// <summary>Checks content and geometry shared by graph and document notes.</summary>
    public static void ValidateNote(StickyNote note)
    {
        if (note == null ||
            !Identifier(note.Id, 64) ||
            !Text(note.Title, MaximumTitleLength) ||
            !Text(note.Body, MaximumBodyBytes) ||
            Encoding.UTF8.GetByteCount(note.Body) > MaximumBodyBytes ||
            !MapLayoutCodec.Colors.Any(color => color.Value == note.Color) ||
            !MapLayoutCodec.Icons.Any(icon => icon.Value == note.Icon) ||
            !Bound(note.X, -MapLayoutCodec.MaximumCoordinate, MapLayoutCodec.MaximumCoordinate) ||
            !Bound(note.Y, -MapLayoutCodec.MaximumCoordinate, MapLayoutCodec.MaximumCoordinate) ||
            !Bound(note.Width, 160, 1200) ||
            !Bound(note.Height, 120, 1200) ||
            note.ZIndex is < 0 or > 1000000)
        {
            throw Invalid("Invalid sticky-note content, appearance or position.");
        }

        if (note.Anchor != null)
        {
            ValidateAnchor(note.Anchor);
        }
    }

    /// <summary>Validates generic element identity and optional normalized asset attachment coordinates.</summary>
    public static void ValidateAnchor(NoteAnchor anchor)
    {
        if (anchor == null ||
            !Identifier(anchor.TargetId, 128) ||
            anchor.Kind == null ||
            !Regex.IsMatch(anchor.Kind, "\\A[a-z][a-z0-9-]{0,31}\\z") ||
            !Hash(anchor.FragmentHash) ||
            !Text(anchor.Quote, 512) ||
            !Text(anchor.Context, 512) ||
            anchor.DocumentHash == null ||
            anchor.DocumentHash.Length > 0 &&
            !Hash(anchor.DocumentHash) ||
            anchor.AssetPath == null ||
            anchor.U.HasValue != anchor.V.HasValue ||
            anchor.U is { } horizontal &&
            !Bound(horizontal, 0, 1) ||
            anchor.V is { } vertical &&
            !Bound(vertical, 0, 1))
        {
            throw Invalid("Invalid sticky-note anchor.");
        }

        if (anchor.AssetPath.Length > 0)
        {
            ValidatePath(anchor.AssetPath, markdown: false);
        }
    }

    /// <summary>Rejects duplicate members at any nesting level, including objects inside arrays.</summary>
    internal static void RejectDuplicateProperties(JsonElement element)
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
                throw Invalid("Duplicate sticky-note properties are unsupported.");
            }

            RejectDuplicateProperties(property.Value);
        }
    }

    /// <summary>Ensures a stored path is canonical, relative and outside ignored directories.</summary>
    private static void ValidatePath(string path, bool markdown)
    {
        if (path == null ||
            WorkspacePath.Normalize(path, true) != path ||
            WorkspacePath.IsIgnored(path) ||
            markdown &&
            !WorkspacePath.IsMarkdown(path))
        {
            throw Invalid();
        }
    }

    /// <summary>Checks finite geometry without accepting NaN or infinities.</summary>
    private static bool Bound(double value, double minimum, double maximum) => double.IsFinite(value) && value >= minimum && value <= maximum;
    /// <summary>Checks a bounded literal string while retaining Markdown line breaks.</summary>
    private static bool Text(string? value, int maximum) => value != null && value.Length <= maximum && !value.Contains('\0');
    /// <summary>Checks portable ids suitable for wire messages and DOM lookup.</summary>
    private static bool Identifier(string? value, int maximum) => value != null && value.Length <= maximum && Regex.IsMatch(value, "\\A[a-zA-Z0-9_-]+\\z");
    /// <summary>Checks the lowercase SHA-256 format used by parsed Markdown fingerprints.</summary>
    private static bool Hash(string? value) => value != null && Regex.IsMatch(value, "\\A[a-f0-9]{64}\\z");
    /// <summary>Rejects unsupported legacy map-note metadata without discarding it.</summary>
    private static WorkspaceException Invalid(string message = "This sticky-note file is unsupported or invalid. The original file is preserved.") => new("invalid-notes", message);
}
