/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; localized responsive connection-result dialog; 2026-10-16
 * Human guidance: Umberto Giacobbi; report explicit test outcomes without claiming local login proves connectivity
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Shows the result of one explicit provider test, distinct from local configuration status.</summary>
public partial class AiConnectionResultWindow : ChildWindow
{
    private readonly FrameworkElement? _applicationRoot;
    private bool _templateLoaded;

    /// <summary>Builds a short localized result with the same live theme resources as other application dialogs.</summary>
    internal AiConnectionResultWindow(AiConnectionTestResult result)
    {
        InitializeComponent();
        AppLocalization.SetText(OutcomeText, result.Passed ? "Test passed" : "Test failed");
        AppLocalization.SetText(MessageText, result.Message);
        ResultIcon.Symbol = result.Passed ? "review" : "close";
        ResultIcon.Foreground = (Brush)Application.Current.Resources[result.Passed ? "SystemAccent" : "ErrorText"];
        _applicationRoot = Application.Current.RootVisual as FrameworkElement;
        ResizeDialog();
        Loaded += OnLoaded;
        UiStrings.Changed += RefreshLanguage;
        if (_applicationRoot != null)
        {
            _applicationRoot.SizeChanged += RootSizeChanged;
        }

        Closed += OnClosed;
    }

    /// <summary>Localizes the displayed result and gives keyboard users a clear dismissal action.</summary>
    private void OnLoaded(object sender, RoutedEventArgs arguments)
    {
        _templateLoaded = true;
        ResizeDialog();
        RefreshLanguage();
        Dispatcher.BeginInvoke(() => CloseResultButton.Focus());
    }

    /// <summary>Refreshes the title and content when the application's language changes.</summary>
    private void RefreshLanguage()
    {
        Title = UiStrings.T("Connection test");
        AppLocalization.Apply(this);
    }

    /// <summary>Keeps result content within the resized application viewport.</summary>
    private void RootSizeChanged(object sender, SizeChangedEventArgs arguments) => ResizeDialog();

    /// <summary>Resizes the content frame while preserving the modal overlay owned by OpenSilver.</summary>
    private void ResizeDialog()
    {
        var width = Math.Max(260, Math.Min(460, (_applicationRoot?.ActualWidth ?? 508) - 48));
        var height = Math.Max(190, Math.Min(270, (_applicationRoot?.ActualHeight ?? 318) - 48));
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

    /// <summary>Releases root and language subscriptions when the modal is dismissed.</summary>
    private void OnClosed(object? sender, EventArgs arguments)
    {
        UiStrings.Changed -= RefreshLanguage;
        if (_applicationRoot != null)
        {
            _applicationRoot.SizeChanged -= RootSizeChanged;
        }
    }

    /// <summary>Dismisses the result without changing provider or credential settings.</summary>
    private void Dismiss(object sender, RoutedEventArgs arguments) => Close();
}
