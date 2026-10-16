/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; semantic Markdown outline elements; 2026-10-11
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using Markdig.Extensions.Tables;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkVibe.Core;
/// <summary>A source-backed Markdown element; source offsets are zero-based with an exclusive end.</summary>
public sealed record MarkdownElement(
    string Id,
    string Kind,
    string BlockId,
    string Quote,
    int SourceStart,
    int SourceEnd,
    string AssetPath = ""
);
/// <summary>Catalogs semantic source elements for the managed outline and renderer navigation.</summary>
public static class MarkdownElements
{
    /// <summary>Catalogs a top-level block and its nested source elements before rendering their attributes.</summary>
    internal static MarkdownElement Attach(
        Block block,
        string blockId,
        string source,
        IReadOnlyDictionary<LinkInline, string> imageAssets,
        List<MarkdownElement> targets,
        HashSet<string> ids
    )
    {
        var rootTarget = AttachElement(block, Kind(block), blockId, source, "", targets, ids);
        foreach (var nestedBlock in block.Descendants<Block>())
        {
            AttachElement(nestedBlock, Kind(nestedBlock), blockId, source, "", targets, ids);
        }

        foreach (var image in block.Descendants<LinkInline>().Where(link => link.IsImage))
        {
            imageAssets.TryGetValue(image, out var assetPath);
            AttachElement(image, "image", blockId, source, assetPath ?? "", targets, ids);
        }

        return rootTarget;
    }

    /// <summary>Assigns a unique render ID and a bounded source caption for the outline.</summary>
    private static MarkdownElement AttachElement(MarkdownObject element, string kind, string blockId, string source, string assetPath, List<MarkdownElement> targets, HashSet<string> ids)
    {
        var start = Math.Clamp(element.Span.Start, 0, source.Length);
        var end = Math.Clamp(element.Span.End + 1, start, source.Length);
        var fragment = source[start..end];
        var fragmentHash = MarkdownParser.Fingerprint(fragment);
        var stem = "element-" + MarkdownParser.Fingerprint(kind + "\u001f" + fragmentHash + "\u001f" + start)[..24];
        var id = stem;
        var suffix = 1;
        while (!ids.Add(id))
        {
            id = stem + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            suffix++;
        }

        var trimmedFragment = fragment.Trim();
        var quote = trimmedFragment.Length <= 240 ? trimmedFragment : trimmedFragment[..239] + "…";
        var target = new MarkdownElement(id, kind, blockId, quote, start, end, assetPath);
        element.GetAttributes().AddProperty("data-element-id", id);
        targets.Add(target);
        return target;
    }

    /// <summary>Uses semantic syntax kinds rather than positions or generated DOM structure.</summary>
    private static string Kind(Block block)
    {
        return block switch
        {
            HeadingBlock heading => "heading-" + heading.Level.ToString(System.Globalization.CultureInfo.InvariantCulture),
            FencedCodeBlock fenced when string.Equals(fenced.Info?.ToString().Trim(), "mermaid", StringComparison.OrdinalIgnoreCase) => "diagram",
            CodeBlock => "code",
            ParagraphBlock => "paragraph",
            ListItemBlock => "list-item",
            ListBlock => "list",
            QuoteBlock => "quote",
            TableCell => "table-cell",
            TableRow => "table-row",
            Table => "table",
            ThematicBreakBlock => "thematic-break",
            _ => "block"
        };
    }
}
