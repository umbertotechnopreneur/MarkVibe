/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; inert embedded Markdown HTML; 2026-10-23
 * Human guidance: Umberto Giacobbi; fix comments and collapsible document rendering
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkVibe.Core;

/// <summary>Renders inert formatting tags without accepting document attributes, resources or bridge metadata.</summary>
internal static class SafeHtmlRendering
{
    private static readonly Regex Tokens = new(
        @"<!--[\s\S]*?(?:-->|$)|<[^>]*>",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex FormattingTag = new(
        @"^<(?<closing>/)?(?<name>details|summary|br|kbd|sup|sub|mark|del|s|ins|small|b|i|strong|em)(?:\s[^<>]*)?/?>$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>Keeps Markdown following a same-line comment on the normal inline parsing path.</summary>
    internal static void ConfigureParser(MarkdownPipelineBuilder builder)
    {
        var htmlParser = builder.BlockParsers.OfType<HtmlBlockParser>().First();
        var index = builder.BlockParsers.IndexOf(htmlParser);
        builder.BlockParsers[index] = new CommentPrefixHtmlParser();
    }

    /// <summary>Avoids treating a comment-prefixed Markdown paragraph as an opaque raw HTML block.</summary>
    private sealed class CommentPrefixHtmlParser : HtmlBlockParser
    {
        /// <summary>Defers only same-line comment prefixes with following Markdown to the inline parser.</summary>
        public override BlockState TryOpen(BlockProcessor processor)
        {
            var line = processor.Line.ToString().TrimStart();
            if (line.StartsWith("<!--", StringComparison.Ordinal))
            {
                var commentEnd = line.IndexOf("-->", StringComparison.Ordinal);
                if (commentEnd >= 0)
                {
                    var remainder = line[(commentEnd + 3)..].TrimStart();
                    if (remainder.Length > 0 && !remainder.StartsWith('<'))
                    {
                        return BlockState.None;
                    }
                }
            }

            return base.TryOpen(processor);
        }
    }

    /// <summary>Replaces raw HTML writers so the managed model itself never carries active HTML.</summary>
    internal static void Configure(HtmlRenderer renderer)
    {
        foreach (var existing in renderer.ObjectRenderers.OfType<HtmlBlockRenderer>().ToArray())
        {
            renderer.ObjectRenderers.Remove(existing);
        }
        foreach (var existing in renderer.ObjectRenderers.OfType<HtmlInlineRenderer>().ToArray())
        {
            renderer.ObjectRenderers.Remove(existing);
        }

        renderer.ObjectRenderers.Insert(0, new SafeBlockRenderer());
        renderer.ObjectRenderers.Insert(0, new SafeInlineRenderer());
    }

    /// <summary>Hides comments, emits only known tag names and encodes all remaining source text.</summary>
    private static void WriteHtml(HtmlRenderer renderer, string html)
    {
        var position = 0;
        foreach (Match token in Tokens.Matches(html))
        {
            renderer.Write(HtmlEncoder.Default.Encode(WebUtility.HtmlDecode(html[position..token.Index])));
            position = token.Index + token.Length;
            if (token.Value.StartsWith("<!--", StringComparison.Ordinal))
            {
                continue;
            }

            var tag = FormattingTag.Match(token.Value);
            if (tag.Success)
            {
                var name = tag.Groups["name"].Value.ToLowerInvariant();
                var closing = tag.Groups["closing"].Success;
                // No source attribute survives, including event handlers and data-mv-*.
                // Details begins collapsed; its summary remains keyboard operable.
                renderer.Write("<" + (closing ? "/" : "") + name + ">");
            }
            else
            {
                renderer.Write(HtmlEncoder.Default.Encode(token.Value));
            }
        }

        renderer.Write(HtmlEncoder.Default.Encode(WebUtility.HtmlDecode(html[position..])));
    }

    /// <summary>Preserves block delimiters across the surrounding Markdown blocks.</summary>
    private sealed class SafeBlockRenderer : HtmlObjectRenderer<HtmlBlock>
    {
        /// <summary>Filters complete HTML blocks, including multiline comments and summaries.</summary>
        protected override void Write(HtmlRenderer renderer, HtmlBlock block)
        {
            WriteHtml(renderer, block.Lines.ToString());
            renderer.WriteLine();
        }
    }

    /// <summary>Uses the same restricted vocabulary for inline formatting and comments.</summary>
    private sealed class SafeInlineRenderer : HtmlObjectRenderer<HtmlInline>
    {
        /// <summary>Filters the original inline token without trusting source attributes.</summary>
        protected override void Write(HtmlRenderer renderer, HtmlInline inline)
        {
            WriteHtml(renderer, inline.Tag);
        }
    }
}
