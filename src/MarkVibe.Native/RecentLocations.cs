/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; bounded atomic recent location storage; 2026-10-23
 * Human guidance: Umberto Giacobbi; remember both files and folders, sorted alphabetically
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Native;

/// <summary>Retains twenty recent paths of each kind locally, independently of automatic session restoration.</summary>
public sealed class RecentLocations
{
    private const int MaximumPerKind = 20;
    private readonly string _filename;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private RecentLocation[] _entries = [];

    /// <summary>Restores bounded, valid records without changing or deleting an unreadable history file.</summary>
    public RecentLocations(string dataDirectory)
    {
        _filename = System.IO.Path.Combine(dataDirectory, "recent-locations.json");
        try
        {
            if (File.Exists(_filename) && new FileInfo(_filename).Length <= 4 * 1024 * 1024)
            {
                _entries = Retain(Wire.Decode<RecentLocation[]>(File.ReadAllText(_filename)));
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            _entries = [];
        }
    }

    /// <summary>Returns one kind alphabetically by full path, rather than by most recent use.</summary>
    public RecentLocation[] List(string kind)
    {
        return _entries.Where(entry => entry.Kind == kind)
            .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Accepts reopening only a stored selection and rejects missing or unsupported targets.</summary>
    public RecentLocation Resolve(string path, string kind)
    {
        var entry = _entries.FirstOrDefault(entry => entry.Kind == kind &&
            string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase));
        if (entry == null || (kind == "folder" ? !Directory.Exists(entry.Path) : !File.Exists(entry.Path)))
        {
            throw new WorkspaceException("missing-location", "This recent file or folder is unavailable. Browse to its new location.");
        }

        return entry;
    }

    /// <summary>Refreshes recency and atomically persists deduplicated history after an explicit native selection.</summary>
    public async Task RememberAsync(string path, string kind, CancellationToken token)
    {
        var entry = new RecentLocation(System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(path)),
            kind, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        if (!IsValid(entry))
        {
            return;
        }

        await _writes.WaitAsync(token);
        try
        {
            var retained = Retain(new[] { entry }.Concat(_entries));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_filename)!);
            await AtomicFile.WriteTextAsync(_filename, Wire.Encode(retained), token);
            _entries = retained;
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <summary>Keeps the newest instance of each case-insensitive path within its file or folder list.</summary>
    private static RecentLocation[] Retain(IEnumerable<RecentLocation> entries)
    {
        return entries.Where(IsValid)
            .OrderByDescending(entry => entry.OpenedAt)
            .GroupBy(entry => entry.Kind)
            .SelectMany(group => group.DistinctBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase).Take(MaximumPerKind))
            .ToArray();
    }

    /// <summary>Rejects malformed stored paths and kinds before they can become native open requests.</summary>
    private static bool IsValid(RecentLocation? entry)
    {
        return entry != null && (entry.Kind is "files" or "folder") &&
            !string.IsNullOrWhiteSpace(entry.Path) && entry.Path.Length <= 32767 &&
            !entry.Path.Contains('\0') && System.IO.Path.IsPathFullyQualified(entry.Path) &&
            !entry.Path.StartsWith("\\\\", StringComparison.Ordinal) &&
            (entry.Kind == "folder" || WorkspacePath.IsMarkdown(entry.Path));
    }
}
