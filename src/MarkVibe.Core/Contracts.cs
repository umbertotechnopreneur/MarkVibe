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
 * Modified with AI: OpenAI Codex; separate persisted application UI font preference; 2026-10-16
 * Modified with AI: OpenAI Codex; persist an explicit API-key or Codex CLI AI provider; 2026-10-16
 * Human guidance: Umberto Giacobbi; choose an installed monospace UI font without changing document typography
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text.Json;

namespace MarkVibe.Core;

/// <summary>Bounds document parsing, catalog growth and visible graph projection independently.</summary>
public static class Limits
{
    public const int Entries = 4096;
    public const int Documents = 1024;
    public const int DocumentBytes = 1024 * 1024;
    public const int IndexBytes = 8 * 1024 * 1024;
    public const int Blocks = 2048;
    public const int Headings = 512;
    public const int Links = 4096;
    public const int MapNodes = 120;
    public const int MapEdges = 600;
    public const int CachedDocuments = 3;
}

/// <summary>Serializes the shared camel-case JSON contract used by managed, browser and native adapters.</summary>
public static class Wire
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    /// <summary>Serializes a value using the shared camel-case browser and native wire contract.</summary>
    public static string Encode<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    /// <summary>Deserializes a shared wire value and rejects an empty response.</summary>
    public static T Decode<T>(string value)
    {
        var decoded = JsonSerializer.Deserialize<T>(value, Options);
        return decoded ?? throw new InvalidDataException("The browser returned an empty response.");
    }
}

/// <summary>Describes available provider actions; capabilities do not imply granted write permission.</summary>
public sealed record WorkspaceCapabilities(
    bool CanRefresh = false,
    bool CanReopen = false,
    bool CanWriteSidecars = false,
    bool CanExport = true,
    bool CanAddFiles = false,
    bool HandleRemembered = false,
    bool CanOpenParent = false
);

/// <summary>Identifies the active workspace and its provider-specific capabilities.</summary>
public sealed record WorkspaceInfo(string Id, string Name, string Mode, WorkspaceCapabilities Capabilities);

/// <summary>Describes a canonical workspace resource before its content is read.</summary>
public sealed record WorkspaceEntry(string Path, long Bytes, string Kind);

/// <summary>Tracks a local asset URL whose lifetime is explicitly owned and released by a view.</summary>
public sealed record AssetLease(string Url, string LeaseId, long Bytes);

/// <summary>Links a parsed heading to its stable reader anchor and one-based source line.</summary>
public sealed record Heading(string Id, string Text, int Level, int Line);

/// <summary>Maps a rendered block back to its source range and retained quote for navigation.</summary>
public sealed record SourceBlock(string Id, int StartLine, int EndLine, string Quote);

/// <summary>Identifies a resolved document link and the source line that declared it.</summary>
public sealed record DocumentLink(string Path, string Anchor, int Line);

/// <summary>Retains reader and source positions independently of the current rendering layout.</summary>
public sealed record ReadingPosition(string BlockId = "", double Fraction = 0, double SourceLine = 1);

/// <summary>Retains one navigation visit with its workspace, document, anchor and reading position.</summary>
public sealed record HistoryEntry(string Id, string WorkspaceId, string Path, string Anchor, ReadingPosition Position);

/// <summary>Stores validated presentation and AI-provider choices without retaining credentials.</summary>
public sealed record Preferences(
    string Appearance = "system",
    string DocumentStyle = "technical",
    int Zoom = 100,
    bool SimpleSource = false,
    bool RememberLastDocument = true,
    string Language = "system",
    bool Acrylic = true,
    string LightDocumentTheme = "markvibe",
    string DarkDocumentTheme = "markvibe",
    string LightCodeTheme = "auto",
    string DarkCodeTheme = "auto",
    string UiFont = "system",
    string AiProvider = AiProviders.ApiKey,
    bool UnderlineLinks = true,
    string DocumentFont = "system"
)
{
    /// <summary>Returns preferences with supported choices and bounded zoom while retaining the existing defaults.</summary>
    public Preferences Validated() => this with
    {
        Appearance = Appearance is "system" or "light" or "dark" ? Appearance : "system",
        DocumentStyle = DocumentStyle is "technical" or "editorial" or "compact" ? DocumentStyle : "technical",
        Zoom = Math.Clamp(Zoom, 80, 150),
        Language = Language == "system" || UiStrings.LanguageCodes.Contains(Language, StringComparer.Ordinal) ? Language : "system",
        LightDocumentTheme = ReaderThemes.DocumentId(LightDocumentTheme, "light"),
        DarkDocumentTheme = ReaderThemes.DocumentId(DarkDocumentTheme, "dark"),
        LightCodeTheme = ReaderThemes.CodeId(LightCodeTheme, "light"),
        DarkCodeTheme = ReaderThemes.CodeId(DarkCodeTheme, "dark"),
        UiFont = UiFontCatalog.Normalize(UiFont),
        DocumentFont = UiFontCatalog.Normalize(DocumentFont),
        AiProvider = AiProviders.Normalize(AiProvider)
    };
}

/// <summary>Stores only reader choices for one folder, independently of global application settings.</summary>
public sealed record ReaderAppearance(
    string LightDocumentTheme,
    string DarkDocumentTheme,
    string LightCodeTheme,
    string DarkCodeTheme,
    bool UnderlineLinks,
    string DocumentFont
)
{
    /// <summary>Captures a folder's effective reader choices without copying unrelated global settings.</summary>
    public static ReaderAppearance From(Preferences preferences) => new(
        preferences.LightDocumentTheme,
        preferences.DarkDocumentTheme,
        preferences.LightCodeTheme,
        preferences.DarkCodeTheme,
        preferences.UnderlineLinks,
        preferences.DocumentFont);

    /// <summary>Applies folder choices while retaining the global language, shell appearance and provider.</summary>
    public Preferences ApplyTo(Preferences preferences) => (preferences with
    {
        LightDocumentTheme = LightDocumentTheme,
        DarkDocumentTheme = DarkDocumentTheme,
        LightCodeTheme = LightCodeTheme,
        DarkCodeTheme = DarkCodeTheme,
        UnderlineLinks = UnderlineLinks,
        DocumentFont = DocumentFont
    }).Validated();
}

/// <summary>Owns bounded workspace reads, asset leases and refresh within one provider's containment rules.</summary>
public interface IWorkspaceProvider : IAsyncDisposable
{
    WorkspaceInfo Info { get; }

    /// <summary>Lists relevant workspace entries using the provider's existing containment and cancellation rules.</summary>
    Task<IReadOnlyList<WorkspaceEntry>> EnumerateAsync(CancellationToken cancellationToken);
    /// <summary>Reads a supported Markdown page within the provider's document limits.</summary>
    Task<string> ReadDocumentAsync(string path, CancellationToken cancellationToken);
    /// <summary>Acquires a local asset lease for the specified resource owner.</summary>
    Task<AssetLease> ReadAssetAsync(string path, string owner, CancellationToken cancellationToken);
    /// <summary>Releases all asset leases held by the specified owner.</summary>
    Task ReleaseAssetsAsync(string owner);
    /// <summary>Refreshes the provider's workspace view with cancellation.</summary>
    Task RefreshAsync(CancellationToken cancellationToken);
}

/// <summary>Exchanges cancellable JSON operations and application events with the browser adapter.</summary>
public interface IBrowserRuntime : IAsyncDisposable
{
    /// <summary>Invokes a browser operation with JSON arguments and cancellable response handling.</summary>
    Task<string> CallAsync(string operation, string arguments = "{}", CancellationToken cancellationToken = default);
    /// <summary>Registers the asynchronous receiver for browser-originated application events.</summary>
    void OnEvent(Func<string, Task> handler);
}

/// <summary>Carries an application error code and safe explanation across provider and presentation boundaries.</summary>
public sealed class WorkspaceException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
