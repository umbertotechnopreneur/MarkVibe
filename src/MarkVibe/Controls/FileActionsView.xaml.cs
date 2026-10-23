/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared document file action controls; 2026-10-16
 * Human guidance: Umberto Giacobbi; consistent file context menus and explicit delete confirmation
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;

namespace MarkVibe.Controls;

/// <summary>Identifies the workspace-relative target captured when a context menu opens.</summary>
public sealed record FileMenuTarget(string WorkspaceId, string Path, string Title, bool IsFolder = false, string SelectedText = "");

/// <summary>Shares action captions and availability between document menus and the card appearance picker.</summary>
public partial class FileActionsView : UserControl
{
    public event Func<string, Task>? ActionRequested;

    /// <summary>Creates consistent local icons and captions for all file menus.</summary>
    public FileActionsView()
    {
        InitializeComponent();
        CopyButton.Content = new IconLabel("copy", "Copy");
        CopyFileButton.Content = new IconLabel("copy", "Copy entire file content");
        BrowserButton.Content = new IconLabel("map-globe", "Open in browser");
        ExplorerButton.Content = new IconLabel("folder", "Show in File Explorer");
        TerminalButton.Content = new IconLabel("code", "Open in terminal");
        DeleteButton.Content = new IconLabel("delete", "Delete file…");
    }

    /// <summary>Hides actions unavailable for virtual workspaces, folders or non-document assets.</summary>
    public void Configure(bool localFiles, bool markdown, bool folder, bool hasSelection = false)
    {
        CopyButton.Visibility = folder ? Visibility.Collapsed : Visibility.Visible;
        CopyButton.IsEnabled = hasSelection;
        CopyFileButton.Visibility = markdown && !folder ? Visibility.Visible : Visibility.Collapsed;
        ExplorerButton.Visibility = localFiles ? Visibility.Visible : Visibility.Collapsed;
        TerminalButton.Visibility = localFiles ? Visibility.Visible : Visibility.Collapsed;
        BrowserButton.Visibility = localFiles && markdown && !folder ? Visibility.Visible : Visibility.Collapsed;
        DeleteButton.Visibility = localFiles && !folder ? Visibility.Visible : Visibility.Collapsed;
        DeleteDivider.Visibility = DeleteButton.Visibility;
    }

    /// <summary>Copies the text captured before the menu received focus.</summary>
    private async void CopySelection(object sender, RoutedEventArgs arguments) => await RequestAsync("copySelection");
    /// <summary>Copies the captured document's complete Markdown source.</summary>
    private async void CopyFile(object sender, RoutedEventArgs arguments) => await RequestAsync("copyFile");

    /// <summary>Requests a standalone browser preview of the captured document.</summary>
    private async void OpenBrowser(object sender, RoutedEventArgs arguments) => await RequestAsync("browser");
    /// <summary>Requests Explorer selection of the captured local file.</summary>
    private async void Reveal(object sender, RoutedEventArgs arguments) => await RequestAsync("reveal");
    /// <summary>Requests an interactive terminal in the captured location.</summary>
    private async void OpenTerminal(object sender, RoutedEventArgs arguments) => await RequestAsync("terminal");
    /// <summary>Requests the owner's frosted confirmation before any deletion can occur.</summary>
    private async void Delete(object sender, RoutedEventArgs arguments) => await RequestAsync("delete");
    /// <summary>Routes actions through the owner's guarded operation handler.</summary>
    private Task RequestAsync(string action) => ActionRequested?.Invoke(action) ?? Task.CompletedTask;
}
