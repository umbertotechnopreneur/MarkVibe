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
using System.Text;
using MarkVibe.Core;
using MarkVibe.Native;

var artifactRoot = Environment.GetEnvironmentVariable("MARKVIBE_NATIVE_ARTIFACTS_DIR") ?? Path.Combine("artifacts", "checks", "native-files");
var directory = Path.GetFullPath(Path.Combine(artifactRoot, Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(directory);
var root = Path.Combine(directory, "workspace");
Directory.CreateDirectory(root);
var profile = Path.Combine(directory, "profile");
const string source = "# Native fixture\n\nA stable paragraph to annotate.\n\n[Next](next.md)\n";
await File.WriteAllTextAsync(Path.Combine(root, "README.md"), source);
await File.WriteAllTextAsync(Path.Combine(root, "next.md"), "# Next\n\nAnother page.");
var files = new NativeFiles(profile);
var info = files.Open(root);
var model = new MarkdownParser().Parse("README.md", source);
var checks = new (string Name, Func<Task> Run)[]
{
    ("native enumeration ignores generated files and includes adjacent pages", async () =>
    {
        Directory.CreateDirectory(Path.Combine(root, "node_modules"));
        await File.WriteAllTextAsync(Path.Combine(root, "node_modules", "hidden.md"), "hidden");
        Equal(2, files.Entries(info.Id).Count);
        Equal(source, Encoding.UTF8.GetString(await files.ReadAsync(info.Id, "README.md")));
    }),
    ("native paths reject traversal, absolute paths, alternate streams and reserved names", () =>
    {
        foreach (var path in new[]
        {
            "../secret.md",
            "a/../README.md",
            "C:/secret.md",
            "a.md:stream",
            "CON.md",
            "dir./a.md",
            "a\\b.md",
            "node_modules/hidden.md"
        }

        )
        {
            Throws<WorkspaceException>(() => files.Resolve(info.Id, path));
        }

        return Task.CompletedTask;
    }),
    ("remembered native workspace reopens and disabling memory clears its metadata", async () =>
    {
        var entry = new HistoryEntry("test", info.Id, "next.md", "", new("", 0.3, 2));
        await files.RememberAsync(entry, true);
        var reopened = new NativeFiles(profile);
        Equal(info.Id, reopened.Resume().Id);
        Equal("next.md", reopened.Session!.Entry!.Path);
        await reopened.RememberAsync(null, false);
        Equal(true, new NativeFiles(profile).Session == null);
    }),
    ("cancelled atomic exports leave the previous destination intact", async () =>
    {
        var destination = Path.Combine(directory, "export.html");
        await File.WriteAllTextAsync(destination, "original");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await ThrowsAsync<OperationCanceledException>(() => AtomicFile.WriteTextAsync(destination, "replacement", cancellation.Token));
        Equal("original", await File.ReadAllTextAsync(destination));
        Equal(0, Directory.GetFiles(directory, ".markvibe-*.tmp").Length);
    }),
    ("map writes preserve concurrent external changes and never modify Markdown", async () =>
    {
        var layout = new MapLayout(MapLayoutCodec.SchemaVersion, new Dictionary<string, MapCardLayout>
        {
            ["README.md"] = new(10, 20, MapLayoutCodec.DefaultColor, MapLayoutCodec.DefaultIcon, 370, 240)
        });
        var initial = await files.ReadMapAsync(info.Id);
        Equal("", initial.Revision);
        var json = MapLayoutCodec.Write(layout);
        var revision = await files.WriteMapAsync(info.Id, initial.Revision, json);
        Equal(revision, (await files.ReadMapAsync(info.Id)).Revision);
        var path = Path.Combine(root, MapLayoutCodec.FileName);
        var external = json + "\n";
        await File.WriteAllTextAsync(path, external);
        await ThrowsAsync<WorkspaceException>(() => files.WriteMapAsync(info.Id, revision, json));
        Equal(external, await File.ReadAllTextAsync(path));
        Equal(source, await File.ReadAllTextAsync(Path.Combine(root, "README.md")));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var externalRevision = (await files.ReadMapAsync(info.Id)).Revision;
        await ThrowsAsync<OperationCanceledException>(() => files.WriteMapAsync(info.Id, externalRevision, json, cancellation.Token));
        Equal(external, await File.ReadAllTextAsync(path));
    }),
    ("corrupt session metadata cannot prevent desktop startup", async () =>
    {
        await File.WriteAllTextAsync(Path.Combine(profile, "session.json"), "null");
        Equal(true, new NativeFiles(profile).Session == null);
        await File.WriteAllTextAsync(Path.Combine(profile, "session.json"), "{broken");
        Equal(true, new NativeFiles(profile).Session == null);
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
        Console.Error.WriteLine("FAIL " + check.Name + ": " + error);
    }
}

return failed == 0 ? 0 : 1;
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
