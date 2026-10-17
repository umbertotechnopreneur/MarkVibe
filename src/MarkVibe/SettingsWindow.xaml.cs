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
 * Modified with AI: OpenAI Codex; credential presence feedback and installed UI font options; 2026-10-16
 * Human guidance: Umberto Giacobbi; clearer key management without exposing saved credentials
 * Modified with AI: OpenAI Codex; live theme-aware validation feedback; 2026-10-16
 * Modified with AI: OpenAI Codex; selectable cancellable provider tests with explicit modal results; 2026-10-16
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private bool _openAiConfigured;
    private bool _codexAvailable;
    private bool _codexConfigured;
    private bool _connectionStatusesLoaded;
    private bool _apiStatusLoaded;
    private bool _codexStatusLoaded;
    private CancellationTokenSource? _connectionStatusCancellation;
    private bool _preferencesBusy;
    private bool _closing;
    private AiConnectionResultWindow? _testResultDialog;
    private bool _uiFontsLoaded;
    private bool _desktop;
    private bool _closed;

    /// <summary>Reports a successful explicit connection test so the shell can refresh the selected provider.</summary>
    public bool AiConnectionChanged { get; private set; }

    /// <summary>Loads available themes and connects persisted appearance and account settings.</summary>
    internal SettingsWindow(Preferences preferences, SettingsAvailability availability, Func<Preferences, Task<bool>> apply)
    {
        _preferences = preferences;
        _apply = apply;
        _updating = true;
        InitializeComponent();
        TestCodexButton.Content = new IconLabel("review", "Test");
        TestApiButton.Content = new IconLabel("review", "Test");
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

        Closing += OnClosing;
        Closed += OnClosed;
    }

    /// <summary>Cancels pending connection work as soon as the close transition starts.</summary>
    private void OnClosing(object? sender, CancelEventArgs arguments)
    {
        _closing = true;
        _openAiLifetime.Cancel();
        _testResultDialog?.Close();
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
            await Task.WhenAll(LoadOpenAiStatusAsync(), LoadUiFontsAsync());
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

        // Give translated provider labels their own line before the test action
        // would crowd either the radio button or its information icon.
        ArrangeConnectionTest(TestCodexButton, narrow);
        ArrangeConnectionTest(TestApiButton, narrow);
    }

    /// <summary>Keeps a provider's test action readable when settings sections stack.</summary>
    private static void ArrangeConnectionTest(Button button, bool narrow)
    {
        Grid.SetRow(button, narrow ? 2 : 0);
        Grid.SetColumn(button, narrow ? 0 : 2);
        Grid.SetColumnSpan(button, narrow ? 3 : 1);
        button.HorizontalAlignment = HorizontalAlignment.Left;
        button.Margin = narrow ? new Thickness(0, 8, 0, 0) : new Thickness(0);
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
        SelectTheme(UiFontCombo, preferences.UiFont);
        StyleCombo.SelectedIndex = Array.IndexOf(DocumentStyles, preferences.DocumentStyle);
        SelectTheme(LightDocumentThemeCombo, preferences.LightDocumentTheme);
        SelectTheme(DarkDocumentThemeCombo, preferences.DarkDocumentTheme);
        SelectTheme(LightCodeThemeCombo, preferences.LightCodeTheme);
        SelectTheme(DarkCodeThemeCombo, preferences.DarkCodeTheme);
        AcrylicCheck.IsChecked = preferences.Acrylic;
        AcrylicCheck.IsEnabled = availability.Desktop && availability.AcrylicAvailable;
        SimpleSourceCheck.IsChecked = preferences.SimpleSource;
        RememberDocumentCheck.IsChecked = preferences.RememberLastDocument;
        CodexProviderRadio.IsChecked = preferences.AiProvider == AiProviders.CodexCli;
        ApiProviderRadio.IsChecked = preferences.AiProvider != AiProviders.CodexCli;
        var dark = Application.Current.Theme is OpenSilver.Themes.Modern.ModernTheme theme
            && theme.CurrentPalette == OpenSilver.Themes.Modern.ModernTheme.Palettes.Dark;
        OpenAiLogo.Source = IconLabel.Asset("images/openai-blossom-" + (dark ? "white" : "black") + ".svg");
        var acrylicHelp = "Acrylic is available in the Windows app on Windows 11. A solid background is used here.";
        if (availability.Desktop)
        {
            acrylicHelp = "Native window transparency is disabled for stable rendering.";
            if (availability.AcrylicAvailable)
            {
                acrylicHelp = "Windows transparency settings are respected. The document stays opaque.";
            }
        }

        AppLocalization.SetText(AcrylicHelp, acrylicHelp);
        _updating = false;
        RefreshOpenAiConnection();
        RefreshLanguage();
    }

    /// <summary>Translates captions, accessible labels and tooltips together.</summary>
    private void RefreshLanguage()
    {
        Title = UiStrings.T("Settings");
        AppLocalization.Apply(this);
        // Localization supplies ordinary tooltip labels first; richer provider
        // help must be attached afterwards so that it remains a ToolTip object.
        SetConnectionInformation(CodexInfoButton,
            "Uses the installed Codex CLI and its own login. Test sends one short synthetic request. Your documents are not included.");
        SetConnectionInformation(ApiInfoButton,
            "Tests access to the OpenAI model list. A new key is saved only after a successful test; the saved key is kept if it fails.",
            _desktop
                ? "After validation, the key is saved as the Windows user environment variable OPENAI_API_KEY. Other programs running as your user can read it."
                : "Warning: isolated browser storage is not secure. The key is saved without encryption in this site's localStorage and can be read by its scripts.");
    }

    /// <summary>Shares compact pointer and keyboard help without keeping provider explanations in the form.</summary>
    private static void SetConnectionInformation(Button button, string key, string? details = null)
    {
        if (ToolTipService.GetToolTip(button) is not ToolTip tooltip)
        {
            tooltip = new ToolTip
            {
                Content = new TextBlock { MaxWidth = 300, TextWrapping = TextWrapping.Wrap }
            };
            ToolTipService.SetToolTip(button, tooltip);
        }

        ((TextBlock)tooltip.Content).Text = UiStrings.T(key)
            + (details == null ? "" : "\n\n" + UiStrings.T(details));
    }

    /// <summary>Lets keyboard and touch users open the same information available on hover.</summary>
    private void ShowConnectionInformation(object sender, RoutedEventArgs arguments)
    {
        if (ToolTipService.GetToolTip((DependencyObject)sender) is ToolTip tooltip)
        {
            tooltip.IsOpen = !tooltip.IsOpen;
        }
    }

    /// <summary>Dismisses explicitly opened provider help after focus leaves its action.</summary>
    private void HideConnectionInformation(object sender, RoutedEventArgs arguments)
    {
        if (ToolTipService.GetToolTip((DependencyObject)sender) is ToolTip tooltip)
        {
            tooltip.IsOpen = false;
        }
    }

    /// <summary>Lists only supported fonts reported by the current device while retaining the system fallback.</summary>
    private async Task LoadUiFontsAsync()
    {
        try
        {
            var fonts = await BrowserServices.CallAsync<UiFontOption[]>("ui.fonts", cancellationToken: _openAiLifetime.Token);
            if (_closed || _closing)
            {
                return;
            }

            var wasUpdating = _updating;
            _updating = true;
            try
            {
                UiFontCombo.Items.Clear();
                UiFontCombo.Items.Add(new ComboBoxItem { Content = "System default", Tag = "system" });
                foreach (var font in fonts.Where(font => font.Id != "system").DistinctBy(font => font.Id))
                {
                    UiFontCombo.Items.Add(new ComboBoxItem { Content = font.Label, Tag = font.Id });
                }

                SelectTheme(UiFontCombo, _preferences.UiFont);
                _uiFontsLoaded = true;
                UiFontCombo.IsEnabled = true;
            }
            finally
            {
                _updating = wasUpdating;
            }

            RefreshLanguage();
        }
        catch (OperationCanceledException) when (_closed || _closing)
        {
        }
        catch (Exception)
        {
            // An unavailable device font list leaves the always-supported system choice in place.
        }
    }

    /// <summary>Describes local configuration separately from an explicit successful remote test.</summary>
    private void RefreshOpenAiConnection()
    {
        var apiStatus = "Not checked";
        if (_apiStatusLoaded)
        {
            apiStatus = !_openAiStorageAvailable ? "Unavailable"
                : _openAiConfigured ? "Key saved" : "Not configured";
        }

        var codexStatus = _desktop ? "Not checked" : "Windows app only";
        if (_desktop && _codexStatusLoaded)
        {
            codexStatus = !_codexAvailable ? "CLI unavailable"
                : _codexConfigured ? "Signed in locally" : "Not signed in";
        }

        AppLocalization.SetText(ApiConnectionStatus, apiStatus);
        AppLocalization.SetText(CodexConnectionStatus, codexStatus);
        AppLocalization.SetText(OpenAiKeyLabel, _openAiConfigured ? "Replace API key" : "API key");
        AppLocalization.SetText(OpenAiKeyHint, _openAiConfigured
            ? "Leave this blank to keep the saved key."
            : "Paste a key to connect AI writing tools.");
        OpenAiKeyForm.Visibility = ApiProviderRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        UpdateOpenAiAction();
    }

    /// <summary>Blocks concurrent configuration changes while a connection test or preference save is in flight.</summary>
    private void UpdateOpenAiAction()
    {
        var canInteract = !_closed && !_closing && !_openAiBusy && !_preferencesBusy && _testResultDialog == null;
        PreferencesColumn.IsEnabled = canInteract;
        ActionsColumn.IsEnabled = canInteract;
        CodexProviderRadio.IsEnabled = canInteract && _desktop;
        ApiProviderRadio.IsEnabled = canInteract;
        TestCodexButton.IsEnabled = canInteract && _desktop && _connectionStatusesLoaded;
        TestApiButton.IsEnabled = canInteract && _connectionStatusesLoaded;
        OpenAiApiKeyBox.IsEnabled = canInteract && _openAiStorageAvailable;
    }

    /// <summary>Clears stale feedback while the user edits the replacement, without touching persisted credentials.</summary>
    private void OpenAiKeyChanged(object sender, RoutedEventArgs arguments)
    {
        if (_closed || _closing || _openAiBusy || TestApiButton == null || OpenAiFeedback == null)
        {
            return;
        }

        OpenAiFeedback.Visibility = Visibility.Collapsed;
        UpdateOpenAiAction();
    }

    /// <summary>Shows brief progress while an explicit connection test is running.</summary>
    private void ShowOpenAiStatus(string message)
    {
        AppLocalization.SetText(OpenAiStatus, message);
        // Retain the shared brush so visible feedback follows later appearance changes.
        OpenAiStatus.Foreground = (Brush)Application.Current.Resources["ShellText"];
        OpenAiFeedback.Visibility = Visibility.Visible;
    }

    /// <summary>Reads only the selected provider; CLI mode never probes the saved API credential.</summary>
    private async Task LoadOpenAiStatusAsync()
    {
        if (_closed || _closing)
        {
            return;
        }

        _connectionStatusCancellation?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_openAiLifetime.Token);
        _connectionStatusCancellation = cancellation;
        _connectionStatusesLoaded = false;
        UpdateOpenAiAction();
        var provider = _preferences.AiProvider;
        try
        {
            var status = await BrowserServices.CallAsync<AiConnectionStatus>(
                "ai.connection.status", new { provider }, cancellation.Token);
            if (_closed || _closing || cancellation.IsCancellationRequested)
            {
                return;
            }

            SetConnectionPresence(provider, status.Available, status.Configured);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!_closed && !_closing && !cancellation.IsCancellationRequested)
            {
                SetConnectionPresence(provider, false, false);
            }
        }
        finally
        {
            if (ReferenceEquals(_connectionStatusCancellation, cancellation))
            {
                _connectionStatusCancellation = null;
                if (!_closed && !_closing)
                {
                    _connectionStatusesLoaded = true;
                    RefreshOpenAiConnection();
                }
            }
        }
    }

    /// <summary>Retains local presence per provider without interpreting it as a successful remote test.</summary>
    private void SetConnectionPresence(string provider, bool available, bool configured)
    {
        if (provider == AiProviders.CodexCli)
        {
            _codexAvailable = available;
            _codexConfigured = configured;
            _codexStatusLoaded = true;
        }
        else
        {
            _openAiStorageAvailable = available;
            _openAiConfigured = configured;
            _apiStatusLoaded = true;
        }
    }

    /// <summary>Tests the CLI's own connection only after its provider-specific action is clicked.</summary>
    private async void TestCodexConnection(object sender, RoutedEventArgs arguments) => await TestConnectionAsync(AiProviders.CodexCli);

    /// <summary>Tests the saved API key or validates and saves an explicitly entered replacement.</summary>
    private async void TestApiConnection(object sender, RoutedEventArgs arguments) => await TestConnectionAsync(AiProviders.ApiKey);

    /// <summary>Runs one cancellable connection operation and publishes an explicit result while settings remain open.</summary>
    private async Task TestConnectionAsync(string provider)
    {
        if (_closed || _closing || _openAiBusy || _preferencesBusy || _testResultDialog != null)
        {
            return;
        }

        _openAiBusy = true;
        UpdateOpenAiAction();
        ShowOpenAiStatus("Testing connection…");
        try
        {
            var candidateKey = provider == AiProviders.ApiKey && !string.IsNullOrWhiteSpace(OpenAiApiKeyBox.Password)
                ? OpenAiKeyRules.Normalize(OpenAiApiKeyBox.Password)
                : null;
            var result = await BrowserServices.CallAsync<AiConnectionTestResult>(
                "ai.connection.test", new { provider, apiKey = candidateKey }, _openAiLifetime.Token);
            if (_closed || _closing)
            {
                return;
            }

            if (result.Passed)
            {
                AiConnectionChanged = true;
                SetConnectionPresence(provider, true, true);
                if (candidateKey != null)
                {
                    // The transport saves candidates only after success. Never read
                    // a stored credential back into this masked input.
                    OpenAiApiKeyBox.Password = "";
                }
            }

            await LoadOpenAiStatusAsync();
            ShowConnectionResult(result);
        }
        catch (OperationCanceledException) when (_closed || _closing)
        {
        }
        catch (Exception error)
        {
            ShowConnectionResult(new AiConnectionTestResult(false,
                error is WorkspaceException ? error.Message : "The connection test could not be completed. Try again."));
        }
        finally
        {
            _openAiBusy = false;
            if (!_closed && !_closing)
            {
                OpenAiFeedback.Visibility = Visibility.Collapsed;
                RefreshOpenAiConnection();
            }
        }
    }

    /// <summary>Shows one styled modal result, rejecting completions after the settings close transition begins.</summary>
    private void ShowConnectionResult(AiConnectionTestResult result)
    {
        if (_closed || _closing)
        {
            return;
        }

        var dialog = new AiConnectionResultWindow(result);
        _testResultDialog = dialog;
        dialog.Closed += (_, _) =>
        {
            _testResultDialog = null;
            UpdateOpenAiAction();
        };
        dialog.Show();
    }

    /// <summary>Saves current selections and reports whether storage accepted them.</summary>
    private async Task SaveAsync()
    {
        if (_updating || _closed || _closing || _openAiBusy || _preferencesBusy
            || AppearanceCombo.SelectedIndex < 0 || LanguageCombo.SelectedIndex < 0 || StyleCombo.SelectedIndex < 0)
        {
            return;
        }

        _preferences = _preferences with
        {
            Appearance = Appearances[AppearanceCombo.SelectedIndex],
            Language = Languages[LanguageCombo.SelectedIndex],
            // Other controls can save before asynchronous font discovery finishes; preserve the existing choice then.
            UiFont = _uiFontsLoaded ? ThemeId(UiFontCombo) : _preferences.UiFont,
            DocumentStyle = DocumentStyles[StyleCombo.SelectedIndex],
            Acrylic = AcrylicCheck.IsChecked == true,
            LightDocumentTheme = ThemeId(LightDocumentThemeCombo),
            DarkDocumentTheme = ThemeId(DarkDocumentThemeCombo),
            LightCodeTheme = ThemeId(LightCodeThemeCombo),
            DarkCodeTheme = ThemeId(DarkCodeThemeCombo),
            SimpleSource = SimpleSourceCheck.IsChecked == true,
            RememberLastDocument = RememberDocumentCheck.IsChecked == true,
            AiProvider = CodexProviderRadio.IsChecked == true ? AiProviders.CodexCli : AiProviders.ApiKey
        };
        _preferencesBusy = true;
        UpdateOpenAiAction();
        try
        {
            var persisted = await _apply(_preferences);
            AppLocalization.SetText(SettingsStatus, persisted ? "Changes are saved automatically." : "Preferences are available for this session. Browser storage is unavailable.");
        }
        catch (Exception error)
        {
            AppLocalization.SetText(SettingsStatus, error is WorkspaceException ? error.Message : "Preferences could not be saved. Try again.");
        }
        finally
        {
            _preferencesBusy = false;
            if (!_closed && !_closing)
            {
                RefreshOpenAiConnection();
            }
        }
    }

    /// <summary>Persists the selected provider through the same settings pipeline as all other preferences.</summary>
    private async void ProviderChanged(object sender, RoutedEventArgs arguments)
    {
        if (_updating)
        {
            return;
        }

        await SaveAsync();
        await LoadOpenAiStatusAsync();
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
        if (_closed || _closing || _openAiBusy || _preferencesBusy || _testResultDialog != null)
        {
            return;
        }

        _preferencesBusy = true;
        UpdateOpenAiAction();
        try
        {
            var persisted = await _apply(new Preferences());
            await LoadOpenAiStatusAsync();
            AppLocalization.SetText(SettingsStatus, persisted ? "Default preferences restored." : "Preferences are available for this session. Browser storage is unavailable.");
        }
        catch (Exception)
        {
            AppLocalization.SetText(SettingsStatus, "Preferences could not be saved. Try again.");
        }
        finally
        {
            _preferencesBusy = false;
            if (!_closed && !_closing)
            {
                RefreshOpenAiConnection();
            }
        }
    }

    /// <summary>Closes settings without discarding saved changes.</summary>
    private void Dismiss(object sender, RoutedEventArgs arguments) => Close();
}
