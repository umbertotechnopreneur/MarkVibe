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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OpenSilver.WebAssembly;

namespace MarkVibe.Browser;
internal static class Program
{
    /// <summary>Creates the WebAssembly host and browser runtime before running the OpenSilver application.</summary>
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
    /// <summary>Leaves DOM rendering to the OpenSilver runner rather than adding Blazor component markup.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    /// <summary>Starts the OpenSilver application after the WebAssembly root component initializes.</summary>
    protected override async Task OnInitializedAsync() => await Runner.RunApplicationAsync<MarkVibe.App>();
}
