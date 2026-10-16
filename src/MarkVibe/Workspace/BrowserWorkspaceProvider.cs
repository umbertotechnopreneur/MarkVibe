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

namespace MarkVibe.Workspace;
public sealed class BrowserWorkspaceProvider(WorkspaceInfo info) : IWorkspaceProvider
{
    public WorkspaceInfo Info { get; } = info;

    /// <summary>Obtains the current workspace entries through the browser provider boundary.</summary>
    public async Task<IReadOnlyList<WorkspaceEntry>> EnumerateAsync(CancellationToken cancellationToken) => await BrowserServices.CallAsync<List<WorkspaceEntry>>("workspace.entries", new { workspaceId = Info.Id }, cancellationToken);
    /// <summary>Reads the requested Markdown document through the browser provider with cancellation.</summary>
    public Task<string> ReadDocumentAsync(string path, CancellationToken cancellationToken) => BrowserServices.CallAsync<string>("workspace.text", new { workspaceId = Info.Id, path = WorkspacePath.Normalize(path) }, cancellationToken);
    /// <summary>Acquires a bounded local asset lease for the requested owner.</summary>
    public Task<AssetLease> ReadAssetAsync(string path, string owner, CancellationToken cancellationToken) => BrowserServices.CallAsync<AssetLease>("assets.acquire", new { workspaceId = Info.Id, path = WorkspacePath.Normalize(path), owner }, cancellationToken);
    /// <summary>Releases browser asset leases held by the specified owner.</summary>
    public Task ReleaseAssetsAsync(string owner) => BrowserServices.CallAsync("assets.release", new { owner });
    /// <summary>Refreshes the browser workspace enumeration with cancellation.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken) => BrowserServices.CallAsync("workspace.refresh", new { workspaceId = Info.Id }, cancellationToken);
    /// <summary>Releases the workspace provider and its browser resources.</summary>
    public async ValueTask DisposeAsync() => await BrowserServices.CallAsync("workspace.release", new { workspaceId = Info.Id });
}
