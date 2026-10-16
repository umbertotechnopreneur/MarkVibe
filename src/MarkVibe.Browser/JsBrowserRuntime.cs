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
using Microsoft.JSInterop;
using MarkVibe.Core;
using System.Text.Json;

namespace MarkVibe.Browser;
internal sealed class JsBrowserRuntime : IBrowserRuntime
{
    private readonly IJSRuntime _javascript;
    private readonly DotNetObjectReference<JsBrowserRuntime> _reference;
    private Func<string, Task>? _handler;
    private bool _disposed;
    /// <summary>Retains the JavaScript runtime and creates the managed event callback reference.</summary>
    public JsBrowserRuntime(IJSRuntime javascript)
    {
        _javascript = javascript;
        _reference = DotNetObjectReference.Create(this);
    }

    /// <summary>Binds the managed event receiver to the browser bridge.</summary>
    public void OnEvent(Func<string, Task> handler)
    {
        _handler = handler;
        _ = _javascript.InvokeVoidAsync("markvibe.bind", _reference);
    }

    /// <summary>Delivers a browser event to the registered handler while the runtime is active.</summary>
    [JSInvokable]
    public Task Receive(string json) => _disposed ? Task.CompletedTask : _handler?.Invoke(json) ?? Task.CompletedTask;
    /// <summary>Invokes a uniquely identified browser job and translates structured failures and cancellation.</summary>
    public async Task<string> CallAsync(string operation, string arguments = "{}", CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        var request = Guid.NewGuid().ToString("N");
        using var registration = cancellationToken.Register(() =>
        {
            _ = CancelAsync(request);
        });
        var response = await _javascript.InvokeAsync<string>("markvibe.call", cancellationToken, operation, arguments, request);
        using var document = JsonDocument.Parse(response);
        var root = document.RootElement;
        if (!root.GetProperty("ok").GetBoolean())
        {
            var code = root.GetProperty("code").GetString() ?? "read-failed";
            if (code == "cancelled")
            {
                throw new OperationCanceledException(cancellationToken);
            }

            throw new WorkspaceException(code, root.GetProperty("message").GetString() ?? "This action could not be completed.");
        }

        return root.GetProperty("value").GetRawText();
    }

    /// <summary>Requests cancellation of a browser job while tolerating an unavailable JavaScript bridge.</summary>
    private async Task CancelAsync(string request)
    {
        try
        {
            await _javascript.InvokeVoidAsync("markvibe.cancel", request);
        }
        catch (JSException)
        {
        }
    }

    /// <summary>Unbinds the browser bridge and releases the managed callback reference once.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            await _javascript.InvokeVoidAsync("markvibe.unbind");
        }
        finally
        {
            _reference.Dispose();
            _handler = null;
        }
    }
}
