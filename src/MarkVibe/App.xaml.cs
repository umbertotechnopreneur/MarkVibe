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
using System.Windows;
using MarkVibe.Services;

namespace MarkVibe;
/// <summary>Composes application models and services before creating the OpenSilver shell.</summary>
public partial class App : Application
{
    /// <summary>Owns document interaction data and services independently of the visual shell.</summary>
    public DocumentApplicationServices Documents { get; } = new();

    /// <summary>Initializes application resources and creates the main OpenSilver shell.</summary>
    public App()
    {
        InitializeComponent();
        // Attach the dictionary before loading its children. OpenSilver resolves
        // sibling StaticResource references through Application.Current.Resources.
        LoadComponent(Resources, new Uri("/MarkVibe;component/Styles/AppStyles.xaml", UriKind.Relative));
        Startup += (_, _) => RootVisual = new MainPage();
    }
}
