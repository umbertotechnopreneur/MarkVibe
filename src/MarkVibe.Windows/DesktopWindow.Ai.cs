/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; current-only AI bridge dispatch; 2026-10-17
 * Human guidance: Umberto Giacobbi; consolidate duplicate routes and remove obsolete compatibility
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Windows;

/// <summary>Keeps native AI argument decoding separate from file, printing and shell operations.</summary>
internal sealed partial class DesktopWindow
{
    /// <summary>Dispatches the current provider-explicit contract after authentication and smoke-mode isolation.</summary>
    private async Task<object?> DispatchAiAsync(
        string operation,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var provider = arguments.GetProperty("provider").GetString()
            ?? throw new WorkspaceException("invalid-request", "Missing native argument.");

        switch (operation)
        {
            case "ai.connection.status":
                return await _aiConnections.StatusAsync(provider, cancellationToken);

            case "ai.connection.test":
            {
                string? candidateKey = null;
                if (arguments.TryGetProperty("apiKey", out var candidate)
                    && candidate.ValueKind != JsonValueKind.Null)
                {
                    candidateKey = candidate.GetString();
                }

                return await _aiConnections.TestAsync(provider, candidateKey, cancellationToken);
            }

            case "ai.models":
                return await _aiConnections.ModelsAsync(provider, cancellationToken);

            case "ai.prepare":
            case "ai.edit":
            {
                var requestJson = arguments.GetProperty("request").GetRawText();
                var request = Wire.Decode<MarkdownAiRequest>(requestJson);
                if (operation == "ai.prepare")
                {
                    return await _aiConnections.PrepareAsync(provider, request, cancellationToken);
                }

                int[]? prefixTokens = null;
                if (arguments.TryGetProperty("prefixTokens", out var prefixes)
                    && prefixes.ValueKind == JsonValueKind.Array)
                {
                    prefixTokens = Wire.Decode<int[]>(prefixes.GetRawText());
                }

                return await _aiConnections.EditAsync(provider, request, prefixTokens, cancellationToken);
            }

            default:
                throw new WorkspaceException("unknown-action", "This native operation is unavailable.");
        }
    }
}
