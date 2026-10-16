/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; readability and OpenAI key settings; 2026-10-11
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;
/// <summary>Native appearance capabilities available to the open settings dialog.</summary>
internal sealed record SettingsAvailability(bool Desktop, bool AcrylicAvailable);
/// <summary>Edits persistent preferences in a responsive, two-column dialog.</summary>
public partial class SettingsWindow : ChildWindow
{
    private static readonly string[] Languages = ["system", "en", "it", "fr", "vi", "zh-CN", "yue-HK", "id", "ko", "ja"];
    private static readonly string[] Appearances = ["system", "light", "dark"];
    private static readonly string[] DocumentStyles = ["technical", "editorial", "compact"];
    private readonly Func<Preferences, Task<bool>> _apply;
    private readonly FrameworkElement? _applicationRoot;
    private Preferences _preferences;
    private bool _updating;
    private bool _templateLoaded;
    private readonly CancellationTokenSource _openAiLifetime = new();
    private bool _openAiLoaded;
    private bool _openAiBusy;
    private bool _openAiStorageAvailable;
    private bool _desktop;
    private bool _closed;
    /// <summary>Loads available themes and connects persisted appearance and account settings.</summary>
    internal SettingsWindow(Preferences preferences, SettingsAvailability availability, Func<Preferences, Task<bool>> apply)
    {
        _preferences = preferences;
        _apply = apply;
        _updating = true;
        InitializeComponent();
        PopulateThemes(LightDocumentThemeCombo, ReaderThemes.Documents("light"));
        PopulateThemes(DarkDocumentThemeCombo, ReaderThemes.Documents("dark"));
        PopulateThemes(LightCodeThemeCombo, ReaderThemes.Code("light"));
        PopulateThemes(DarkCodeThemeCombo, ReaderThemes.Code("dark"));
        _applicationRoot = Application.Current.RootVisual as FrameworkElement;
        ResizeDialog();
        Update(preferences, availability);
        Loaded += OnLoaded;
        UiStrings.Changed += RefreshLanguage;
        if (_applicationRoot != null)
        {
            _applicationRoot.SizeChanged += ApplicationResized;
        }

        Closed += OnClosed;
    }

    /// <summary>Refreshes template controls after the dialog has been displayed.</summary>
    private async void OnLoaded(object sender, RoutedEventArgs arguments)
    {
        _templateLoaded = true;
        ResizeDialog();
        RefreshLanguage();
        Dispatcher.BeginInvoke(RefreshLanguage);
        if (!_openAiLoaded)
        {
            _openAiLoaded = true;
            await LoadOpenAiStatusAsync();
        }
    }

    /// <summary>Reflows the dialog when its application window is resized.</summary>
    private void ApplicationResized(object sender, SizeChangedEventArgs arguments)
    {
        ResizeDialog();
    }

    /// <summary>Releases application and language event subscriptions on close.</summary>
    private void OnClosed(object? sender, EventArgs arguments)
    {
        _closed = true;
        _openAiLifetime.Cancel();
        _openAiLifetime.Dispose();
        OpenAiApiKeyBox.Password = "";
        UiStrings.Changed -= RefreshLanguage;
        if (_applicationRoot != null)
        {
            _applicationRoot.SizeChanged -= ApplicationResized;
        }
    }

    /// <summary>Keeps two columns at normal widths and stacks sections on narrow windows.</summary>
    private void ResizeDialog()
    {
        var dialogWidth = Math.Max(260, Math.Min(1000, (_applicationRoot?.ActualWidth ?? 1048) - 48));
        var dialogHeight = Math.Max(240, Math.Min(700, (_applicationRoot?.ActualHeight ?? 748) - 48));
        // OpenSilver expands ChildWindow itself to the overlay size after opening.
        // Resize the named content frame, leaving the modal overlay at window size.
        if (_templateLoaded && GetTemplateChild("ContentRoot")is FrameworkElement frame)
        {
            frame.Width = dialogWidth;
            frame.Height = dialogHeight;
        }
        else
        {
            Width = dialogWidth;
            Height = dialogHeight;
        }

        var narrow = dialogWidth < 680;
        Grid.SetColumnSpan(PreferencesColumn, narrow ? 3 : 1);
        Grid.SetRow(ActionsColumn, narrow ? 1 : 0);
        Grid.SetColumn(ActionsColumn, narrow ? 0 : 2);
        Grid.SetColumnSpan(ActionsColumn, narrow ? 3 : 1);
        ActionsColumn.Margin = narrow ? new Thickness(0, 16, 0, 0) : new Thickness(0);
        Grid.SetRow(OpenAiColumn, narrow ? 2 : 1);
        SettingsColumns.Margin = new Thickness(narrow ? 16 : 28, 4, narrow ? 16 : 28, 24);
    }

    /// <summary>Adds catalog names while retaining stable preference identifiers.</summary>
    private static void PopulateThemes(ComboBox combo, IEnumerable<ReaderThemeChoice> choices)
    {
        foreach (var theme in choices)
        {
            combo.Items.Add(new ComboBoxItem { Content = theme.Label, Tag = theme.Id });
        }
    }

    /// <summary>Selects a persisted theme, falling back to the catalog default.</summary>
    private static void SelectTheme(ComboBox combo, string id)
    {
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(choice => (string)choice.Tag == id) ?? combo.Items[0];
    }

    /// <summary>Gets the stable catalog identifier instead of a translated label.</summary>
    private static string ThemeId(ComboBox combo)
    {
        return (combo.SelectedItem as ComboBoxItem)?.Tag as string ?? "markvibe";
    }

    /// <summary>Synchronizes external state without triggering another preference save.</summary>
    internal void Update(Preferences preferences, SettingsAvailability availability)
    {
        _updating = true;
        _preferences = preferences;
        _desktop = availability.Desktop;
        AppearanceCombo.SelectedIndex = Array.IndexOf(Appearances, preferences.Appearance);
        LanguageCombo.SelectedIndex = Array.IndexOf(Languages, preferences.Language);
        StyleCombo.SelectedIndex = Array.IndexOf(DocumentStyles, preferences.DocumentStyle);
        SelectTheme(LightDocumentThemeCombo, preferences.LightDocumentTheme);
        SelectTheme(DarkDocumentThemeCombo, preferences.DarkDocumentTheme);
        SelectTheme(LightCodeThemeCombo, preferences.LightCodeTheme);
        SelectTheme(DarkCodeThemeCombo, preferences.DarkCodeTheme);
        AcrylicCheck.IsChecked = preferences.Acrylic;
        AcrylicCheck.IsEnabled = availability.Desktop && availability.AcrylicAvailable;
        SimpleSourceCheck.IsChecked = preferences.SimpleSource;
        RememberDocumentCheck.IsChecked = preferences.RememberLastDocument;
        var dark = Application.Current.Theme is OpenSilver.Themes.Modern.ModernTheme theme
            && theme.CurrentPalette == OpenSilver.Themes.Modern.ModernTheme.Palettes.Dark;
        OpenAiLogo.Source = IconLabel.Asset("images/openai-blossom-" + (dark ? "white" : "black") + ".svg");
        AppLocalization.SetText(AcrylicHelp, availability.Desktop && availability.AcrylicAvailable ? "Windows transparency settings are respected. The document stays opaque." : "Acrylic is available in the Windows app on Windows 11. A solid background is used here.");
        AppLocalization.SetText(OpenAiStorageHelp, _desktop
            ? "After validation, the key is saved as the Windows user environment variable OPENAI_API_KEY. Other programs running as your user can read it."
            : "Warning: isolated browser storage is not secure. The key is saved without encryption in this site's localStorage and can be read by its scripts.");
        _updating = false;
        RefreshLanguage();
    }

    /// <summary>Translates captions, accessible labels and tooltips together.</summary>
    private void RefreshLanguage()
    {
        Title = UiStrings.T("Settings");
        AppLocalization.Apply(this);
    }

    /// <summary>Loads credential presence and storage availability without reading the key into the dialog.</summary>
    private async Task LoadOpenAiStatusAsync()
    {
        try
        {
            var status = await BrowserServices.CallAsync<OpenAiKeyStatus>(
                "openai.status", cancellationToken: _openAiLifetime.Token);
            if (_closed)
            {
                return;
            }

            _openAiStorageAvailable = status.StorageAvailable;
            ValidateOpenAiButton.IsEnabled = status.StorageAvailable;
            AppLocalization.SetText(OpenAiStatus, !status.StorageAvailable
                ? "Key storage is unavailable on this device."
                : status.Configured
                    ? "A key is already saved. Paste a replacement to validate and save it."
                    : "Paste a key, then validate to save it.");
        }
        catch (OperationCanceledException) when (_closed)
        {
        }
        catch (Exception)
        {
            if (!_closed)
            {
                AppLocalization.SetText(OpenAiStatus, "Key storage is unavailable on this device.");
            }
        }
    }

    /// <summary>Validates and saves the candidate explicitly, preserving the saved key on request failure.</summary>
    private async void ValidateOpenAiKey(object sender, RoutedEventArgs arguments)
    {
        if (_closed || _openAiBusy || !_openAiStorageAvailable)
        {
            return;
        }

        _openAiBusy = true;
        ValidateOpenAiButton.IsEnabled = false;
        OpenAiApiKeyBox.IsEnabled = false;
        AppLocalization.SetText(OpenAiStatus, "Validating with OpenAI…");
        try
        {
            var candidateKey = OpenAiKeyRules.Normalize(OpenAiApiKeyBox.Password);
            await BrowserServices.CallAsync<OpenAiKeyStatus>(
                "openai.validate", new { apiKey = candidateKey }, _openAiLifetime.Token);
            if (_closed)
            {
                return;
            }

            // Never reload a persisted key into the password control after validation.
            OpenAiApiKeyBox.Password = "";
            AppLocalization.SetText(OpenAiStatus, _desktop
                ? "Key validated and saved to OPENAI_API_KEY."
                : "Key validated and saved in insecure isolated browser storage.");
        }
        catch (OperationCanceledException) when (_closed)
        {
        }
        catch (Exception error)
        {
            if (!_closed)
            {
                AppLocalization.SetText(OpenAiStatus, error is WorkspaceException
                    ? error.Message
                    : "OpenAI validation failed. The saved key was not changed.");
            }
        }
        finally
        {
            _openAiBusy = false;
            if (!_closed)
            {
                ValidateOpenAiButton.IsEnabled = _openAiStorageAvailable;
                OpenAiApiKeyBox.IsEnabled = true;
            }
        }
    }

    /// <summary>Saves current selections and reports whether storage accepted them.</summary>
    private async Task SaveAsync()
    {
        if (_updating || AppearanceCombo.SelectedIndex < 0 || LanguageCombo.SelectedIndex < 0 || StyleCombo.SelectedIndex < 0)
        {
            return;
        }

        _preferences = _preferences with
        {
            Appearance = Appearances[AppearanceCombo.SelectedIndex],
            Language = Languages[LanguageCombo.SelectedIndex],
            DocumentStyle = DocumentStyles[StyleCombo.SelectedIndex],
            Acrylic = AcrylicCheck.IsChecked == true,
            LightDocumentTheme = ThemeId(LightDocumentThemeCombo),
            DarkDocumentTheme = ThemeId(DarkDocumentThemeCombo),
            LightCodeTheme = ThemeId(LightCodeThemeCombo),
            DarkCodeTheme = ThemeId(DarkCodeThemeCombo),
            SimpleSource = SimpleSourceCheck.IsChecked == true,
            RememberLastDocument = RememberDocumentCheck.IsChecked == true
        };
        try
        {
            var persisted = await _apply(_preferences);
            AppLocalization.SetText(SettingsStatus, persisted ? "Changes are saved automatically." : "Preferences are available for this session. Browser storage is unavailable.");
        }
        catch (Exception error)
        {
            AppLocalization.SetText(SettingsStatus, error is WorkspaceException ? error.Message : "Preferences could not be saved. Try again.");
        }
    }

    /// <summary>Applies changes from appearance, language and theme selectors.</summary>
    private async void PreferenceChanged(object sender, SelectionChangedEventArgs arguments)
    {
        await SaveAsync();
    }

    /// <summary>Applies changes from the preference checkboxes.</summary>
    private async void CheckChanged(object sender, RoutedEventArgs arguments)
    {
        await SaveAsync();
    }

    /// <summary>Restores all settings and reports any persistence limitation.</summary>
    private async void ResetPreferences(object sender, RoutedEventArgs arguments)
    {
        try
        {
            var persisted = await _apply(new Preferences());
            AppLocalization.SetText(SettingsStatus, persisted ? "Default preferences restored." : "Preferences are available for this session. Browser storage is unavailable.");
        }
        catch (Exception)
        {
            AppLocalization.SetText(SettingsStatus, "Preferences could not be saved. Try again.");
        }
    }

    /// <summary>Closes settings without discarding saved changes.</summary>
    private void Dismiss(object sender, RoutedEventArgs arguments) => Close();
}
