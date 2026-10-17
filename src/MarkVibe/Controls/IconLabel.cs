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
 * Modified with AI: OpenAI Codex; Markdown AI toolbar entry; 2026-10-11
 * Modified with AI: OpenAI Codex; map arrangement, metadata and reset icon captions; 2026-10-16
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Modified with AI: OpenAI Codex; idempotent icon lifecycle and shared declared UI traversal; 2026-10-16
 * Human guidance: Umberto Giacobbi; avoid repeated event handlers and preserve caller-owned labels
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MarkVibe.Core;

namespace MarkVibe.Controls;
/// <summary>Accessible XAML captions with scalable local vector templates.</summary>
public sealed class IconLabel : Grid
{
    private readonly TextBlock _caption;
    private readonly VectorIcon _glyph;
    private ButtonBase? _ownerButton;
    public static readonly DependencyProperty HideIconProperty = DependencyProperty.RegisterAttached("HideIcon", typeof(bool), typeof(IconLabel), new PropertyMetadata(false));
    /// <summary>Reads the explicit opt-out used by text-only close and footer links.</summary>
    public static bool GetHideIcon(DependencyObject element) => (bool)element.GetValue(HideIconProperty);
    /// <summary>Marks a button or link as text-only.</summary>
    public static void SetHideIcon(DependencyObject element, bool value) => element.SetValue(HideIconProperty, value);
    public string CaptionKey { get; private set; }

    /// <summary>Creates an icon and caption that can wrap inside compact action grids.</summary>
    public IconLabel(string icon, string text)
    {
        CaptionKey = text;
        VerticalAlignment = VerticalAlignment.Center;
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _glyph = new VectorIcon { Symbol = icon, VerticalAlignment = VerticalAlignment.Center };
        Children.Add(_glyph);

        _caption = new TextBlock
        {
            Text = UiStrings.T(text),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(7, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        SetColumn(_caption, 1);
        Children.Add(_caption);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Compact = text.Length == 0;
    }

    /// <summary>Inherits the owning button's text colors and font settings.</summary>
    private void OnLoaded(object sender, RoutedEventArgs arguments)
    {
        DetachOwner();
        DependencyObject? parent = this;
        while (parent != null && parent is not ButtonBase)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        if (parent is ButtonBase button)
        {
            _ownerButton = button;
            _glyph.SetBinding(Control.ForegroundProperty, new Binding("Foreground") { Source = button });
            button.MouseEnter += OwnerMouseEntered;
            button.MouseLeave += OwnerMouseLeft;
            _caption.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") { Source = button });
            _caption.SetBinding(TextBlock.FontSizeProperty, new Binding("FontSize") { Source = button });
            _caption.SetBinding(TextBlock.FontWeightProperty, new Binding("FontWeight") { Source = button });
        }
    }

    /// <summary>Releases subscriptions and owner bindings when the caption leaves its button's visual tree.</summary>
    private void OnUnloaded(object sender, RoutedEventArgs arguments)
    {
        DetachOwner();
    }

    /// <summary>Removes the previous owner's handlers before unloading or attaching to a new button.</summary>
    private void DetachOwner()
    {
        if (_ownerButton == null)
        {
            return;
        }

        _ownerButton.MouseEnter -= OwnerMouseEntered;
        _ownerButton.MouseLeave -= OwnerMouseLeft;
        _ownerButton = null;
        _glyph.ClearValue(Control.ForegroundProperty);
        _caption.ClearValue(TextBlock.ForegroundProperty);
        _caption.ClearValue(TextBlock.FontSizeProperty);
        _caption.ClearValue(TextBlock.FontWeightProperty);
    }

    /// <summary>Highlights the glyph while its owning action is hovered.</summary>
    private void OwnerMouseEntered(object sender, MouseEventArgs arguments)
    {
        _glyph.Foreground = (Brush)Application.Current.Resources["SystemAccent"];
    }

    /// <summary>Restores the owner's foreground binding after its hover highlight ends.</summary>
    private void OwnerMouseLeft(object sender, MouseEventArgs arguments)
    {
        if (_ownerButton != null)
        {
            _glyph.SetBinding(Control.ForegroundProperty, new Binding("Foreground") { Source = _ownerButton });
        }
    }

    /// <summary>Resolves assets against the browser or native WebView application root.</summary>
    public static BitmapImage Asset(string path)
    {
        var address = Convert.ToString(OpenSilver.Interop.ExecuteJavaScript("new URL($0, document.baseURI).href", path)) ?? "";
        return new BitmapImage(new Uri(address, UriKind.Absolute));
    }

    public string Text { get => _caption.Text; set => _caption.Text = value; }

    /// <summary>Retains the original caption key for future language changes.</summary>
    public void SetCaption(string key)
    {
        CaptionKey = key;
        Text = UiStrings.T(key);
    }

    /// <summary>Updates the caption after the selected application language changes.</summary>
    public void RefreshLanguage()
    {
        Text = UiStrings.T(CaptionKey);
    }

    public bool Compact { get => _caption.Visibility == Visibility.Collapsed; set => _caption.Visibility = value ? Visibility.Collapsed : Visibility.Visible; }
}

/// <summary>Applies icon captions while preserving accessible labels and tooltips.</summary>
internal static class ButtonIcons
{
    /// <summary>Decorates declared content, including controls not yet in the visual tree.</summary>
    public static void Decorate(DependencyObject parent)
    {
        Walk(parent, new HashSet<DependencyObject>());
    }

    /// <summary>Visits each application child once while leaving caller-owned labels and declared-content templates intact.</summary>
    private static void Walk(DependencyObject parent, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(parent) || AppLocalization.IsRaw(parent))
        {
            return;
        }

        if (parent is ButtonBase button && parent is (Button or HyperlinkButton) && button.Content is string caption)
        {
            var(icon, text) = Describe(caption);
            var accessibleName = System.Windows.Automation.AutomationProperties.GetName(button);
            if (string.IsNullOrEmpty(accessibleName))
            {
                accessibleName = caption;
                System.Windows.Automation.AutomationProperties.SetName(button, accessibleName);
            }

            ToolTipService.SetToolTip(button, ToolTipService.GetToolTip(button) ?? accessibleName);
            if (!IconLabel.GetHideIcon(button))
            {
                button.Content = new IconLabel(icon, text);
            }

            return;
        }

        // Declared children may belong to a collapsed tab without a template yet.
        var declaredChildren = UiTree.DeclaredChildren(parent).ToArray();
        foreach (var child in declaredChildren)
        {
            Walk(child, visited);
        }

        // Content templates own their glyphs; decorate application content without reinterpreting template controls.
        if (parent is Panel or Border or UserControl or Popup || parent is ContentControl && declaredChildren.Length > 0)
        {
            return;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            Walk(VisualTreeHelper.GetChild(parent, index), visited);
        }
    }

    /// <summary>Updates a button caption without losing its icon or translation key.</summary>
    public static void SetText(Button button, string text)
    {
        if (button.Content is IconLabel label)
        {
            label.SetCaption(text);
        }
        else
        {
            button.Content = UiStrings.T(text);
        }
    }

    /// <summary>Hides the caption at narrow widths while retaining its tooltip.</summary>
    public static void SetCompact(Button button, bool compact)
    {
        if (button.Content is IconLabel label)
        {
            label.Compact = compact;
        }
    }

    /// <summary>Maps shell actions to their local visual symbols.</summary>
    private static (string, string) Describe(string text) => text switch
    {
        "≡" => ("menu", ""),
        "←" => ("back", ""),
        "→" => ("forward", ""),
        "◐" => ("settings", ""),
        "Settings" => ("settings", ""),
        "ⓘ" => ("about", ""),
        "×" => ("close", ""),
        "−" => ("zoom-out", ""),
        "+" => ("zoom-in", ""),
        "Open" or "Open file" or "Choose missing file" => ("file", text),
        "Open folder" or "Reopen folder" => ("folder", text),
        "Read" => ("book", text),
        "Source" => ("code", text),
        "Split" => ("split", text),
        "Map" => ("map", text),
        "Star" => ("layout-star", text),
        "Circle" => ("layout-circle", text),
        "Grid" => ("layout-grid", text),
        "Advanced view" => ("review", text),
        "Reset" or "Reset all" => ("reset", text),
        "AI" => ("sparkles", text),
        "Pages" => ("pages", text),
        "Outline" => ("outline", text),
        "Reopen last workspace" => ("history", text),
        "Explore sample workspace" => ("sparkles", text),
        "Cancel" or "Close" => ("close", text),
        "Reset preferences" => ("reset", text),
        "Export HTML…" => ("html", text),
        "Export PDF…" => ("pdf", text),
        "Import folder" => ("import", text),
        "Import documentation ZIP" => ("archive", text),
        "Add related files" => ("add", text),
        "Refresh folder" => ("refresh", text),
        "About MarkVibe" => ("about", text),
        "Umberto Giacobbi" => ("person", text),
        "GitHub repository" => ("github", text),
        "MIT license" => ("license", text),
        "VibeWare manifesto" => ("vibeware", text),
        "Report a problem" => ("external", text),
        _ => ("file", text)};
}
