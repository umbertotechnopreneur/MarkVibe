/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; graph selection and managed link updates; 2026-10-16
 * Human guidance: Umberto Giacobbi; single-click selection and double-click document navigation
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using MarkVibe.Core;

namespace MarkVibe.Controls;

public partial class DocumentMapView
{
    /// <summary>Refreshes selection and link navigation while preserving the renderer's current camera.</summary>
    public Task DisplaySelectionAsync(MapSnapshot map, string selected, string workspaceId, object state, bool focused, bool center = false)
    {
        PreparePresentation(map, selected, workspaceId, state, focused);
        return BrowserServices.CallAsync("map.selection", new { hostId = HostId, map, selected, workspaceId, state, focused, advanced = _advanced, depth = _depth, center });
    }
}
