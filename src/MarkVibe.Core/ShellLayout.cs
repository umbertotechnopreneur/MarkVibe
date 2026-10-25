/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared shell layout model; 2026-10-25
 * Human guidance: Umberto Giacobbi; keep models and services separate from the UI
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Stores shell geometry only; missing preferences start with the assistant closed.</summary>
public sealed record ShellLayout(
    bool AiOpen = false,
    double AiWidth = 360,
    bool SidebarOpen = true,
    double SidebarWidth = 220,
    double SourceRatio = 0.5,
    double MapInspectorWidth = 290,
    bool ReaderExpanded = false)
{
    /// <summary>Rejects nonfinite sizes and bounds restored geometry before applying it to XAML.</summary>
    public ShellLayout Validated() => this with
    {
        AiWidth = double.IsFinite(AiWidth) ? Math.Clamp(AiWidth, 300, 600) : 360,
        SidebarWidth = double.IsFinite(SidebarWidth) ? Math.Clamp(SidebarWidth, 180, 520) : 220,
        SourceRatio = double.IsFinite(SourceRatio) ? Math.Clamp(SourceRatio, 0.2, 0.8) : 0.5,
        MapInspectorWidth = double.IsFinite(MapInspectorWidth) ? Math.Clamp(MapInspectorWidth, 220, 600) : 290
    };
}
