namespace MarkVibe.Core;

public sealed class NavigationHistory
{
    private readonly List<HistoryEntry> _entries = new();
    private int _cursor = -1;
    public HistoryEntry? Current => _cursor >= 0 ? _entries[_cursor] : null;
    public bool CanBack => _cursor > 0;
    public bool CanForward => _cursor + 1 < _entries.Count;
    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public HistoryEntry Push(string workspaceId, string path, string anchor = "")
    {
        if (_cursor + 1 < _entries.Count) _entries.RemoveRange(_cursor + 1, _entries.Count - _cursor - 1);
        var entry = new HistoryEntry(Guid.NewGuid().ToString("N"), workspaceId, path, anchor, new());
        _entries.Add(entry);
        if (_entries.Count > 80) _entries.RemoveAt(0);
        _cursor = _entries.Count - 1;
        return entry;
    }
    public HistoryEntry? Select(string id)
    {
        var cursor = _entries.FindIndex(e => e.Id == id);
        if (cursor < 0) return null;
        _cursor = cursor; return Current;
    }
    public void Restore(HistoryEntry entry)
    {
        var selected = Select(entry.Id);
        if (selected == null)
        {
            // A prior browser session can have history entries outside the retained in-memory timeline.
            _entries.Clear(); _entries.Add(entry); _cursor = 0;
        }
        Capture(entry.Position);
    }
    public void Capture(ReadingPosition position)
    {
        if (_cursor >= 0)
            _entries[_cursor] = _entries[_cursor] with { Position = position with { Fraction = double.IsFinite(position.Fraction) ? Math.Clamp(position.Fraction, 0, 1) : 0, SourceLine = double.IsFinite(position.SourceLine) ? Math.Max(1, position.SourceLine) : 1 } };
    }
}
