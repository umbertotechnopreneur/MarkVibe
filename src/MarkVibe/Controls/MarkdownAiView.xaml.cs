/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; managed XAML composer and model popup; 2026-10-11
 * Human guidance: Umberto Giacobbi; application controls in XAML, renderer adapters only in JavaScript
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using System.Windows;
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
    private JsonElement _state;
    private bool _updating;
    private bool _compact;
    private Point? _dragPoint;
    private int _promptClear = -1;
    private string _modelsSignature = "";
    private string _selectedEffort = "medium";
    private readonly SolidColorBrush _effortBrush = new();
    private Color? _effortTargetColor;
    private Storyboard? _effortAnimation;
    private readonly DispatcherTimer _promptTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private static readonly string[] EffortNames = ["Low", "Medium", "High", "Very high", "Maximum"];
    public string HostId { get; } = Guid.NewGuid().ToString("N");
    public string PromptText => PromptInput.Text;
    public event Func<JsonElement, Task>? ActionRequested;
    public event Action<Point>? MoveRequested;
    public event Action<bool>? CompactChanged;

    /// <summary>Initializes XAML state and delays estimates without rebuilding the prompt editor.</summary>
    public MarkdownAiView()
    {
        InitializeComponent();
        EffortSlider.Foreground = FlyoutEffort.Foreground = _effortBrush;
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
                root.MouseLeftButtonDown += OutsideFlyout;
        };
        Unloaded += (_, _) =>
        {
            if (Application.Current.RootVisual is UIElement root)
                root.MouseLeftButtonDown -= OutsideFlyout;
        };
        CloseButton.MouseEnter += (_, _) => CloseButton.Foreground = Brush("#DF3D50");
        CloseButton.MouseLeave += (_, _) => CloseButton.Foreground = Resource("ShellText");
    }

    /// <summary>Updates managed controls and retains a browser-only close guard without mounting DOM UI.</summary>
    public async Task DisplayAsync(object state, CancellationToken cancellationToken = default)
    {
        _state = JsonSerializer.SerializeToElement(state, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _updating = true;
        try
        {
            var configured = Flag("configured");
            KeyBanner.Visibility = configured ? Visibility.Collapsed : Visibility.Visible;
            Composer.Visibility = configured ? Visibility.Visible : Visibility.Collapsed;
            Heading.Text = "AI Assistant";
            KeyHint.Text = UiStrings.T("Open Settings and add your OpenAI key to enable AI features.");
            KeyButton.Content = UiStrings.T("Settings");
            KeyboardHint.Text = UiStrings.T("Enter to send · Shift+Enter for a new line");
            PromptPlaceholder.Text = UiStrings.T("Describe how to edit this Markdown…");
            UsageExpander.Header = UiStrings.T("Budget and usage");
            LowLabel.Text = UiStrings.T("Low");
            MaxLabel.Text = UiStrings.T("Maximum");
            var clear = Number("promptClear");
            if (_promptClear != clear)
            {
                _promptClear = clear;
                PromptInput.Text = Text("prompt");
            }
            var models = Models();
            var selected = models.FirstOrDefault(model => model.Id == Text("resolvedModel")) ?? models.FirstOrDefault();
            ModelLabel.Text = selected?.Label ?? UiStrings.T("Loading models…");
            var effort = Math.Max(0, Array.IndexOf(MarkdownAi.Efforts, Text("effort")));
            _selectedEffort = MarkdownAi.Efforts[effort];
            EffortLabel.Text = FlyoutEffort.Text = UiStrings.T(EffortNames[effort]);
            EffortSlider.Value = effort;
            RefreshEffortAppearance(effort, animate: false);
            PromptInput.IsEnabled = !Flag("busy");
            ModelButton.IsEnabled = !Flag("busy") && Flag("modelsReady") && models.Length > 0;
            SendIcon.Symbol = Flag("busy") ? "close" : "send";
            if (Flag("busy")) ModelFlyout.IsOpen = false;
            SendButton.IsEnabled = Flag("busy") || configured && Flag("documentAvailable") && Flag("modelsReady") && models.Length > 0 && !Flag("unsupportedSelection") && PromptText.Trim().Length > 0;
            ToolTipService.SetToolTip(SendButton, UiStrings.T(Flag("busy") ? "Stop" : "Send"));
            Status.Text = UiStrings.T(Text("message"));
            Status.Visibility = Status.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            DraftActions.Visibility = Flag("draft") ? Visibility.Visible : Visibility.Collapsed;
            UndoButton.IsEnabled = !Flag("busy") && Flag("canUndo");
            RedoButton.IsEnabled = !Flag("busy") && Flag("canRedo");
            SaveButton.IsEnabled = RestoreButton.IsEnabled = !Flag("busy");
            var result = _state.GetProperty("result");
            var review = result.ValueKind == JsonValueKind.Object && result.GetProperty("kind").GetString() == "review";
            ReviewText.Visibility = review ? Visibility.Visible : Visibility.Collapsed;
            ReviewText.Text = review ? result.GetProperty("markdown").GetString() ?? "" : "";
            RefreshUsage();
            RefreshCounter();
            if (ModelFlyout.IsOpen) BuildModelOptions();
        }
        finally { _updating = false; }
        await BrowserServices.CallAsync("ai.state", new { hostId = HostId, state }, cancellationToken);
    }

    /// <summary>Formats context and usage; approximate counts remain visibly qualified.</summary>
    private void RefreshUsage()
    {
        var capacity = _state.GetProperty("capacity");
        var usage = _state.GetProperty("contextUsage");
        CapacityLabel.Text = UiStrings.T("Context budget {0} / {1}", capacity.GetProperty("inputTokens").GetInt32().ToString("N0"), capacity.GetProperty("contextWindow").GetInt32().ToString("N0"));
        ContextBar.Value = capacity.GetProperty("usedPercent").GetDouble();
        CapacityHint.Text = UiStrings.T("Input limit {0} · Response budget {1} · Available {2}", capacity.GetProperty("maxInputTokens").GetInt32().ToString("N0"), capacity.GetProperty("responseTokens").GetInt32().ToString("N0"), capacity.GetProperty("freeTokens").GetInt32().ToString("N0"));
        NextSend.Text = UiStrings.T("Estimated next request") + " ≈ " + usage.GetProperty("input").GetInt32().ToString("N0") + " token";
        var names = new[] { "System", "Prompt", "Document", "Response", "Reasoning" };
        var keys = new[] { "system", "prompt", "document", "output", "reasoning" };
        var colors = new[] { "#D88B80", "#AE95D9", "#77A9D4", "#79BDA6", "#D7AA61" };
        TokenSegments.Children.Clear();
        TokenSegments.ColumnDefinitions.Clear();
        var legend = new List<string>();
        for (var index = 0; index < keys.Length; index++)
        {
            var count = usage.GetProperty(keys[index]).GetInt32();
            legend.Add(UiStrings.T(names[index]) + " ≈ " + count.ToString("N0"));
            TokenSegments.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0, count), GridUnitType.Star) });
            var color = ((SolidColorBrush)Brush(colors[index])).Color;
            var lighter = Color.FromArgb(255, (byte)Math.Min(255, color.R + 12), (byte)Math.Min(255, color.G + 12), (byte)Math.Min(255, color.B + 12));
            var segment = new Border { Background = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1), GradientStops = { new GradientStop { Color = lighter, Offset = 0 }, new GradientStop { Color = color, Offset = 1 } } }, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 1, 0) };
            Grid.SetColumn(segment, index);
            TokenSegments.Children.Add(segment);
        }
        TokenLegend.Text = string.Join(" · ", legend);
        SessionUsage.Text = UiStrings.T("Session usage: {0} tokens across {1} completed requests", _state.GetProperty("totalTokens").GetInt64().ToString("N0"), Number("requestCount"));
    }

    /// <summary>Shows a compact, approximate token count and updates the empty-prompt placeholder.</summary>
    private void RefreshCounter()
    {
        var tokens = PromptText.Length == 0 ? 0 : MarkdownAi.Estimate("", PromptText).Prompt;
        PromptCounter.Text = "≈ " + tokens.ToString("N0") + " TK";
        PromptPlaceholder.Visibility = PromptText.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Decodes only the model metadata supplied by the component owner.</summary>
    private MarkdownAiModel[] Models() => _state.ValueKind == JsonValueKind.Object ? Wire.Decode<MarkdownAiModel[]>(_state.GetProperty("models").GetRawText()) : [];
    /// <summary>Reads an optional managed state flag.</summary>
    private bool Flag(string key) => _state.ValueKind == JsonValueKind.Object && _state.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.True;
    /// <summary>Reads an optional managed state caption.</summary>
    private string Text(string key) => _state.ValueKind == JsonValueKind.Object && _state.TryGetProperty(key, out var value) ? value.GetString() ?? "" : "";
    /// <summary>Reads an optional bounded state counter.</summary>
    private int Number(string key) => _state.ValueKind == JsonValueKind.Object && _state.TryGetProperty(key, out var value) ? value.GetInt32() : 0;
    /// <summary>Resolves a shared theme brush.</summary>
    private static Brush Resource(string key) => (Brush)Application.Current.Resources[key];
    /// <summary>Creates a local semantic-color brush without CSS.</summary>
    private static Brush Brush(string color) => new SolidColorBrush(Color.FromArgb(255, Convert.ToByte(color[1..3], 16), Convert.ToByte(color[3..5], 16), Convert.ToByte(color[5..7], 16)));

    /// <summary>Routes XAML actions to the existing managed document and request gates.</summary>
    private async Task DispatchAsync(string action, string? model = null, string? effort = null)
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
            expanded = UsageExpander.IsExpanded
        });
        await ActionRequested(payload);
    }

    /// <summary>Retains prompt drafts and refreshes send availability immediately.</summary>
    private void PromptChanged(object sender, TextChangedEventArgs arguments)
    {
        if (_updating) return;
        // The close guard must see drafts before the delayed token estimate reaches the owner.
        OpenSilver.Interop.ExecuteJavaScriptVoid("window.markvibe.updateAiPrompt($0, $1)", HostId, PromptText);
        RefreshCounter();
        SendButton.IsEnabled = Flag("busy") || Flag("configured") && Flag("documentAvailable") && Flag("modelsReady") && !Flag("unsupportedSelection") && PromptText.Trim().Length > 0;
        _promptTimer.Stop();
        _promptTimer.Start();
    }
    /// <summary>Sends on Enter and leaves Shift+Enter for plain multiline editing.</summary>
    private async void PromptKeyDown(object sender, KeyEventArgs arguments)
    {
        if (arguments.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && SendButton.IsEnabled)
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
        var cost = model.CostTier switch { "cheap" => "Cheap", "prohibitive" => "Prohibitive", _ => "Affordable" };
        content.Children.Add(new TextBlock
        {
            Text = UiStrings.T(cost) + " · " + model.ContextWindow.ToString("N0") + " token",
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
        if (!ModelFlyout.IsOpen) return;
        var current = arguments.OriginalSource as DependencyObject;
        while (current != null)
        {
            if (ReferenceEquals(current, ModelFlyout.Child) || ReferenceEquals(current, ModelButton)) return;
            current = VisualTreeHelper.GetParent(current);
        }
        ModelFlyout.IsOpen = false;
    }
    /// <summary>Restores keyboard focus after closing the separate popup.</summary>
    private void ModelFlyoutClosed(object sender, EventArgs arguments) { ModelButton.Focus(); }
    /// <summary>Closes the model popup with Escape.</summary>
    private void FlyoutKeyDown(object sender, KeyEventArgs arguments) { if (arguments.Key == Key.Escape) { ModelFlyout.IsOpen = false; arguments.Handled = true; } }
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
        EffortLabel.Text = FlyoutEffort.Text = UiStrings.T(EffortNames[effortIndex]);
        RefreshEffortAppearance(effortIndex, animate: true);
        await DispatchAsync("effort", effort: effort);
    }

    /// <summary>Increases accent saturation across the existing five effort levels with a short color transition.</summary>
    private void RefreshEffortAppearance(int effortIndex, bool animate)
    {
        var accent = ((SolidColorBrush)Resource("SystemAccent")).Color;
        var luminance = accent.R * 0.2126 + accent.G * 0.7152 + accent.B * 0.0722;
        var saturation = 0.25 + Math.Clamp(effortIndex, 0, 4) * 0.1875;
        var target = Color.FromArgb(accent.A,
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
    /// <summary>Asks the owner to resize the floating composer when usage expands.</summary>
    private async void UsageChanged(object sender, RoutedEventArgs arguments) { if (!_updating) await DispatchAsync("layout"); }
    /// <summary>Closes the panel after retaining the latest prompt draft.</summary>
    private async void ClosePanel(object sender, RoutedEventArgs arguments) { _promptTimer.Stop(); await DispatchAsync("prompt"); await DispatchAsync("close"); }
    /// <summary>Opens application settings from the missing-key banner.</summary>
    private async void OpenSettings(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("settings");
    }
    /// <summary>Sends the current scoped prompt or cancels an active request.</summary>
    private async void SendPrompt(object sender, RoutedEventArgs arguments) { _promptTimer.Stop(); await DispatchAsync(Flag("busy") ? "cancel" : "submit"); }
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
    /// <summary>Restores the original source through the managed history contract.</summary>
    private async void RestoreEdit(object sender, RoutedEventArgs arguments)
    {
        await DispatchAsync("restore");
    }

    /// <summary>Captures only title-bar gestures, leaving header buttons and prompt selection independent.</summary>
    private void BeginPanelDrag(object sender, MouseButtonEventArgs arguments)
    {
        var current = arguments.OriginalSource as DependencyObject;
        while (current != null && !ReferenceEquals(current, TitleBar))
        {
            if (current is Button)
            {
                return;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        ModelFlyout.IsOpen = false;
        _dragPoint = arguments.GetPosition(Application.Current.RootVisual);
        TitleBar.CaptureMouse();
        arguments.Handled = true;
    }

    /// <summary>Reports root-relative pointer deltas to the owner, which clamps the panel inside its viewport.</summary>
    private void MovePanel(object sender, MouseEventArgs arguments)
    {
        if (_dragPoint is not Point previous)
        {
            return;
        }
        var point = arguments.GetPosition(Application.Current.RootVisual);
        _dragPoint = point;
        MoveRequested?.Invoke(new Point(point.X - previous.X, point.Y - previous.Y));
    }

    /// <summary>Completes a title-bar move without affecting prompt drafts.</summary>
    private void EndPanelDrag(object sender, MouseButtonEventArgs arguments)
    {
        _dragPoint = null;
        TitleBar.ReleaseMouseCapture();
    }

    /// <summary>Stops dragging when another control or the operating system takes pointer capture.</summary>
    private void CancelPanelDrag(object sender, MouseEventArgs arguments)
    {
        _dragPoint = null;
    }

    /// <summary>Compacts to the title bar while retaining prompt, model and budget state.</summary>
    private void ToggleCompact(object sender, RoutedEventArgs arguments)
    {
        _compact = !_compact;
        ModelFlyout.IsOpen = false;
        PanelContent.Visibility = _compact ? Visibility.Collapsed : Visibility.Visible;
        CompactIcon.RenderTransform = new RotateTransform { Angle = _compact ? 180 : 0, CenterX = 8, CenterY = 8 };
        ToolTipService.SetToolTip(CompactButton, _compact ? "Expand AI Assistant" : "Compact AI Assistant");
        CompactChanged?.Invoke(_compact);
    }
}
