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
 * Modified with AI: OpenAI Codex; accept the composed browser through its shared interface; 2026-10-16
 * Human guidance: Umberto Giacobbi; coordinate native material with stable Windows rendering
 * Modified with AI: OpenAI Codex; keep native presentation opaque; 2026-10-16
 * Human guidance: Umberto Giacobbi; remove compositing holes during map navigation
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;

namespace MarkVibe.Windows;
/// <summary>Applies native theme colors while keeping the browser and desktop surface opaque.</summary>
internal static class WindowsBackdrop
{
    /// <summary>Reports native Acrylic availability; this host deliberately uses opaque presentation.</summary>
    internal static bool Supported => false;

    /// <summary>Reads the current system accent, falling back to the application accent when DWM is unavailable.</summary>
    internal static string Accent
    {
        get
        {
            if (DwmGetColorizationColor(out var color, out _) < 0)
            {
                return "#525CE8";
            }

            return "#" + (color & 0xFFFFFF).ToString("X6", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Applies the title-bar theme and opaque client colors, returning false for native Acrylic.</summary>
    internal static bool Apply(Window window, IWebView2 view, bool dark)
    {
        var windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        var darkMode = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(windowHandle, 20, ref darkMode, sizeof(int));
        // Do not extend glass behind the document browser. Partial native repaints
        // must never reveal the desktop beneath an otherwise opaque application.
        var backdropType = 1;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
        {
            _ = DwmSetWindowAttribute(windowHandle, 38, ref backdropType, sizeof(int));
        }

        var margins = new Margins();
        _ = DwmExtendFrameIntoClientArea(windowHandle, ref margins);

        var color = dark ? Color.FromRgb(17, 22, 33) : Color.FromRgb(244, 245, 247);
        window.Background = new SolidColorBrush(color);
        if (HwndSource.FromHwnd(windowHandle)?.CompositionTarget is { } compositionTarget)
        {
            compositionTarget.BackgroundColor = color;
        }

        view.DefaultBackgroundColor = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        return false;
    }

    /// <summary>Describes native frame margins; zero values keep glass outside the client area.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left, Right, Top, Bottom;
    }

    /// <summary>Sets a native Desktop Window Manager attribute and returns its HRESULT.</summary>
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    /// <summary>Extends the native window frame by the supplied margins and returns its HRESULT.</summary>
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr window, ref Margins margins);
    /// <summary>Reads the Windows accent color and opacity flag from Desktop Window Manager.</summary>
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(out uint color, [MarshalAs(UnmanagedType.Bool)] out bool opaque);
}
