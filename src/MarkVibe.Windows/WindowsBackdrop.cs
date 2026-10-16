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
using Microsoft.Win32;

namespace MarkVibe.Windows;
/// <summary>Coordinates Windows backdrop material with the visible browser and WPF composition surface.</summary>
internal static class WindowsBackdrop
{
    /// <summary>Reports whether this Windows version exposes the system-backdrop window attribute.</summary>
    internal static bool Supported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);

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

    /// <summary>Applies the requested Windows backdrop and keeps the host and WebView backgrounds consistent with its availability.</summary>
    internal static bool Apply(Window window, IWebView2 view, bool requested, bool dark)
    {
        var windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        var darkMode = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(windowHandle, 20, ref darkMode, sizeof(int));
        var transparency = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);
        var enabled = requested && Supported && !SystemParameters.HighContrast && !Equals(transparency, 0);
        var backdropType = enabled ? 3 : 1;
        if (Supported && DwmSetWindowAttribute(windowHandle, 38, ref backdropType, sizeof(int)) < 0)
        {
            enabled = false;
        }

        var margins = new Margins
        {
            Left = enabled ? -1 : 0
        };
        if (DwmExtendFrameIntoClientArea(windowHandle, ref margins) < 0)
        {
            enabled = false;
            backdropType = 1;
            if (Supported)
            {
                _ = DwmSetWindowAttribute(windowHandle, 38, ref backdropType, sizeof(int));
            }
        }

        var color = dark ? Color.FromRgb(17, 22, 33) : Color.FromRgb(244, 245, 247);
        window.Background = enabled ? Brushes.Transparent : new SolidColorBrush(color);
        if (HwndSource.FromHwnd(windowHandle)?.CompositionTarget is { } compositionTarget)
        {
            compositionTarget.BackgroundColor = enabled ? Colors.Transparent : color;
        }

        view.DefaultBackgroundColor = enabled ? System.Drawing.Color.Transparent : System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        return enabled;
    }

    /// <summary>Describes the native frame extension; a negative left margin extends the entire client area.</summary>
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
