/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; XAML vector resource selection; 2026-10-11
 * Human guidance: Umberto Giacobbi; all application controls in XAML
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MarkVibe.Controls;

/// <summary>Scales a local XAML icon template with semantic action colors and inherited fallback foreground.</summary>
public partial class VectorIcon : UserControl
{
    private Button? _ownerButton;
    public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
        nameof(Symbol), typeof(string), typeof(VectorIcon), new PropertyMetadata("file", SymbolChanged));
    public string Symbol { get => (string)GetValue(SymbolProperty); set => SetValue(SymbolProperty, value); }

    /// <summary>Initializes the vector presenter; no browser-created icon markup is involved.</summary>
    public VectorIcon()
    {
        InitializeComponent();
        Glyph.SetBinding(ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });
        Loaded += (_, _) =>
        {
            Refresh();
            AttachHover();
        };
        Unloaded += (_, _) => DetachHover();
    }

    /// <summary>Colors direct button icons on hover; caption controls manage their own icons.</summary>
    private void AttachHover()
    {
        DetachHover();
        DependencyObject? current = VisualTreeHelper.GetParent(this);
        while (current != null)
        {
            if (current is IconLabel)
            {
                return;
            }
            if (current is Button button)
            {
                _ownerButton = button;
                BindOwnerForeground();
                button.MouseEnter += HighlightIcon;
                button.MouseLeave += RestoreIcon;
                return;
            }
            current = VisualTreeHelper.GetParent(current);
        }
    }

    /// <summary>Releases hover handlers when an icon is unloaded or reparented.</summary>
    private void DetachHover()
    {
        if (_ownerButton == null)
        {
            return;
        }
        _ownerButton.MouseEnter -= HighlightIcon;
        _ownerButton.MouseLeave -= RestoreIcon;
        _ownerButton = null;
    }

    /// <summary>Highlights an enabled glyph without adding any button chrome.</summary>
    private void HighlightIcon(object sender, System.Windows.Input.MouseEventArgs arguments)
    {
        if (_ownerButton?.IsEnabled != true)
        {
            return;
        }
        Foreground = Symbol switch
        {
            "close" => new SolidColorBrush(Color.FromArgb(255, 223, 61, 80)),
            "send" => new SolidColorBrush(Color.FromArgb(255, 214, 244, 229)),
            _ => ActionIconPalette.ForSymbol(Symbol) ?? (Brush)Application.Current.Resources["SystemAccent"]
        };
    }

    /// <summary>Restores the owner's live foreground binding after hover.</summary>
    private void RestoreIcon(object sender, System.Windows.Input.MouseEventArgs arguments)
    {
        BindOwnerForeground();
    }

    /// <summary>Uses shared semantic palette brushes or the owning button's live foreground binding.</summary>
    private void BindOwnerForeground()
    {
        if (_ownerButton != null)
        {
            var color = ActionIconPalette.ForSymbol(Symbol);
            if (color != null)
            {
                Foreground = color;
            }
            else
            {
                SetBinding(ForegroundProperty, new Binding(nameof(Foreground)) { Source = _ownerButton });
            }
        }
    }

    /// <summary>Refreshes the template when a reusable icon changes its symbol.</summary>
    private static void SymbolChanged(DependencyObject element, DependencyPropertyChangedEventArgs arguments)
    {
        var icon = (VectorIcon)element;
        icon.Refresh();
        icon.BindOwnerForeground();
    }

    /// <summary>Uses a repository template with an explicit file-symbol fallback.</summary>
    private void Refresh()
    {
        if (Glyph == null) return;
        var resources = Application.Current.Resources;
        var key = "Vector-" + Symbol;
        Glyph.Style = (Style)resources[resources.Contains(key) ? key : "Vector-file"];
    }
}
