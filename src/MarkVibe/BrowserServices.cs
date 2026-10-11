using MarkVibe.Core;

namespace MarkVibe;

public static class BrowserServices
{
    public static IBrowserRuntime Runtime { get; set; } = null!;
    public static async Task<T> CallAsync<T>(string operation, object? arguments = null, CancellationToken cancellationToken = default) =>
        Wire.Decode<T>(await Runtime.CallAsync(operation, arguments == null ? "{}" : Wire.Encode(arguments), cancellationToken));
    public static async Task CallAsync(string operation, object? arguments = null, CancellationToken cancellationToken = default) =>
        _ = await Runtime.CallAsync(operation, arguments == null ? "{}" : Wire.Encode(arguments), cancellationToken);
}
