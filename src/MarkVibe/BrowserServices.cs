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

/// <summary>Encodes managed requests and decodes typed replies at the browser-runtime boundary.</summary>
public static class BrowserServices
{
    /// <summary>References the runtime installed by the application before browser operations begin.</summary>
    public static IBrowserRuntime Runtime { get; set; } = null!;

    /// <summary>Sends a cancellable operation and decodes its response using the shared wire contract.</summary>
    public static async Task<T> CallAsync<T>(
        string operation,
        object? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var requestJson = EncodeArguments(arguments);
        var responseJson = await Runtime.CallAsync(operation, requestJson, cancellationToken);
        return Wire.Decode<T>(responseJson);
    }

    /// <summary>Awaits an operation whose response value is intentionally not consumed.</summary>
    public static async Task CallAsync(
        string operation,
        object? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var requestJson = EncodeArguments(arguments);
        await Runtime.CallAsync(operation, requestJson, cancellationToken);
    }

    /// <summary>Uses an empty JSON object for argument-free calls and camel-case serialization otherwise.</summary>
    private static string EncodeArguments(object? arguments)
    {
        return arguments == null ? "{}" : Wire.Encode(arguments);
    }
}
