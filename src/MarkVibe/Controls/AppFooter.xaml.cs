/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; interaction-aware publication footer lifecycle; 2026-10-16
 * Human guidance: Umberto Giacobbi; show links for 30 seconds, fade for 3, retain access on hover and keyboard focus
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MarkVibe.Controls;

/// <summary>Provides quiet, keyboard-accessible publication links without shifting the document when they fade.</summary>
public partial class AppFooter : UserControl
{
    private readonly DispatcherTimer _idleTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private Storyboard? _fade;
    private bool _pointerInside;
    private bool _focusInside;

    /// <summary>Lets the owning shell open its About dialog without coupling the footer to a page implementation.</summary>
    public event RoutedEventHandler? AboutRequested;

    /// <summary>Initializes one lifecycle timer; loading and unloading control whether it can run.</summary>
    public AppFooter()
    {
        InitializeComponent();
        _idleTimer.Tick += FadeAfterIdle;
        Loaded += FooterLoaded;
        Unloaded += FooterUnloaded;
    }

    /// <summary>Shows the footer on load and starts the initial reading interval.</summary>
    private void FooterLoaded(object sender, RoutedEventArgs arguments)
    {
        Reveal();
        ScheduleFade();
    }

    /// <summary>Stops delayed work so a detached footer cannot retain an active animation or timer.</summary>
    private void FooterUnloaded(object sender, RoutedEventArgs arguments)
    {
        _idleTimer.Stop();
        _fade?.Stop();
        _pointerInside = false;
        _focusInside = false;
    }

    /// <summary>Reveals links immediately and cancels a pending fade when the footer is being used.</summary>
    private void Reveal()
    {
        _idleTimer.Stop();
        _fade?.Stop();
        _fade = null;
        FooterLinks.Opacity = 1;
    }

    /// <summary>Starts a fresh reading interval only after both pointer and keyboard interaction have left.</summary>
    private void ScheduleFade()
    {
        _idleTimer.Stop();
        if (IsLoaded && !_pointerInside && !_focusInside)
        {
            _idleTimer.Start();
        }
    }

    /// <summary>Fades opacity rather than visibility, retaining layout space and keyboard navigation.</summary>
    private void FadeAfterIdle(object? sender, EventArgs arguments)
    {
        _idleTimer.Stop();
        if (!IsLoaded || _pointerInside || _focusInside)
        {
            return;
        }

        var reducedMotion = Convert.ToBoolean(OpenSilver.Interop.ExecuteJavaScript(
            "window.matchMedia('(prefers-reduced-motion: reduce)').matches"));
        if (reducedMotion)
        {
            FooterLinks.Opacity = 0;
            return;
        }

        var transition = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromSeconds(3))
        };
        Storyboard.SetTarget(transition, FooterLinks);
        Storyboard.SetTargetProperty(transition, new PropertyPath("Opacity"));
        _fade = new Storyboard();
        _fade.Children.Add(transition);
        _fade.Begin();
    }

    /// <summary>Restores the links as soon as the pointer enters their reserved area.</summary>
    private void PointerEntered(object sender, MouseEventArgs arguments)
    {
        _pointerInside = true;
        Reveal();
    }

    /// <summary>Restarts the reading interval after pointer interaction ends.</summary>
    private void PointerLeft(object sender, MouseEventArgs arguments)
    {
        _pointerInside = false;
        ScheduleFade();
    }

    /// <summary>Restores hidden links before a keyboard user interacts with a focused action.</summary>
    private void FocusEntered(object sender, RoutedEventArgs arguments)
    {
        _focusInside = true;
        Reveal();
    }

    /// <summary>Checks settled framework focus so moving between footer links never starts a fade.</summary>
    private void FocusLeft(object sender, RoutedEventArgs arguments)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsLoaded)
            {
                return;
            }

            _focusInside = HasFooterFocus();
            ScheduleFade();
        });
    }

    /// <summary>Recognizes focused descendants using the same focus system as the application's keyboard controls.</summary>
    private bool HasFooterFocus()
    {
        var focused = FocusManager.GetFocusedElement() as DependencyObject;
        while (focused != null)
        {
            if (ReferenceEquals(focused, this))
            {
                return true;
            }

            focused = VisualTreeHelper.GetParent(focused);
        }

        return false;
    }

    /// <summary>Requests the application's About dialog when its footer action is activated.</summary>
    private void ShowAbout(object sender, RoutedEventArgs arguments) => AboutRequested?.Invoke(this, arguments);

    /// <summary>Opens the shared terms page after explicit link activation.</summary>
    private void OpenTerms(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Terms);

    /// <summary>Opens the shared privacy page after explicit link activation.</summary>
    private void OpenPrivacy(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Privacy);

    /// <summary>Opens the canonical initiative manifesto after explicit link activation.</summary>
    private void OpenManifesto(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Manifesto);
}
