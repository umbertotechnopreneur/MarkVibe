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
 * Modified with AI: OpenAI Codex; publication links, local vector icons and readable version metadata; 2026-10-16
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;

/// <summary>Explains MarkVibe's origin, human responsibility and publication links without implying release acceptance.</summary>
public partial class AboutWindow : ChildWindow
{
    private static readonly FontFamily AboutFont = new("Cascadia Mono, Consolas, monospace");

    /// <summary>Initializes the About dialog, local images, version text and language refresh handlers.</summary>
    public AboutWindow()
    {
        InitializeComponent();
        ProductIcon.Source = IconLabel.Asset("favicon.svg");
        VibeWareLogo.Source = IconLabel.Asset("images/vibeware-logo.png");
        RepositoryLink.Content = new IconLabel("github", "MarkVibe on GitHub");
        InitiativeLink.Content = new IconLabel("github", "VibeWare on GitHub");
        AuthorLink.Content = new IconLabel("map-globe", "umbertogiacobbi.biz");
        TermsLink.Content = new IconLabel("file", "Terms");
        PrivacyLink.Content = new IconLabel("license", "Privacy");

        var assembly = typeof(AboutWindow).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = informationalVersion?.Split('+')[0] ?? assembly.GetName().Version?.ToString();
        AppLocalization.SetText(VersionText, version == null ? "Development" : "{0}", version);
        var buildTimestamp = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(item => item.Key == "BuildTimestamp")?.Value;
        AppLocalization.SetText(
            BuiltText,
            string.IsNullOrEmpty(buildTimestamp) ? "Local development build" : "{0}",
            buildTimestamp);

        var root = Application.Current.RootVisual as FrameworkElement;
        Width = Math.Max(280, Math.Min(Width, (root?.ActualWidth ?? 688) - 48));
        Height = Math.Max(260, Math.Min(Height, (root?.ActualHeight ?? 628) - 48));

        Loaded += (_, _) =>
        {
            RefreshLanguage();
            Dispatcher.BeginInvoke(RefreshLanguage);
        };
        UiStrings.Changed += RefreshLanguage;
        Closed += (_, _) => UiStrings.Changed -= RefreshLanguage;
    }

    /// <summary>Refreshes the About title and UI labels for the selected language.</summary>
    private void RefreshLanguage()
    {
        Title = UiStrings.T("About MarkVibe");
        AppLocalization.Apply(this, AboutFont);
    }

    /// <summary>Opens the maintainer's website.</summary>
    private void Author(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Author);

    /// <summary>Opens the project's GitHub repository.</summary>
    private void Repository(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Repository);

    /// <summary>Opens the repository's MIT license page.</summary>
    private void License(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Repository + "/blob/main/LICENSE");

    /// <summary>Opens the initiative's own repository, distinct from the application's authorship.</summary>
    private void InitiativeRepository(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.InitiativeRepository);

    /// <summary>Opens the VibeWare manifesto.</summary>
    private void Manifesto(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Manifesto);

    /// <summary>Opens the shared terms page only after the user activates its link.</summary>
    private void Terms(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Terms);

    /// <summary>Opens the shared privacy page only after the user activates its link.</summary>
    private void Privacy(object sender, RoutedEventArgs arguments) => PublicationLinks.Open(PublicationLinks.Privacy);

    /// <summary>Closes the About dialog.</summary>
    private void Dismiss(object sender, RoutedEventArgs arguments) => Close();
}
