/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; Markdown default-app status contract; 2026-10-25
 * Human guidance: Umberto Giacobbi; show Markdown associations in Settings
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Reports this installation's effective Windows defaults; null means a query was unavailable.</summary>
public sealed record FileAssociationStatus(bool CanConfigure, bool? MdDefault, bool? MarkdownDefault);
