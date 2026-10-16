/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared traversal of declared application controls; 2026-10-16
 * Human guidance: Umberto Giacobbi; remove duplicated UI traversal and keep document content separate
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MarkVibe.Controls;

/// <summary>Enumerates application-owned children without entering control templates or browser document hosts.</summary>
internal static class UiTree
{
    /// <summary>Returns declared content, popup children and item headers, including unloaded or collapsed controls.</summary>
    public static IEnumerable<DependencyObject> DeclaredChildren(DependencyObject element)
    {
        // OpenSilver UserControl derives from Control, not ContentControl, and owns a separate Content property.
        if (element is UserControl userControl && userControl.Content is DependencyObject userContent)
        {
            yield return userContent;
        }

        if (element is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                yield return child;
            }
        }

        if (element is Border border && border.Child != null)
        {
            yield return border.Child;
        }

        if (element is Popup popup && popup.Child != null)
        {
            yield return popup.Child;
        }

        if (element is ContentControl content && content.Content is DependencyObject contentChild)
        {
            yield return contentChild;
        }

        if (element is TreeViewItem treeItem && treeItem.Header is DependencyObject treeHeader)
        {
            yield return treeHeader;
        }

        if (element is TabItem tab && tab.Header is DependencyObject tabHeader)
        {
            yield return tabHeader;
        }

        if (element is ItemsControl items)
        {
            foreach (var item in items.Items.OfType<DependencyObject>())
            {
                yield return item;
            }
        }
    }
}
