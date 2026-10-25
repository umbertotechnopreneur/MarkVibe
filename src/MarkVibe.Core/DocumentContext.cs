/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; source-backed document command models; 2026-10-25
 * Human guidance: Umberto Giacobbi; context menus call services above a shared data model
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Captures a workspace target and optional source identity before a context menu takes focus.</summary>
public sealed record FileMenuTarget(
    string WorkspaceId,
    string Path,
    string Title,
    bool IsFolder = false,
    string SelectedText = "",
    string Fingerprint = "",
    int SourceLine = 0,
    string ElementId = "",
    string HeadingId = "",
    string View = ""
);

/// <summary>Describes one service-owned menu action without depending on a particular UI toolkit.</summary>
public sealed record DocumentMenuItem(
    string Action,
    string Label,
    string Icon,
    bool Enabled = true,
    bool Checked = false,
    string Group = ""
);

/// <summary>Contains only context identities verified against the current parsed document snapshot.</summary>
public sealed record DocumentContextInfo(
    int SourceLine = 0,
    string HeadingId = "",
    string TableElementId = "",
    string CodeElementId = ""
)
{
    /// <summary>Whether the captured context still identifies a valid one-based source line.</summary>
    public bool CanGoToSource => SourceLine > 0;

    /// <summary>Whether the context refers to a heading with a parser-generated anchor.</summary>
    public bool CanCopySection => HeadingId.Length > 0;

    /// <summary>Whether the context falls inside a source-backed Markdown table.</summary>
    public bool CanCopyTable => TableElementId.Length > 0;

    /// <summary>Whether the context falls inside a source-backed code or diagram fence.</summary>
    public bool CanCopyCode => CodeElementId.Length > 0;
}

/// <summary>Requests a presentation change after the command service has validated its target.</summary>
public sealed record DocumentCommandResult(string Presentation = "", int SourceLine = 0);

/// <summary>Provides a captured workspace and a live validity check for asynchronous commands.</summary>
public sealed record DocumentCommandSession(
    WorkspaceInfo Workspace,
    DocumentCatalog Catalog,
    IWorkspaceProvider Provider,
    MarkdownModel? Document,
    Preferences Preferences,
    bool LocalFiles,
    Func<bool> IsCurrent);
