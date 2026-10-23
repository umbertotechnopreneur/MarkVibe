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
 * Modified with AI: OpenAI Codex; stop importing retired annotation sidecars; 2026-10-16
 * Human guidance: Umberto Giacobbi; remove unreleased legacy compatibility without deleting user files
 * Modified with AI: OpenAI Codex; preserve the validated read size during concurrent editor writes; 2026-10-23
 * Human guidance: Umberto Giacobbi; review and deliver the performance improvements
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Native;
public sealed record NativeSession(string Root, HistoryEntry? Entry, bool Remember = true);
public static class AtomicFile
{
    /// <summary>Writes a temporary sibling file and atomically replaces the destination after a completed cancellable write.</summary>
    public static async Task WriteAsync(string destination, Stream source, CancellationToken token = default)
    {
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destination))!, ".markvibe-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await source.CopyToAsync(output, token);
                await output.FlushAsync(token);
                output.Flush(true);
            }

            token.ThrowIfCancellationRequested();
            File.Move(temporary, destination, true);
            // The sibling temporary file keeps the replacement on the destination filesystem.
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>Encodes text as UTF-8 and commits it through the atomic file writer.</summary>
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
    /// <summary>Initializes session storage and restores only valid remembered local workspace metadata.</summary>
    public NativeFiles(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _sessionFile = Path.Combine(dataDirectory, "session.json");
        try
        {
            if (File.Exists(_sessionFile) && new FileInfo(_sessionFile).Length < 20000)
            {
                var loaded = Wire.Decode<NativeSession>(File.ReadAllText(_sessionFile, Utf8));
                if (loaded.Remember && loaded.Root != null && Directory.Exists(loaded.Root))
                {
                    _ = Open(loaded.Root);
                    if (loaded.Entry != null &&
                        loaded.Entry.WorkspaceId == Id(loaded.Root) &&
                        WorkspacePath.Normalize(loaded.Entry.Path, true) == loaded.Entry.Path)
                    {
                        Session = loaded;
                    }
                }
            }
        }
        catch (Exception error)when (error is IOException or UnauthorizedAccessException or JsonException or WorkspaceException or ArgumentException or InvalidDataException)
        {
            Session = null;
        }
    }

    /// <summary>Registers a local documentation root after rejecting unsupported paths and reparse points.</summary>
    public WorkspaceInfo Open(string root)
    {
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if (!Directory.Exists(root) || root.StartsWith("\\\\", StringComparison.Ordinal))
        {
            throw Error("invalid-folder", "Choose a local documentation folder.");
        }

        RejectLinks(root);
        var id = Id(root);
        _roots[id] = root;
        return new(id, Path.GetFileName(root), "native", new(CanRefresh: true, CanReopen: true, CanWriteSidecars: true));
    }

    /// <summary>Reopens the remembered local workspace or reports that it is unavailable.</summary>
    public WorkspaceInfo Resume() => Session != null ? Open(Session.Root) : throw Error("missing-folder", "Open the last document's folder again.");
    /// <summary>Returns a registered native workspace root or requires the folder to be reopened.</summary>
    public string Root(string id) => _roots.TryGetValue(id, out var root) ? root : throw Error("workspace-unavailable", "Open this folder again.");
    /// <summary>Resolves an existing local file or folder for an explicit shell action, including the registered root.</summary>
    public string ResolveLocation(string id, string path)
    {
        if (path.Length > 0)
        {
            return Resolve(id, path);
        }
        var root = Root(id);
        RejectLinks(root);
        return root;
    }
    /// <summary>Enumerates bounded relevant files while excluding ignored directories and symbolic links.</summary>
    public IReadOnlyList<WorkspaceEntry> Entries(string id)
    {
        var root = Root(id);
        RejectLinks(root);
        var entries = new List<WorkspaceEntry>();
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((root, 0));
        var visited = 0;
        var directories = 0;
        var documents = 0;
        while (queue.TryDequeue(out var folder))
        {
            if (++directories > 1024)
            {
                throw Error("folder-too-large", "Choose a smaller documentation folder.");
            }

            foreach (var entry in new DirectoryInfo(folder.Path).EnumerateFileSystemInfos())
            {
                if (++visited > 10000)
                {
                    throw Error("folder-too-large", "Choose a smaller documentation folder.");
                }

                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                var path = Path.GetRelativePath(root, entry.FullName).Replace('\\', '/');
                if (WorkspacePath.IsIgnored(path))
                {
                    continue;
                }

                if (entry is DirectoryInfo)
                {
                    if (folder.Depth < 16)
                    {
                        queue.Enqueue((entry.FullName, folder.Depth + 1));
                    }

                    continue;
                }

                if (!Relevant(path))
                {
                    continue;
                }

                _ = Resolve(id, path);
                var kind = WorkspacePath.IsMarkdown(path) ? "document" : path == MapLayoutCodec.FileName ? "sidecar" : "asset";
                if (kind == "document" && ++documents > Limits.Documents || entries.Count >= Limits.Entries)
                {
                    throw Error("folder-too-large", "Choose a folder with fewer than 1,024 pages and 4,096 relevant files.");
                }

                entries.Add(new(path, ((FileInfo)entry).Length, kind));
            }
        }

        return entries.OrderBy(e => e.Path, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Reads a contained bounded file; watchers may permit concurrent editor writes and atomic replacement.</summary>
    public async Task<byte[]> ReadAsync(
        string id,
        string path,
        CancellationToken token = default,
        FileShare sharing = FileShare.Read)
    {
        if (!Relevant(path))
        {
            throw Error("unsupported-file", "Only documentation and local images can be read.");
        }

        var fullPath = Resolve(id, path);
        var maximum = WorkspacePath.IsMarkdown(path) || path == MapLayoutCodec.FileName ? Limits.DocumentBytes : 8 * 1048576;
        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, sharing, 65536, FileOptions.Asynchronous);
        var length = stream.Length;
        if (length > maximum)
        {
            throw Error("file-too-large", "This file exceeds the reading limit.");
        }

        // Concurrent writes must not enlarge the allocation after its limit was checked.
        var bytes = new byte[checked((int)length)];
        await stream.ReadExactlyAsync(bytes, token);
        return bytes;
    }

    /// <summary>Reads the workspace map sidecar and its content revision when it exists.</summary>
    public async Task<MapFileState> ReadMapAsync(string id, CancellationToken token = default)
    {
        var file = Resolve(id, MapLayoutCodec.FileName, true);
        if (!File.Exists(file))
        {
            return new(null, "", true);
        }

        var bytes = await ReadAsync(id, MapLayoutCodec.FileName, token);
        return new(Utf8.GetString(bytes), Hash(bytes), true);
    }

    /// <summary>Validates and atomically writes a map layout only when its expected revision still matches.</summary>
    public async Task<string> WriteMapAsync(string id, string revision, string json, CancellationToken token = default)
    {
        _ = MapLayoutCodec.Read(json);
        await _writes.WaitAsync(token);
        try
        {
            var existing = await ReadMapAsync(id, token);
            if (existing.Revision != revision)
            {
                throw Error("map-conflict", "The map layout changed on disk. Download your layout before reopening the folder; the existing file was preserved.");
            }

            await AtomicFile.WriteTextAsync(Resolve(id, MapLayoutCodec.FileName, true), json, token);
            return Hash(Encoding.UTF8.GetBytes(json));
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <summary>Persists or clears the last-document session under the shared native write gate.</summary>
    public async Task RememberAsync(HistoryEntry? entry, bool enabled, CancellationToken token = default)
    {
        await _writes.WaitAsync(token);
        try
        {
            if (!enabled)
            {
                Session = null;
                if (File.Exists(_sessionFile))
                {
                    File.Delete(_sessionFile);
                }

                return;
            }

            if (entry == null || !_roots.TryGetValue(entry.WorkspaceId, out var root))
            {
                return;
            }

            _ = Resolve(entry.WorkspaceId, entry.Path);
            Session = new(root, entry);
            await AtomicFile.WriteTextAsync(_sessionFile, Wire.Encode(Session), token);
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <summary>Resolves a canonical workspace path while enforcing root containment and Windows filename and reparse-point rules.</summary>
    public string Resolve(string id, string path, bool allowMissingLeaf = false)
    {
        if (WorkspacePath.Normalize(path, true) != path || WorkspacePath.IsIgnored(path))
        {
            throw Error("invalid-path", "This path is outside the documentation workspace.");
        }

        foreach (var segment in path.Split('/'))
        {
            if (segment.EndsWith('.') ||
                segment.EndsWith(' ') ||
                segment.IndexOfAny(['<', '>', '"', '|', '?', '*']) >= 0 ||
                System.Text.RegularExpressions.Regex.IsMatch(segment, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\\.|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                throw Error("invalid-path", "This filename is unsafe on Windows.");
            }
        }

        var root = Root(id);
        var fullPath = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw Error("outside-workspace", "This resource is outside the opened folder.");
        }

        RejectLinks(allowMissingLeaf && !File.Exists(fullPath) ? Path.GetDirectoryName(fullPath)! : fullPath);
        return fullPath;
    }

    /// <summary>Rejects a file or any ancestor that is a symbolic link or junction.</summary>
    private static void RejectLinks(string path)
    {
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                // A contained path can still escape through a linked ancestor directory.
                throw Error("unsafe-link", "Symbolic links and junctions are excluded from native file access.");
            }
        }
    }

    /// <summary>Derives a stable native workspace identifier from its canonical case-normalized root.</summary>
    private static string Id(string root) => "native:" + Hash(Encoding.UTF8.GetBytes(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)).ToUpperInvariant()));
    /// <summary>Computes the lowercase SHA-256 revision of exact file bytes.</summary>
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    /// <summary>Includes supported documents, the current map sidecar and local images; retired annotation files stay untouched.</summary>
    private static bool Relevant(string path) => path == MapLayoutCodec.FileName ||
        WorkspacePath.IsMarkdown(path) ||
        Path.GetExtension(path).ToLowerInvariant()is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg";
    /// <summary>Creates a workspace error with the native service's existing code and message contract.</summary>
    private static WorkspaceException Error(string code, string message) => new(code, message);
}
