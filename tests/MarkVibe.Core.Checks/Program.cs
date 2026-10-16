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
using MarkVibe.Core;
using System.Text;

var checks = new (string Name, Func<Task> Run)[]
{
    ("relative paths decode exactly once and remain inside the workspace", Sync(() =>
    {
        var target = WorkspacePath.Resolve("guide/reading.md", "../reference/setup%20notes.md#caf%C3%A9");
        Equal(LinkKind.Document, target.Kind);
        Equal("reference/setup notes.md", target.Path);
        Equal("café", target.Anchor);
        Equal("guide/%2e%2e/file.md", WorkspacePath.Resolve("guide/read.md", "%252e%252e/file.md").Path);
        foreach (var unsafePath in new[]
        {
            "../../secret.md",
            "%2e%2e/%2e%2e/secret.md",
            "/root.md",
            "C:/secret.md",
            "a%5Cb.md",
            "a%00b.md",
            ".git/config",
            "a%ZZ.md"
        }

        )
        {
            Equal(LinkKind.Blocked, WorkspacePath.Resolve("guide/read.md", unsafePath).Kind);
        }

        Throws<WorkspaceException>(() => WorkspacePath.Normalize("a/../b.md", imported: true));
    })),
    ("only explicit safe external schemes and credentials-free links survive", Sync(() =>
    {
        foreach (var unsafeUrl in new[]
        {
            "javascript:alert(1)",
            "data:text/html,hi",
            "file:///tmp/a",
            "//example.com/a",
            "https://user:pass@example.com"
        }

        )
        {
            Equal(LinkKind.Blocked, WorkspacePath.Resolve("README.md", unsafeUrl).Kind);
        }

        Equal(LinkKind.External, WorkspacePath.Resolve("README.md", "https://example.com/docs").Kind);
        Equal(LinkKind.External, WorkspacePath.Resolve("README.md", "mailto:reader@example.com").Kind);
    })),
    ("one Markdown parse owns metadata, duplicate anchors, links and source blocks", Sync(() =>
    {
        var model = new MarkdownParser().Parse("guide/read.md", "---\ntitle: Reading notes\n---\n# Start\n\nA useful introduction to a connected reading space.\n\n## Café\n\n## Café\n\n[Setup](../setup.md#install)\n\n![Image](../assets/photo.png)\n\n<script>alert(1)</script>\n");
        Equal("Reading notes", model.Title);
        Equal("café", model.Headings[1].Id);
        Equal("café-1", model.Headings[2].Id);
        Equal("setup.md", model.Links.Single().Path);
        Equal("install", model.Links.Single().Anchor);
        True(model.Html.Contains("data-mv-asset=\"assets/photo.png\"", StringComparison.Ordinal));
        True(!model.Html.Contains("<script>", StringComparison.Ordinal));
        True(model.Blocks.All(b => b.StartLine > 0 && b.EndLine >= b.StartLine));
        True(model.Blocks.Select(b => b.Id).Distinct().Count() == model.Blocks.Count);
        Equal(64, model.Fingerprint.Length);
    })),
    ("browser positions retain fractional source lines and history truncates safely", Sync(() =>
    {
        var decoded = Wire.Decode<ReadingPosition>("{\"blockId\":\"b-1\",\"fraction\":0.4,\"sourceLine\":12.5}");
        Equal(12.5, decoded.SourceLine);
        var history = new NavigationHistory();
        var first = history.Push("demo", "README.md");
        history.Capture(decoded);
        history.Push("demo", "guide/a.md");
        history.Select(first.Id);
        Equal(decoded, history.Current!.Position);
        history.Push("demo", "guide/b.md");
        True(!history.CanForward);
        Equal(2, history.Entries.Count);
        history.Capture(new("", double.NaN, double.PositiveInfinity));
        Equal(0d, history.Current!.Position.Fraction);
        Equal(1d, history.Current.Position.SourceLine);
        for (var index = 0; index < 100; index++)
        {
            history.Push("demo", index + ".md");
        }

        Equal(80, history.Entries.Count);
    })),
    ("map edges represent actual Markdown links and search reaches indexed content", async () =>
    {
        await using var workspace = new MemoryWorkspace(new Dictionary<string, string> { ["README.md"] = "# Home\n\n[Setup](guide/setup.md#install)\n", ["guide/setup.md"] = "# Setup\n\nA searchable lantern phrase.\n\n[Home](../README.md)\n", ["lonely.md"] = "# Lonely\n\nAn unrelated page.\n" });
        var catalog = new DocumentCatalog(new());
        catalog.Seed(await workspace.EnumerateAsync(default));
        await catalog.IndexAsync(workspace, (_, _) =>
        {
        }, default);
        Equal("guide/setup.md", catalog.Search("lantern").Single().Document.Path);
        var map = catalog.Map("README.md", "", true);
        Equal(2, map.Nodes.Count);
        Equal(2, map.Edges.Count);
        True(map.Nodes.All(n => n.SearchText.Length == 0));
        Equal("guide/setup.md", map.Incoming.Single().Path);
    }),
    ("selected document caching is bounded and cancellation does not read", async () =>
    {
        await using var workspace = new MemoryWorkspace(Enumerable.Range(0, 4).ToDictionary(i => i + ".md", i => "# Page " + i));
        var catalog = new DocumentCatalog(new());
        catalog.Seed(await workspace.EnumerateAsync(default));
        await catalog.OpenAsync(workspace, "0.md", default);
        await catalog.OpenAsync(workspace, "1.md", default);
        await catalog.OpenAsync(workspace, "2.md", default);
        await catalog.OpenAsync(workspace, "0.md", default);
        await catalog.OpenAsync(workspace, "3.md", default);
        await catalog.OpenAsync(workspace, "1.md", default);
        Equal(1, workspace.Reads["0.md"]);
        Equal(2, workspace.Reads["1.md"]);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var fresh = new DocumentCatalog(new());
        fresh.Seed(await workspace.EnumerateAsync(default));
        await ThrowsAsync<OperationCanceledException>(() => fresh.OpenAsync(workspace, "2.md", cancelled.Token));
        Equal(1, workspace.Reads["2.md"]);
    }),
    ("partial indexing keeps remaining paths discoverable under its byte budget", async () =>
    {
        var source = "# Large page\n\n" + new string ('a', 850000);
        await using var workspace = new MemoryWorkspace(Enumerable.Range(0, 11).ToDictionary(i => i.ToString("D2") + ".md", _ => source));
        var catalog = new DocumentCatalog(new());
        catalog.Seed(await workspace.EnumerateAsync(default));
        await catalog.IndexAsync(workspace, (_, _) =>
        {
        }, default);
        True(!catalog.IndexComplete);
        True(catalog.Warnings.Any(w => w.Contains("8 MiB", StringComparison.Ordinal)));
        Equal(11, catalog.Search("").Count);
        Equal(9, catalog.IndexedCount);
        Equal("10.md", (await catalog.OpenAsync(workspace, "10.md", default)).Path);
    })
};
var failed = 0;
foreach (var check in checks)
{
    try
    {
        await check.Run();
        Console.WriteLine("PASS " + check.Name);
    }
    catch (Exception error)
    {
        failed++;
        Console.Error.WriteLine("FAIL " + check.Name + ": " + error.Message);
    }
}

return failed == 0 ? 0 : 1;
// Adapts a synchronous fixture action to the asynchronous check runner.
static Func<Task> Sync(Action action) => () =>
{
    action();
    return Task.CompletedTask;
};
// Fails the current check when its required condition is false.
static void True(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("The expected condition was not met.");
    }
}

// Checks that the observed value matches the expected synthetic fixture value.
static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}; received {actual}.");
    }
}

// Requires a synchronous fixture action to fail with the expected workspace error code.
static void Throws<T>(Action action)
    where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException("Expected " + typeof(T).Name);
}

// Requires an asynchronous fixture action to fail with the expected workspace error code.
static async Task ThrowsAsync<T>(Func<Task> action)
    where T : Exception
{
    try
    {
        await action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException("Expected " + typeof(T).Name);
}

sealed class MemoryWorkspace(Dictionary<string, string> documents) : IWorkspaceProvider
{
    public WorkspaceInfo Info { get; } = new("memory", "Check fixture", "files", new(CanAddFiles: true));
    public Dictionary<string, int> Reads { get; } = new(StringComparer.Ordinal);

    /// <summary>Lists the synthetic Markdown entries used by the catalog checks.</summary>
    public Task<IReadOnlyList<WorkspaceEntry>> EnumerateAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<WorkspaceEntry>>(documents
        .Select(pair => new WorkspaceEntry(pair.Key, Encoding.UTF8.GetByteCount(pair.Value), "document"))
        .ToList());
    /// <summary>Returns the synthetic document text for the requested fixture path.</summary>
    public Task<string> ReadDocumentAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Reads[path] = Reads.GetValueOrDefault(path) + 1;
        return Task.FromResult(documents[path]);
    }

    /// <summary>Rejects asset reads because the in-memory catalog fixture supplies no assets.</summary>
    public Task<AssetLease> ReadAssetAsync(string path, string owner, CancellationToken cancellationToken) => throw new NotSupportedException();
    /// <summary>Completes asset cleanup for the fixture, which holds no asset leases.</summary>
    public Task ReleaseAssetsAsync(string owner) => Task.CompletedTask;
    /// <summary>Completes refresh for the fixed in-memory fixture.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    /// <summary>Completes disposal for the fixture, which owns no external resources.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
