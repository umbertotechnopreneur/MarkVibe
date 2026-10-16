/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; native OpenAI key validation and storage; 2026-10-11
 * Human guidance: Umberto Giacobbi; save validated WebView keys as user environment variables
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Windows;

/// <summary>Validates desktop credentials over HTTPS and persists them in the Windows user environment.</summary>
internal static class OpenAiKeySettings
{
    private const string EnvironmentVariableName = "OPENAI_API_KEY";
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(20),
        MaxResponseContentBufferSize = 4 * 1024 * 1024
    };

    /// <summary>Reports whether a user environment key exists without exposing its value to the WebView.</summary>
    public static OpenAiKeyStatus Status()
    {
        try
        {
            var configured = !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(EnvironmentVariableName, EnvironmentVariableTarget.User));
            return new(configured, true, "environment");
        }
        catch (Exception error) when (error is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return new(false, false, "environment");
        }
    }

    /// <summary>Saves a candidate only after OpenAI accepts its authenticated models-list request.</summary>
    public static async Task<OpenAiKeyStatus> ValidateAndSaveAsync(string apiKey, CancellationToken cancellationToken)
    {
        var candidateKey = OpenAiKeyRules.Normalize(apiKey);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        // Keep authorization on this request, rather than shared HttpClient defaults.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", candidateKey);

        try
        {
            using var response = await Client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw OpenAiKeyRules.RequestFailure((int)response.StatusCode);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseJson);
            if (!OpenAiKeyRules.IsModelsResponse(document.RootElement))
            {
                throw new WorkspaceException("openai-response-invalid", "OpenAI returned an unexpected validation response. The key was not saved.");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WorkspaceException("openai-timeout", "OpenAI validation timed out. Check your connection and try again.");
        }
        catch (HttpRequestException)
        {
            throw new WorkspaceException("openai-network", "OpenAI could not be reached. Check your connection and try again.");
        }
        catch (JsonException)
        {
            throw new WorkspaceException("openai-response-invalid", "OpenAI returned an unexpected validation response. The key was not saved.");
        }

        // Closing Settings cancels validation and must not turn a delayed reply into a save.
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, candidateKey, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable(EnvironmentVariableName, candidateKey, EnvironmentVariableTarget.Process);
        }
        catch (Exception error) when (error is System.Security.SecurityException or UnauthorizedAccessException or ArgumentException)
        {
            throw new WorkspaceException("openai-storage", "OpenAI accepted the key, but it could not be saved to the user environment.");
        }

        return new(true, true, "environment");
    }
}
