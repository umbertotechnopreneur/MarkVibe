using System.Text;
using MarkVibe.Core;
using MarkVibe.Native;

var directory = Path.GetFullPath(Path.Combine("artifacts", "native-core", Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(directory);
var root = Path.Combine(directory, "workspace"); Directory.CreateDirectory(root);
var profile = Path.Combine(directory, "profile");
const string source = "# Native fixture\n\nA stable paragraph to annotate.\n\n[Next](next.md)\n";
await File.WriteAllTextAsync(Path.Combine(root, "README.md"), source);
await File.WriteAllTextAsync(Path.Combine(root, "next.md"), "# Next\n\nAnother page.");
var files = new NativeFiles(profile); var info = files.Open(root); var model = new MarkdownParser().Parse("README.md", source);
var block = model.Blocks.First(b => b.Quote.Contains("stable paragraph", StringComparison.Ordinal));
var stroke = new InkStroke("#2458c7", 4, "pen", [new(20, 30, 0.2, 0), new(100, 90, 0.8, 10)]);
var checks = new (string Name, Func<Task> Run)[]
{
    ("native enumeration ignores generated files and includes adjacent pages", async () =>
    {
        Directory.CreateDirectory(Path.Combine(root, "node_modules")); await File.WriteAllTextAsync(Path.Combine(root, "node_modules", "hidden.md"), "hidden");
        Equal(2, files.Entries(info.Id).Count); Equal(source, Encoding.UTF8.GetString(await files.ReadAsync(info.Id, "README.md")));
    }),
    ("native paths reject traversal, absolute paths, alternate streams and reserved names", () =>
    {
        foreach (var path in new[] { "../secret.md", "a/../README.md", "C:/secret.md", "a.md:stream", "CON.md", "dir./a.md", "a\\b.md", "node_modules/hidden.md" })
            Throws<WorkspaceException>(() => files.Resolve(info.Id, path));
        return Task.CompletedTask;
    }),
    ("ink roundtrip retains pressure and separate document metadata", () =>
    {
        var session = new InkSession(model); session.Add(block.Id, stroke);
        var roundtrip = InkCodec.Read(InkCodec.Write(session.Value)); Equal(model.Fingerprint, roundtrip.Document.ContentHash);
        Equal(model.Fingerprint, InkCodec.Read("\uFEFF" + InkCodec.Write(session.Value)).Document.ContentHash);
        Equal(0.8, roundtrip.Annotations[0].Strokes[0].Points[1].Pressure); Equal(1, InkCodec.Resolve(roundtrip, model).Count);
        session.Undo(); Equal(0, session.Value.Annotations.Count); session.Redo(); Equal(1, session.Value.Annotations.Count);
        session.Erase(session.Value.Annotations[0].Id, 0); Equal(0, session.Value.Annotations.Count); return Task.CompletedTask;
    }),
    ("unsupported versions, unknown fields and invalid point budgets fail explicitly", () =>
    {
        var session = new InkSession(model); session.Add(block.Id, stroke); var json = InkCodec.Write(session.Value);
        Throws<WorkspaceException>(() => InkCodec.Read(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":2", StringComparison.Ordinal)));
        Throws<WorkspaceException>(() => InkCodec.Read(json.Replace("\"schemaVersion\":1", "\"unexpected\":true,\"schemaVersion\":1", StringComparison.Ordinal)));
        Throws<WorkspaceException>(() => session.Add(block.Id, stroke with { Points = [new(-1, 5), new(5, 5)] }));
        Throws<WorkspaceException>(() => session.Add(block.Id, stroke with { Points = Enumerable.Repeat(new InkPoint(1, 2), 20001).ToArray() }));
        Throws<WorkspaceException>(() => session.Add(block.Id, stroke with { Points = [new(2, 3, 0.2, 5), new(2, 3, 0.8, 4)] })); return Task.CompletedTask;
    }),
    ("changed documents require explicit review and ambiguous anchors stay unresolved", () =>
    {
        var original = new InkSession(model); original.Add(block.Id, stroke);
        var changed = new MarkdownParser().Parse("README.md", "# Inserted\n\n" + source);
        var restored = new InkSession(changed, original.Value); Equal(true, restored.ChangedDocument); Equal(0, restored.Unresolved);
        Throws<WorkspaceException>(() => restored.Add(changed.Blocks[0].Id, stroke)); restored.Rebase(); Equal(false, restored.ChangedDocument);
        var unrelated = new MarkdownParser().Parse("README.md", "# Different\n\nNo matching paragraph.");
        var orphan = new InkSession(unrelated, original.Value); Equal(1, orphan.Unresolved); Throws<WorkspaceException>(orphan.Rebase);
        var repeated = new MarkdownParser().Parse("README.md", source + "\nA stable paragraph to annotate.\n");
        Equal(1, new InkSession(repeated, original.Value).Unresolved);
        Equal(original.Value.Document.ContentHash, orphan.Value.Document.ContentHash); return Task.CompletedTask;
    }),
    ("sidecar commits preserve Markdown and reject stale sidecar revisions", async () =>
    {
        var session = new InkSession(model); session.Add(block.Id, stroke); var json = InkCodec.Write(session.Value);
        var revision = await files.WriteSidecarAsync(info.Id, model.Path, model.Fingerprint, "", json);
        Equal(source, await File.ReadAllTextAsync(Path.Combine(root, "README.md")));
        Equal(revision, (await files.ReadSidecarAsync(info.Id, model.Path)).Revision);
        await File.WriteAllTextAsync(Path.Combine(root, "README.md.ink.json"), "external edit");
        await ThrowsAsync<WorkspaceException>(() => files.WriteSidecarAsync(info.Id, model.Path, model.Fingerprint, revision, json));
        Equal("external edit", await File.ReadAllTextAsync(Path.Combine(root, "README.md.ink.json")));
    }),
    ("disk Markdown changes reject annotation writes and BOM documents remain supported", async () =>
    {
        var json = InkCodec.Write(InkCodec.Empty(model));
        await File.WriteAllTextAsync(Path.Combine(root, "README.md"), source + "\nChanged.");
        var revision = (await files.ReadSidecarAsync(info.Id, model.Path)).Revision;
        await ThrowsAsync<WorkspaceException>(() => files.WriteSidecarAsync(info.Id, model.Path, model.Fingerprint, revision, json));
        await File.WriteAllTextAsync(Path.Combine(root, "README.md"), source, new UTF8Encoding(true));
        await files.WriteSidecarAsync(info.Id, model.Path, model.Fingerprint, revision, json);
    }),
    ("remembered native workspace reopens and disabling memory clears its metadata", async () =>
    {
        var entry = new HistoryEntry("test", info.Id, "next.md", "", new("", 0.3, 2));
        await files.RememberAsync(entry, true);
        var reopened = new NativeFiles(profile); Equal(info.Id, reopened.Resume().Id); Equal("next.md", reopened.Session!.Entry!.Path);
        await reopened.RememberAsync(null, false); Equal(true, new NativeFiles(profile).Session == null);
    }),
    ("cancelled atomic exports leave the previous destination intact", async () =>
    {
        var destination = Path.Combine(directory, "export.html"); await File.WriteAllTextAsync(destination, "original");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await ThrowsAsync<OperationCanceledException>(() => AtomicFile.WriteTextAsync(destination, "replacement", cancellation.Token));
        Equal("original", await File.ReadAllTextAsync(destination)); Equal(0, Directory.GetFiles(directory, ".markvibe-*.tmp").Length);
    }),
    ("corrupt session metadata cannot prevent desktop startup", async () =>
    {
        await File.WriteAllTextAsync(Path.Combine(profile, "session.json"), "null"); Equal(true, new NativeFiles(profile).Session == null);
        await File.WriteAllTextAsync(Path.Combine(profile, "session.json"), "{broken"); Equal(true, new NativeFiles(profile).Session == null);
    })
};
var failed = 0;
foreach (var check in checks)
{
    try { await check.Run(); Console.WriteLine("PASS " + check.Name); }
    catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + check.Name + ": " + error); }
}
return failed == 0 ? 0 : 1;
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}; received {actual}."); }
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
