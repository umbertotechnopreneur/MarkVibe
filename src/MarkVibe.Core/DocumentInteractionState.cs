/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared document interaction state; 2026-10-25
 * Human guidance: Umberto Giacobbi; services update models and views only present them
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Owns the document menu, search and reading state shared by application services and views.</summary>
public sealed class DocumentInteractionState
{
    /// <summary>The captured menu target, retained independently of subsequent pointer and selection changes.</summary>
    public FileMenuTarget? MenuTarget { get; set; }

    /// <summary>The command service's currently available actions for the captured target.</summary>
    public IReadOnlyList<DocumentMenuItem> MenuItems { get; set; } = [];

    /// <summary>The active search session and latest accepted result.</summary>
    public DocumentFindState Find { get; } = new();

    /// <summary>The current reader layout and folder appearance.</summary>
    public ReaderPresentationState Reader { get; } = new();
}

/// <summary>Retains search identity and results without depending on a renderer or UI control.</summary>
public sealed class DocumentFindState
{
    /// <summary>The document being searched, or null when no search session is active.</summary>
    public FileMenuTarget? Target { get; set; }

    /// <summary>The renderer to search, either read or source.</summary>
    public string View { get; set; } = "read";

    /// <summary>The query owned by the current search session.</summary>
    public string Query { get; set; } = "";

    /// <summary>The service's monotonically advancing request identity used to reject stale results.</summary>
    public int Revision { get; set; }

    /// <summary>The one-based selected result, or zero when the query has no match.</summary>
    public int Current { get; set; }

    /// <summary>The number of matches accepted for the current query.</summary>
    public int Total { get; set; }

    /// <summary>Whether a document search session is currently active.</summary>
    public bool IsOpen => Target != null;
}

/// <summary>Retains reader preferences while services own their validation, updates and persistence.</summary>
public sealed class ReaderPresentationState
{
    /// <summary>The validated global application and document preferences.</summary>
    public Preferences Preferences { get; set; } = new();

    /// <summary>Whether the latest preference application was retained by platform storage.</summary>
    public bool PreferencesPersisted { get; set; }

    /// <summary>The saved shell geometry and reader width selection.</summary>
    public ShellLayout Layout { get; set; } = new();

    /// <summary>Whether initial layout restoration has finished and deliberate changes may be persisted.</summary>
    public bool LayoutReady { get; set; }

    /// <summary>The active folder's reading overrides, or null to use global preferences.</summary>
    public ReaderAppearance? Appearance { get; set; }

    /// <summary>The workspace owning the currently restored appearance.</summary>
    public string WorkspaceId { get; set; } = "";

    /// <summary>The service request identity used to reject stale workspace appearance responses.</summary>
    public int Revision { get; set; }
}

/// <summary>Reports the one-based selected match and total matches returned by a document renderer.</summary>
public sealed record DocumentFindResult(int Current, int Total);

/// <summary>Reports folder appearance and whether its persistence operation succeeded.</summary>
public sealed record ReaderAppearanceResult(ReaderAppearance? Appearance, bool Persisted);
