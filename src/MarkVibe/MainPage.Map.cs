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
 * Modified with AI: OpenAI Codex; serialized map arrangement commands; 2026-10-16
 * Human guidance: Umberto Giacobbi; star, circle and grid layouts around the most-connected document
 * Modified with AI: OpenAI Codex; managed map selection without document navigation; 2026-10-16
 * Human guidance: Umberto Giacobbi; select cards on one click and open documents on double-click
 * Modified with AI: OpenAI Codex; serialized card resizing and map reset; 2026-10-16
 * Human guidance: Umberto Giacobbi; resize map cards and restore their default arrangement and appearance
 * Modified with AI: OpenAI Codex; serialize atlas state, route selection and viewpoint persistence; 2026-10-16
 * Human guidance: Umberto Giacobbi; implement the seven approved document atlas improvements
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text.Json;
using MarkVibe.Core;
using MarkVibe.Workspace;

namespace MarkVibe;
public partial class MainPage
{
    private readonly SemaphoreSlim _mapGate = new(1, 1);
    private sealed record MapCopyResult(bool Saved);
    private sealed class MapWorkspaceState
    {
        public MapLayoutSession Session { get; set; } = new();
        public string Revision { get; set; } = "";
        public string Message { get; set; } = "";
        public bool HasError { get; set; }
        public bool CanWrite { get; set; }
        public bool CanEnableWrite { get; set; }
        public bool Invalid { get; set; }
        public bool Busy { get; set; }
        public string StateId { get; } = Guid.NewGuid().ToString("N");
        public int Generation { get; set; }
        public int ArrangementRevision { get; set; }
        public bool FolderGroups
        {
            get => Session.Value.Atlas?.FolderGroups ?? false;
            set => Session.SetAtlas((Session.Value.Atlas ?? new()) with { FolderGroups = value });
        }
        public string RouteStart { get; set; } = "";
        public string RouteEnd { get; set; } = "";
        public MapCamera? Camera { get; set; }
        public int CameraRevision { get; set; }
        public string SelectedPath { get; set; } = "";
    }

    /// <summary>Loads the persistent map layout and preserves unsupported originals and file revision state.</summary>
    private static async Task<MapWorkspaceState> LoadMapAsync(BrowserWorkspaceProvider provider, DocumentCatalog catalog, CancellationToken cancellation)
    {
        var state = new MapWorkspaceState();
        try
        {
            var stored = await BrowserServices.CallAsync<MapFileState>("map.read", new { workspaceId = provider.Info.Id }, cancellation);
            state.Revision = stored.Revision;
            state.CanWrite = stored.CanWrite;
            state.CanEnableWrite = stored.CanEnableWrite;
            if (stored.Json != null)
            {
                state.Session = new(MapLayoutCodec.Read(stored.Json));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            state.Invalid = true;
            state.HasError = true;
            state.Message = error is WorkspaceException ? error.Message : "The map layout could not be read. The original file is preserved; download a separate copy of your changes.";
        }

        state.Session.EnsureDocuments(catalog.Cards);
        return state;
    }

    /// <summary>Builds the browser map state from the managed layout, save capabilities and available styles.</summary>
    private object MapPresentation(MapWorkspaceState state, string acknowledged = "", string rejected = "") => new
    {
        layout = state.Session.Value,
        palette = MapLayoutCodec.Colors,
        icons = MapLayoutCodec.Icons,
        metrics = new
        {
            width = MapLayoutCodec.CardWidth,
            height = MapLayoutCodec.CardHeight,
            minimumWidth = MapLayoutCodec.MinimumCardWidth,
            minimumHeight = MapLayoutCodec.MinimumCardHeight,
            maximumWidth = MapLayoutCodec.MaximumCardWidth,
            maximumHeight = MapLayoutCodec.MaximumCardHeight,
            maximumCoordinate = MapLayoutCodec.MaximumCoordinate
        },
        defaultColor = MapLayoutCodec.DefaultColor,
        defaultIcon = MapLayoutCodec.DefaultIcon,
        readOnly = false,
        dirty = state.Session.Dirty,
        busy = state.Busy,
        invalid = state.Invalid,
        canWrite = state.CanWrite && !state.Invalid,
        canEnableWrite = state.CanEnableWrite && !state.Invalid,
        message = state.Message,
        hasError = state.HasError,
        acknowledged,
        rejected,
        stateId = state.StateId,
        arrangementRevision = state.ArrangementRevision,
        folderGroups = state.FolderGroups,
        focusDepth = state.Session.Value.Atlas?.FocusDepth ?? 0,
        routeStart = state.RouteStart,
        routeEnd = state.RouteEnd,
        camera = state.Camera,
        cameraRevision = state.CameraRevision,
        aiContextPaths = AiContextPaths(),
        aiContextBusy = _aiContextBusy || _aiBusy,
        generation = ++state.Generation
    };
    /// <summary>Publishes the active workspace's current map session state to the browser host.</summary>
    private Task RenderMapStateAsync(Context context, string acknowledged = "", string rejected = "") => _contexts.TryGetValue(context.Provider.Info.Id, out var current) && ReferenceEquals(current, context) ? MapHost.DisplayStateAsync(context.Provider.Info.Id, MapPresentation(context.Map, acknowledged, rejected)) : Task.CompletedTask;

    /// <summary>Searches titles and paths across the complete catalog, including cards outside the graph's display limit.</summary>
    private IReadOnlyList<DocumentCard> FindMapDocuments(string workspaceId, string query)
    {
        if (_view != "map" || _active == null || _active.Provider.Info.Id != workspaceId || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var terms = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return _active.Catalog.Cards
            .Where(card => terms.All(term => card.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || card.Path.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(card => card.Title.Equals(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(card => card.Title.StartsWith(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .ThenBy(card => card.Path, StringComparer.Ordinal)
            .Take(20)
            .ToArray();
    }

    /// <summary>Selects a catalog search result before projecting the graph, then requests cancellable animated centering.</summary>
    private async Task CenterMapDocumentAsync(string workspaceId, string path)
    {
        var context = _active;
        if (_view != "map" || context == null || context.Provider.Info.Id != workspaceId)
        {
            return;
        }

        // Edit acknowledgments acquire this gate, so pending browser operations must finish first.
        await BrowserServices.CallAsync("map.flush");
        await _mapGate.WaitAsync();
        try
        {
            if (_view != "map" || !ReferenceEquals(context, _active)
                || !_contexts.TryGetValue(workspaceId, out var current) || !ReferenceEquals(current, context)
                || !context.Catalog.Cards.Any(card => card.Path == path))
            {
                return;
            }

            context.Map.SelectedPath = path;
            var atlas = context.Map.Session.Value.Atlas;
            var folder = path.Contains('/') ? path[..path.LastIndexOf('/')] : "";
            if (atlas?.CollapsedFolders?.Contains(folder, StringComparer.Ordinal) == true)
            {
                context.Map.Session.SetAtlas(atlas with { CollapsedFolders = atlas.CollapsedFolders.Where(value => value != folder).ToArray() });
                if (context.Map.CanWrite && !context.Map.Invalid)
                {
                    try
                    {
                        await SaveMapAsync(context);
                    }
                    catch (Exception error)
                    {
                        // Keep the expanded selection usable and publish the unsaved state if its revision check fails.
                        context.Map.HasError = true;
                        context.Map.Message = error is WorkspaceException ? error.Message
                            : "The map could not be saved. Your changes remain available; download a JSON copy before reopening.";
                    }
                }
            }
            await MapHost.DisplaySelectionAsync(BuildAtlasMap(context, path), path, workspaceId,
                MapPresentation(context.Map), context.Map.Session.Value.Atlas?.FocusDepth > 0, center: true);
        }
        finally
        {
            _mapGate.Release();
        }
    }

    /// <summary>Selects a visible graph card without loading its document or changing reading history.</summary>
    private async Task HandleMapSelectionAsync(JsonElement request)
    {
        if (_view != "map" || _active == null
            || !request.TryGetProperty("workspaceId", out var workspaceId)
            || !request.TryGetProperty("stateId", out var stateId)
            || !request.TryGetProperty("path", out var pathValue))
        {
            return;
        }

        var context = _active;
        var path = pathValue.GetString() ?? "";
        if (workspaceId.GetString() != context.Provider.Info.Id
            || stateId.GetString() != context.Map.StateId)
        {
            return;
        }

        // Drain browser edits before taking the managed gate: their acknowledgments need this same gate.
        await BrowserServices.CallAsync("map.flush");
        await _mapGate.WaitAsync();
        try
        {
            // A folder or view can change while pending layout writes are finishing.
            if (_view != "map" || !ReferenceEquals(context, _active)
                || !_contexts.TryGetValue(context.Provider.Info.Id, out var current)
                || !ReferenceEquals(current, context)
                || !context.Catalog.Cards.Any(card => card.Path == path))
            {
                return;
            }

            context.Map.SelectedPath = path;
            var map = BuildAtlasMap(context, path);
            await MapHost.DisplaySelectionAsync(
                map,
                path,
                context.Provider.Info.Id,
                MapPresentation(context.Map),
                context.Map.Session.Value.Atlas?.FocusDepth > 0,
                center: request.TryGetProperty("center", out var center) && center.GetBoolean()
            );
        }
        finally
        {
            _mapGate.Release();
        }
    }

    /// <summary>Serializes map commands and rejects events from obsolete workspace or layout sessions.</summary>
    private async Task HandleMapAsync(JsonElement request)
    {
        await _mapGate.WaitAsync();
        try
        {
            var workspaceId = request.GetProperty("workspaceId").GetString() ?? "";
            if (!_contexts.TryGetValue(workspaceId, out var context))
            {
                return;
            }

            var state = context.Map;
            // A reopened folder has the same workspace id but a different layout session.
            if (!request.TryGetProperty("stateId", out var stateId) || stateId.GetString() != state.StateId)
            {
                return;
            }

            var acknowledged = request.TryGetProperty("changeId", out var changeId) ? changeId.GetString() ?? "" : "";
            var action = request.GetProperty("action").GetString();
            var rejected = "";
            var mutationCommitted = false;
            state.Busy = true;
            try
            {
                if (action?.StartsWith("atlas-", StringComparison.Ordinal) == true
                    || action is "move" or "style" or "resize" or "map-reset" or "arrange-star" or "arrange-circle" or "arrange-grid" or "arrange-tree" or "arrange-folders")
                {
                    if (action.StartsWith("atlas-", StringComparison.Ordinal))
                    {
                        ApplyAtlasAction(context, request, action);
                    }
                    else if (action == "map-reset")
                    {
                        state.Session.Reset(context.Catalog.Cards);
                        state.FolderGroups = false;
                        state.RouteStart = "";
                        state.RouteEnd = "";
                        state.Camera = null;
                        state.CameraRevision++;
                        state.ArrangementRevision++;
                    }
                    else if (action is "arrange-star" or "arrange-circle" or "arrange-grid" or "arrange-tree" or "arrange-folders")
                    {
                        // Presets use the full catalog, so changing a filter never changes persistent arrangement semantics.
                        state.Session.Arrange(context.Catalog.Cards, action["arrange-".Length..]);
                        state.FolderGroups = action == "arrange-folders";
                        state.ArrangementRevision++;
                    }
                    else
                    {
                        var path = request.GetProperty("path").GetString() ?? "";
                        if (!context.Catalog.Cards.Any(card => card.Path == path))
                        {
                            throw new WorkspaceException("missing-map-card", "This page is no longer in the workspace.");
                        }

                        if (action == "move")
                        {
                            state.Session.Move(path, request.GetProperty("x").GetDouble(), request.GetProperty("y").GetDouble());
                        }
                        else if (action == "resize")
                        {
                            state.Session.Resize(path, request.GetProperty("width").GetDouble(), request.GetProperty("height").GetDouble());
                        }
                        else
                        {
                            state.Session.Style(path, request.GetProperty("color").GetString() ?? "", request.GetProperty("icon").GetString() ?? "");
                        }
                    }

                    mutationCommitted = true;
                    if (!state.Invalid)
                    {
                        state.Message = "";
                        state.HasError = false;
                    }

                    if ((action.StartsWith("atlas-", StringComparison.Ordinal) || action == "map-reset") && ReferenceEquals(context, _active))
                    {
                        await MapHost.DisplaySelectionAsync(BuildAtlasMap(context, state.SelectedPath), state.SelectedPath,
                            workspaceId, MapPresentation(state, acknowledged), state.Session.Value.Atlas?.FocusDepth > 0);
                    }
                    else
                    {
                        await RenderMapStateAsync(context, acknowledged);
                    }
                    if (state.CanWrite && !state.Invalid && state.Session.Dirty)
                    {
                        await SaveMapAsync(context);
                    }
                }
                else if (action == "save")
                {
                    if (state.Invalid)
                    {
                        throw new WorkspaceException("invalid-map", "Download a copy of your layout; the original file is preserved.");
                    }

                    var access = await BrowserServices.CallAsync<MapFileState>("map.access", new { workspaceId });
                    state.CanWrite = access.CanWrite;
                    state.CanEnableWrite = access.CanEnableWrite;
                    if (!state.CanWrite)
                    {
                        throw new WorkspaceException("map-permission", "Allow folder write access to save the layout, or download a JSON copy.");
                    }

                    await SaveMapAsync(context, force: true);
                }
                else if (action == "download")
                {
                    var copy = await BrowserServices.CallAsync<MapCopyResult>("map.download", new { workspaceId, json = MapLayoutCodec.Write(state.Session.Value), filename = MapLayoutCodec.FileName });
                    // A downloaded copy is the persistence route for imported workspaces.
                    if (!state.CanWrite && !state.CanEnableWrite)
                    {
                        state.Session.Saved();
                    }

                    state.Message = (copy.Saved ? "Layout copy saved" : "Layout download started") + " · keep .markvibe-map.json in the top-level folder";
                    state.HasError = false;
                }
                else if (action == "discard")
                {
                    var loaded = await LoadMapAsync(context.Provider, context.Catalog, CancellationToken.None);
                    state.Session = loaded.Session;
                    state.Revision = loaded.Revision;
                    state.Message = loaded.Message;
                    state.CanWrite = loaded.CanWrite;
                    state.CanEnableWrite = loaded.CanEnableWrite;
                    state.Invalid = loaded.Invalid;
                    state.HasError = loaded.HasError;
                    state.RouteStart = "";
                    state.RouteEnd = "";
                    state.Camera = null;
                    state.CameraRevision++;
                }
                else
                {
                    throw new WorkspaceException("unknown-action", "This map action is unavailable.");
                }
            }
            catch (Exception error)
            {
                // A committed arrangement remains accepted if persistence fails; only the save is pending.
                if (!mutationCommitted && action is ("move" or "style" or "resize" or "map-reset" or "arrange-star" or "arrange-circle" or "arrange-grid" or "arrange-tree" or "arrange-folders"))
                {
                    rejected = acknowledged;
                }

                state.Message = error is WorkspaceException ? error.Message : "The map could not be saved. Your changes remain available; download a JSON copy before reopening.";
                state.HasError = true;
            }
            finally
            {
                state.Busy = false;
                if (action == "discard" && !state.HasError && ReferenceEquals(context, _active))
                {
                    await MapHost.DisplaySelectionAsync(BuildAtlasMap(context, state.SelectedPath), state.SelectedPath,
                        workspaceId, MapPresentation(state), state.Session.Value.Atlas?.FocusDepth > 0);
                }
                await RenderMapStateAsync(context, acknowledged, rejected);
                if (context == _active)
                {
                    SetStatus(state.Message.Length > 0 ? state.Message : state.Session.Dirty ? "Unsaved map changes · save or download the layout" : "Map layout saved in .markvibe-map.json");
                }
            }
        }
        finally
        {
            _mapGate.Release();
        }
    }

    /// <summary>Writes or downloads a validated map layout while preserving revisions and unsaved edits on failure.</summary>
    private async Task SaveMapAsync(Context context, bool force = false)
    {
        var state = context.Map;
        if (!force && !state.Session.Dirty)
        {
            return;
        }

        if (state.Invalid)
        {
            throw new WorkspaceException("invalid-map", "The original map layout is preserved. Download your changes as a separate copy.");
        }

        var json = MapLayoutCodec.Write(state.Session.Value);
        state.Revision = await BrowserServices.CallAsync<string>("map.write", new { workspaceId = context.Provider.Info.Id, revision = state.Revision, json });
        state.Session.Saved();
        state.Message = "Layout saved · .markvibe-map.json";
        state.HasError = false;
    }

    /// <summary>Drains map edits and handles pending save requirements before navigation or workspace replacement.</summary>
    private async Task FlushMapAsync(bool changingWorkspace = false)
    {
        await BrowserServices.CallAsync("map.flush");
        await _mapGate.WaitAsync();
        try
        {
            if (_active?.Map.Session.Dirty != true)
            {
                return;
            }

            var state = _active.Map;
            if (state.CanWrite && !state.Invalid)
            {
                await SaveMapAsync(_active);
                await RenderMapStateAsync(_active);
            }
            else if (changingWorkspace)
            {
                throw new WorkspaceException("map-unsaved", "Save or download your map layout before opening another workspace, or discard its changes in Map.");
            }
        }
        finally
        {
            _mapGate.Release();
        }
    }
}
