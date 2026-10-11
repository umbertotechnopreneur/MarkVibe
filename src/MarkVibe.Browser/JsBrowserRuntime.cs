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

    public JsBrowserRuntime(IJSRuntime javascript)
    {
        _javascript = javascript;
        _reference = DotNetObjectReference.Create(this);
    }

    public void OnEvent(Func<string, Task> handler)
    {
        _handler = handler;
        _ = _javascript.InvokeVoidAsync("markvibe.bind", _reference);
    }

    [JSInvokable]
    public Task Receive(string json) => _disposed ? Task.CompletedTask : _handler?.Invoke(json) ?? Task.CompletedTask;

    public async Task<string> CallAsync(string operation, string arguments = "{}", CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        var request = Guid.NewGuid().ToString("N");
        using var registration = cancellationToken.Register(() => { _ = CancelAsync(request); });
        var response = await _javascript.InvokeAsync<string>("markvibe.call", cancellationToken, operation, arguments, request);
        using var document = JsonDocument.Parse(response);
        var root = document.RootElement;
        if (!root.GetProperty("ok").GetBoolean())
        {
            var code = root.GetProperty("code").GetString() ?? "read-failed";
            if (code == "cancelled") throw new OperationCanceledException(cancellationToken);
            throw new WorkspaceException(code, root.GetProperty("message").GetString() ?? "This action could not be completed.");
        }
        return root.GetProperty("value").GetRawText();
    }

    private async Task CancelAsync(string request)
    {
        try { await _javascript.InvokeVoidAsync("markvibe.cancel", request); }
        catch (JSException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _javascript.InvokeVoidAsync("markvibe.unbind"); }
        finally { _reference.Dispose(); _handler = null; }
    }
}
