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
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    public static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Decode<T>(string value) => JsonSerializer.Deserialize<T>(value, Options) ?? throw new InvalidDataException("The browser returned an empty response.");
}

public sealed record WorkspaceCapabilities(bool CanRefresh = false, bool CanReopen = false, bool CanWriteSidecars = false, bool CanExport = true, bool CanAddFiles = false, bool HandleRemembered = false);
public sealed record WorkspaceInfo(string Id, string Name, string Mode, WorkspaceCapabilities Capabilities);
public sealed record WorkspaceEntry(string Path, long Bytes, string Kind);
public sealed record AssetLease(string Url, string LeaseId, long Bytes);
public sealed record Heading(string Id, string Text, int Level, int Line);
public sealed record SourceBlock(string Id, int StartLine, int EndLine, string Quote);
public sealed record DocumentLink(string Path, string Anchor, int Line);
public sealed record ReadingPosition(string BlockId = "", double Fraction = 0, double SourceLine = 1);
public sealed record HistoryEntry(string Id, string WorkspaceId, string Path, string Anchor, ReadingPosition Position);
public sealed record Preferences(string Appearance = "system", string DocumentStyle = "technical", int Zoom = 100, bool SimpleSource = false, bool RememberLastDocument = true)
{
    public Preferences Validated() => this with
    {
        Appearance = Appearance is "system" or "light" or "dark" ? Appearance : "system",
        DocumentStyle = DocumentStyle is "technical" or "editorial" or "compact" ? DocumentStyle : "technical",
        Zoom = Math.Clamp(Zoom, 80, 150)
    };
}

public interface IWorkspaceProvider : IAsyncDisposable
{
    WorkspaceInfo Info { get; }
    Task<IReadOnlyList<WorkspaceEntry>> EnumerateAsync(CancellationToken cancellationToken);
    Task<string> ReadDocumentAsync(string path, CancellationToken cancellationToken);
    Task<AssetLease> ReadAssetAsync(string path, string owner, CancellationToken cancellationToken);
    Task ReleaseAssetsAsync(string owner);
    Task RefreshAsync(CancellationToken cancellationToken);
}

public interface IBrowserRuntime : IAsyncDisposable
{
    Task<string> CallAsync(string operation, string arguments = "{}", CancellationToken cancellationToken = default);
    void OnEvent(Func<string, Task> handler);
}

public sealed class WorkspaceException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
