/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; bounded Markdown HTML generation
 * Human guidance: address availability issues found in the Linux browser review
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Globalization;
using System.Text;

namespace MarkVibe.Core;

/// <summary>Collects rendered HTML, enforcing the document limit before each append.</summary>
internal sealed class BoundedHtmlWriter : TextWriter
{
    private readonly StringBuilder _html = new();

    /// <summary>Uses the same invariant formatting as the Markdown renderer.</summary>
    public BoundedHtmlWriter() : base(CultureInfo.InvariantCulture)
    {
    }

    /// <summary>The writer retains UTF-16 characters in memory.</summary>
    public override Encoding Encoding => Encoding.Unicode;

    /// <summary>Appends one character if it fits the remaining budget.</summary>
    public override void Write(char value)
    {
        EnsureSpace(1);
        _html.Append(value);
    }

    /// <summary>Appends a string without allocating a separate character array.</summary>
    public override void Write(string? value) => Write(value.AsSpan());

    /// <summary>Appends the requested portion of a character buffer.</summary>
    public override void Write(char[] buffer, int index, int count) => Write(buffer.AsSpan(index, count));

    /// <summary>Checks the entire slice before retaining any of its characters.</summary>
    public override void Write(ReadOnlySpan<char> value)
    {
        EnsureSpace(value.Length);
        _html.Append(value);
    }

    /// <summary>Materializes the checked output for the document model.</summary>
    public override string ToString() => _html.ToString();

    /// <summary>Rejects an append that would exceed the configured UTF-16 length.</summary>
    private void EnsureSpace(int characterCount)
    {
        if (characterCount > Limits.RenderedHtmlCharacters - _html.Length)
        {
            throw new WorkspaceException("render-too-large", "This document expands beyond the display limit.");
        }
    }
}
