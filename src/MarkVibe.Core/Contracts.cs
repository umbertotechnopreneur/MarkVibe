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

public static class Wire
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    /// <summary>Serializes a value using the shared camel-case browser and native wire contract.</summary>
    public static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    /// <summary>Deserializes a shared wire value and rejects an empty response.</summary>
    public static T Decode<T>(string value) => JsonSerializer.Deserialize<T>(value, Options) ?? throw new InvalidDataException("The browser returned an empty response.");
}

public sealed record WorkspaceCapabilities(
    bool CanRefresh = false,
    bool CanReopen = false,
    bool CanWriteSidecars = false,
    bool CanExport = true,
    bool CanAddFiles = false,
    bool HandleRemembered = false
);
public sealed record WorkspaceInfo(string Id, string Name, string Mode, WorkspaceCapabilities Capabilities);
public sealed record WorkspaceEntry(string Path, long Bytes, string Kind);
public sealed record AssetLease(string Url, string LeaseId, long Bytes);
public sealed record Heading(string Id, string Text, int Level, int Line);
public sealed record SourceBlock(string Id, int StartLine, int EndLine, string Quote);
public sealed record DocumentLink(string Path, string Anchor, int Line);
public sealed record ReadingPosition(string BlockId = "", double Fraction = 0, double SourceLine = 1);
public sealed record HistoryEntry(string Id, string WorkspaceId, string Path, string Anchor, ReadingPosition Position);
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
    string DarkCodeTheme = "auto"
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
        DarkCodeTheme = ReaderThemes.CodeId(DarkCodeTheme, "dark")
    };
}

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

public interface IBrowserRuntime : IAsyncDisposable
{
    /// <summary>Invokes a browser operation with JSON arguments and cancellable response handling.</summary>
    Task<string> CallAsync(string operation, string arguments = "{}", CancellationToken cancellationToken = default);
    /// <summary>Registers the asynchronous receiver for browser-originated application events.</summary>
    void OnEvent(Func<string, Task> handler);
}

public sealed class WorkspaceException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
