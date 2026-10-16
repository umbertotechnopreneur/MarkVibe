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
 * Modified with AI: OpenAI Codex; source identity for Markdown selection; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using CSHTML5.Native.Html.Controls;
using MarkVibe.Core;

namespace MarkVibe.Controls;
public abstract class DomHost : HtmlPresenter
{
    public string HostId { get; } = Guid.NewGuid().ToString("N");

    private readonly string _kind;
    /// <summary>Creates a tagged HTML presenter and attaches browser lifecycle handlers for the requested view kind.</summary>
    protected DomHost(string kind)
    {
        _kind = kind;
        // HtmlPresenter preserves adapter content through OpenSilver layout updates.
        Html = "<div data-mv-host=\"" + HostId + "\" style=\"position:absolute;inset:0;overflow:hidden\"></div>";
        ScrollMode = ScrollMode.Disabled;
        Loaded += Attach;
        Unloaded += Detach;
        SizeChanged += (_, _) =>
        {
            if (IsLoaded)
            {
                OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.resize($0)", HostId);
            }
        };
    }

    /// <summary>Attaches the browser view after its managed host is loaded.</summary>
    private void Attach(object sender, RoutedEventArgs arguments) => OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.attach($0,$1)", HostId, _kind);
    /// <summary>Detaches browser resources when the managed host is unloaded.</summary>
    private void Detach(object sender, RoutedEventArgs arguments) => OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.detach($0)", HostId);
}

public sealed class MarkdownView() : DomHost("markdown")
{
    /// <summary>Renders the Markdown model with its workspace assets and requested reading position.</summary>
    public Task DisplayAsync(MarkdownModel model, string workspaceId, ReadingPosition? position, string anchor, CancellationToken cancellationToken) => BrowserServices.CallAsync("markdown.render", new { hostId = HostId, workspaceId, document = model with { Source = "" }, position, anchor }, cancellationToken);
    /// <summary>Captures the reader's current source-backed reading position.</summary>
    public Task<ReadingPosition> CaptureAsync() => BrowserServices.CallAsync<ReadingPosition>("markdown.capture", new { hostId = HostId });
}

public sealed class SourceRenderer() : DomHost("source") { }

public sealed class MapRenderer() : DomHost("map") { }
