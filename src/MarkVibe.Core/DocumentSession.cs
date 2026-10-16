/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; authoritative document revision and bounded local history; 2026-10-16
 * Human guidance: Umberto Giacobbi; simplify the document model without legacy compatibility
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>The operation that produces the next validated Markdown revision.</summary>
public enum DocumentRevisionAction
{
    Edit,
    Undo,
    Redo,
    Restore
}

/// <summary>Distinguishes the original file from an unsaved revision and an explicitly exported copy.</summary>
public enum DocumentRevisionState
{
    Original,
    Unsaved,
    Exported
}

/// <summary>Owns an edited document's effective source and local history independently of parsed-model eviction.</summary>
public sealed class DocumentSession
{
    private const int MaximumHistoryEntries = 16;
    private const int MaximumHistoryCharacters = 4 * 1024 * 1024;
    private readonly List<string> _undo = [];
    private readonly List<string> _redo = [];

    public string Path { get; }
    public string OriginalSource { get; }
    public string CurrentSource { get; private set; }
    public string ExportedSource { get; private set; }
    public long Revision { get; private set; }
    public bool IsModified => CurrentSource != OriginalSource;
    public bool NeedsSave => IsModified && CurrentSource != ExportedSource;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public DocumentRevisionState State => !IsModified
        ? DocumentRevisionState.Original
        : NeedsSave ? DocumentRevisionState.Unsaved : DocumentRevisionState.Exported;

    /// <summary>Captures the original source only when the document first receives a local edit.</summary>
    internal DocumentSession(MarkdownModel original)
    {
        Path = original.Path;
        OriginalSource = original.Source;
        CurrentSource = original.Source;
        ExportedSource = original.Source;
    }

    /// <summary>Returns the next source for parsing before committing history; unavailable undo or redo is rejected.</summary>
    public string PreviewRevision(DocumentRevisionAction action)
    {
        return action switch
        {
            DocumentRevisionAction.Edit => CurrentSource,
            DocumentRevisionAction.Restore => OriginalSource,
            DocumentRevisionAction.Undo when CanUndo => _undo[^1],
            DocumentRevisionAction.Redo when CanRedo => _redo[^1],
            _ => throw new WorkspaceException("markdown-history", "This Markdown history action is no longer available.")
        };
    }

    /// <summary>Commits a previously parsed revision and updates both history stacks as one synchronous operation.</summary>
    internal void ApplyRevision(string source, DocumentRevisionAction action)
    {
        if (!Enum.IsDefined(action) || action != DocumentRevisionAction.Edit && source != PreviewRevision(action))
        {
            throw new WorkspaceException("markdown-revision", "The Markdown history changed before this revision could be applied.");
        }

        switch (action)
        {
            case DocumentRevisionAction.Edit:
                if (source == CurrentSource)
                {
                    return;
                }

                Remember(_undo, CurrentSource);
                _redo.Clear();
                break;
            case DocumentRevisionAction.Undo:
                Remember(_redo, CurrentSource);
                _undo.RemoveAt(_undo.Count - 1);
                break;
            case DocumentRevisionAction.Redo:
                Remember(_undo, CurrentSource);
                _redo.RemoveAt(_redo.Count - 1);
                break;
            case DocumentRevisionAction.Restore:
                _undo.Clear();
                _redo.Clear();
                break;
        }

        if (CurrentSource != source)
        {
            CurrentSource = source;
            Revision++;
        }
    }

    /// <summary>Records the exact source of an exported copy without changing the current document or its history.</summary>
    internal void MarkExported(string source)
    {
        ExportedSource = source;
    }

    /// <summary>Bounds one history stack by entry count and UTF-16 character storage while preserving the newest revision.</summary>
    private static void Remember(List<string> history, string source)
    {
        history.Add(source);
        var characters = history.Sum(entry => (long)entry.Length);
        while (history.Count > MaximumHistoryEntries || characters > MaximumHistoryCharacters)
        {
            characters -= history[0].Length;
            history.RemoveAt(0);
        }
    }
}
