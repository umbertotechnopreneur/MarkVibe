/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; bounded workspace collection of AI reference paths; 2026-10-16
 * Human guidance: Umberto Giacobbi; collect documents and show estimated context before sending
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Owns only selected paths; document content remains authoritative in the workspace catalog.</summary>
public sealed class AiDocumentCollection
{
    public const int MaximumDocuments = 16;
    private readonly List<string> _paths = [];
    public IReadOnlyList<string> Paths => _paths.ToArray();
    public int Count => _paths.Count;

    /// <summary>Adds a canonical Markdown path once, retaining selection order and rejecting an overfull collection.</summary>
    public bool Add(string path)
    {
        path = CanonicalPath(path);
        if (_paths.Contains(path, StringComparer.Ordinal))
        {
            return false;
        }
        if (_paths.Count >= MaximumDocuments)
        {
            throw new WorkspaceException("openai-reference-limit", "Choose at most 16 reference documents for one AI request.");
        }
        _paths.Add(path);
        return true;
    }

    /// <summary>Removes a selected path without affecting its document or the order of the remaining references.</summary>
    public bool Remove(string path)
    {
        return _paths.Remove(CanonicalPath(path));
    }

    /// <summary>Clears this workspace's selected references without changing any document.</summary>
    public void Clear()
    {
        _paths.Clear();
    }

    /// <summary>Restricts reference identities to normalized supported Markdown paths inside the workspace.</summary>
    internal static string CanonicalPath(string path)
    {
        var canonical = WorkspacePath.Normalize(path, imported: true);
        if (!WorkspacePath.IsMarkdown(canonical) || WorkspacePath.IsIgnored(canonical))
        {
            throw new WorkspaceException("openai-reference-path", "Choose Markdown documents from the current workspace.");
        }
        return canonical;
    }
}
