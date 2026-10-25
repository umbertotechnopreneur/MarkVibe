/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; document search service; 2026-10-25
 * Human guidance: Umberto Giacobbi; keep search behavior outside MainPage
 * Modified with AI: OpenAI Codex; own shared search state and reject stale renderer responses; 2026-10-25
 * Human guidance: Umberto Giacobbi; services update the intermediate model and views display it
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using MarkVibe.Core;

namespace MarkVibe.Services;

/// <summary>Owns document search transitions and renderer operations above a shared presentation-independent model.</summary>
public sealed class DocumentFindService
{
    private readonly SemaphoreSlim _rendererOperations = new(1, 1);

    /// <summary>The shared model updated by this service and read by the search controls.</summary>
    public DocumentFindState State { get; }

    /// <summary>Connects the service to the application-owned search model.</summary>
    public DocumentFindService(DocumentFindState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        State = state;
    }

    /// <summary>Starts a new search session using the captured document, renderer and selected text.</summary>
    public void Open(FileMenuTarget target, string view)
    {
        State.Revision++;
        State.Target = target;
        State.View = target.View == "source" || view == "source" ? "source" : "read";
        State.Query = InitialQuery(target.SelectedText);
        State.Current = 0;
        State.Total = 0;
    }

    /// <summary>Searches the current renderer and updates the model only if the request still owns the session.</summary>
    public async Task<bool> SearchAsync(string query, int direction, CancellationToken cancellationToken = default)
    {
        var target = State.Target;
        if (target == null)
        {
            return false;
        }

        var revision = ++State.Revision;
        var view = State.View;
        if (State.Query != query)
        {
            State.Current = 0;
            State.Total = 0;
        }
        State.Query = query;
        await _rendererOperations.WaitAsync(cancellationToken);
        try
        {
            // Superseded queries never reach a renderer after a close or a newer query.
            if (!IsCurrent(target, revision))
            {
                return false;
            }

            var result = await SearchRendererAsync(target, view, query, direction, cancellationToken);
            if (!IsCurrent(target, revision))
            {
                return false;
            }

            State.Total = Math.Max(0, result.Total);
            State.Current = Math.Clamp(result.Current, 0, State.Total);
            return true;
        }
        finally
        {
            _rendererOperations.Release();
        }
    }

    /// <summary>Clears shared search state immediately and removes renderer highlights in request order.</summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        var target = State.Target;
        var view = State.View;
        State.Revision++;
        State.Target = null;
        State.Query = "";
        State.Current = 0;
        State.Total = 0;
        State.View = "read";
        if (target == null)
        {
            return;
        }

        // Serialize clearing with search so a late query cannot repaint highlights after closing.
        await _rendererOperations.WaitAsync(cancellationToken);
        try
        {
            await SearchRendererAsync(target, view, "", 0, cancellationToken);
        }
        finally
        {
            _rendererOperations.Release();
        }
    }

    /// <summary>Normalizes a captured selection into a bounded single-line search input.</summary>
    public string InitialQuery(string selection)
    {
        var query = selection.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return query.Length <= 256 ? query : query[..256];
    }

    /// <summary>Checks that an asynchronous response still belongs to the same active search request.</summary>
    private bool IsCurrent(FileMenuTarget target, int revision) =>
        State.Revision == revision && ReferenceEquals(State.Target, target);

    /// <summary>Calls the browser adapter with an explicit document identity and bounded navigation direction.</summary>
    private static Task<DocumentFindResult> SearchRendererAsync(FileMenuTarget target, string view,
        string query, int direction, CancellationToken cancellationToken) =>
        BrowserServices.CallAsync<DocumentFindResult>("document.find",
            new { target.WorkspaceId, target.Path, query, direction = Math.Sign(direction), view }, cancellationToken);
}
