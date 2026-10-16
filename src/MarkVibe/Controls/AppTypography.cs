/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; inherited application UI typography; 2026-10-16
 * Human guidance: Umberto Giacobbi; change the shell, sidebar and dialogs without changing document or editor themes
 * Modified with AI: OpenAI Codex; traverse declared UserControl content through the shared UI tree; 2026-10-16
 * Human guidance: Umberto Giacobbi; consistent UI fonts and less duplicated traversal
 * Modified with AI: OpenAI Codex; semilight interface text with medium emphasis; 2026-10-16
 * Human guidance: Umberto Giacobbi; lighter, readable panel and menu typography
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Applies the installed UI family and light text hierarchy without entering document renderers or theme glyphs.</summary>
internal static class AppTypography
{
    /// <summary>Gets the installed family shared by application controls.</summary>
    public static FontFamily CurrentFamily { get; private set; } = new("Segoe UI");

    /// <summary>Retains the host-resolved UI family; unavailable and unsupported choices use the system UI default.</summary>
    public static void SetFont(string? font)
    {
        var family = UiFontCatalog.Normalize(font);
        CurrentFamily = new FontFamily(family == "system" ? "Segoe UI" : family);
    }

    /// <summary>Refreshes declared UI controls before localization, including dialogs opened after the preference changes.</summary>
    public static void Apply(DependencyObject root)
    {
        Walk(root, new HashSet<DependencyObject>());
    }

    /// <summary>Visits only declared controls and item headers, preserving theme-specific glyph fonts inside templates.</summary>
    private static void Walk(DependencyObject element, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(element))
        {
            return;
        }

        if (element is Control control)
        {
            control.FontFamily = CurrentFamily;
            control.FontWeight = InterfaceWeight(control.FontWeight);
        }
        else if (element is TextBlock text)
        {
            text.FontFamily = CurrentFamily;
            // Icon captions bind to their owning button; retain that binding for selection changes.
            if (text.GetBindingExpression(TextBlock.FontWeightProperty) == null)
            {
                text.FontWeight = InterfaceWeight(text.FontWeight);
            }
        }

        foreach (var child in UiTree.DeclaredChildren(element))
        {
            Walk(child, visited);
        }
    }

    /// <summary>Uses semilight for body text and medium for headings; repeated preference refreshes keep the same weights.</summary>
    private static FontWeight InterfaceWeight(FontWeight weight)
    {
        var isBodyText = weight == FontWeights.Normal
            || weight == FontWeights.SemiLight
            || weight == FontWeights.Light
            || weight == FontWeights.ExtraLight
            || weight == FontWeights.Thin;

        return isBodyText ? FontWeights.SemiLight : FontWeights.Medium;
    }
}
