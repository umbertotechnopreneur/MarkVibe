/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; managed source toolbar; 2026-10-11
 * Human guidance: Umberto Giacobbi; application controls in XAML
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>A XAML source view whose browser adapter owns only Monaco and its plain-text fallback.</summary>
public partial class MonacoEditorView : UserControl
{
    private bool _simple;
    public string HostId => Renderer.HostId;
    public event Func<bool, Task>? SimpleRequested;
    public event Func<Func<Task>, Task>? ActionRequested;
    /// <summary>Loads the toolbar and isolated editor host.</summary>
    public MonacoEditorView()
    {
        InitializeComponent();
    }
    /// <summary>Displays source metadata in XAML and passes source content to Monaco.</summary>
    public Task DisplayAsync(MarkdownModel model, string workspaceId, Preferences preferences, CancellationToken cancellationToken)
    {
        SourcePath.Text = model.Path;
        _simple = preferences.SimpleSource;
        ToolTipService.SetToolTip(SimpleButton, UiStrings.T(_simple ? "Editor view" : "Simple view"));
        return BrowserServices.CallAsync("source.render", new { hostId = HostId, workspaceId, path = model.Path, source = model.Source, fingerprint = model.Fingerprint, preferences }, cancellationToken);
    }
    /// <summary>Searches the renderer using text from the managed input.</summary>
    private async void FindSource(object sender, TextChangedEventArgs arguments)
    {
        await RunActionAsync(() => BrowserServices.CallAsync("source.find", new { hostId = HostId, query = SourceSearch.Text }));
    }
    /// <summary>Changes source preferences through the component owner.</summary>
    private async void ToggleSimple(object sender, RoutedEventArgs arguments)
    {
        if (SimpleRequested != null)
        {
            await SimpleRequested(!_simple);
        }
    }
    /// <summary>Copies source using the browser clipboard adapter.</summary>
    private async void CopySource(object sender, RoutedEventArgs arguments)
    {
        await RunActionAsync(() => BrowserServices.CallAsync("source.copy", new { hostId = HostId }));
    }

    /// <summary>Routes clipboard and renderer failures through the owner's visible action notice.</summary>
    private Task RunActionAsync(Func<Task> action)
    {
        return ActionRequested?.Invoke(action) ?? Task.CompletedTask;
    }
}
