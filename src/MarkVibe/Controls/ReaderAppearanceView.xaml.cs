/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; catalog-backed context appearance controls; 2026-10-23
 * Human guidance: Umberto Giacobbi; immediate document and code themes and optional link underlines
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Edits the same mode-specific theme IDs as Settings without taking ownership of unrelated preferences.</summary>
public partial class ReaderAppearanceView : UserControl
{
    private bool _updating = true;

    /// <summary>Requests one preference change through the shell's normal persistence and error boundary.</summary>
    public event Func<string, string, Task>? PreferenceChanged;

    /// <summary>Keeps the containing menu open while a theme list handles its pointer gesture.</summary>
    public bool IsThemeListOpen => LightDocumentThemeCombo.IsDropDownOpen
        || DarkDocumentThemeCombo.IsDropDownOpen
        || LightCodeThemeCombo.IsDropDownOpen
        || DarkCodeThemeCombo.IsDropDownOpen
        || DocumentFontCombo.IsDropDownOpen;

    /// <summary>Loads all four lists from the shared theme catalog used by Settings.</summary>
    public ReaderAppearanceView()
    {
        InitializeComponent();
        Populate(LightDocumentThemeCombo, ReaderThemes.Documents("light"));
        Populate(DarkDocumentThemeCombo, ReaderThemes.Documents("dark"));
        Populate(LightCodeThemeCombo, ReaderThemes.Code("light"));
        Populate(DarkCodeThemeCombo, ReaderThemes.Code("dark"));
        DocumentFontCombo.Items.Add(new ComboBoxItem { Content = "Theme font", Tag = "system" });
    }

    /// <summary>Lists verified installed monospace families and keeps the theme's original font available.</summary>
    public void SetFonts(IEnumerable<UiFontOption> fonts, string selectedFont)
    {
        _updating = true;
        DocumentFontCombo.Items.Clear();
        DocumentFontCombo.Items.Add(new ComboBoxItem { Content = "Theme font", Tag = "system" });
        foreach (var font in fonts.Where(font => font.Id != "system").DistinctBy(font => font.Id))
        {
            DocumentFontCombo.Items.Add(new ComboBoxItem
            {
                Content = font.Label,
                Tag = font.Id,
                FontFamily = new System.Windows.Media.FontFamily(font.Label)
            });
        }

        Select(DocumentFontCombo, selectedFont);
        DocumentFontCombo.IsEnabled = DocumentFontCombo.Items.Count > 1;
        _updating = false;
    }

    /// <summary>Uses stable IDs for persistence while displaying catalog labels.</summary>
    private static void Populate(ComboBox combo, IEnumerable<ReaderThemeChoice> themes)
    {
        foreach (var theme in themes)
        {
            combo.Items.Add(new ComboBoxItem { Content = theme.Label, Tag = theme.Id });
        }
    }

    /// <summary>Refreshes selections without recursively saving programmatic changes.</summary>
    public void Update(Preferences preferences)
    {
        _updating = true;
        Select(LightDocumentThemeCombo, preferences.LightDocumentTheme);
        Select(DarkDocumentThemeCombo, preferences.DarkDocumentTheme);
        Select(LightCodeThemeCombo, preferences.LightCodeTheme);
        Select(DarkCodeThemeCombo, preferences.DarkCodeTheme);
        UnderlineLinksCheck.IsChecked = preferences.UnderlineLinks;
        Select(DocumentFontCombo, preferences.DocumentFont);
        _updating = false;
    }

    /// <summary>Matches the persisted ID to the existing catalog item.</summary>
    private static void Select(ComboBox combo, string id)
    {
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(item => (string)item.Tag == id)
            ?? combo.Items[0];
    }

    /// <summary>Publishes only the changed theme, keeping rapid selections independent.</summary>
    private async void ThemeChanged(object sender, SelectionChangedEventArgs arguments)
    {
        if (!_updating && sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && PreferenceChanged != null)
        {
            await PreferenceChanged((string)combo.Tag, (string)item.Tag);
        }
    }

    /// <summary>Changes link decoration independently of the selected document palette.</summary>
    private async void UnderlineChanged(object sender, RoutedEventArgs arguments)
    {
        if (!_updating && PreferenceChanged != null)
        {
            await PreferenceChanged("underlineLinks", UnderlineLinksCheck.IsChecked == true ? "true" : "false");
        }
    }

    /// <summary>Removes this folder's override so future global Settings changes apply again.</summary>
    private async void ResetFolderAppearance(object sender, RoutedEventArgs arguments)
    {
        if (!_updating && PreferenceChanged != null)
        {
            await PreferenceChanged("reset", "");
        }
    }
}
