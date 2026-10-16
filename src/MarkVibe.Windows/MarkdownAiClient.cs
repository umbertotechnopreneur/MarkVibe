/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; native text-only Responses transport; 2026-10-11
 * Human guidance: Umberto Giacobbi; edit supplied Markdown and report token usage
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Windows;

/// <summary>Owns desktop HTTPS and saved credentials; neither keys nor arbitrary endpoints reach the WebView.</summary>
internal static class MarkdownAiClient
{
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(MarkdownAi.RequestTimeoutSeconds),
        MaxResponseContentBufferSize = 8 * 1024 * 1024
    };

    /// <summary>Reads and validates the Windows user key only inside the native process.</summary>
    private static string SavedKey()
    {
        try
        {
            return OpenAiKeyRules.Normalize(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));
        }
        catch (Exception error) when (error is System.Security.SecurityException or UnauthorizedAccessException)
        {
            throw new WorkspaceException("openai-storage", "The OpenAI key could not be read. Open Settings and validate it again.");
        }
    }

    /// <summary>Returns only account model IDs, never the raw authenticated reply or credential.</summary>
    public static async Task<string[]> ModelsAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync("models", null, SavedKey(), cancellationToken);
        if (!OpenAiKeyRules.IsModelsResponse(response))
        {
            throw new WorkspaceException("openai-response-invalid", "OpenAI returned an unexpected model list.");
        }

        var modelIds = new List<string>();
        foreach (var model in response.GetProperty("data").EnumerateArray())
        {
            if (model.ValueKind != JsonValueKind.Object
                || !model.TryGetProperty("id", out var identifier)
                || identifier.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            modelIds.Add(identifier.GetString()!);
            if (modelIds.Count == 4096)
            {
                break;
            }
        }

        return modelIds.ToArray();
    }

    /// <summary>Counts the fixed system/document/request prefixes before generation without disclosing the native credential.</summary>
    public static async Task<int[]> PrepareAsync(MarkdownAiRequest request, CancellationToken cancellationToken)
    {
        request = MarkdownAi.Validate(request);
        var apiKey = SavedKey();
        var counts = MarkdownAi.CountPayloads(request)
            .Select(payload => CountAsync(payload, apiKey, cancellationToken)).ToArray();
        var prefixes = await Task.WhenAll(counts);
        cancellationToken.ThrowIfCancellationRequested();
        return prefixes;
    }

    /// <summary>Runs exactly one generation after managed preflight; no retry or prior response can duplicate billed work.</summary>
    public static async Task<MarkdownAiExchange> EditAsync(MarkdownAiRequest request, int[]? prefixTokens, CancellationToken cancellationToken)
    {
        request = MarkdownAi.Validate(request);
        MarkdownAi.EnsureFits(request, prefixTokens);
        var response = await SendAsync("responses", MarkdownAi.Payload(request), SavedKey(), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return new(response, prefixTokens);
    }

    /// <summary>Allows unavailable token counting to fall back to labelled estimates without rejecting the edit.</summary>
    private static async Task<int> CountAsync(object payload, string apiKey, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var result = await SendAsync("responses/input_tokens", payload, apiKey, timeout.Token);
            var hasTokenCount = result.TryGetProperty("input_tokens", out var count)
                && count.ValueKind == JsonValueKind.Number;
            if (!hasTokenCount || !count.TryGetInt32(out var tokenCount))
            {
                return -1;
            }

            return tokenCount is >= 0 and <= 10000000 ? tokenCount : -1;
        }
        catch (Exception error) when (error is WorkspaceException or OperationCanceledException)
        {
            return -1;
        }
    }

    /// <summary>Calls a fixed OpenAI endpoint with bounded JSON and fixed error messages that never echo remote content.</summary>
    private static async Task<JsonElement> SendAsync(string endpoint, object? payload, string apiKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(payload == null ? HttpMethod.Get : HttpMethod.Post, "https://api.openai.com/v1/" + endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        if (payload != null)
        {
            request.Content = new StringContent(Wire.Encode(payload), Encoding.UTF8, "application/json");
        }
        try
        {
            using var response = await Client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw Failure((int)response.StatusCode);
            }
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json, new() { MaxDepth = 32 });
            return document.RootElement.Clone();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WorkspaceException("openai-timeout", "OpenAI took too long. Try a lower reasoning level or a shorter selection.");
        }
        catch (HttpRequestException)
        {
            throw new WorkspaceException("openai-network", "OpenAI could not be reached. Check your connection and try again.");
        }
        catch (JsonException)
        {
            throw new WorkspaceException("openai-response-invalid", "OpenAI returned an unexpected response. Try again.");
        }
    }

    /// <summary>Distinguishes unsupported model/input requests from authentication and service failures.</summary>
    private static WorkspaceException Failure(int status)
    {
        if (status is 400 or 404)
        {
            return new("openai-model-unavailable", "This model could not process the request. Refresh models or try a shorter selection.");
        }
        if (status is 401 or 403 or 429)
        {
            return OpenAiKeyRules.RequestFailure(status);
        }
        return new("openai-request-failed", "OpenAI could not complete the Markdown edit. Try again later.");
    }
}
