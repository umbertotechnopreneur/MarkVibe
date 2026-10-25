/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared reader and source search controls; 2026-10-25
 * Human guidance: Umberto Giacobbi; context commands call services above the document model
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MarkVibe.Controls;

/// <summary>Collects find queries and navigation gestures while the document service owns search results.</summary>
public partial class DocumentFindView : UserControl
{
    private readonly DispatcherTimer _queryTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private bool _isOpen;

    /// <summary>Requests a new query, next result or previous result using directions zero, one and minus one.</summary>
    public event Func<string, int, Task>? SearchRequested;

    /// <summary>Requests that the owner hide the bar and clear document highlights.</summary>
    public event Action? CloseRequested;

    /// <summary>Initializes compact controls and stops pending query work when the view leaves the visual tree.</summary>
    public DocumentFindView()
    {
        InitializeComponent();
        PreviousButton.Content = new IconLabel("back", "Previous match") { Compact = true };
        NextButton.Content = new IconLabel("forward", "Next match") { Compact = true };
        CloseButton.Content = new IconLabel("close", "Close") { Compact = true };
        _queryTimer.Tick += QueryTimerTick;
        Unloaded += FindUnloaded;
        ShowResult(0, 0);
    }

    /// <summary>Displays the service-owned query and focuses its editable input.</summary>
    public void Open(string query)
    {
        _isOpen = true;
        QueryInput.Text = query;

        AppLocalization.Apply(this);
        QueueSearch();
        Dispatcher.BeginInvoke(() =>
        {
            if (_isOpen)
            {
                QueryInput.Focus();
                QueryInput.SelectAll();
            }
        });
    }

    /// <summary>Stops queued input after the owner closes the bar or switches documents.</summary>
    public void Close()
    {
        _isOpen = false;
        _queryTimer.Stop();
    }

    /// <summary>Displays the latest service result without moving keyboard focus away from the query.</summary>
    public void ShowResult(int current, int count)
    {
        count = Math.Max(0, count);
        current = Math.Clamp(current, 0, count);
        AppLocalization.SetText(MatchCount, "{0} of {1}", current, count);
        PreviousButton.IsEnabled = count > 0;
        NextButton.IsEnabled = count > 0;
    }

    /// <summary>Debounces typing so only a stable query is forwarded to the document service.</summary>
    private void QueryChanged(object sender, TextChangedEventArgs arguments)
    {
        if (_isOpen)
        {
            QueueSearch();
        }
    }

    /// <summary>Replaces any pending query with the current input.</summary>
    private void QueueSearch()
    {
        _queryTimer.Stop();
        _queryTimer.Start();
    }

    /// <summary>Forwards one debounced query and leaves result ordering to the service adapter.</summary>
    private async void QueryTimerTick(object? sender, EventArgs arguments)
    {
        _queryTimer.Stop();
        await RequestSearchAsync(0);
    }

    /// <summary>Navigates to the preceding match using the current query.</summary>
    private async void PreviousMatch(object sender, RoutedEventArgs arguments) => await RequestSearchAsync(-1);

    /// <summary>Navigates to the following match using the current query.</summary>
    private async void NextMatch(object sender, RoutedEventArgs arguments) => await RequestSearchAsync(1);

    /// <summary>Routes Enter and Shift+Enter to match navigation and Escape to closing the bar.</summary>
    private async void FindKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Escape)
        {
            arguments.Handled = true;
            RequestClose();
        }
        else if (arguments.Key == Key.Enter)
        {
            arguments.Handled = true;
            await RequestSearchAsync((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? -1 : 1);
        }
    }

    /// <summary>Closes from the explicit button without retaining a pending debounce callback.</summary>
    private void CloseFind(object sender, RoutedEventArgs arguments) => RequestClose();

    /// <summary>Stops input work before asking the owner to clear the service's search state.</summary>
    private void RequestClose()
    {
        Close();
        CloseRequested?.Invoke();
    }

    /// <summary>Stops queued searches when the host is detached.</summary>
    private void FindUnloaded(object sender, RoutedEventArgs arguments) => Close();

    /// <summary>Forwards current input only while this search bar remains active.</summary>
    private Task RequestSearchAsync(int direction)
    {
        _queryTimer.Stop();
        return _isOpen && SearchRequested != null
            ? SearchRequested(QueryInput.Text, direction)
            : Task.CompletedTask;
    }
}
