/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; explicit AI connection preferences and sanitized status contracts; 2026-10-16
 * Human guidance: choose Codex CLI or an API key and test the selected connection
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
namespace MarkVibe.Core;

/// <summary>Defines the supported persisted provider identifiers without storing authentication data.</summary>
public static class AiProviders
{
    /// <summary>Uses the existing API transport and its separately persisted API key.</summary>
    public const string ApiKey = "api-key";

    /// <summary>Uses the locally installed desktop Codex CLI and its own authentication.</summary>
    public const string CodexCli = "codex-cli";

    /// <summary>Preserves the existing API-key default for missing or unsupported stored choices.</summary>
    public static string Normalize(string? provider) => provider == CodexCli ? CodexCli : ApiKey;
}

/// <summary>Reports local configuration and provider availability without exposing credentials or claiming a remote test.</summary>
public sealed record AiConnectionStatus(string Provider, bool Configured, bool Available, string Message);

/// <summary>Returns a short, credential-free outcome from an explicitly requested connection test.</summary>
public sealed record AiConnectionTestResult(bool Passed, string Message);
