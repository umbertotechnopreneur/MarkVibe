using System.IO;

namespace MarkVibe.Windows;

internal sealed record DesktopOptions(string DataDirectory, string AssetsDirectory, string? Document, bool SmokeTest, int DebugPort, string? ExportDirectory)
{
    public static DesktopOptions Parse(string[] args)
    {
        var defaultData = File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag"))
            ? Path.Combine(AppContext.BaseDirectory, "data")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarkVibe", "Windows");
        var data = defaultData;
        var assets = Path.Combine(AppContext.BaseDirectory, "app");
        string? document = null, exports = null; var smoke = false; var port = 0;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--data-dir": data = Path.GetFullPath(args[++i]); break;
                case "--assets-dir": assets = Path.GetFullPath(args[++i]); break;
                case "--open": document = Path.GetFullPath(args[++i]); break;
                case "--smoke-test": smoke = true; break;
                case "--debug-port": port = int.Parse(args[++i]); break;
                case "--export-dir": exports = Path.GetFullPath(args[++i]); break;
                default: throw new ArgumentException("Unknown desktop argument: " + args[i]);
            }
        }
        // Diagnostic capabilities are opt-in, isolated, and unavailable in Release.
#if !DEBUG
        if (smoke || port != 0 || exports != null) throw new ArgumentException("Smoke options are available only in Debug builds.");
#endif
        if ((port != 0 || exports != null) && !smoke || smoke && (string.Equals(data, defaultData, StringComparison.OrdinalIgnoreCase) || exports == null))
            throw new ArgumentException("Smoke mode requires an isolated --data-dir and --export-dir.");
        if (port is < 0 or > 65535) throw new ArgumentException("Invalid debug port.");
        return new(data, assets, document, smoke, port, exports);
    }
}
