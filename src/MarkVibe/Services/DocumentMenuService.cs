/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; service-owned document menu commands; 2026-10-25
 * Human guidance: Umberto Giacobbi; model, services and presentation separation
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Services;

/// <summary>Owns document command availability and execution; views only present its results.</summary>
public sealed class DocumentMenuService
{
    private readonly DocumentInteractionState _state;

    /// <summary>Uses application-owned menu state so controls do not retain command targets.</summary>
    public DocumentMenuService(DocumentInteractionState state) => _state = state;

    /// <summary>Captures the target and builds its presentation model before opening a menu.</summary>
    public void Open(FileMenuTarget target, bool localFiles, MarkdownModel? document, bool readerExpanded)
    {
        _state.MenuTarget = target;
        _state.MenuItems = Create(target, localFiles, document, readerExpanded);
    }

    /// <summary>Validates a target before a delayed UI transition touches the currently displayed document.</summary>
    public bool IsCurrentDocument(FileMenuTarget target, string? workspaceId, MarkdownModel? document) =>
        document != null && target.WorkspaceId == workspaceId && target.Path == document.Path &&
        (target.Fingerprint.Length == 0 || target.Fingerprint == document.Fingerprint);

    /// <summary>Builds file commands shared by graph cards, folders and document surfaces.</summary>
    public static IReadOnlyList<DocumentMenuItem> CreateFileActions(
        bool localFiles, bool markdown, bool folder, bool hasSelection)
    {
        var items = new List<DocumentMenuItem>();
        if (!folder)
        {
            items.Add(new("copySelection", "Copy", "copy", hasSelection));
        }
        if (markdown && !folder)
        {
            items.Add(new("copyFile", "Copy entire file content", "copy"));
        }
        if (!folder || localFiles)
        {
            items.Add(new("copyPath", "Copy file path", "link"));
        }
        if (localFiles)
        {
            if (markdown && !folder)
            {
                items.Add(new("browser", "Open in browser", "map-globe"));
            }
            items.Add(new("reveal", "Show in File Explorer", "folder"));
            items.Add(new("terminal", "Open in terminal", "code"));
            if (!folder)
            {
                items.Add(new("delete", "Delete file…", "delete"));
            }
        }
        return items;
    }

    /// <summary>Adds only source-backed commands that apply to the clicked current document.</summary>
    public static IReadOnlyList<DocumentMenuItem> Create(
        FileMenuTarget target, bool localFiles, MarkdownModel? document, bool readerExpanded)
    {
        var items = CreateFileActions(localFiles, WorkspacePath.IsMarkdown(target.Path),
            target.IsFolder, target.SelectedText.Length > 0).ToList();
        if (target.IsFolder || document?.Path != target.Path)
        {
            return items;
        }

        var context = DocumentContentService.Describe(document, target);
        var contextual = new List<DocumentMenuItem>
        {
            new("find", "Find in document", "search"),
            new("column", "Column", "reader-column", Checked: !readerExpanded),
            new("expandedColumn", "Expanded column", "reader-expanded", Checked: readerExpanded)
        };
        if (context.CanGoToSource)
        {
            contextual.Add(new("sourceHere", "Go to source here", "code"));
        }
        if (context.CanCopySection)
        {
            contextual.Add(new("copySection", "Copy link to section", "link"));
        }
        if (context.CanCopyTable)
        {
            contextual.Add(new("copyTableTsv", "For Excel", "copy", Group: "Copy table"));
            contextual.Add(new("copyTableMarkdown", "Markdown", "copy", Group: "Copy table"));
        }
        if (context.CanCopyCode)
        {
            contextual.Add(new("copyCode", "Copy code block", "code"));
        }
        contextual.Add(new("exportPdf", "PDF", "pdf", Group: "Export"));
        contextual.Add(new("exportHtml", "HTML", "code", Group: "Export"));
        items.InsertRange(Math.Min(2, items.Count), contextual);
        return items;
    }

    /// <summary>Executes a permitted captured command and returns any required UI transition.</summary>
    public async Task<DocumentCommandResult> ExecuteAsync(DocumentCommandSession session,
        FileMenuTarget target, string action, CancellationToken cancellationToken)
    {
        if (!session.IsCurrent() || session.Workspace.Id != target.WorkspaceId)
        {
            return new();
        }
        var available = Create(target, session.LocalFiles, session.Document, false);
        if (!available.Any(item => item.Action == action && item.Enabled))
        {
            return new();
        }

        switch (action)
        {
            case "copySelection":
                await CopyAsync(target.SelectedText, cancellationToken);
                break;
            case "copyPath":
                var path = session.LocalFiles
                    ? await BrowserServices.CallAsync<string>("file.path",
                        new { target.WorkspaceId, target.Path }, cancellationToken)
                    : target.Path;
                if (session.IsCurrent())
                {
                    await CopyAsync(path, cancellationToken);
                }
                break;
            case "copyFile":
            case "browser":
                var document = session.Document?.Path == target.Path
                    ? session.Document
                    : await session.Catalog.OpenAsync(session.Provider, target.Path, cancellationToken);
                if (!session.IsCurrent())
                {
                    return new();
                }
                if (action == "copyFile")
                {
                    await CopyAsync(document.Source, cancellationToken);
                }
                else
                {
                    await BrowserServices.CallAsync("document.browser",
                        new { document, target.WorkspaceId, preferences = session.Preferences }, cancellationToken);
                }
                break;
            case "copySection":
                await CopyAsync(DocumentContentService.CopySectionLink(session.Document!, target), cancellationToken);
                break;
            case "copyTableMarkdown":
                await CopyAsync(DocumentContentService.CopyTableMarkdown(session.Document!, target), cancellationToken);
                break;
            case "copyTableTsv":
                await CopyAsync(DocumentContentService.CopyTableTsv(session.Document!, target), cancellationToken);
                break;
            case "copyCode":
                await CopyAsync(DocumentContentService.CopyCode(session.Document!, target), cancellationToken);
                break;
            case "sourceHere":
                return new(action, DocumentContentService.Describe(session.Document!, target).SourceLine);
            case "reveal":
            case "terminal":
                await BrowserServices.CallAsync("file." + action,
                    new { target.WorkspaceId, target.Path }, cancellationToken);
                break;
            default:
                // Navigation, dialogs and layout remain presentation responsibilities.
                return new(action);
        }
        return new();
    }

    /// <summary>Exports the same current snapshot for toolbar and context-menu invocations.</summary>
    public Task<JsonElement> ExportAsync(MarkdownModel document, string workspaceId,
        Preferences preferences, string format, CancellationToken cancellationToken)
    {
        if (format is not ("pdf" or "html"))
        {
            throw new ArgumentException("Unsupported export format.", nameof(format));
        }
        return BrowserServices.CallAsync<JsonElement>("document.export",
            new { document, workspaceId, preferences, format }, cancellationToken);
    }

    /// <summary>Copies text through the platform clipboard adapter.</summary>
    private static Task CopyAsync(string text, CancellationToken cancellationToken) =>
        BrowserServices.CallAsync("document.copy", new { text }, cancellationToken);
}
