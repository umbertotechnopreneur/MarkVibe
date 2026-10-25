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

namespace MarkVibe.Windows;
/// <summary>Starts the desktop workspace or the installer's optional default-app setup.</summary>
public partial class App : Application
{
    /// <summary>Parses startup options and opens the desktop window, reporting initialization failures.</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var options = DesktopOptions.Parse(e.Args);
        DesktopLog.Initialize(options.DataDirectory);
        DispatcherUnhandledException += (_, args) => DesktopLog.Error("application.dispatcherFailure", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception error)
            {
                DesktopLog.Error("application.unhandledFailure", error);
            }
        };
        TaskScheduler.UnobservedTaskException += (_, args) => DesktopLog.Error("application.unobservedTask", args.Exception);
        try
        {
            if (options.ConfigureFileAssociations)
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                MarkdownFileAssociations.Offer(options);
                Shutdown();
                return;
            }

            MainWindow = new DesktopWindow(options);
            MainWindow.ContentRendered += (_, _) => MarkdownFileAssociations.Offer(options, MainWindow);
            MainWindow.Show();
        }
        catch (Exception error)
        {
            DesktopLog.Error("application.startupFailure", error);
            throw;
        }
    }

    /// <summary>Records normal shutdown without changing the application's exit status.</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        DesktopLog.Write("info", "application.exit", new { exitCode = e.ApplicationExitCode });
        base.OnExit(e);
    }
}
