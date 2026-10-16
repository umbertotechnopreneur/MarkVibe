/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; declarative XAML backdrop material; 2026-10-11
 * Human guidance: Umberto Giacobbi; shared blur resources for XAML dialogs and flyouts
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Globalization;
using System.Windows;

namespace MarkVibe.Controls;

/// <summary>Maps a XAML material property to CSS on OpenSilver's existing element; creates no DOM controls.</summary>
public static class Backdrop
{
    public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.RegisterAttached(
        "BlurRadius", typeof(double), typeof(Backdrop), new PropertyMetadata(0d, RadiusChanged));
    /// <summary>Reads the blur radius defined by the owning resource dictionary.</summary>
    public static double GetBlurRadius(DependencyObject element) => (double)element.GetValue(BlurRadiusProperty);
    /// <summary>Assigns a declarative blur radius to a XAML surface.</summary>
    public static void SetBlurRadius(DependencyObject element, double value) => element.SetValue(BlurRadiusProperty, value);
    /// <summary>Updates loaded visuals and registers one rendering hook for unloaded controls.</summary>
    private static void RadiusChanged(DependencyObject element, DependencyPropertyChangedEventArgs arguments)
    {
        if (element is not FrameworkElement surface) return;
        surface.Loaded -= SurfaceLoaded;
        surface.Loaded += SurfaceLoaded;
        if (surface.IsLoaded) Apply(surface);
    }
    /// <summary>Applies the material after OpenSilver has allocated its visual element.</summary>
    private static void SurfaceLoaded(object sender, RoutedEventArgs arguments) => Apply((FrameworkElement)sender);
    /// <summary>Decorates only the DOM element allocated by the XAML renderer.</summary>
    private static void Apply(FrameworkElement surface)
    {
        var radius = Math.Clamp(GetBlurRadius(surface), 0, 64);
        var filter = radius > 0 ? "blur(" + radius.ToString(CultureInfo.InvariantCulture) + "px)" : "none";
        OpenSilver.Interop.ExecuteJavaScriptVoid("$0.style.backdropFilter = $1; $0.style.webkitBackdropFilter = $1;", OpenSilver.Interop.GetDiv(surface), filter);
    }
}
