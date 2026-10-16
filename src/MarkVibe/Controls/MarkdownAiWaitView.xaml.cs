/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; managed waiting overlay; 2026-10-11
 * Human guidance: Umberto Giacobbi; all controls in XAML
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>A XAML waiting surface with an explicit cancellation event.</summary>
public partial class MarkdownAiWaitView : UserControl
{
    public event Action? CancelRequested;
    /// <summary>Loads the waiting controls from XAML.</summary>
    public MarkdownAiWaitView()
    {
        InitializeComponent();
    }
    /// <summary>Shows request progress without passing document text or credentials to another renderer.</summary>
    public Task DisplayAsync(object state)
    {
        var snapshot = JsonSerializer.SerializeToElement(state, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Title.Text = "AI Assistant";
        Stage.Text = UiStrings.T(snapshot.GetProperty("stage").GetString() ?? "Working…");
        StopButton.Content = UiStrings.T("Stop");
        return Task.CompletedTask;
    }
    /// <summary>Requests cancellation; only the owner changes request state.</summary>
    private void Cancel(object sender, RoutedEventArgs arguments)
    {
        CancelRequested?.Invoke();
    }
}
