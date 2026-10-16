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
using System.Globalization;
using System.Text.Json;

namespace MarkVibe.Core;
/// <summary>One local catalog shared by the OpenSilver, browser and Windows UI.</summary>
public static class UiStrings
{
    private sealed record Catalog(string[] Languages, Dictionary<string, string[]> Strings, Dictionary<string, string> Aliases);
    private static readonly Catalog Messages = Load();
    public static readonly string[] LanguageCodes = ["en", "it", "fr", "vi", "zh-CN", "yue-HK", "id", "ko", "ja"];
    public static string Language { get; private set; } = "en";

    public static event Action? Changed;
    /// <summary>Load the embedded catalog once and fail clearly if packaging omitted it.</summary>
    private static Catalog Load()
    {
        using var stream = typeof(UiStrings).Assembly.GetManifestResourceStream("MarkVibe.Core.Localization.strings.json") ?? throw new InvalidOperationException("The UI language catalog is missing.");
        return JsonSerializer.Deserialize<Catalog>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("The UI language catalog is invalid.");
    }

    /// <summary>Resolve a preference or host locale to a supported language, with English as fallback.</summary>
    public static string ResolveLanguage(string? preference, string? systemLanguage = null)
    {
        var candidateLanguage = preference is null or "system" ? systemLanguage ?? CultureInfo.CurrentUICulture.Name : preference;
        if (candidateLanguage.StartsWith("yue", StringComparison.OrdinalIgnoreCase) ||
            candidateLanguage.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
            candidateLanguage.Equals("zh-MO", StringComparison.OrdinalIgnoreCase))
        {
            return "yue-HK";
        }

        if (candidateLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zh-CN";
        }

        var neutralLanguage = candidateLanguage.Split('-')[0].ToLowerInvariant();
        return LanguageCodes.Contains(neutralLanguage, StringComparer.Ordinal) ? neutralLanguage : "en";
    }

    /// <summary>Publish a language change only when its resolved catalog language differs.</summary>
    public static void SetLanguage(string preference, string? systemLanguage = null)
    {
        var language = ResolveLanguage(preference, systemLanguage);
        if (Language == language)
        {
            return;
        }

        Language = language;
        Changed?.Invoke();
    }

    /// <summary>Identify catalog-backed labels without treating arbitrary document text as UI text.</summary>
    public static bool Contains(string key)
    {
        return Messages.Strings.ContainsKey(key) || Messages.Aliases.ContainsKey(key);
    }

    /// <summary>Translate a key or alias and format its optional arguments using invariant culture.</summary>
    public static string T(string key, params object? [] values)
    {
        var originalKey = key;
        if (Messages.Aliases.TryGetValue(key, out var alias))
        {
            key = alias;
        }

        var languageIndex = Array.IndexOf(Messages.Languages, Language);
        var translatedText = languageIndex >= 0 && Messages.Strings.TryGetValue(key, out var translations) && languageIndex < translations.Length ? translations[languageIndex] : originalKey;
        return values.Length == 0 ? translatedText : string.Format(CultureInfo.InvariantCulture, translatedText, values);
    }
}
