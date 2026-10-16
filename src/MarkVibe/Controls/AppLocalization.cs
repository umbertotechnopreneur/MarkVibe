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
        "WorkspaceHelp"
    ];
    /// <summary>Register a dynamic catalog label and retain its arguments for later refreshes.</summary>
    public static void SetText(TextBlock element, string key, params object? [] arguments)
    {
        var labels = Originals.GetOrCreateValue(element);
        labels.Text = key;
        labels.Arguments = arguments;
        element.Text = UiStrings.T(key, arguments);
    }

    /// <summary>Display caller-owned text and discard any previously registered translation.</summary>
    public static void SetRaw(TextBlock element, string text)
    {
        var labels = Originals.GetOrCreateValue(element);
        labels.Text = null;
        labels.Arguments = [];
        element.Text = text;
    }

    /// <summary>Decorate button captions before refreshing their text and accessible labels.</summary>
    public static void Apply(DependencyObject root)
    {
        ButtonIcons.Decorate(root);
        Walk(root, new HashSet<DependencyObject>());
    }

    /// <summary>Visit declared and visual children once, translating only recognized UI labels.</summary>
    private static void Walk(DependencyObject element, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(element))
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
            labels.Tooltip ??= ToolTipService.GetToolTip(element) as string;
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

            if (string.IsNullOrEmpty(labels.Tooltip))
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

        if (element is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                Walk(child, visited);
            }
        }

        if (element is Border border && border.Child != null)
        {
            Walk(border.Child, visited);
        }

        if (element is ContentControl contentControl && contentControl.Content is DependencyObject declaredContent)
        {
            Walk(declaredContent, visited);
        }

        if (element is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items.OfType<DependencyObject>())
            {
                Walk(item, visited);
            }
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
