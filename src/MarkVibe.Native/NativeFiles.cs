using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Native;

public sealed record NativeSession(string Root, HistoryEntry? Entry, bool Remember = true);
public sealed record SidecarRead(string? Json, string Revision);

public static class AtomicFile
{
    public static async Task WriteAsync(string destination, Stream source, CancellationToken token = default)
    {
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destination))!, ".markvibe-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            { await source.CopyToAsync(output, token); await output.FlushAsync(token); output.Flush(true); }
            token.ThrowIfCancellationRequested();
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static async Task WriteTextAsync(string destination, string text, CancellationToken token = default)
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await WriteAsync(destination, source, token);
    }
}

public sealed class NativeFiles
{
    private readonly Dictionary<string, string> _roots = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _writes = new(1, 1);
    private readonly string _sessionFile;
    public NativeSession? Session { get; private set; }
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public NativeFiles(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory); _sessionFile = Path.Combine(dataDirectory, "session.json");
        try
        {
            if (File.Exists(_sessionFile) && new FileInfo(_sessionFile).Length < 20000)
            {
                var loaded = Wire.Decode<NativeSession>(File.ReadAllText(_sessionFile, Utf8));
                if (loaded.Remember && loaded.Root != null && Directory.Exists(loaded.Root))
                {
                    _ = Open(loaded.Root);
                    if (loaded.Entry != null && loaded.Entry.WorkspaceId == Id(loaded.Root) && WorkspacePath.Normalize(loaded.Entry.Path, true) == loaded.Entry.Path)
                        Session = loaded;
                }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or WorkspaceException or ArgumentException or InvalidDataException) { Session = null; }
    }
    public WorkspaceInfo Open(string root)
    {
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if (!Directory.Exists(root) || root.StartsWith("\\\\", StringComparison.Ordinal)) throw Error("invalid-folder", "Choose a local documentation folder.");
        RejectLinks(root); var id = Id(root); _roots[id] = root;
        return new(id, Path.GetFileName(root), "native", new(CanRefresh: true, CanReopen: true, CanWriteSidecars: true));
    }
    public WorkspaceInfo Resume() => Session != null ? Open(Session.Root) : throw Error("missing-folder", "Open the last document's folder again.");
    public string Root(string id) => _roots.TryGetValue(id, out var root) ? root : throw Error("workspace-unavailable", "Open this folder again.");
    public IReadOnlyList<WorkspaceEntry> Entries(string id)
    {
        var root = Root(id); RejectLinks(root);
        var entries = new List<WorkspaceEntry>(); var queue = new Queue<(string Path, int Depth)>(); queue.Enqueue((root, 0));
        var visited = 0; var directories = 0; var documents = 0;
        while (queue.TryDequeue(out var folder))
        {
            if (++directories > 1024) throw Error("folder-too-large", "Choose a smaller documentation folder.");
            foreach (var entry in new DirectoryInfo(folder.Path).EnumerateFileSystemInfos())
            {
                if (++visited > 10000) throw Error("folder-too-large", "Choose a smaller documentation folder.");
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                var path = Path.GetRelativePath(root, entry.FullName).Replace('\\', '/');
                if (WorkspacePath.IsIgnored(path)) continue;
                if (entry is DirectoryInfo) { if (folder.Depth < 16) queue.Enqueue((entry.FullName, folder.Depth + 1)); continue; }
                if (!Relevant(path)) continue;
                _ = Resolve(id, path);
                var kind = WorkspacePath.IsMarkdown(path) ? "document" : Sidecar(path) ? "sidecar" : "asset";
                if (kind == "document" && ++documents > Limits.Documents || entries.Count >= Limits.Entries)
                    throw Error("folder-too-large", "Choose a folder with fewer than 1,024 pages and 4,096 relevant files.");
                entries.Add(new(path, ((FileInfo)entry).Length, kind));
            }
        }
        return entries.OrderBy(e => e.Path, StringComparer.Ordinal).ToArray();
    }
    public async Task<byte[]> ReadAsync(string id, string path, CancellationToken token = default)
    {
        if (!Relevant(path)) throw Error("unsupported-file", "Only documentation and local images can be read.");
        var full = Resolve(id, path);
        var maximum = WorkspacePath.IsMarkdown(path) || Sidecar(path) ? Limits.DocumentBytes : 8 * 1048576;
        await using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous);
        if (stream.Length > maximum) throw Error("file-too-large", "This file exceeds the reading limit.");
        var bytes = new byte[checked((int)stream.Length)]; await stream.ReadExactlyAsync(bytes, token); return bytes;
    }
    public async Task<SidecarRead> ReadSidecarAsync(string id, string document, CancellationToken token = default)
    {
        if (!WorkspacePath.IsMarkdown(document)) throw Error("invalid-sidecar", "Annotations require a Markdown page.");
        var file = Resolve(id, document + ".ink.json", true);
        if (!File.Exists(file)) return new(null, "");
        var bytes = await ReadAsync(id, document + ".ink.json", token);
        return new(Utf8.GetString(bytes), Hash(bytes));
    }
    public async Task<string> WriteSidecarAsync(string id, string document, string documentHash, string revision, string json, CancellationToken token = default)
    {
        var ink = InkCodec.Read(json);
        if (ink.Document.Path != document || ink.Document.ContentHash != documentHash) throw Error("ink-conflict", "Annotation metadata does not match the open page.");
        await _writes.WaitAsync(token);
        try
        {
            // Keep the Markdown file read-locked until the sidecar is committed.
            await using var page = new FileStream(Resolve(id, document), FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous);
            if (page.Length > Limits.DocumentBytes) throw Error("file-too-large", "This page exceeds the reading limit.");
            var bytes = new byte[(int)page.Length]; await page.ReadExactlyAsync(bytes, token);
            var text = Utf8.GetString(bytes); if (text.StartsWith('\uFEFF')) text = text[1..];
            if (MarkdownParser.Fingerprint(text) != documentHash) throw Error("ink-conflict", "The Markdown changed on disk. Reload it before saving ink. Your edits are still available.");
            var existing = await ReadSidecarAsync(id, document, token);
            if (existing.Revision != revision) throw Error("ink-conflict", "The annotation file changed on disk. Export your ink before reloading; the original was preserved.");
            var destination = Resolve(id, document + ".ink.json", true);
            await AtomicFile.WriteTextAsync(destination, json, token);
            return Hash(Encoding.UTF8.GetBytes(json));
        }
        finally { _writes.Release(); }
    }
    public async Task RememberAsync(HistoryEntry? entry, bool enabled, CancellationToken token = default)
    {
        await _writes.WaitAsync(token);
        try
        {
            if (!enabled) { Session = null; if (File.Exists(_sessionFile)) File.Delete(_sessionFile); return; }
            if (entry == null || !_roots.TryGetValue(entry.WorkspaceId, out var root)) return;
            _ = Resolve(entry.WorkspaceId, entry.Path);
            Session = new(root, entry);
            await AtomicFile.WriteTextAsync(_sessionFile, Wire.Encode(Session), token);
        }
        finally { _writes.Release(); }
    }
    public string Resolve(string id, string path, bool allowMissingLeaf = false)
    {
        if (WorkspacePath.Normalize(path, true) != path || WorkspacePath.IsIgnored(path)) throw Error("invalid-path", "This path is outside the documentation workspace.");
        foreach (var segment in path.Split('/'))
            if (segment.EndsWith('.') || segment.EndsWith(' ') || segment.IndexOfAny(['<', '>', '"', '|', '?', '*']) >= 0 ||
                System.Text.RegularExpressions.Regex.IsMatch(segment, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\\.|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                throw Error("invalid-path", "This filename is unsafe on Windows.");
        var root = Root(id); var full = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw Error("outside-workspace", "This resource is outside the opened folder.");
        RejectLinks(allowMissingLeaf && !File.Exists(full) ? Path.GetDirectoryName(full)! : full); return full;
    }
    private static void RejectLinks(string path)
    {
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw Error("unsafe-link", "Symbolic links and junctions are excluded from native file access.");
    }
    private static string Id(string root) => "native:" + Hash(Encoding.UTF8.GetBytes(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)).ToUpperInvariant()));
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static bool Sidecar(string path) => path.EndsWith(".ink.json", StringComparison.OrdinalIgnoreCase) && WorkspacePath.IsMarkdown(path[..^9]);
    private static bool Relevant(string path) => WorkspacePath.IsMarkdown(path) || Sidecar(path) || Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg";
    private static WorkspaceException Error(string code, string message) => new(code, message);
}
