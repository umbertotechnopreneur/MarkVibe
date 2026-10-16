/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; contained Explorer and terminal actions; 2026-10-16
 * Human guidance: Umberto Giacobbi; expose local file actions in document context menus
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using MarkVibe.Core;
using MarkVibe.Native;

namespace MarkVibe.Windows;

/// <summary>Opens user-selected local locations without treating their names as shell commands.</summary>
internal static class DesktopFileActions
{
    /// <summary>Reveals a validated item in Explorer or opens an interactive terminal in its containing directory.</summary>
    public static void OpenLocation(NativeFiles files, string workspaceId, string path, bool terminal)
    {
        var fullPath = files.ResolveLocation(workspaceId, path);
        var directory = Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath)!;
        if (terminal)
        {
            var terminalAlias = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "wt.exe");
            var start = new ProcessStartInfo { WorkingDirectory = directory, UseShellExecute = false };
            if (File.Exists(terminalAlias))
            {
                start.FileName = terminalAlias;
                start.ArgumentList.Add("-d");
                start.ArgumentList.Add(directory);
            }
            else
            {
                start.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                start.ArgumentList.Add("-NoLogo");
                start.ArgumentList.Add("-NoProfile");
                start.ArgumentList.Add("-NoExit");
            }
            Process.Start(start);
            return;
        }

        if (Directory.Exists(fullPath))
        {
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) { UseShellExecute = false };
            start.ArgumentList.Add(fullPath);
            Process.Start(start);
            return;
        }

        // An item identifier avoids quoting or command-line parsing for document names.
        Marshal.ThrowExceptionForHR(SHParseDisplayName(fullPath, IntPtr.Zero, out var item, 0, out _));
        try
        {
            Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(item, 0, IntPtr.Zero, 0));
        }
        finally
        {
            Marshal.FreeCoTaskMem(item);
        }
    }

    /// <summary>Converts an absolute filesystem item into a Shell-owned identifier.</summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(string name, IntPtr bindContext, out IntPtr item, uint attributes, out uint actualAttributes);

    /// <summary>Opens the parent folder and selects the single fully qualified Shell item.</summary>
    [DllImport("shell32.dll")]
    private static extern int SHOpenFolderAndSelectItems(IntPtr item, uint count, IntPtr children, uint flags);
}
