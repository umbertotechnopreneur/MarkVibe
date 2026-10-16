/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; managed atlas commands and complete-catalog route projection; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement all seven document atlas improvements
 * Modified with AI: OpenAI Codex; calculate full-catalog attention before bounded graph presentation; 2026-10-16
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe;

/// <summary>Projects complete-catalog graph facts and applies validated persisted atlas commands.</summary>
public partial class MainPage
{
    /// <summary>Projects routes and neighborhood distances before applying the existing visible-card limit.</summary>
    private static MapSnapshot BuildAtlasMap(Context context, string selected)
    {
        var atlas = context.Map.Session.Value.Atlas ?? new();
        var exploration = context.Map.Exploration;
        exploration.Refresh(context.Catalog);
        var map = context.Catalog.Map(selected, false, context.Map.RouteStart, context.Map.RouteEnd,
            atlas.FocusDepth, exploration.PriorityPaths());
        var visiblePaths = map.Nodes.Select(node => node.Path).ToHashSet(StringComparer.Ordinal);
        return map with
        {
            Attention = exploration.Attention.Where(item => visiblePaths.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal),
            IndexComplete = exploration.IndexComplete
        };
    }

    /// <summary>Handles atlas edits while the shared map gate is held; document contents and saved positions are untouched.</summary>
    private static void ApplyAtlasAction(Context context, JsonElement request, string action)
    {
        var state = context.Map;
        var atlas = state.Session.Value.Atlas ?? new();
        var folders = (atlas.CollapsedFolders ?? []).ToHashSet(StringComparer.Ordinal);
        var views = (atlas.Views ?? []).ToList();
        switch (action)
        {
            case "atlas-focus":
                atlas = atlas with { FocusDepth = request.GetProperty("value").GetInt32() };
                break;
            case "atlas-folder":
                var folder = request.GetProperty("folder").GetString() ?? "";
                if (!context.Catalog.Cards.Any(card => (card.Path.Contains('/') ? card.Path[..card.Path.LastIndexOf('/')] : "") == folder))
                {
                    throw new WorkspaceException("missing-map-folder", "This folder is no longer in the workspace.");
                }
                if (!folders.Add(folder))
                {
                    folders.Remove(folder);
                }
                atlas = atlas with { CollapsedFolders = folders.OrderBy(path => path, StringComparer.Ordinal).ToArray() };
                break;
            case "atlas-save-view":
                var name = (request.GetProperty("name").GetString() ?? "").Trim();
                var camera = request.GetProperty("camera").Deserialize<MapCamera>(Wire.Options)
                    ?? throw new WorkspaceException("invalid-camera", "The map camera is unavailable.");
                var viewpoint = new MapViewpoint(name, camera, state.SelectedPath, atlas.FocusDepth,
                    atlas.FolderGroups, folders.OrderBy(path => path, StringComparer.Ordinal).ToArray());
                views.RemoveAll(view => view.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (views.Count >= MapAtlas.MaximumViews)
                {
                    throw new WorkspaceException("map-view-limit", "Eight views are already saved. Delete a view or reuse its name.");
                }
                views.Add(viewpoint);
                atlas = atlas with { Views = views };
                break;
            case "atlas-delete-view":
                var deletedName = request.GetProperty("name").GetString() ?? "";
                views.RemoveAll(view => view.Name == deletedName);
                atlas = atlas with { Views = views };
                break;
            case "atlas-restore-view":
                var restoredName = request.GetProperty("name").GetString() ?? "";
                var restored = views.FirstOrDefault(view => view.Name == restoredName)
                    ?? throw new WorkspaceException("missing-map-view", "This saved view is unavailable.");
                atlas = atlas with { FocusDepth = restored.FocusDepth, FolderGroups = restored.FolderGroups,
                    CollapsedFolders = restored.CollapsedFolders };
                // Validate before publishing either the camera or the selected document.
                MapLayoutCodec.Write(state.Session.Value with { Atlas = atlas });
                if (context.Catalog.Cards.Any(card => card.Path == restored.SelectedPath))
                {
                    state.SelectedPath = restored.SelectedPath;
                }
                state.Camera = restored.Camera;
                state.CameraRevision++;
                state.RouteStart = "";
                state.RouteEnd = "";
                break;
            case "atlas-route-start":
                state.RouteStart = state.SelectedPath;
                state.RouteEnd = "";
                return;
            case "atlas-route-end":
                state.RouteEnd = state.SelectedPath;
                var route = context.Catalog.Map(state.SelectedPath, false, state.RouteStart, state.RouteEnd).Route ?? [];
                var routeFolders = route.Select(path => path.Contains('/') ? path[..path.LastIndexOf('/')] : "").ToHashSet(StringComparer.Ordinal);
                atlas = atlas with { CollapsedFolders = folders.Where(folderPath => !routeFolders.Contains(folderPath)).ToArray() };
                break;
            case "atlas-route-clear":
                state.RouteStart = "";
                state.RouteEnd = "";
                return;
            default:
                throw new WorkspaceException("unknown-action", "This map action is unavailable.");
        }
        state.Session.SetAtlas(atlas);
    }
}
