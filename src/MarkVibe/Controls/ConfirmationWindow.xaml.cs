/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; asynchronous native map-reset confirmation; 2026-10-16
 * Human guidance: Umberto Giacobbi; map reset requires explicit confirmation
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Shares one frosted, keyboard-accessible confirmation for destructive document and map actions.</summary>
public partial class ConfirmationWindow : ChildWindow
{
    private bool _accepted;
    private readonly FrameworkElement? _applicationRoot;
    private bool _templateLoaded;
    private readonly string _titleKey;
    private readonly double _contentHeight;
    private static int _openDialogs;

    /// <summary>Prevents an external-file notification from stacking on an existing confirmation.</summary>
    internal static bool IsShowing => _openDialogs > 0;

    /// <summary>Creates a localized confirmation that fits inside the current application viewport.</summary>
    private ConfirmationWindow(string title, string message, string action, string? target, string? details, bool destructive, string cancelAction)
    {
        InitializeComponent();
        _titleKey = title;
        _contentHeight = target == null ? 270 : 350;
        AppLocalization.SetText(MessageText, message);
        AppLocalization.SetRaw(TargetText, target ?? "");
        TargetSurface.Visibility = target == null ? Visibility.Collapsed : Visibility.Visible;
        AppLocalization.SetText(DetailsText, details ?? "");
        DetailsText.Visibility = string.IsNullOrEmpty(details) ? Visibility.Collapsed : Visibility.Visible;
        ConfirmButton.Content = action;
        CancelButton.Content = cancelAction;
        if (destructive)
        {
            ConfirmButton.Background = new SolidColorBrush(Color.FromRgb(180, 48, 67));
        }
        _applicationRoot = Application.Current.RootVisual as FrameworkElement;
        ResizeDialog();
        Loaded += RefreshLabels;
        if (_applicationRoot != null)
        {
            _applicationRoot.SizeChanged += RootSizeChanged;
        }
        Closed += OnClosed;
    }

    /// <summary>Waits for the native modal to close; Escape and the close icon cancel the operation.</summary>
    public static Task<bool> ConfirmAsync(string title, string message, string action, string? target = null, string? details = null, bool destructive = false, string cancelAction = "Cancel")
    {
        var completion = new TaskCompletionSource<bool>();
        var dialog = new ConfirmationWindow(title, message, action, target, details, destructive, cancelAction);
        _openDialogs++;
        dialog.Closed += (_, _) =>
        {
            _openDialogs--;
            completion.TrySetResult(dialog._accepted);
        };
        try
        {
            dialog.Show();
        }
        catch
        {
            _openDialogs--;
            throw;
        }
        return completion.Task;
    }

    /// <summary>Translates controls after the modal template exists and initially focuses Cancel.</summary>
    private void RefreshLabels(object sender, RoutedEventArgs arguments)
    {
        _templateLoaded = true;
        ResizeDialog();
        Title = UiStrings.T(_titleKey);
        AppLocalization.Apply(this);
        Dispatcher.BeginInvoke(() =>
        {
            ResizeDialog();
            CancelButton.Focus();
        });
    }

    /// <summary>Keeps the confirmation inside the application viewport when the window is resized.</summary>
    private void RootSizeChanged(object sender, SizeChangedEventArgs arguments) => ResizeDialog();

    /// <summary>Sizes the content frame after OpenSilver expands the modal root to cover its overlay.</summary>
    private void ResizeDialog()
    {
        var width = Math.Max(260, Math.Min(460, (_applicationRoot?.ActualWidth ?? 508) - 48));
        var height = Math.Max(190, Math.Min(_contentHeight, (_applicationRoot?.ActualHeight ?? 398) - 48));
        if (_templateLoaded && GetTemplateChild("ContentRoot") is FrameworkElement frame)
        {
            frame.Width = width;
            frame.Height = height;
        }
        else
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>Releases the viewport subscription after the native modal is fully closed.</summary>
    private void OnClosed(object? sender, EventArgs arguments)
    {
        if (_applicationRoot != null)
        {
            _applicationRoot.SizeChanged -= RootSizeChanged;
        }
    }

    /// <summary>Closes the confirmation without authorizing any map mutation.</summary>
    private void Cancel(object sender, RoutedEventArgs arguments)
    {
        Close();
    }

    /// <summary>Records explicit approval before the shared close animation begins.</summary>
    private void Confirm(object sender, RoutedEventArgs arguments)
    {
        _accepted = true;
        Close();
    }
}
