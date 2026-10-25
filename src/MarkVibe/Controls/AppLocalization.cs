/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; preserve response-owned text during localization; 2026-10-16
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Modified with AI: OpenAI Codex; apply the selected font to newly localized UI and dialogs; 2026-10-16
 * Human guidance: Umberto Giacobbi; independent font preference for application controls
 * Modified with AI: OpenAI Codex; durable raw content markers and shared declared UI traversal; 2026-10-16
 * Human guidance: Umberto Giacobbi; preserve document labels and remove duplicated UI traversal
 * Modified with AI: OpenAI Codex; respect text-only confirmation tooltip suppression; 2026-10-25
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MarkVibe.Core;

namespace MarkVibe.Controls;
/// <summary>Refresh catalog-backed shell labels while preserving document-owned text.</summary>
internal static class AppLocalization
{
    private sealed class Labels
    {
        public bool Raw;
        public string? Text;
        public string? Tooltip;
        public string? Automation;
        public object? [] Arguments = [];
    }

    private static readonly ConditionalWeakTable<DependencyObject, Labels> Originals = new();
    private static readonly HashSet<string> DynamicNames = [
        "DocumentTitle",
        "WorkspaceName",
        "WorkspaceMode",
        "IndexLabel",
        "StatusText",
        "Breadcrumb",
        "VersionText",
        "BuiltText",
        "MessageTitle",
        "MessageText",
        "ExportHelp",
        "WorkspaceHelp",
        "Status",
        "ReviewText"
    ];
    /// <summary>Register a dynamic catalog label and retain its arguments for later refreshes.</summary>
    public static void SetText(TextBlock element, string key, params object? [] arguments)
    {
        var labels = Originals.GetOrCreateValue(element);
        labels.Raw = false;
        labels.Text = key;
        labels.Arguments = arguments;
        element.Text = UiStrings.T(key, arguments);
    }

    /// <summary>Displays caller-owned text and keeps it excluded from every subsequent language refresh.</summary>
    public static void SetRaw(TextBlock element, string text)
    {
        SetRaw(element);
        element.Text = text;
    }

    /// <summary>Excludes caller-owned content, tooltips and accessible labels from translation and icon decoration.</summary>
    public static void SetRaw(DependencyObject element)
    {
        var labels = Originals.GetOrCreateValue(element);
        labels.Raw = true;
        labels.Text = null;
        labels.Tooltip = null;
        labels.Automation = null;
        labels.Arguments = [];
    }

    /// <summary>Reports an explicit caller-owned subtree without allocating translation metadata.</summary>
    internal static bool IsRaw(DependencyObject element) => Originals.TryGetValue(element, out var labels) && labels.Raw;

    /// <summary>Decorate button captions before refreshing their text and accessible labels.</summary>
    public static void Apply(DependencyObject root)
    {
        ButtonIcons.Decorate(root);
        AppTypography.Apply(root);
        Walk(root, new HashSet<DependencyObject>());
    }

    /// <summary>Visit declared and visual children once, translating only recognized UI labels.</summary>
    private static void Walk(DependencyObject element, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(element) || IsRaw(element))
        {
            return;
        }

        if (element is IconLabel iconLabel)
        {
            iconLabel.RefreshLanguage();
            return;
        }

        var labels = Originals.GetOrCreateValue(element);
        if (element is FrameworkElement frameworkElement)
        {
            if (IconLabel.GetHideToolTip(element))
            {
                labels.Tooltip = null;
                ToolTipService.SetToolTip(element, null);
            }
            else
            {
                labels.Tooltip ??= ToolTipService.GetToolTip(element) as string;
            }
            labels.Automation ??= AutomationProperties.GetName(element);
            if (!string.IsNullOrEmpty(labels.Tooltip))
            {
                ToolTipService.SetToolTip(element, UiStrings.T(labels.Tooltip));
            }

            if (!string.IsNullOrEmpty(labels.Automation))
            {
                AutomationProperties.SetName(element, UiStrings.T(labels.Automation));
            }

            if (frameworkElement is TextBlock textBlock && textBlock.DataContext is not (DocumentCard or Heading))
            {
                if (labels.Text == null && !DynamicNames.Contains(textBlock.Name) && UiStrings.Contains(textBlock.Text))
                {
                    labels.Text = textBlock.Text;
                }

                if (labels.Text != null)
                {
                    textBlock.Text = UiStrings.T(labels.Text, labels.Arguments);
                }
            }
        }

        if (element is ButtonBase button)
        {
            if (button.Content is string caption)
            {
                labels.Text ??= caption;
                button.Content = UiStrings.T(labels.Text);
            }

            if (!IconLabel.GetHideToolTip(button) && string.IsNullOrEmpty(labels.Tooltip))
            {
                labels.Tooltip = ResolveButtonTooltip(button, labels);
                ToolTipService.SetToolTip(button, UiStrings.T(labels.Tooltip));
            }
        }

        if (element is TabItem tab && tab.Header is string header)
        {
            labels.Text ??= header;
            tab.Header = UiStrings.T(labels.Text);
        }

        if (element is ComboBoxItem choice && choice.Content is string choiceText && UiStrings.Contains(choiceText))
        {
            labels.Text ??= choiceText;
            choice.Content = UiStrings.T(labels.Text);
        }

        if (element is CheckBox checkBox && checkBox.Content is string checkLabel)
        {
            labels.Text ??= checkLabel;
            checkBox.Content = UiStrings.T(labels.Text);
        }

        foreach (var child in UiTree.DeclaredChildren(element))
        {
            Walk(child, visited);
        }

        for (var childIndex = 0; childIndex < VisualTreeHelper.GetChildrenCount(element); childIndex++)
        {
            Walk(VisualTreeHelper.GetChild(element, childIndex), visited);
        }
    }

    /// <summary>Prefer the button's accessible name and caption before generic action labels.</summary>
    private static string ResolveButtonTooltip(ButtonBase button, Labels labels)
    {
        if (labels.Automation is { Length: > 0 })
        {
            return labels.Automation;
        }

        var captionKey = labels.Text ?? (button.Content as IconLabel)?.CaptionKey;
        if (captionKey != null)
        {
            return captionKey;
        }

        if (button.Name.Contains("Close", StringComparison.OrdinalIgnoreCase))
        {
            return "Close";
        }

        if (button is ToggleButton)
        {
            return "Open options";
        }

        if (button is RepeatButton)
        {
            return "Scroll";
        }

        return "Activate control";
    }
}
