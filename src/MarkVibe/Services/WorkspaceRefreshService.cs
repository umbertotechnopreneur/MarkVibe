/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; guarded workspace refresh command; 2026-10-25
 * Human guidance: Umberto Giacobbi; sidebar refresh invokes a service, not UI-owned business logic
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using MarkVibe.Core;

namespace MarkVibe.Services;

/// <summary>Refreshes supported providers and requests the existing catalog/view synchronization.</summary>
public sealed class WorkspaceRefreshService
{
    private readonly WorkspaceRefreshState _state;

    /// <summary>Notifies adapters to display changed operation availability.</summary>
    public event Action? StateChanged;

    /// <summary>Uses the application-owned refresh model shared by all entry points.</summary>
    public WorkspaceRefreshService(WorkspaceRefreshState state) => _state = state;

    /// <summary>Reports provider capability and rejects overlapping or incompatible operations.</summary>
    public bool CanExecute(WorkspaceInfo? workspace, bool blocked) =>
        workspace?.Capabilities.CanRefresh == true && !_state.IsBusy && !blocked;

    /// <summary>Flushes pending map input, refreshes files and rejects results from a replaced or busy workspace.</summary>
    public async Task ExecuteAsync(
        IWorkspaceProvider provider,
        Func<bool> isCurrent,
        Func<Task> synchronizePresentation,
        CancellationToken cancellationToken)
    {
        if (!CanExecute(provider.Info, !isCurrent()))
        {
            return;
        }

        _state.IsBusy = true;
        try
        {
            StateChanged?.Invoke();
            await BrowserServices.CallAsync("map.flush", cancellationToken: cancellationToken);
            if (!isCurrent())
            {
                return;
            }

            await provider.RefreshAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (isCurrent())
            {
                // The existing projection retains catalog drafts, navigation history
                // and expanded tree folders rather than replacing the provider.
                await synchronizePresentation();
            }
        }
        finally
        {
            _state.IsBusy = false;
            StateChanged?.Invoke();
        }
    }
}
