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
 * Modified with AI: OpenAI Codex; exact literal source mapping for Markdown selections; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Yaml;

namespace MarkVibe.Core;
public sealed record MarkdownModel(
    string Path,
    string Title,
    string Introduction,
    string Source,
    string Html,
    string Fingerprint,
    IReadOnlyList<Heading> Headings,
    IReadOnlyList<SourceBlock> Blocks,
    IReadOnlyList<DocumentLink> Links,
    int LineCount
)
{
    public IReadOnlyList<MarkdownElement> Elements { get; init; } = [];
}

/// <summary>Builds bounded source-backed Markdown models with restricted embedded HTML and local navigation.</summary>
public sealed class MarkdownParser
{
    private readonly MarkdownPipeline _pipeline = CreatePipeline();

    /// <summary>Preserves normal Markdown after inline comment prefixes while recognizing disclosure blocks.</summary>
    internal static MarkdownPipeline CreatePipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .UseEmphasisExtras()
            .UseYamlFrontMatter()
            .UsePreciseSourceLocation();
        SafeHtmlRendering.ConfigureParser(builder);
        return builder.Build();
    }
    /// <summary>Encodes source text before embedding it in generated HTML.</summary>
    private static string Escape(string value) => HtmlEncoder.Default.Encode(value);
    /// <summary>Computes the exact UTF-8 source fingerprint used by document and note identities.</summary>
    public static string Fingerprint(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    /// <summary>Parses bounded Markdown and renders source-backed ink blocks and note targets.</summary>
    public MarkdownModel Parse(string path, string source)
    {
        path = WorkspacePath.Normalize(path);
        if (Encoding.UTF8.GetByteCount(source) > Limits.DocumentBytes)
        {
            throw new WorkspaceException("document-too-large", "This document exceeds the 1 MiB reading limit.");
        }

        var document = Markdown.Parse(source, _pipeline);
        var allBlocks = document.Descendants<Block>().Take(Limits.Blocks + 1).ToList();
        if (allBlocks.Count > Limits.Blocks)
        {
            throw new WorkspaceException("too-many-blocks", "This document contains too many blocks to display safely.");
        }

        var headings = new List<Heading>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in document.Descendants<HeadingBlock>())
        {
            if (headings.Count >= Limits.Headings)
            {
                throw new WorkspaceException("too-many-headings", "This document contains too many headings.");
            }

            var text = PlainInline(block.Inline);
            var stem = Slug(text);
            var id = stem;
            for (var suffix = 1; !seen.Add(id); suffix++)
            {
                id = stem + "-" + suffix.ToString(CultureInfo.InvariantCulture);
            }

            block.GetAttributes().Id = id;
            headings.Add(new(id, text, block.Level, block.Line + 1));
        }

        var imageAssets = new Dictionary<LinkInline, string>();
        var links = new List<DocumentLink>();
        var references = 0;
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (++references > Limits.Links)
            {
                throw new WorkspaceException("too-many-links", "This document contains too many link references.");
            }

            var target = WorkspacePath.Resolve(path, link.Url ?? "");
            var attributes = link.GetAttributes();
            link.Url = "#";
            link.GetDynamicUrl = null;
            if (link.IsImage)
            {
                imageAssets[link] = target.Kind == LinkKind.Asset ? target.Path : "";
                if (target.Kind == LinkKind.Asset)
                {
                    attributes.AddProperty("data-mv-asset", target.Path);
                }
                else
                {
                    attributes.AddProperty("data-mv-blocked", "image");
                }
            }
            else if (target.Kind == LinkKind.Document)
            {
                attributes.AddProperty("data-mv-path", target.Path);
                attributes.AddProperty("data-mv-anchor", target.Anchor);
                if (links.Count >= Limits.Links)
                {
                    throw new WorkspaceException("too-many-links", "This document contains too many links.");
                }

                links.Add(new(target.Path, target.Anchor, link.Line + 1));
            }
            else if (target.Kind == LinkKind.External)
            {
                attributes.AddProperty("data-mv-external", target.ExternalUrl);
            }
            else if (target.Kind == LinkKind.Asset)
            {
                attributes.AddProperty("data-mv-attachment", target.Path);
            }
            else
            {
                attributes.AddProperty("data-mv-blocked", "link");
            }
        }

        foreach (var link in document.Descendants<AutolinkInline>())
        {
            if (++references > Limits.Links)
            {
                throw new WorkspaceException("too-many-links", "This document contains too many link references.");
            }

            // Autolinks do not provide workspace navigation. Validate them in the DOM adapter before attaching href.
            if (WorkspacePath.Resolve(path, link.Url).Kind != LinkKind.External)
            {
                link.Url = "#";
            }
        }

        var title = MetadataTitle(document) ?? headings.FirstOrDefault()?.Text ?? path.Split('/').Last();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = path.Split('/').Last();
        }

        title = Truncate(title, 160);
        var introduction = document.Descendants<ParagraphBlock>().Select(p => PlainInline(p.Inline)).FirstOrDefault(p => p.Length > 20) ?? "Open this page to explore its contents.";
        var blocks = new List<SourceBlock>();
        var blockIds = new HashSet<string>(StringComparer.Ordinal);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        SafeHtmlRendering.Configure(renderer);
        // One renderer enforces the same diagram limits for top-level and nested fences.
        foreach (var codeRenderer in renderer.ObjectRenderers.OfType<CodeBlockRenderer>().ToArray())
        {
            renderer.ObjectRenderers.Remove(codeRenderer);
        }

        renderer.ObjectRenderers.Insert(0, new DiagramCodeBlockRenderer());
        foreach (var paragraphRenderer in renderer.ObjectRenderers.OfType<ParagraphRenderer>().ToArray())
        {
            renderer.ObjectRenderers.Remove(paragraphRenderer);
        }

        renderer.ObjectRenderers.Insert(0, new OutlineParagraphRenderer());
        foreach (var literalRenderer in renderer.ObjectRenderers.OfType<LiteralInlineRenderer>().ToArray())
        {
            renderer.ObjectRenderers.Remove(literalRenderer);
        }
        renderer.ObjectRenderers.Insert(0, new SourceLiteralRenderer(source));
        var elements = new List<MarkdownElement>();
        var elementIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in document)
        {
            if (block is YamlFrontMatterBlock)
            {
                continue;
            }

            // HTML delimiters can span Markdown blocks. A section around each
            // delimiter would prematurely close details before its Markdown body.
            if (block is HtmlBlock)
            {
                renderer.Render(block);
                continue;
            }

            var start = Math.Clamp(block.Span.Start, 0, source.Length);
            var end = Math.Clamp(block.Span.End + 1, start, source.Length);
            var quote = source[start..end];
            var stem = "b-" + Fingerprint(quote)[..16];
            var blockId = stem;
            for (var suffix = 1; !blockIds.Add(blockId); suffix++)
            {
                blockId = stem + "-" + suffix;
            }

            var startLine = block.Line + 1;
            var endLine = startLine + quote.Count(c => c == '\n');
            blocks.Add(new(blockId, startLine, endLine, Truncate(quote.Trim(), 160)));
            // Outline elements share source blocks without changing their reading-position identities.
            MarkdownElements.Attach(block, blockId, source, imageAssets, elements, elementIds);
            writer.Write("<section data-block-id=\"" + blockId + "\" data-source-start=\"" + startLine + "\" data-source-end=\"" + endLine + "\">");
            renderer.Render(block);
            writer.Write("</section>");
        }

        writer.Flush();
        var html = writer.ToString();
        if (html.Length > 3 * Limits.DocumentBytes)
        {
            throw new WorkspaceException("render-too-large", "This document expands beyond the display limit.");
        }

        return new(path, title, Truncate(introduction, 240), source, html, Fingerprint(source), headings, blocks, links, source.Count(c => c == '\n') + 1)
        {
            Elements = elements
        };
    }

    /// <summary>Maps ordinary rendered text to exact UTF-16 source offsets for reader selections.</summary>
    private sealed class SourceLiteralRenderer(string source) : LiteralInlineRenderer
    {
        /// <summary>Annotates only text with an exact source match; escaped/entity text remains selectable in Source view.</summary>
        protected override void Write(HtmlRenderer renderer, LiteralInline literal)
        {
            var text = literal.Content.ToString();
            var start = literal.Span.Start;
            var end = literal.Span.End + 1;
            var exact = renderer.EnableHtmlForInline && start >= 0 && end >= start && end <= source.Length
                && source[start..end] == text;
            if (exact)
            {
                renderer.Write("<span data-md-start=\"").Write(start.ToString(CultureInfo.InvariantCulture))
                    .Write("\" data-md-end=\"").Write(end.ToString(CultureInfo.InvariantCulture)).Write("\">");
            }
            base.Write(renderer, literal);
            if (exact)
            {
                renderer.Write("</span>");
            }
        }
    }

    /// <summary>Preserves targets for paragraphs whose surrounding list or table suppresses a paragraph tag.</summary>
    private sealed class OutlineParagraphRenderer : ParagraphRenderer
    {
        /// <summary>Uses an inline wrapper for implicit paragraphs without adding block margins to tight lists.</summary>
        protected override void Write(HtmlRenderer renderer, ParagraphBlock paragraph)
        {
            if (!renderer.ImplicitParagraph || !renderer.EnableHtmlForBlock)
            {
                base.Write(renderer, paragraph);
                return;
            }

            renderer.Write("<span").WriteAttributes(paragraph).Write(">");
            renderer.WriteLeafInline(paragraph);
            renderer.Write("</span>");
        }
    }

    /// <summary>Retains a source-owning diagram wrapper even after the client replaces its SVG contents.</summary>
    private sealed class DiagramCodeBlockRenderer : CodeBlockRenderer
    {
        private int _diagramCount;
        /// <summary>Renders bounded Mermaid fences at any nesting depth and delegates ordinary code blocks.</summary>
        protected override void Write(HtmlRenderer renderer, CodeBlock block)
        {
            if (block is not FencedCodeBlock fenced ||
                !string.Equals(fenced.Info?.ToString().Trim(), "mermaid", StringComparison.OrdinalIgnoreCase))
            {
                base.Write(renderer, block);
                return;
            }

            var diagram = fenced.Lines.ToString();
            _diagramCount++;
            renderer.EnsureLine();
            block.GetAttributes().AddClass("mv-diagram");
            renderer.Write("<div").WriteAttributes(block).Write(">");
            if (_diagramCount > 12 || diagram.Length > 20000)
            {
                renderer.Write("<p class=\"resource-message\">This diagram exceeds the display limit. Its source is available in Source view.</p>");
            }
            else
            {
                renderer.Write("<pre><code class=\"language-mermaid\">").Write(Escape(diagram)).Write("</code></pre>");
            }

            renderer.WriteLine("</div>");
        }
    }

    /// <summary>Reads a bounded plain title from the optional YAML front matter.</summary>
    private static string? MetadataTitle(MarkdownDocument document)
    {
        if (document.FirstOrDefault()is not YamlFrontMatterBlock frontmatter)
        {
            return null;
        }

        foreach (var line in frontmatter.Lines.ToString().Split('\n').Take(40))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed[6..].Trim().Trim('"', '\'');
                if (value.Length > 0 && value.Length <= 160 && !value.Any(char.IsControl))
                {
                    return value;
                }
            }
        }

        return null;
    }

    /// <summary>Collects visible inline text without retaining Markdown syntax or HTML.</summary>
    public static string PlainInline(ContainerInline? inline)
    {
        if (inline == null)
        {
            return "";
        }

        var builder = new StringBuilder();
        foreach (var child in inline.Descendants())
        {
            switch (child)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case AutolinkInline auto:
                    builder.Append(auto.Url);
                    break;
            }
        }

        return builder.ToString().Trim();
    }

    /// <summary>Creates the stable readable stem used by heading navigation anchors.</summary>
    public static string Slug(string value)
    {
        var builder = new StringBuilder();
        var separator = false;
        foreach (var character in value.Normalize(NormalizationForm.FormC).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                if (separator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                separator = false;
            }
            else if (char.IsWhiteSpace(character) || character == '-')
            {
                separator = true;
            }
        }

        return builder.Length == 0 ? "section" : builder.ToString();
    }

    /// <summary>Bounds display text while indicating omitted characters.</summary>
    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
}
