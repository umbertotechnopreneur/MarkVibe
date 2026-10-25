/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; workspace refresh operation model; 2026-10-25
 * Human guidance: Umberto Giacobbi; sidebar refresh uses services and model-owned state
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Owns the active refresh operation independently of the sidebar control.</summary>
public sealed class WorkspaceRefreshState
{
    /// <summary>Whether the workspace service is refreshing files or updating their presentation.</summary>
    public bool IsBusy { get; set; }
}
