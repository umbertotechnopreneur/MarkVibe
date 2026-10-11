using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CSHTML5.Native.Html.Controls;

namespace MarkVibe.Controls;

/// <summary>Accessible button text with a decorative, local SVG symbol.</summary>
public sealed class IconLabel : StackPanel
{
    private readonly TextBlock _caption;
    public IconLabel(string icon, string text)
    {
        Orientation = Orientation.Horizontal; VerticalAlignment = VerticalAlignment.Center;
        var svg = new HtmlPresenter { Width = 18, Height = 18, IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Center };
        svg.Html = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"18\" height=\"18\" viewBox=\"0 0 24 24\" aria-hidden=\"true\" focusable=\"false\" style=\"display:block;pointer-events:none\"><use href=\"icons.svg#" + SecurityElement.Escape(icon) + "\"/></svg>";
        _caption = new TextBlock { Text = text, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Children.Add(svg); Children.Add(_caption);
        Loaded += (_, _) =>
        {
            DependencyObject? parent = this;
            while (parent != null && parent is not Button) parent = VisualTreeHelper.GetParent(parent);
            if (parent is Button button)
            {
                _caption.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") { Source = button });
                _caption.SetBinding(TextBlock.FontSizeProperty, new Binding("FontSize") { Source = button });
                _caption.SetBinding(TextBlock.FontWeightProperty, new Binding("FontWeight") { Source = button });
            }
        };
        Compact = text.Length == 0;
    }
    public string Text { get => _caption.Text; set => _caption.Text = value; }
    public bool Compact { get => _caption.Visibility == Visibility.Collapsed; set => _caption.Visibility = value ? Visibility.Collapsed : Visibility.Visible; }
}

internal static class ButtonIcons
{
    public static void Decorate(DependencyObject parent)
    {
        if (parent is Button button && button.Content is string caption)
        {
            var (icon, text) = Describe(caption);
            if (string.IsNullOrEmpty(System.Windows.Automation.AutomationProperties.GetName(button)))
                System.Windows.Automation.AutomationProperties.SetName(button, caption);
            ToolTipService.SetToolTip(button, ToolTipService.GetToolTip(button) ?? caption);
            button.Content = new IconLabel(icon, text);
            return;
        }
        // Walk declared content too: collapsed settings may not have templates yet.
        if (parent is Panel panel) { foreach (var child in panel.Children) Decorate(child); return; }
        if (parent is Border border) { if (border.Child != null) Decorate(border.Child); return; }
        if (parent is ContentControl content && content.Content is DependencyObject declared) { Decorate(declared); return; }
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) Decorate(VisualTreeHelper.GetChild(parent, i));
    }
    public static void SetText(Button button, string text)
    {
        if (button.Content is IconLabel label) label.Text = text;
        else button.Content = text;
    }
    public static void SetCompact(Button button, bool compact)
    {
        if (button.Content is IconLabel label) label.Compact = compact;
    }
    private static (string, string) Describe(string text) => text switch
    {
        "≡" => ("menu", ""), "←" => ("back", ""), "→" => ("forward", ""),
        "◐" => ("settings", ""), "ⓘ" => ("about", ""), "×" => ("close", ""),
        "−" => ("zoom-out", ""), "+" => ("zoom-in", ""),
        "Open" or "Open file" or "Choose missing file" => ("file", text),
        "Open folder" or "Reopen folder" => ("folder", text),
        "Read" => ("book", text), "Source" => ("code", text), "Split" => ("split", text), "Map" => ("map", text),
        "Pages" => ("pages", text), "Outline" => ("outline", text),
        "Reopen last workspace" => ("history", text), "Explore sample workspace" => ("sparkles", text),
        "Cancel" or "Close" => ("close", text), "Reset preferences" => ("reset", text),
        "Export HTML…" => ("html", text), "Export PDF…" => ("pdf", text),
        "Import folder" => ("import", text), "Import documentation ZIP" => ("archive", text),
        "Add related files" => ("add", text), "Refresh folder" => ("refresh", text),
        "About MarkVibe" => ("about", text), "Umberto Giacobbi" => ("person", text),
        "GitHub repository" => ("github", text), "MIT license" => ("license", text),
        "VibeWare manifesto" => ("vibeware", text),
        _ => ("file", text)
    };
}
