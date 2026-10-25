/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; application-owned document service composition; 2026-10-25
 * Human guidance: Umberto Giacobbi; intermediate object model owns state, UI displays it
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using MarkVibe.Core;

namespace MarkVibe.Services;

/// <summary>Owns the document interaction model and its services for the application's lifetime.</summary>
public sealed class DocumentApplicationServices
{
    /// <summary>Authoritative menu, search and reader presentation data.</summary>
    public DocumentInteractionState State { get; } = new();

    /// <summary>Resolves and executes commands against document models.</summary>
    public DocumentMenuService Menus { get; }

    /// <summary>Changes persisted reader presentation data.</summary>
    public ReaderPresentationService Reader { get; }

    /// <summary>Searches the active document and updates its result model.</summary>
    public DocumentFindService Find { get; }

    /// <summary>Composes services over the same application-owned model without creating any views.</summary>
    public DocumentApplicationServices()
    {
        Menus = new(State);
        Reader = new(State.Reader);
        Find = new(State.Find);
    }
}
