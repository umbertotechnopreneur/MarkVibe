/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; graph selection and managed link updates; 2026-10-16
 * Human guidance: Umberto Giacobbi; single-click selection and double-click document navigation
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using MarkVibe.Core;
using System.Text.Json;

namespace MarkVibe.Controls;

/// <summary>Routes document selection to an existing graph while recognizing a replacement workspace session.</summary>
public partial class DocumentMapView
{
    /// <summary>Refreshes selection and link navigation while preserving the renderer's current camera.</summary>
    public Task DisplaySelectionAsync(MapSnapshot map, string selected, string workspaceId, object state, bool focused, bool center = false)
    {
        var previousWorkspace = _workspaceId;
        var previousStateId = _state.ValueKind == JsonValueKind.Object
            && _state.TryGetProperty("stateId", out var stateId)
                ? stateId.GetString()
                : null;

        PreparePresentation(map, selected, workspaceId, state, focused);

        // Reopening even the same folder creates a new layout session; it must replace the old graph.
        var sameSession = previousWorkspace == workspaceId
            && previousStateId == _state.GetProperty("stateId").GetString();
        var operation = sameSession ? "map.selection" : "map.render";
        return BrowserServices.CallAsync(operation, new
        {
            hostId = HostId,
            map,
            selected,
            workspaceId,
            state,
            focused,
            advanced = _advanced,
            depth = _depth,
            center
        });
    }
}
