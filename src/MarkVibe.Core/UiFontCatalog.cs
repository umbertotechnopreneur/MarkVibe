/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; supported installed UI font choices; 2026-10-16
 * Human guidance: Umberto Giacobbi; one UI font selector with installed monospace families and the system default
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;

namespace MarkVibe.Core;

public sealed record UiFontOption(string Id, string Label);

/// <summary>Limits shell typography to known monospace families without installing or fetching fonts.</summary>
public static class UiFontCatalog
{
    public static IReadOnlyList<string> Families { get; } = LoadFamilies();

    /// <summary>Loads the same family identifiers that the browser uses to validate persisted preferences.</summary>
    private static IReadOnlyList<string> LoadFamilies()
    {
        using var stream = typeof(UiFontCatalog).Assembly.GetManifestResourceStream("MarkVibe.Core.Presentation.ui-fonts.json")
            ?? throw new InvalidOperationException("The UI font catalog is missing.");
        var families = JsonSerializer.Deserialize<string[]>(stream)
            ?? throw new InvalidOperationException("The UI font catalog is invalid.");
        return Array.AsReadOnly(families);
    }

    /// <summary>Accepts a supported family identifier or restores the separate system-default UI choice.</summary>
    public static string Normalize(string? font) => Families.FirstOrDefault(family => family.Equals(font, StringComparison.OrdinalIgnoreCase)) ?? "system";

    /// <summary>Returns only supported families present in the host's read-only installed-font inventory.</summary>
    public static IReadOnlyList<UiFontOption> Installed(IEnumerable<string> installedFamilyNames)
    {
        var installed = installedFamilyNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var options = new List<UiFontOption> { new("system", "System default") };
        options.AddRange(Families.Where(installed.Contains).Select(family => new UiFontOption(family, family)));
        return options;
    }
}
