/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; recent file and folder contract; 2026-10-23
 * Human guidance: Umberto Giacobbi; persistent alphabetic recent paths with middle abbreviation
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>A previously selected native file or folder, with recency used only to bound retained history.</summary>
public sealed record RecentLocation(string Path, string Kind, long OpenedAt);
