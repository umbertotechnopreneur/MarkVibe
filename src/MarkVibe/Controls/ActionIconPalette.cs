/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; semantic action icon colors; 2026-10-23
 * Human guidance: Umberto Giacobbi; distinguish toolbar, graph, PDF, code, settings and information icons
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Media;

namespace MarkVibe.Controls;

/// <summary>Assigns shared theme-aware colors to action glyphs while leaving captions and custom artwork alone.</summary>
internal static class ActionIconPalette
{
    /// <summary>Returns a semantic brush for known actions, or null to retain the owner's existing foreground.</summary>
    internal static Brush? ForSymbol(string symbol)
    {
        var resource = symbol switch
        {
            "pdf" or "delete" => "IconCoral",
            "code" or "html" or "layout-circle" or "fit" => "IconTeal",
            "settings" or "folder" or "archive" or "import" or "layout-star" => "IconGold",
            "about" or "file" or "layout-grid" or "external" or "map-globe" => "IconBlue",
            "book" or "outline" or "save" or "download" or "review" or "refresh" => "IconGreen",
            "map" or "split" or "pages" or "sparkles" or "link" => "IconPurple",
            _ => null
        };

        return resource == null ? null : (Brush)Application.Current.Resources[resource];
    }
}
