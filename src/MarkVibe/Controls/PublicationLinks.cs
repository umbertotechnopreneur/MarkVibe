/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; canonical publication links shared by the shell and About window; 2026-10-16
 * Human guidance: Umberto Giacobbi; follow the VibeWare application publication template
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Controls;

/// <summary>Keeps owner-approved publication destinations consistent across application controls.</summary>
internal static class PublicationLinks
{
    /// <summary>Identifies the application's public source repository.</summary>
    internal const string Repository = "https://github.com/umbertotechnopreneur/MarkVibe";

    /// <summary>Identifies the initiative's repository separately from application authorship.</summary>
    internal const string InitiativeRepository = "https://github.com/umbertotechnopreneur/VibeWare";

    /// <summary>Identifies the actual creator's personal website.</summary>
    internal const string Author = "https://umbertogiacobbi.biz";

    /// <summary>Identifies the canonical initiative manifesto.</summary>
    internal const string Manifesto = Author + "/vibeware/manifesto";

    /// <summary>Identifies the shared terms page without replacing the project's MIT license.</summary>
    internal const string Terms = Author + "/terms/";

    /// <summary>Identifies the shared privacy page, opened only after explicit user activation.</summary>
    internal const string Privacy = Author + "/privacy/";

    /// <summary>Opens a publication destination only in response to a user-activated application link.</summary>
    internal static void Open(string url)
    {
        OpenSilver.Interop.ExecuteJavaScriptVoid("window.open($0, '_blank', 'noopener,noreferrer')", url);
    }
}
