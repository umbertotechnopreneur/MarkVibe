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
namespace MarkVibe.Core;
public sealed class NavigationHistory
{
    private readonly List<HistoryEntry> _entries = new();
    private int _cursor = -1;
    public HistoryEntry? Current => _cursor >= 0 ? _entries[_cursor] : null;
    public bool CanBack => _cursor > 0;
    public bool CanForward => _cursor + 1 < _entries.Count;
    public IReadOnlyList<HistoryEntry> Entries => _entries;

    /// <summary>Adds a document visit, discards the abandoned forward branch and bounds retained history.</summary>
    public HistoryEntry Push(string workspaceId, string path, string anchor = "")
    {
        if (_cursor + 1 < _entries.Count)
        {
            _entries.RemoveRange(_cursor + 1, _entries.Count - _cursor - 1);
        }

        var entry = new HistoryEntry(Guid.NewGuid().ToString("N"), workspaceId, path, anchor, new());
        _entries.Add(entry);
        if (_entries.Count > 80)
        {
            _entries.RemoveAt(0);
        }

        _cursor = _entries.Count - 1;
        return entry;
    }

    /// <summary>Selects a retained history entry by identifier without changing the timeline when it is absent.</summary>
    public HistoryEntry? Select(string id)
    {
        var cursor = _entries.FindIndex(e => e.Id == id);
        if (cursor < 0)
        {
            return null;
        }

        _cursor = cursor;
        return Current;
    }

    /// <summary>Restores a visit and position, rebuilding the retained timeline when the entry came from a previous session.</summary>
    public void Restore(HistoryEntry entry)
    {
        var selected = Select(entry.Id);
        if (selected == null)
        {
            // A prior browser session can have history entries outside the retained in-memory timeline.
            _entries.Clear();
            _entries.Add(entry);
            _cursor = 0;
        }

        Capture(entry.Position);
    }

    /// <summary>Updates the selected entry with finite bounded reading coordinates.</summary>
    public void Capture(ReadingPosition position)
    {
        if (_cursor >= 0)
        {
            _entries[_cursor] = _entries[_cursor] with
            {
                Position = position with
                {
                    Fraction = double.IsFinite(position.Fraction) ? Math.Clamp(position.Fraction, 0, 1) : 0,
                    SourceLine = double.IsFinite(position.SourceLine) ? Math.Max(1, position.SourceLine) : 1
                }
            };
        }
    }
}
