/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; validated atlas settings and saved viewpoints; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement all seven document atlas improvements
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

public sealed record MapCamera(
    [property: System.Text.Json.Serialization.JsonRequired] double CenterX,
    [property: System.Text.Json.Serialization.JsonRequired] double CenterY,
    [property: System.Text.Json.Serialization.JsonRequired] double Scale);
public sealed record MapViewpoint(
    [property: System.Text.Json.Serialization.JsonRequired] string Name,
    [property: System.Text.Json.Serialization.JsonRequired] MapCamera Camera,
    [property: System.Text.Json.Serialization.JsonRequired] string SelectedPath,
    [property: System.Text.Json.Serialization.JsonRequired] int FocusDepth,
    [property: System.Text.Json.Serialization.JsonRequired] bool FolderGroups,
    [property: System.Text.Json.Serialization.JsonRequired] IReadOnlyList<string> CollapsedFolders);
public sealed record MapAtlasSettings(int FocusDepth = 0, bool FolderGroups = false,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? CollapsedFolders = null,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<MapViewpoint>? Views = null);
public sealed record MapFolder(string Path, int Count);

/// <summary>Bounds persisted atlas presentation independently of document coordinates and source content.</summary>
public static class MapAtlas
{
    public const int MaximumViews = 8;

    /// <summary>Rejects malformed lenses, folder paths, camera coordinates and duplicate viewpoint names.</summary>
    public static void Validate(MapAtlasSettings? atlas)
    {
        if (atlas == null)
        {
            return;
        }

        ValidateLens(atlas.FocusDepth, atlas.CollapsedFolders ?? []);
        var views = atlas.Views ?? [];
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (views.Count > MaximumViews)
        {
            throw Invalid();
        }

        foreach (var view in views)
        {
            if (view == null || string.IsNullOrWhiteSpace(view.Name) || view.Name.Length > 64
                || view.Name != view.Name.Trim() || view.Name.Any(char.IsControl) || !names.Add(view.Name)
                || view.Camera == null || !double.IsFinite(view.Camera.CenterX) || !double.IsFinite(view.Camera.CenterY)
                || Math.Abs(view.Camera.CenterX) > 1000000 || Math.Abs(view.Camera.CenterY) > 1000000
                || !double.IsFinite(view.Camera.Scale) || view.Camera.Scale < .001 || view.Camera.Scale > 1.8
                || view.SelectedPath == null || view.CollapsedFolders == null)
            {
                throw Invalid();
            }

            if (view.SelectedPath.Length > 0 && (WorkspacePath.Normalize(view.SelectedPath, true) != view.SelectedPath
                || !WorkspacePath.IsMarkdown(view.SelectedPath) || WorkspacePath.IsIgnored(view.SelectedPath)))
            {
                throw Invalid();
            }
            ValidateLens(view.FocusDepth, view.CollapsedFolders);
        }
    }

    /// <summary>Validates the small focus range and canonical distinct folder identifiers, including the workspace root.</summary>
    private static void ValidateLens(int depth, IReadOnlyList<string> folders)
    {
        if (depth < 0 || depth > 3 || folders.Count > Limits.Documents)
        {
            throw Invalid();
        }

        var distinct = new HashSet<string>(StringComparer.Ordinal);
        foreach (var folder in folders)
        {
            if (folder == null || folder.Length > 1024 || !distinct.Add(folder)
                || folder.Length > 0 && WorkspacePath.Normalize(folder + "/atlas.md", true) != folder + "/atlas.md")
            {
                throw Invalid();
            }
        }
    }

    /// <summary>Returns an actionable validation error without rewriting an unsupported sidecar.</summary>
    private static WorkspaceException Invalid() => new("invalid-map-atlas", "The map viewpoint or folder settings are invalid.");
}
