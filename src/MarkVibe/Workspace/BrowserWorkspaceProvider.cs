using MarkVibe.Core;

namespace MarkVibe.Workspace;

public sealed class BrowserWorkspaceProvider(WorkspaceInfo info) : IWorkspaceProvider
{
    public WorkspaceInfo Info { get; } = info;
    public async Task<IReadOnlyList<WorkspaceEntry>> EnumerateAsync(CancellationToken cancellationToken) =>
        await BrowserServices.CallAsync<List<WorkspaceEntry>>("workspace.entries", new { workspaceId = Info.Id }, cancellationToken);
    public Task<string> ReadDocumentAsync(string path, CancellationToken cancellationToken) =>
        BrowserServices.CallAsync<string>("workspace.text", new { workspaceId = Info.Id, path = WorkspacePath.Normalize(path) }, cancellationToken);
    public Task<AssetLease> ReadAssetAsync(string path, string owner, CancellationToken cancellationToken) =>
        BrowserServices.CallAsync<AssetLease>("assets.acquire", new { workspaceId = Info.Id, path = WorkspacePath.Normalize(path), owner }, cancellationToken);
    public Task ReleaseAssetsAsync(string owner) => BrowserServices.CallAsync("assets.release", new { owner });
    public Task RefreshAsync(CancellationToken cancellationToken) =>
        BrowserServices.CallAsync("workspace.refresh", new { workspaceId = Info.Id }, cancellationToken);
    public async ValueTask DisposeAsync() => await BrowserServices.CallAsync("workspace.release", new { workspaceId = Info.Id });
}
