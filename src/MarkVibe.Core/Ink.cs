using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MarkVibe.Core;

public sealed record InkDocument(string Path, string HashAlgorithm, string ContentHash);
public sealed record InkAnchor(string Kind, string HeadingId, string BlockId, string Quote, string Prefix, string Suffix, int SourceLine);
public sealed record InkCanvas(double Width, double Height, string CoordinateSpace = "logical");
public sealed record InkPoint(double X, double Y, double Pressure = 0.5, int ElapsedMs = 0);
public sealed record InkStroke(string Color, double Width, string Tool, IReadOnlyList<InkPoint> Points);
public sealed record InkAnnotation(string Id, InkAnchor Anchor, string Placement, InkCanvas Canvas, IReadOnlyList<InkStroke> Strokes);
public sealed record InkSidecar(int SchemaVersion, InkDocument Document, IReadOnlyList<InkAnnotation> Annotations);
public sealed record ResolvedInk(InkAnnotation Annotation, string BlockId);

public static class InkCodec
{
    public const int MaximumBytes = 1024 * 1024;
    public const int MaximumPoints = 20000;
    private static readonly JsonSerializerOptions Options = new(Wire.Options)
    {
        PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16
    };

    public static InkSidecar Empty(MarkdownModel model) => new(1, new(model.Path, "sha256-utf8-exact", model.Fingerprint), []);
    public static InkSidecar Read(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw Invalid("This annotation file exceeds 1 MiB.");
        if (json.StartsWith('\uFEFF')) json = json[1..];
        try
        {
            var value = JsonSerializer.Deserialize<InkSidecar>(json, Options) ?? throw Invalid("Empty annotation file.");
            Validate(value); return value;
        }
        catch (JsonException) { throw Invalid("This annotation format is unsupported. The original file is preserved."); }
    }
    public static string Write(InkSidecar value)
    {
        Validate(value);
        var json = JsonSerializer.Serialize(value, Options);
        if (Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw Invalid("This annotation file exceeds 1 MiB.");
        return json;
    }
    public static void Validate(InkSidecar value)
    {
        if (value.SchemaVersion != 1 || value.Document is not { } doc || value.Annotations == null ||
            doc.HashAlgorithm != "sha256-utf8-exact" || doc.ContentHash == null ||
            !Regex.IsMatch(doc.ContentHash, "^[a-f0-9]{64}$") || doc.Path == null || doc.Path.Length > 1024 ||
            WorkspacePath.Normalize(doc.Path) != doc.Path || !WorkspacePath.IsMarkdown(doc.Path) || value.Annotations.Count > 128)
            throw Invalid("This annotation format is unsupported. The original file is preserved.");
        var ids = new HashSet<string>(StringComparer.Ordinal); var points = 0;
        foreach (var annotation in value.Annotations)
        {
            if (annotation == null || annotation.Id == null || !Regex.IsMatch(annotation.Id, "^[a-zA-Z0-9_-]{1,64}$") || !ids.Add(annotation.Id) ||
                annotation.Anchor is not { } anchor || annotation.Canvas is not { } canvas || annotation.Strokes == null ||
                annotation.Placement is not ("overlay" or "after-block") || canvas.CoordinateSpace != "logical" ||
                !Bound(canvas.Width, 1, 10000) || !Bound(canvas.Height, 1, 10000) || annotation.Strokes.Count > 256 ||
                anchor.Kind is not ("section" or "text") || !Text(anchor.HeadingId, 256) || !Text(anchor.BlockId, 128) ||
                !Text(anchor.Quote, 512) || !Text(anchor.Prefix, 128) || !Text(anchor.Suffix, 128) || anchor.SourceLine is < 1 or > 1000000)
                throw Invalid("Invalid annotation anchor or canvas.");
            foreach (var stroke in annotation.Strokes)
            {
                ValidateStroke(stroke, canvas);
                points += stroke.Points.Count;
                if (points > MaximumPoints) throw Invalid("This page has reached its annotation point limit.");
            }
        }
    }
    public static void ValidateStroke(InkStroke stroke, InkCanvas canvas)
    {
        if (stroke == null || stroke.Color == null || !Regex.IsMatch(stroke.Color, "^#[a-fA-F0-9]{6}(?:[a-fA-F0-9]{2})?$") ||
            !Bound(stroke.Width, 0.1, 64) || stroke.Tool is not ("pen" or "highlighter") ||
            stroke.Points == null || stroke.Points.Count is < 2 or > MaximumPoints)
            throw Invalid("Invalid ink stroke.");
        var time = 0;
        foreach (var point in stroke.Points)
        {
            if (point == null || !Bound(point.X, 0, canvas.Width) || !Bound(point.Y, 0, canvas.Height) ||
                !Bound(point.Pressure, 0, 1) || point.ElapsedMs < time || point.ElapsedMs > 3600000)
                throw Invalid("Invalid ink coordinates or timing.");
            time = point.ElapsedMs;
        }
    }
    public static IReadOnlyList<ResolvedInk> Resolve(InkSidecar ink, MarkdownModel model)
    {
        if (ink.Document.Path != model.Path) return [];
        var result = new List<ResolvedInk>();
        foreach (var annotation in ink.Annotations)
        {
            var anchor = annotation.Anchor;
            var exact = ink.Document.ContentHash == model.Fingerprint ? model.Blocks.FirstOrDefault(b => b.Id == anchor.BlockId &&
                (anchor.Quote.Length == 0 || b.Quote == anchor.Quote)) : null;
            if (exact == null && anchor.Quote.Length > 0)
            {
                var candidates = model.Blocks.Where(b => b.Quote == anchor.Quote).Take(2).ToArray();
                if (candidates.Length == 1) exact = candidates[0];
            }
            if (exact != null) result.Add(new(annotation, exact.Id));
        }
        return result;
    }
    private static bool Bound(double value, double min, double max) => double.IsFinite(value) && value >= min && value <= max;
    private static bool Text(string? value, int max) => value != null && value.Length <= max && !value.Contains('\0');
    private static WorkspaceException Invalid(string message) => new("invalid-ink", message);
}

public sealed class InkSession(MarkdownModel model, InkSidecar? loaded = null)
{
    public MarkdownModel Model { get; } = model;
    public InkSidecar Value { get; private set; } = loaded ?? InkCodec.Empty(model);
    public bool Dirty { get; private set; }
    public bool ChangedDocument => Value.Document.ContentHash != Model.Fingerprint || Value.Document.Path != Model.Path;
    public IReadOnlyList<ResolvedInk> Resolved => InkCodec.Resolve(Value, Model);
    public int Unresolved => Value.Annotations.Count - Resolved.Count;
    private readonly Stack<InkSidecar> _undo = new(), _redo = new();
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public void Saved() => Dirty = false;
    public void Rebase()
    {
        if (Unresolved > 0 || Value.Document.Path != Model.Path) throw new WorkspaceException("ink-conflict", "Unresolved annotations must remain in the original sidecar.");
        var resolved = Resolved;
        Change(new(1, new(Model.Path, "sha256-utf8-exact", Model.Fingerprint), resolved.Select(item =>
            item.Annotation with { Anchor = Anchor(Model.Blocks.First(b => b.Id == item.BlockId)) }).ToArray()));
    }
    public void Add(string blockId, InkStroke stroke)
    {
        EnsureEditable(); var block = Model.Blocks.FirstOrDefault(b => b.Id == blockId) ?? throw new WorkspaceException("ink-anchor", "This paragraph is no longer available.");
        var annotations = Value.Annotations.ToList();
        var index = annotations.FindIndex(a => a.Anchor.BlockId == blockId && a.Placement == "after-block");
        var current = index < 0 ? new InkAnnotation(Guid.NewGuid().ToString("N"), Anchor(block), "after-block", new(1000, 240), []) : annotations[index];
        InkCodec.ValidateStroke(stroke, current.Canvas);
        var updated = current with { Strokes = current.Strokes.Append(stroke).ToArray() };
        if (index < 0) annotations.Add(updated); else annotations[index] = updated;
        Change(Value with { Annotations = annotations });
    }
    public void Erase(string annotationId, int strokeIndex)
    {
        EnsureEditable(); var annotations = Value.Annotations.ToList();
        var index = annotations.FindIndex(a => a.Id == annotationId);
        if (index < 0 || strokeIndex < 0 || strokeIndex >= annotations[index].Strokes.Count) return;
        var strokes = annotations[index].Strokes.Where((_, i) => i != strokeIndex).ToArray();
        if (strokes.Length == 0) annotations.RemoveAt(index); else annotations[index] = annotations[index] with { Strokes = strokes };
        Change(Value with { Annotations = annotations });
    }
    public void Undo() { if (_undo.TryPop(out var old)) { _redo.Push(Value); Value = old; Dirty = true; } }
    public void Redo() { if (_redo.TryPop(out var next)) { _undo.Push(Value); Value = next; Dirty = true; } }
    private InkAnchor Anchor(SourceBlock block) => new("text", Model.Headings.LastOrDefault(h => h.Line <= block.StartLine)?.Id ?? "", block.Id, block.Quote, "", "", block.StartLine);
    private void Change(InkSidecar next)
    {
        _ = InkCodec.Write(next); if (_undo.Count >= 20) _undo.Clear(); _undo.Push(Value); _redo.Clear(); Value = next; Dirty = true;
    }
    private void EnsureEditable() { if (ChangedDocument) throw new WorkspaceException("ink-conflict", "Review the changed document before editing its annotations."); }
}
