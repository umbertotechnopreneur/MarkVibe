/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; OpenAI key settings contract; 2026-10-11
 * Human guidance: Umberto Giacobbi; validate keys and choose storage by host
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Text.Json;

namespace MarkVibe.Core;

/// <summary>Describes credential availability without returning the saved API key.</summary>
public sealed record OpenAiKeyStatus(bool Configured, bool StorageAvailable, string Storage);

/// <summary>Shared input and response rules for the OpenAI authentication check.</summary>
public static class OpenAiKeyRules
{
    /// <summary>Trims pasted whitespace and rejects empty or unsafe authorization-header values.</summary>
    public static string Normalize(string? apiKey)
    {
        var normalizedKey = apiKey?.Trim() ?? "";
        if (normalizedKey.Length == 0)
        {
            throw new WorkspaceException("openai-key-required", "Enter an OpenAI API key first.");
        }

        if (normalizedKey.Length > 4096 || normalizedKey.Any(character => character < '!' || character > '~'))
        {
            throw new WorkspaceException("openai-key-format", "The API key contains unsupported characters or is too long.");
        }

        return normalizedKey;
    }

    /// <summary>Accepts the authenticated models-list schema, including an empty permitted list.</summary>
    public static bool IsModelsResponse(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object
            && response.TryGetProperty("object", out var responseKind)
            && responseKind.ValueKind == JsonValueKind.String
            && responseKind.GetString() == "list"
            && response.TryGetProperty("data", out var models)
            && models.ValueKind == JsonValueKind.Array;
    }

    /// <summary>Maps HTTP failures to fixed messages that never contain credentials or remote error bodies.</summary>
    public static WorkspaceException RequestFailure(int statusCode)
    {
        return statusCode switch
        {
            401 => new("openai-key-rejected", "OpenAI rejected this API key. Check it and try again."),
            403 => new("openai-access-denied", "OpenAI denied access. Check this key's permissions and your account restrictions."),
            429 => new("openai-rate-limit", "OpenAI limited the request. Check your account limits and try again later."),
            _ => new("openai-request-failed", "OpenAI could not complete the validation request. Try again later.")
        };
    }
}
