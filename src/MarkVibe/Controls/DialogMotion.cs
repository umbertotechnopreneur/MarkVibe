/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared accessible modal glass transitions; 2026-10-16
 * Human guidance: Umberto Giacobbi; frosted dialogs and 300 ms background blur on open and close
 * Modified with AI: OpenAI Codex; stable template-owned blur and managed modal exit lifecycle; 2026-10-16
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace MarkVibe.Controls;

/// <summary>Preserves native modal ownership while allowing its glass material to fade before disposal.</summary>
public static class DialogMotion
{
    /// <summary>Fences delayed close completions and retains a later subscriber's cancellation.</summary>
    private sealed class MotionState
    {
        public bool Closing;
        public bool Completing;
        public int Generation;
        public CancelEventArgs? CompletingArguments;
    }

    private static readonly ConditionalWeakTable<ChildWindow, MotionState> States = new();
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(DialogMotion), new PropertyMetadata(false, EnabledChanged));

    /// <summary>Reads whether a native modal uses the application's shared entrance and exit material.</summary>
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    /// <summary>Enables the shared motion lifecycle through the XAML dialog style.</summary>
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    /// <summary>Registers one lifecycle subscription per dialog and releases it when the behavior is disabled.</summary>
    private static void EnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs arguments)
    {
        if (element is not ChildWindow dialog)
        {
            return;
        }

        dialog.Loaded -= OnLoaded;
        dialog.Unloaded -= OnUnloaded;
        dialog.Closing -= OnClosing;
        if ((bool)arguments.NewValue)
        {
            dialog.Loaded += OnLoaded;
            dialog.Unloaded += OnUnloaded;
            dialog.Closing += OnClosing;
            if (dialog.IsLoaded)
            {
                ApplyOpening(dialog);
            }
        }
        else
        {
            States.GetOrCreateValue(dialog).Generation++;
            if (dialog.IsLoaded)
            {
                SetClosingPresentation(dialog, false);
            }
        }
    }

    /// <summary>Starts managed close bookkeeping for the current native modal lifecycle.</summary>
    private static void OnLoaded(object sender, RoutedEventArgs arguments)
    {
        ApplyOpening((ChildWindow)sender);
    }

    /// <summary>Resets close bookkeeping; stable template selectors own the opening presentation.</summary>
    private static void ApplyOpening(ChildWindow dialog)
    {
        var state = States.GetOrCreateValue(dialog);
        state.Generation++;
        state.Closing = false;
        state.Completing = false;
        state.CompletingArguments = null;
        // OpenSilver may finish assigning its DOM classes after Loaded. Opening styles
        // therefore target named template elements instead of a transient added class.
        SetClosingPresentation(dialog, false);
    }

    /// <summary>Freezes only dialog content during exit; the modal root continues intercepting background input.</summary>
    private static void SetClosingPresentation(ChildWindow dialog, bool closing)
    {
        OpenSilver.Interop.ExecuteJavaScriptVoid(
            "(function(root, closing) { " +
            "root.classList.toggle('mv-dialog-closing', closing); " +
            "const glass = root.querySelector('[dataid=\"DialogGlassSurface\"]'); " +
            "if (glass) { glass.inert = closing; } " +
            "})($0, $1);", OpenSilver.Interop.GetDiv(dialog), closing);
    }

    /// <summary>Invalidates delayed closures when a dialog is externally detached or reopened.</summary>
    private static void OnUnloaded(object sender, RoutedEventArgs arguments)
    {
        var state = States.GetOrCreateValue((ChildWindow)sender);
        state.Generation++;
        state.Closing = false;
    }

    /// <summary>Defers native disposal for the exit transition without delaying reduced-motion users.</summary>
    private static async void OnClosing(object? sender, CancelEventArgs arguments)
    {
        if (sender is not ChildWindow dialog || !dialog.IsLoaded)
        {
            return;
        }

        var state = States.GetOrCreateValue(dialog);
        if (state.Completing)
        {
            // Keep the same event arguments so later subscribers can still veto the replay.
            state.CompletingArguments = arguments;
            return;
        }

        if (arguments.Cancel)
        {
            return;
        }

        var reducedMotion = Convert.ToBoolean(OpenSilver.Interop.ExecuteJavaScript(
            "window.matchMedia('(prefers-reduced-motion: reduce)').matches"));
        if (reducedMotion)
        {
            return;
        }

        // Native modal focus trapping stays in place until the matching exit finishes.
        arguments.Cancel = true;
        if (state.Closing)
        {
            return;
        }

        state.Closing = true;
        var generation = state.Generation;
        var result = dialog.DialogResult;
        SetClosingPresentation(dialog, true);
        await Task.Delay(300);
        if (!dialog.IsLoaded || state.Generation != generation || !GetIsEnabled(dialog))
        {
            return;
        }

        state.Completing = true;
        state.CompletingArguments = null;
        try
        {
            if (result.HasValue && dialog.DialogResult != result)
            {
                dialog.DialogResult = result;
            }
            else
            {
                dialog.Close();
            }
        }
        finally
        {
            state.Completing = false;
            state.Closing = false;
            if (state.CompletingArguments?.Cancel == true && dialog.IsLoaded && state.Generation == generation)
            {
                // A vetoed close must leave a visible, interactive modal instead of an invisible blocker.
                SetClosingPresentation(dialog, false);
            }
            state.CompletingArguments = null;
        }
    }
}
