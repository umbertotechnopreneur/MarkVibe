/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; selectable native AI connections; 2026-10-16
 * Human guidance: Umberto Giacobbi; API-key and Codex CLI providers with explicit connection tests
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using MarkVibe.Core;

namespace MarkVibe.Windows;

/// <summary>Routes explicit AI requests while keeping API credentials and CLI authentication inside Windows.</summary>
internal sealed class AiConnectionService
{
    private readonly CodexCliClient _codex;

    /// <summary>Places temporary CLI protocol files under this installation's existing application profile.</summary>
    public AiConnectionService(string profileDirectory)
    {
        _codex = new CodexCliClient(profileDirectory);
    }

    /// <summary>Reports local configuration only; neither provider performs a remote connection test here.</summary>
    public Task<AiConnectionStatus> StatusAsync(string provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (provider == AiProviders.CodexCli)
        {
            return _codex.StatusAsync(cancellationToken);
        }

        RequireApiProvider(provider);
        var status = OpenAiKeySettings.Status();
        string message;
        if (!status.StorageAvailable)
        {
            message = "The Windows user environment is unavailable.";
        }
        else if (status.Configured)
        {
            message = "An API key is saved locally. Use Test to verify its connection.";
        }
        else
        {
            message = "Enter an API key and use Test to validate and save it.";
        }

        return Task.FromResult(new AiConnectionStatus(provider, status.Configured, status.StorageAvailable, message));
    }

    /// <summary>Tests the provider; a candidate API key replaces the saved key only after successful validation.</summary>
    public async Task<AiConnectionTestResult> TestAsync(string provider, string? apiKey, CancellationToken cancellationToken)
    {
        try
        {
            if (provider == AiProviders.CodexCli)
            {
                return await _codex.TestAsync(cancellationToken);
            }

            RequireApiProvider(provider);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                await MarkdownAiClient.ModelsAsync(cancellationToken);
            }
            else
            {
                await OpenAiKeySettings.ValidateAndSaveAsync(apiKey, cancellationToken);
            }

            return new(true, "OpenAI accepted the API key and returned its model list.");
        }
        catch (WorkspaceException error)
        {
            // Only fixed application messages reach Settings, never credentials or process output.
            return new(false, error.Message);
        }
    }

    /// <summary>Returns API account models or the explicitly labelled static CLI model catalog.</summary>
    public Task<string[]> ModelsAsync(string provider, CancellationToken cancellationToken)
    {
        if (provider == AiProviders.CodexCli)
        {
            return _codex.ModelsAsync(cancellationToken);
        }

        RequireApiProvider(provider);
        return MarkdownAiClient.ModelsAsync(cancellationToken);
    }

    /// <summary>Counts API input prefixes or returns unavailable counters for the CLI's estimated preflight.</summary>
    public Task<int[]> PrepareAsync(string provider, MarkdownAiRequest request, CancellationToken cancellationToken)
    {
        if (provider == AiProviders.CodexCli)
        {
            return _codex.PrepareAsync(request, cancellationToken);
        }

        RequireApiProvider(provider);
        return MarkdownAiClient.PrepareAsync(request, cancellationToken);
    }

    /// <summary>Runs one provider-specific edit with the shared bounded Markdown response contract.</summary>
    public Task<MarkdownAiExchange> EditAsync(string provider, MarkdownAiRequest request, int[]? prefixTokens, CancellationToken cancellationToken)
    {
        if (provider == AiProviders.CodexCli)
        {
            return _codex.EditAsync(request, cancellationToken);
        }

        RequireApiProvider(provider);
        return MarkdownAiClient.EditAsync(request, prefixTokens, cancellationToken);
    }

    /// <summary>Rejects unknown providers instead of silently changing the authentication or billing path.</summary>
    private static void RequireApiProvider(string provider)
    {
        if (provider != AiProviders.ApiKey)
        {
            throw new WorkspaceException("ai-provider-invalid", "Choose the API key or Codex CLI provider in Settings.");
        }
    }
}
