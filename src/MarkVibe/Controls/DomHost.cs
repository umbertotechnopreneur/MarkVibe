using System.Windows;
using System.Windows.Controls;
using CSHTML5.Native.Html.Controls;
using MarkVibe.Core;

namespace MarkVibe.Controls;

public abstract class DomHost : HtmlPresenter
{
    public string HostId { get; } = Guid.NewGuid().ToString("N");
    private readonly string _kind;
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
            if (IsLoaded) OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.resize($0)", HostId);
        };
    }
    private void Attach(object sender, RoutedEventArgs arguments) =>
        OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.attach($0,$1)", HostId, _kind);
    private void Detach(object sender, RoutedEventArgs arguments) =>
        OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.detach($0)", HostId);
}

public sealed class MarkdownView() : DomHost("markdown")
{
    public Task DisplayAsync(MarkdownModel model, string workspaceId, ReadingPosition? position, string anchor, CancellationToken cancellationToken) =>
        BrowserServices.CallAsync("markdown.render", new { hostId = HostId, workspaceId, document = model with { Source = "" }, position, anchor }, cancellationToken);
    public Task<ReadingPosition> CaptureAsync() => BrowserServices.CallAsync<ReadingPosition>("markdown.capture", new { hostId = HostId });
}

public sealed class MonacoEditorView() : DomHost("source")
{
    public Task DisplayAsync(MarkdownModel model, string workspaceId, Preferences preferences, CancellationToken cancellationToken) =>
        BrowserServices.CallAsync("source.render", new { hostId = HostId, workspaceId, path = model.Path, source = model.Source, preferences }, cancellationToken);
}

public sealed class DocumentMapView() : DomHost("map")
{
    public Task DisplayAsync(MapSnapshot map, string selected, bool focused = false) =>
        BrowserServices.CallAsync("map.render", new { hostId = HostId, map, selected, focused });
}
