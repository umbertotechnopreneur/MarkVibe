/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; managed XAML composer and model popup; 2026-10-11
 * Modified with AI: OpenAI Codex; compact dock, reusable reference rows and readable composer state; 2026-10-16
 * Modified with AI: OpenAI Codex; provider-specific connection and model catalog information; 2026-10-16
 * Human guidance: Umberto Giacobbi; application controls in XAML, renderer adapters only in JavaScript
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MarkVibe.Core;

namespace MarkVibe.Controls;

/// <summary>Owns prompt editing, model choices, review and usage entirely as OpenSilver controls.</summary>
public partial class MarkdownAiView : UserControl
{
    /// <summary>Lets the shell attach its shared document actions to a newly created reference row.</summary>
    public event Action<FrameworkElement, string, string>? FileTargetCreated;

    /// <summary>Contains the current managed projection; credentials and source ownership remain outside this view.</summary>
    private JsonElement _state;

    /// <summary>Suppresses user-action handlers while managed state is applied to the controls.</summary>
    private bool _updating;

    /// <summary>Tracks the owner's explicit prompt reset without overwriting an in-progress local draft.</summary>
    private int _promptClear = -1;

    /// <summary>Avoids rebuilding model rows when only prompt or usage state changes.</summary>
    private string _modelsSignature = "";

    /// <summary>Retains the immediate slider choice until the owner republishes its state.</summary>
    private string _selectedEffort = "medium";

    /// <summary>Distinguishes a new response from routine updates that must preserve reading position.</summary>
    private string _responseSignature = "";

    /// <summary>Retains collection row identity while its membership and action availability are unchanged.</summary>
    private string _collectionSignature = "";

    /// <summary>Provides a shared animated accent for the effort slider and its popup caption.</summary>
    private readonly SolidColorBrush _effortBrush = new();

    /// <summary>Avoids restarting the color transition when its destination is already current.</summary>
    private Color? _effortTargetColor;

    /// <summary>Owns the current effort-color transition so a new choice can replace it.</summary>
    private Storyboard? _effortAnimation;

    /// <summary>Debounces prompt estimates while local draft protection updates immediately.</summary>
    private readonly DispatcherTimer _promptTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    /// <summary>Maps the ordered request effort levels to translatable display captions.</summary>
    private static readonly string[] EffortNames = ["Low", "Medium", "High", "Very high", "Maximum"];

    /// <summary>Identifies this view when routing actions and browser close-guard updates.</summary>
    public string HostId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Exposes the latest local draft, including text not yet sent by the debounce timer.</summary>
    public string PromptText => PromptInput.Text;

    /// <summary>Routes user intent to the shell that owns request validation and document revisions.</summary>
    public event Func<JsonElement, Task>? ActionRequested;

    /// <summary>Initializes XAML state and delays estimates without rebuilding the prompt editor.</summary>
    public MarkdownAiView()
    {
        InitializeComponent();
        EffortSlider.Foreground = _effortBrush;
        FlyoutEffort.Foreground = _effortBrush;
        _promptTimer.Tick += async (_, _) =>
        {
            _promptTimer.Stop();
            await DispatchAsync("prompt");
        };
        Unloaded += (_, _) =>
        {
            _promptTimer.Stop();
            _effortAnimation?.Stop();
            ModelFlyout.IsOpen = false;
        };
        Loaded += (_, _) =>
        {
            if (Application.Current.RootVisual is UIElement root)
            {
                root.MouseLeftButtonDown += OutsideFlyout;
            }
        };
        Unloaded += (_, _) =>
        {
            if (Application.Current.RootVisual is UIElement root)
            {
                root.MouseLeftButtonDown -= OutsideFlyout;
            }
        };
        CloseButton.MouseEnter += (_, _) => CloseButton.Foreground = Brush("#DF3D50");
        CloseButton.MouseLeave += (_, _) => CloseButton.Foreground = Resource("ShellText");
    }

    /// <summary>Updates managed controls and retains a browser-only close guard without mounting DOM UI.</summary>
    public async Task DisplayAsync(object state, CancellationToken cancellationToken = default)
    {
        _state = JsonSerializer.SerializeToElement(state, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        _updating = true;
        try
        {
            var configured = Flag("configured");
            KeyBanner.Visibility = configured ? Visibility.Collapsed : Visibility.Visible;
            Composer.Visibility = configured ? Visibility.Visible : Visibility.Collapsed;
            Heading.Text = UiStrings.T("AI Assistant");
            var usesCodexCli = Text("aiProvider") == AiProviders.CodexCli;
            var providerInformation = usesCodexCli
                ? "\n\n" + UiStrings.T("Codex CLI model names come from the bundled catalog. Availability depends on your account and is checked only by Test or Send. API prices do not describe Codex CLI plan limits.")
                : "";
            SetInformation(
                AssistantInfoButton,
                UiStrings.T("Markdown AI assistant") + "\n\n"
                    + UiStrings.T("Enter to send · Shift+Enter for a new line")
                    + providerInformation);
            AutomationProperties.SetName(ResponseScroll, UiStrings.T("AI response"));
            KeyHint.Text = UiStrings.T(usesCodexCli
                ? "Open Settings and connect Codex CLI to enable AI features."
                : "Open Settings and add your OpenAI key to enable AI features.");
            SetInformation(ModelButton, usesCodexCli
                ? UiStrings.T("Codex CLI model names come from the bundled catalog. Availability depends on your account and is checked only by Test or Send. API prices do not describe Codex CLI plan limits.")
                : UiStrings.T("Model and reasoning"));
            KeyButton.Content = UiStrings.T("Settings");
            PromptPlaceholder.Text = UiStrings.T("Describe how to edit this Markdown…");
            LowLabel.Text = UiStrings.T("Low");
            MaxLabel.Text = UiStrings.T("Maximum");

            var promptResetVersion = Number("promptClear");
            if (_promptClear != promptResetVersion)
            {
                _promptClear = promptResetVersion;
                PromptInput.Text = Text("prompt");
            }

            var models = Models();
            var selectedModel = models.FirstOrDefault(model => model.Id == Text("resolvedModel"))
                ?? models.FirstOrDefault();
            ModelLabel.Text = selectedModel?.Label ?? UiStrings.T("Loading models…");

            var effortIndex = Math.Max(0, Array.IndexOf(MarkdownAi.Efforts, Text("effort")));
            _selectedEffort = MarkdownAi.Efforts[effortIndex];
            EffortLabel.Text = UiStrings.T(EffortNames[effortIndex]);
            FlyoutEffort.Text = EffortLabel.Text;
            EffortSlider.Value = effortIndex;
            RefreshEffortAppearance(effortIndex, animate: false);

            var busy = Flag("busy");
            var modelsAvailable = Flag("modelsReady") && models.Length > 0;
            PromptInput.IsEnabled = !busy;
            ModelButton.IsEnabled = !busy && modelsAvailable;
            SendIcon.Symbol = busy ? "close" : "send";
            if (busy || !Flag("visible"))
            {
                ModelFlyout.IsOpen = false;
            }

            var canSubmit = configured
                && Flag("documentAvailable")
                && modelsAvailable
                && !Flag("unsupportedSelection")
                && PromptText.Trim().Length > 0;
            SendButton.IsEnabled = busy || canSubmit;
            ToolTipService.SetToolTip(SendButton, UiStrings.T(busy ? "Stop" : "Send"));

            RefreshResponse();
            DraftActions.Visibility = Flag("draft") ? Visibility.Visible : Visibility.Collapsed;
            UndoButton.IsEnabled = !busy && Flag("canUndo");
            RedoButton.IsEnabled = !busy && Flag("canRedo");
            SaveButton.IsEnabled = !busy;
            RestoreButton.IsEnabled = !busy;
            RefreshUsage();
            RefreshCollection();
            RefreshPlaceholder();

            if (ModelFlyout.IsOpen)
            {
                BuildModelOptions();
            }
        }
        finally
        {
            _updating = false;
        }

        await BrowserServices.CallAsync("ai.state", new { hostId = HostId, state }, cancellationToken);
    }

    /// <summary>Shows the current reply above the prompt and preserves its scroll position during usage refreshes.</summary>
    private void RefreshResponse()
    {
        var result = _state.GetProperty("result");
        var responseText = "";
        if (result.ValueKind == JsonValueKind.Object)
        {
            var responseProperty = result.GetProperty("kind").GetString() == "review" ? "markdown" : "summary";
            responseText = result.GetProperty(responseProperty).GetString() ?? "";
        }

        Status.Text = UiStrings.T(Text("message"));
        ReviewText.Text = responseText;
        var statusIsRedundant = Status.Text.Length == 0 || Status.Text == responseText;
        Status.Visibility = statusIsRedundant ? Visibility.Collapsed : Visibility.Visible;
        ReviewText.Visibility = responseText.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        ResponseSurface.Visibility = Status.Text.Length > 0 || responseText.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        var responseSignature = Status.Text + "\n" + responseText;
        if (_responseSignature != responseSignature)
        {
            _responseSignature = responseSignature;
            // New replies begin at the top; token updates must not interrupt reading.
            ResponseScroll.ScrollToVerticalOffset(0);
        }
    }

    /// <summary>Shows one request estimate; detailed accounting remains available through the information tooltip.</summary>
    private void RefreshUsage()
    {
        var capacity = _state.GetProperty("capacity");
        var usage = _state.GetProperty("contextUsage");
        NextSend.Text = UiStrings.T("Next request ≈ {0} TK", usage.GetProperty("input").GetInt32().ToString("N0"));
        ContextBar.Value = capacity.GetProperty("usedPercent").GetDouble();
        var details = new List<string>
        {
            UiStrings.T("Context budget {0} / {1}",
                capacity.GetProperty("inputTokens").GetInt32().ToString("N0"),
                capacity.GetProperty("contextWindow").GetInt32().ToString("N0")),
            UiStrings.T("Input limit {0} · Response budget {1} · Available {2}",
                capacity.GetProperty("maxInputTokens").GetInt32().ToString("N0"),
                capacity.GetProperty("responseTokens").GetInt32().ToString("N0"),
                capacity.GetProperty("freeTokens").GetInt32().ToString("N0")),
            ""
        };
        var names = new[] { "System", "Prompt", "Document", "Response", "Reasoning" };
        var keys = new[] { "system", "prompt", "document", "output", "reasoning" };
        for (var index = 0; index < keys.Length; index++)
        {
            var count = usage.GetProperty(keys[index]).GetInt32();
            details.Add(UiStrings.T(names[index]) + " ≈ " + count.ToString("N0"));
        }

        details.Add("");
        details.Add(UiStrings.T("Session usage: {0} tokens across {1} completed requests",
            _state.GetProperty("totalTokens").GetInt64().ToString("N0"), Number("requestCount")));
        SetInformation(UsageInfoButton, string.Join("\n", details));

        // Actionable warnings remain visible instead of being hidden inside help text.
        ContextWarning.Text = "";
        if (capacity.GetProperty("tooLarge").GetBoolean())
        {
            ContextWarning.Text = UiStrings.T("Input estimate exceeds the selected model's limit.");
        }
        else if (Flag("contextIncomplete"))
        {
            ContextWarning.Text = UiStrings.T("Collection needs a refresh before its total can be estimated.");
        }
        ContextWarning.Visibility = ContextWarning.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (Flag("contextIncomplete"))
        {
            NextSend.Text = UiStrings.T("Estimate pending");
        }
    }

    /// <summary>Updates the empty-prompt placeholder without duplicating the request token estimate.</summary>
    private void RefreshPlaceholder()
    {
        PromptPlaceholder.Visibility = PromptText.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Retains a bounded native tooltip so hover and keyboard activation share the same explanation.</summary>
    private static void SetInformation(Button button, string text)
    {
        if (ToolTipService.GetToolTip(button) is not ToolTip tooltip)
        {
            tooltip = new ToolTip
            {
                Content = new TextBlock
                {
                    MaxWidth = 300,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            ToolTipService.SetToolTip(button, tooltip);
        }

        ((TextBlock)tooltip.Content).Text = text;
    }

    /// <summary>Lets keyboard and touch users open the same information shown on pointer hover.</summary>
    private void ShowInformation(object sender, RoutedEventArgs arguments)
    {
        if (ToolTipService.GetToolTip((DependencyObject)sender) is ToolTip tooltip)
        {
            tooltip.IsOpen = !tooltip.IsOpen;
        }
    }

    /// <summary>Dismisses explicitly opened information when focus leaves its button.</summary>
    private void HideInformation(object sender, RoutedEventArgs arguments)
    {
        if (ToolTipService.GetToolTip((DependencyObject)sender) is ToolTip tooltip)
        {
            tooltip.IsOpen = false;
        }
    }

    /// <summary>Decodes only the model metadata supplied by the component owner.</summary>
    private MarkdownAiModel[] Models()
    {
        return _state.ValueKind == JsonValueKind.Object
            ? Wire.Decode<MarkdownAiModel[]>(_state.GetProperty("models").GetRawText())
            : [];
    }

    /// <summary>Reads an optional managed state flag.</summary>
    private bool Flag(string key)
    {
        return _state.ValueKind == JsonValueKind.Object
            && _state.TryGetProperty(key, out var value)
            && value.ValueKind == JsonValueKind.True;
    }

    /// <summary>Reads an optional managed state caption.</summary>
    private string Text(string key)
    {
        if (_state.ValueKind != JsonValueKind.Object || !_state.TryGetProperty(key, out var value))
        {
            return "";
        }

        return value.GetString() ?? "";
    }

    /// <summary>Reads an optional bounded state counter.</summary>
    private int Number(string key)
    {
        return _state.ValueKind == JsonValueKind.Object && _state.TryGetProperty(key, out var value)
            ? value.GetInt32()
            : 0;
    }

    /// <summary>Resolves a shared theme brush.</summary>
    private static Brush Resource(string key) => (Brush)Application.Current.Resources[key];

    /// <summary>Creates a local semantic-color brush without CSS.</summary>
    private static Brush Brush(string color)
    {
        var red = Convert.ToByte(color[1..3], 16);
        var green = Convert.ToByte(color[3..5], 16);
        var blue = Convert.ToByte(color[5..7], 16);
        return new SolidColorBrush(Color.FromArgb(255, red, green, blue));
    }

    /// <summary>Routes XAML actions to the existing managed document and request gates.</summary>
    private async Task DispatchAsync(
        string action,
        string? model = null,
        string? effort = null,
        string? referencePath = null)
    {
        if (ActionRequested == null || _state.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var payload = JsonSerializer.SerializeToElement(new
        {
            hostId = HostId,
            action,
            prompt = PromptText,
            model = model ?? Text("model"),
            effort = effort ?? _selectedEffort,
            path = Text("path"),
            workspaceId = Text("workspaceId"),
            fingerprint = Text("fingerprint"),
            referencePath
        });
        await ActionRequested(payload);
    }

    /// <summary>Retains prompt drafts and refreshes send availability immediately.</summary>
    private void PromptChanged(object sender, TextChangedEventArgs arguments)
    {
        if (_updating)
        {
            return;
        }

        // The close guard must see drafts before the delayed token estimate reaches the owner.
        OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.updateAiPrompt($0, $1)", HostId, PromptText);
        RefreshPlaceholder();
        var canSubmit = Flag("configured")
            && Flag("documentAvailable")
            && Flag("modelsReady")
            && !Flag("unsupportedSelection")
            && PromptText.Trim().Length > 0;
        SendButton.IsEnabled = Flag("busy") || canSubmit;
        _promptTimer.Stop();
        _promptTimer.Start();
    }

    /// <summary>Sends on Enter and leaves Shift+Enter for plain multiline editing.</summary>
    private async void PromptKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Enter
            && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
            && SendButton.IsEnabled)
        {
            arguments.Handled = true;
            _promptTimer.Stop();
            await DispatchAsync(Flag("busy") ? "cancel" : "submit");
        }
    }

    /// <summary>Rebuilds model controls only when catalog or translations change; retains scroll and focus.</summary>
    private void BuildModelOptions()
    {
        var signature = _state.GetProperty("models").GetRawText() + UiStrings.T("Default")
            + UiStrings.T("Cheap") + UiStrings.T("Affordable") + UiStrings.T("Prohibitive");
        if (_modelsSignature != signature)
        {
            _modelsSignature = signature;
            ModelOptions.Children.Clear();
            var models = Models();
            if (models.Length > 0)
            {
                AddModelOption("default", UiStrings.T("Default"), models[0]);
            }

            foreach (var model in models)
            {
                AddModelOption(model.Id, model.Label, model);
            }
        }

        foreach (var option in ModelOptions.Children.OfType<Button>())
        {
            option.Foreground = Resource(Text("model") == (option.Tag as string) ? "SystemAccent" : "ShellText");
        }
    }

    /// <summary>Creates one XAML model option, retaining its identifier in the action closure.</summary>
    private void AddModelOption(string id, string label, MarkdownAiModel model)
    {
        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = label, FontSize = 13 });
        var cost = model.CostTier switch
        {
            "cheap" => "Cheap",
            "prohibitive" => "Prohibitive",
            _ => "Affordable"
        };
        var modelDetails = model.ContextWindow.ToString("N0") + " token";
        if (Text("aiProvider") != AiProviders.CodexCli)
        {
            modelDetails = UiStrings.T(cost) + " · " + modelDetails;
        }

        content.Children.Add(new TextBlock
        {
            Text = modelDetails,
            FontSize = 10,
            Foreground = Resource("MutedText"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        var button = new Button
        {
            Tag = id,
            Content = content,
            Style = (Style)Application.Current.Resources["FlyoutChoiceButton"],
            Foreground = Resource(Text("model") == id ? "SystemAccent" : "ShellText")
        };
        button.Click += async (_, _) =>
        {
            ModelFlyout.IsOpen = false;
            await DispatchAsync("choice", model: id);
            ModelButton.Focus();
        };
        ModelOptions.Children.Add(button);
    }

    /// <summary>Overlays the complete composer, including its selector and Send button.</summary>
    private void OpenModelFlyout(object sender, RoutedEventArgs arguments)
    {
        if (ModelFlyout.IsOpen)
        {
            ModelFlyout.IsOpen = false;
            return;
        }

        BuildModelOptions();
        // Top placement plus the composer's height aligns both bottom edges.
        // This covers the input and action row while keeping the popup inside the viewport.
        ModelFlyout.PlacementTarget = ComposerSurface;
        ModelFlyout.Placement = PlacementMode.Top;
        ModelFlyout.StaysWithinScreenBounds = true;
        ModelFlyoutSurface.Width = ComposerSurface.ActualWidth;
        ModelFlyout.HorizontalOffset = 0;
        ModelFlyout.VerticalOffset = ComposerSurface.ActualHeight;
        ModelFlyout.IsOpen = true;
        ModelOptions.Children.OfType<Button>().FirstOrDefault()?.Focus();
    }

    /// <summary>Dismisses the flyout on clicks outside its XAML visual subtree.</summary>
    private void OutsideFlyout(object sender, MouseButtonEventArgs arguments)
    {
        if (!ModelFlyout.IsOpen)
        {
            return;
        }

        var current = arguments.OriginalSource as DependencyObject;
        while (current != null)
        {
            if (ReferenceEquals(current, ModelFlyout.Child) || ReferenceEquals(current, ModelButton))
            {
                return;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        ModelFlyout.IsOpen = false;
    }

    /// <summary>Restores keyboard focus after closing the separate popup.</summary>
    private void ModelFlyoutClosed(object sender, EventArgs arguments)
    {
        ModelButton.Focus();
    }

    /// <summary>Closes the model popup with Escape.</summary>
    private void FlyoutKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Escape)
        {
            ModelFlyout.IsOpen = false;
            arguments.Handled = true;
        }
    }

    /// <summary>Updates reasoning captions immediately and commits each discrete step without rebuilding UI.</summary>
    private async void EffortChanged(object sender, RoutedPropertyChangedEventArgs<double> arguments)
    {
        if (_updating)
        {
            return;
        }

        var effortIndex = Math.Clamp((int)Math.Round(arguments.NewValue), 0, 4);
        var effort = MarkdownAi.Efforts[effortIndex];
        if (_selectedEffort == effort)
        {
            return;
        }

        _selectedEffort = effort;
        EffortLabel.Text = UiStrings.T(EffortNames[effortIndex]);
        FlyoutEffort.Text = EffortLabel.Text;
        RefreshEffortAppearance(effortIndex, animate: true);
        await DispatchAsync("effort", effort: effort);
    }

    /// <summary>Increases accent saturation across the existing five effort levels with a short color transition.</summary>
    private void RefreshEffortAppearance(int effortIndex, bool animate)
    {
        var accent = ((SolidColorBrush)Resource("SystemAccent")).Color;
        var luminance = accent.R * 0.2126 + accent.G * 0.7152 + accent.B * 0.0722;
        var saturation = 0.25 + Math.Clamp(effortIndex, 0, 4) * 0.1875;
        var target = Color.FromArgb(
            accent.A,
            (byte)Math.Round(luminance + (accent.R - luminance) * saturation),
            (byte)Math.Round(luminance + (accent.G - luminance) * saturation),
            (byte)Math.Round(luminance + (accent.B - luminance) * saturation));
        if (_effortTargetColor == target)
        {
            return;
        }

        var current = _effortBrush.Color;
        var initialized = _effortTargetColor.HasValue;
        _effortTargetColor = target;
        _effortAnimation?.Stop();
        _effortBrush.Color = target;
        if (!animate || !initialized)
        {
            return;
        }

        var transition = new ColorAnimation
        {
            From = current,
            To = target,
            Duration = new Duration(TimeSpan.FromMilliseconds(160)),
            FillBehavior = FillBehavior.Stop
        };
        Storyboard.SetTarget(transition, _effortBrush);
        Storyboard.SetTargetProperty(transition, new PropertyPath("Color"));
        _effortAnimation = new Storyboard();
        _effortAnimation.Children.Add(transition);
        _effortAnimation.Begin();
    }

    /// <summary>Closes the panel after retaining the latest prompt draft.</summary>
    private async void ClosePanel(object sender, RoutedEventArgs arguments)
    {
        _promptTimer.Stop();
        ModelFlyout.IsOpen = false;
        await DispatchAsync("prompt");
        await DispatchAsync("close");
    }

    /// <summary>Opens application settings from the missing-key banner.</summary>
    private async void OpenSettings(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("settings");
    }

    /// <summary>Sends the current scoped prompt or cancels an active request.</summary>
    private async void SendPrompt(object sender, RoutedEventArgs arguments)
    {
        _promptTimer.Stop();
        await DispatchAsync(Flag("busy") ? "cancel" : "submit");
    }

    /// <summary>Restores the preceding local Markdown draft.</summary>
    private async void UndoEdit(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("undo");
    }

    /// <summary>Reapplies the next local Markdown draft.</summary>
    private async void RedoEdit(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("redo");
    }

    /// <summary>Saves a separate Markdown copy through the existing native/browser provider.</summary>
    private async void SaveEdit(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("save");
    }

    /// <summary>Adds the current document to the workspace collection without sending an AI request.</summary>
    private async void AddCurrentContext(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("context-add");
    }

    /// <summary>Clears reference membership while retaining every document and prompt.</summary>
    private async void ClearContext(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("context-clear");
    }

    /// <summary>Shows per-document membership and a local token estimate before any request is sent.</summary>
    private void RefreshCollection()
    {
        var references = _state.GetProperty("references").EnumerateArray().ToArray();
        var canChange = !Flag("busy") && !Flag("contextBusy");
        AddContextButton.IsEnabled = canChange && Flag("documentAvailable");
        ClearContextButton.IsEnabled = canChange && references.Length > 0;
        ClearContextButton.Visibility = references.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        CollectionExpander.Header = UiStrings.T("References · {0}", references.Length);
        SetInformation(CollectionInfoButton,
            UiStrings.T("References are sent with your next request. Only the open document is edited.")
            + "\n\n" + UiStrings.T("{0} documents · ≈ {1} reference tokens",
                references.Length, Number("referenceTokens").ToString("N0")));

        var signature = Text("workspaceId") + UiStrings.Language + canChange + _state.GetProperty("references").GetRawText();
        if (_collectionSignature == signature)
        {
            return;
        }

        // Prompt and usage refreshes preserve row identity, focus and scroll position.
        _collectionSignature = signature;
        CollectionRows.Children.Clear();
        foreach (var reference in references)
        {
            var path = reference.GetProperty("path").GetString() ?? "";
            var row = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var text = new StackPanel();
            var title = new TextBlock { FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis };
            AppLocalization.SetRaw(title, reference.GetProperty("title").GetString() ?? path);
            ToolTipService.SetToolTip(title, path);
            text.Children.Add(title);
            text.Children.Add(new TextBlock
            {
                Text = ReferenceStatus(reference),
                FontSize = 10,
                Foreground = Resource("MutedText")
            });
            row.Children.Add(text);

            var remove = new Button
            {
                Content = new VectorIcon { Symbol = "close", Width = 12, Height = 12 },
                Style = (Style)Application.Current.Resources["CompactIconButton"],
                IsEnabled = canChange
            };
            ToolTipService.SetToolTip(remove, UiStrings.T("Remove from collection"));
            AutomationProperties.SetName(remove, UiStrings.T("Remove from collection") + ": " + path);
            remove.Click += async (_, _) => await DispatchAsync("context-remove", referencePath: path);
            Grid.SetColumn(remove, 1);
            row.Children.Add(remove);
            CollectionRows.Children.Add(row);
            FileTargetCreated?.Invoke(row, path, title.Text);
        }
    }

    /// <summary>Prioritizes target and missing-file states before displaying an available token estimate.</summary>
    private static string ReferenceStatus(JsonElement reference)
    {
        if (reference.GetProperty("isTarget").GetBoolean())
        {
            return UiStrings.T("Editing target");
        }

        if (reference.GetProperty("missing").GetBoolean())
        {
            return UiStrings.T("Missing document");
        }

        var estimatedTokens = reference.GetProperty("tokens");
        if (estimatedTokens.ValueKind == JsonValueKind.Number)
        {
            return "≈ " + estimatedTokens.GetInt32().ToString("N0") + " TK";
        }

        return UiStrings.T("Estimate pending");
    }

    /// <summary>Restores the original source through the managed history contract.</summary>
    private async void RestoreEdit(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("restore");
    }

}
