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
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;
public partial class AboutWindow : ChildWindow
{
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/MarkVibe";
    /// <summary>Initializes the About dialog, local images, version text and language refresh handlers.</summary>
    public AboutWindow()
    {
        InitializeComponent();
        HeroImage.Source = IconLabel.Asset("images/about-hero.png");
        VibeWareLogo.Source = IconLabel.Asset("images/vibeware-logo.png");
        var assembly = typeof(AboutWindow).Assembly;
        VersionText.Text = (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? UiStrings.T("Development")).Split('+')[0];
        var built = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(item => item.Key == "BuildTimestamp")?.Value;
        AppLocalization.SetText(BuiltText, string.IsNullOrEmpty(built) ? "Local development build" : "{0}", built);
        var root = Application.Current.RootVisual as FrameworkElement;
        Width = Math.Max(280, Math.Min(1000, (root?.ActualWidth ?? 1048) - 48));
        Height = Math.Max(300, Math.Min(680, (root?.ActualHeight ?? 728) - 48));
        if (Width < 700)
        {
            Grid.SetRow(LinksColumn, 1);
            Grid.SetColumn(LinksColumn, 0);
            Grid.SetColumnSpan(LinksColumn, 3);
            Grid.SetColumnSpan(StoryColumn, 3);
            LinksColumn.Margin = new Thickness(0, 24, 0, 0);
        }

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
        AppLocalization.Apply(this);
    }

    /// <summary>Opens an About link in a separate browser context with opener access disabled.</summary>
    private static void Open(string url) => OpenSilver.Interop.ExecuteJavaScriptVoid("window.open($0, '_blank', 'noopener,noreferrer')", url);
    /// <summary>Opens the maintainer's website.</summary>
    private void Author(object sender, RoutedEventArgs e) => Open("https://umbertogiacobbi.biz");
    /// <summary>Opens the project's GitHub repository.</summary>
    private void Repository(object sender, RoutedEventArgs e) => Open(RepositoryUrl);
    /// <summary>Opens the repository's MIT license page.</summary>
    private void License(object sender, RoutedEventArgs e) => Open(RepositoryUrl + "/blob/master/LICENSE");
    /// <summary>Opens the repository's issue tracker.</summary>
    private void ReportIssue(object sender, RoutedEventArgs e) => Open(RepositoryUrl + "/issues");
    /// <summary>Opens the VibeWare initiative page.</summary>
    private void VibeWare(object sender, RoutedEventArgs e) => Open("https://umbertogiacobbi.biz/vibeware");
    /// <summary>Opens the VibeWare manifesto.</summary>
    private void Manifesto(object sender, RoutedEventArgs e) => Open("https://umbertogiacobbi.biz/vibeware/manifesto");
    /// <summary>Closes the About dialog.</summary>
    private void Dismiss(object sender, RoutedEventArgs e) => Close();
}
