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
using MarkVibe.Core;

namespace MarkVibe;
public static class BrowserServices
{
    public static IBrowserRuntime Runtime { get; set; } = null !;

    /// <summary>Encodes browser-call arguments and awaits the runtime response using the shared wire contract.</summary>
    public static async Task<T> CallAsync<T>(string operation, object? arguments = null, CancellationToken cancellationToken = default) => Wire.Decode<T>(await Runtime.CallAsync(operation, arguments == null ? "{}" : Wire.Encode(arguments), cancellationToken));
    /// <summary>Encodes browser-call arguments and awaits the runtime response using the shared wire contract.</summary>
    public static async Task CallAsync(string operation, object? arguments = null, CancellationToken cancellationToken = default) => _ = await Runtime.CallAsync(operation, arguments == null ? "{}" : Wire.Encode(arguments), cancellationToken);
}
