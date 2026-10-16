/* VBWR B
 * Project: MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; native OpenSilver document tree; 2026-10-11
 * Human guidance: Umberto Giacobbi; Windows-style folder navigation and flat icons
 * Modified with AI: OpenAI Codex; honor the selected UI font in outline labels; 2026-10-16
 * Human guidance: Umberto Giacobbi; one independent font preference for the application UI
 * Modified with AI: OpenAI Codex; preserve document and folder labels across language changes; 2026-10-16
 * Human guidance: Umberto Giacobbi; translate application controls without translating user content
 * Modified with AI: OpenAI Codex; shared row background and foreground feedback; 2026-10-16
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using MarkVibe.Controls;
using MarkVibe.Core;

namespace MarkVibe;

public partial class MainPage
{
    private readonly Dictionary<string, TreeViewItem> _documentNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TreeViewItem> _folderNodes = new(StringComparer.Ordinal);
    private string _treeWorkspaceId = "";
    private string _outlineFingerprint = "";
    private sealed record OutlineLocation(string TargetId, string BlockId, int Line);

    /// <summary>Builds a semantic document outline with recognizable headings, paragraphs and images.</summary>
    private void RefreshOutline(MarkdownModel model)
    {
        if (_outlineFingerprint == model.Fingerprint)
        {
            return;
        }
        _outlineFingerprint = model.Fingerprint;
        OutlineTree.Items.Clear();
        var headingParents = new Stack<(int Level, TreeViewItem Node)>();
        var blocks = model.Blocks.ToDictionary(block => block.Id);
        var headings = model.Headings.ToDictionary(heading => heading.Line);
        var sourceLine = 1;
        var sourceOffset = 0;
        foreach (var target in model.Elements.OrderBy(target => target.SourceStart))
        {
            var isHeading = target.Kind.StartsWith("heading-", StringComparison.Ordinal);
            if (!isHeading && target.Kind is not ("paragraph" or "image" or "code" or "table"))
            {
                continue;
            }
            while (sourceOffset < target.SourceStart && sourceOffset < model.Source.Length)
            {
                if (model.Source[sourceOffset++] == '\n')
                {
                    sourceLine++;
                }
            }
            var label = target.Kind == "image" ? target.AssetPath.Split('/').Last() : target.Quote;
            var symbol = target.Kind switch
            {
                "image" => "image",
                "code" => "code",
                "table" => "table",
                _ => "paragraph"
            };
            var level = 0;
            if (isHeading)
            {
                int.TryParse(target.Kind[8..], out level);
                symbol = "heading";
                if (headings.TryGetValue(sourceLine, out var heading))
                {
                    label = heading.Text;
                }
                while (headingParents.Count > 0 && headingParents.Peek().Level >= level)
                {
                    headingParents.Pop();
                }
            }
            label = string.Join(" ", label.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(label))
            {
                label = UiStrings.T(target.Kind == "image" ? "Image" : "Paragraph");
            }
            var caption = label.Length > 120 ? label[..117] + "…" : label;
            var node = CreateTreeNode(caption, symbol, label);
            node.Tag = new OutlineLocation(target.Id, target.BlockId,
                blocks.TryGetValue(target.BlockId, out var block) ? block.StartLine : sourceLine);
            ToolTipService.SetToolTip(node, (isHeading ? "H" + level : UiStrings.T(target.Kind == "image" ? "Image" : "Paragraph")) + " · " + label);
            if (isHeading && node.Header is Grid row)
            {
                row.Children.RemoveAt(0);
                var headingMarker = new TextBlock
                {
                    Text = "H" + level,
                    FontFamily = AppTypography.CurrentFamily,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                NavigationRow.BindForeground(headingMarker, TextBlock.ForegroundProperty, node);
                row.Children.Insert(0, headingMarker);
            }
            var siblings = headingParents.Count == 0 ? OutlineTree.Items : headingParents.Peek().Node.Items;
            siblings.Add(node);
            if (isHeading)
            {
                node.IsExpanded = headingParents.Count == 0;
                headingParents.Push((level, node));
            }
        }
    }

    /// <summary>Navigates to a source-backed outline element in the reader or its source line.</summary>
    private async void OutlineSelected(object sender, RoutedPropertyChangedEventArgs<object> arguments)
    {
        if (OutlineTree.SelectedItem is TreeViewItem { Tag: OutlineLocation location } && _document != null)
        {
            await ExecuteAsync(() => NavigateAsync(_document.Path, location.TargetId,
                position: _view == "source" ? new ReadingPosition(location.BlockId, 0, location.Line) : null));
        }
    }

    /// <summary>Rebuilds only when paths change, retaining folder expansion while catalog titles arrive.</summary>
    private void RefreshDocumentTree(IReadOnlyList<DocumentCard> documents, string workspaceId, string? selectedPath)
    {
        var sameWorkspace = workspaceId == _treeWorkspaceId;
        var samePaths = sameWorkspace && documents.Count == _documentNodes.Count
            && documents.All(document => _documentNodes.ContainsKey(document.Path));
        if (!samePaths)
        {
            var expandedFolders = sameWorkspace
                ? _folderNodes.Where(pair => pair.Value.IsExpanded).Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            DocumentTree.Items.Clear();
            _folderNodes.Clear();
            _documentNodes.Clear();
            _treeWorkspaceId = workspaceId;

            // Create the folder skeleton first so folders precede files at every depth.
            foreach (var document in documents)
            {
                var segments = document.Path.Split('/');
                var parentItems = DocumentTree.Items;
                var folderPath = "";
                for (var index = 0; index < segments.Length - 1; index++)
                {
                    folderPath = folderPath.Length == 0 ? segments[index] : folderPath + "/" + segments[index];
                    if (!_folderNodes.TryGetValue(folderPath, out var folder))
                    {
                        folder = CreateTreeNode(segments[index], "folder", folderPath);
                        folder.Tag = folderPath;
                        var targetPath = folderPath;
                        AttachFileMenu(folder, () => new(workspaceId, targetPath, targetPath.Split('/').Last(), true));
                        folder.IsExpanded = expandedFolders.Contains(folderPath);
                        _folderNodes.Add(folderPath, folder);
                        parentItems.Add(folder);
                    }
                    parentItems = folder.Items;
                }
            }

            foreach (var document in documents)
            {
                var lastSeparator = document.Path.LastIndexOf('/');
                var filename = document.Path[(lastSeparator + 1)..];
                var node = CreateTreeNode(filename, "file", document.Path);
                AttachFileMenu(node, () => node.Tag is DocumentCard current ? new(workspaceId, current.Path, current.Title) : null);
                var parentItems = lastSeparator < 0 ? DocumentTree.Items : _folderNodes[document.Path[..lastSeparator]].Items;
                parentItems.Add(node);
                _documentNodes.Add(document.Path, node);
            }
        }

        foreach (var document in documents)
        {
            var node = _documentNodes[document.Path];
            node.Tag = document;
            ToolTipService.SetToolTip(node, document.Title + "\n" + document.Path);
            if (node.IsSelected != (document.Path == selectedPath))
            {
                node.IsSelected = document.Path == selectedPath;
            }
        }
        if (selectedPath != null)
        {
            foreach (var folder in _folderNodes)
            {
                if (selectedPath.StartsWith(folder.Key + "/", StringComparison.Ordinal))
                {
                    folder.Value.IsExpanded = true;
                }
            }
        }
    }

    /// <summary>Creates one compact accessible row; TreeView supplies expansion and keyboard navigation.</summary>
    private static TreeViewItem CreateTreeNode(string name, string symbol, string path)
    {
        var header = new Grid { Margin = new Thickness(4, 5, 6, 5), Background = new SolidColorBrush(Colors.Transparent) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var icon = new VectorIcon { Symbol = symbol, Width = 20, Height = 20, VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(icon);
        var caption = new TextBlock
        {
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        AppLocalization.SetRaw(caption, name);
        Grid.SetColumn(caption, 1);
        header.Children.Add(caption);
        var node = new TreeViewItem
        {
            Header = header,
            Style = (Style)Application.Current.Resources["DocumentTreeItem"],
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        NavigationRow.BindForeground(icon, Control.ForegroundProperty, node);
        NavigationRow.BindForeground(caption, TextBlock.ForegroundProperty, node);
        NavigationRow.Attach(node, header);
        // The row's caption, path, tooltip and any child document rows are caller-owned content.
        AppLocalization.SetRaw(node);
        AutomationProperties.SetName(node, path);
        return node;
    }
}
