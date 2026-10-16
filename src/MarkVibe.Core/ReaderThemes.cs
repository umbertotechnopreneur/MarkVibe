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
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text.Json;

namespace MarkVibe.Core;
/// <summary>A stable theme choice shared by settings and the browser presentation catalog.</summary>
public sealed record ReaderThemeChoice(string Id, string Label, string[] Modes);
/// <summary>Read and validate mode-specific theme IDs from the shared embedded catalog.</summary>
public static class ReaderThemes
{
    private sealed record Catalog(ReaderThemeChoice[] Document, ReaderThemeChoice[] Code);
    private static readonly Catalog Choices = Load();
    /// <summary>Load the browser's theme catalog from its embedded Core resource.</summary>
    private static Catalog Load()
    {
        using var stream = typeof(ReaderThemes).Assembly.GetManifestResourceStream("MarkVibe.Core.Presentation.themes.json") ?? throw new InvalidOperationException("The reader theme catalog is missing.");
        return JsonSerializer.Deserialize<Catalog>(stream, Wire.Options) ?? throw new InvalidOperationException("The reader theme catalog is invalid.");
    }

    /// <summary>Enumerate document themes available in the requested appearance mode.</summary>
    public static IEnumerable<ReaderThemeChoice> Documents(string mode)
    {
        return Choices.Document.Where(theme => theme.Modes.Contains(mode));
    }

    /// <summary>Enumerate code themes available in the requested appearance mode.</summary>
    public static IEnumerable<ReaderThemeChoice> Code(string mode)
    {
        return Choices.Code.Where(theme => theme.Modes.Contains(mode));
    }

    /// <summary>Keep a supported document ID or restore the MarkVibe default for that mode.</summary>
    public static string DocumentId(string? id, string mode)
    {
        return Documents(mode).Any(theme => theme.Id == id) ? id! : "markvibe";
    }

    /// <summary>Keep a supported code ID or restore automatic matching for that mode.</summary>
    public static string CodeId(string? id, string mode)
    {
        return Code(mode).Any(theme => theme.Id == id) ? id! : "auto";
    }
}
