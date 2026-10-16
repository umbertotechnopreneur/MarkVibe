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
using System.IO;

namespace MarkVibe.Windows;
internal sealed record DesktopOptions(string DataDirectory, string AssetsDirectory, string? Document, bool SmokeTest, int DebugPort, string? ExportDirectory)
{
    /// <summary>Parses desktop paths and opt-in diagnostics while enforcing isolated Debug-only smoke settings.</summary>
    public static DesktopOptions Parse(string[] args)
    {
        var defaultData = File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag")) ? Path.Combine(AppContext.BaseDirectory, "data") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarkVibe", "Windows");
        var dataDirectory = defaultData;
        var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "app");
        string? document = null, exportDirectory = null;
        var smokeTest = false;
        var debugPort = 0;
        for (var argumentIndex = 0; argumentIndex < args.Length; argumentIndex++)
        {
            switch (args[argumentIndex])
            {
                case "--data-dir":
                    dataDirectory = Path.GetFullPath(args[++argumentIndex]);
                    break;
                case "--assets-dir":
                    assetsDirectory = Path.GetFullPath(args[++argumentIndex]);
                    break;
                case "--open":
                    document = Path.GetFullPath(args[++argumentIndex]);
                    break;
                case "--smoke-test":
                    smokeTest = true;
                    break;
                case "--debug-port":
                    debugPort = int.Parse(args[++argumentIndex]);
                    break;
                case "--export-dir":
                    exportDirectory = Path.GetFullPath(args[++argumentIndex]);
                    break;
                default:
                    throw new ArgumentException("Unknown desktop argument: " + args[argumentIndex]);
            }
        }

        // Diagnostic capabilities are opt-in, isolated, and unavailable in Release.
#if !DEBUG
        if (smokeTest || debugPort != 0 || exportDirectory != null)
        {
            throw new ArgumentException("Smoke options are available only in Debug builds.");
        }

#endif
        if ((debugPort != 0 ||
            exportDirectory != null) &&
            !smokeTest ||
            smokeTest &&
            (string.Equals(dataDirectory, defaultData, StringComparison.OrdinalIgnoreCase) ||
            exportDirectory == null))
        {
            throw new ArgumentException("Smoke mode requires an isolated --data-dir and --export-dir.");
        }

        if (debugPort is < 0 or > 65535)
        {
            throw new ArgumentException("Invalid debug port.");
        }

        return new(dataDirectory, assetsDirectory, document, smokeTest, debugPort, exportDirectory);
    }
}
