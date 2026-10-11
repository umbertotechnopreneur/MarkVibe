using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OpenSilver.WebAssembly;

namespace MarkVibe.Browser;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<BrowserRoot>("#app");
        var host = builder.Build();
        BrowserServices.Runtime = new JsBrowserRuntime(host.Services.GetRequiredService<IJSRuntime>());
        await host.RunAsync();
    }
}

public sealed class BrowserRoot : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    protected override async Task OnInitializedAsync() => await Runner.RunApplicationAsync<MarkVibe.App>();
}
