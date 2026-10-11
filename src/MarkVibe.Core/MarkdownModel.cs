using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Yaml;

namespace MarkVibe.Core;

public sealed record MarkdownModel(string Path, string Title, string Introduction, string Source, string Html, string Fingerprint,
    IReadOnlyList<Heading> Headings, IReadOnlyList<SourceBlock> Blocks, IReadOnlyList<DocumentLink> Links, int LineCount);

public sealed class MarkdownParser
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables().UseTaskLists().UseAutoLinks().UseEmphasisExtras()
        .UseYamlFrontMatter().UsePreciseSourceLocation().DisableHtml().Build();

    private static string Escape(string value) => HtmlEncoder.Default.Encode(value);
    public static string Fingerprint(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();

    public MarkdownModel Parse(string path, string source)
    {
        path = WorkspacePath.Normalize(path);
        if (Encoding.UTF8.GetByteCount(source) > Limits.DocumentBytes)
            throw new WorkspaceException("document-too-large", "This document exceeds the 1 MiB reading limit.");
        var document = Markdown.Parse(source, _pipeline);
        var allBlocks = document.Descendants<Block>().Take(Limits.Blocks + 1).ToList();
        if (allBlocks.Count > Limits.Blocks)
            throw new WorkspaceException("too-many-blocks", "This document contains too many blocks to display safely.");
        var headings = new List<Heading>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in document.Descendants<HeadingBlock>())
        {
            if (headings.Count >= Limits.Headings) throw new WorkspaceException("too-many-headings", "This document contains too many headings.");
            var text = PlainInline(block.Inline);
            var stem = Slug(text);
            var id = stem;
            for (var suffix = 1; !seen.Add(id); suffix++) id = stem + "-" + suffix.ToString(CultureInfo.InvariantCulture);
            block.GetAttributes().Id = id;
            headings.Add(new(id, text, block.Level, block.Line + 1));
        }
        var links = new List<DocumentLink>();
        var references = 0;
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (++references > Limits.Links) throw new WorkspaceException("too-many-links", "This document contains too many link references.");
            var target = WorkspacePath.Resolve(path, link.Url ?? "");
            var attributes = link.GetAttributes();
            link.Url = "#";
            link.GetDynamicUrl = null;
            if (link.IsImage)
            {
                if (target.Kind == LinkKind.Asset)
                    attributes.AddProperty("data-mv-asset", target.Path);
                else attributes.AddProperty("data-mv-blocked", "image");
            }
            else if (target.Kind == LinkKind.Document)
            {
                attributes.AddProperty("data-mv-path", target.Path);
                attributes.AddProperty("data-mv-anchor", target.Anchor);
                if (links.Count >= Limits.Links) throw new WorkspaceException("too-many-links", "This document contains too many links.");
                links.Add(new(target.Path, target.Anchor, link.Line + 1));
            }
            else if (target.Kind == LinkKind.External) attributes.AddProperty("data-mv-external", target.ExternalUrl);
            else if (target.Kind == LinkKind.Asset) attributes.AddProperty("data-mv-attachment", target.Path);
            else attributes.AddProperty("data-mv-blocked", "link");
        }
        foreach (var link in document.Descendants<AutolinkInline>())
        {
            if (++references > Limits.Links) throw new WorkspaceException("too-many-links", "This document contains too many link references.");
            // Autolinks do not provide workspace navigation. Validate them in the DOM adapter before attaching href.
            if (WorkspacePath.Resolve(path, link.Url).Kind != LinkKind.External) link.Url = "#";
        }
        var title = MetadataTitle(document) ?? headings.FirstOrDefault()?.Text ?? path.Split('/').Last();
        if (string.IsNullOrWhiteSpace(title)) title = path.Split('/').Last();
        title = Truncate(title, 160);
        var introduction = document.Descendants<ParagraphBlock>().Select(p => PlainInline(p.Inline)).FirstOrDefault(p => p.Length > 20) ?? "Open this page to explore its contents.";
        var blocks = new List<SourceBlock>();
        var blockIds = new HashSet<string>(StringComparer.Ordinal);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        var diagramCount = 0;
        foreach (var block in document)
        {
            if (block is YamlFrontMatterBlock) continue;
            var start = Math.Clamp(block.Span.Start, 0, source.Length);
            var end = Math.Clamp(block.Span.End + 1, start, source.Length);
            var quote = source[start..end];
            var stem = "b-" + Fingerprint(quote)[..16];
            var blockId = stem;
            for (var suffix = 1; !blockIds.Add(blockId); suffix++) blockId = stem + "-" + suffix;
            var startLine = block.Line + 1;
            var endLine = startLine + quote.Count(c => c == '\n');
            blocks.Add(new(blockId, startLine, endLine, Truncate(quote.Trim(), 160)));
            writer.Write("<section data-block-id=\"" + blockId + "\" data-source-start=\"" + startLine + "\" data-source-end=\"" + endLine + "\">");
            if (block is FencedCodeBlock fenced && (fenced.Info?.ToString() ?? "").Trim().Equals("mermaid", StringComparison.OrdinalIgnoreCase))
            {
                var diagram = fenced.Lines.ToString();
                if (++diagramCount > 12 || diagram.Length > 20000)
                    writer.Write("<p class=\"resource-message\">This diagram exceeds the display limit. Its source is available in Source view.</p>");
                else writer.Write("<div class=\"mv-diagram\"><pre><code class=\"language-mermaid\">" + Escape(diagram) + "</code></pre></div>");
            }
            else renderer.Render(block);
            writer.Write("</section>");
        }
        writer.Flush();
        var html = writer.ToString();
        if (html.Length > 3 * Limits.DocumentBytes)
            throw new WorkspaceException("render-too-large", "This document expands beyond the display limit.");
        return new(path, title, Truncate(introduction, 240), source, html, Fingerprint(source), headings, blocks, links, source.Count(c => c == '\n') + 1);
    }

    private static string? MetadataTitle(MarkdownDocument document)
    {
        if (document.FirstOrDefault() is not YamlFrontMatterBlock frontmatter) return null;
        foreach (var line in frontmatter.Lines.ToString().Split('\n').Take(40))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed[6..].Trim().Trim('"', '\'');
                if (value.Length > 0 && value.Length <= 160 && !value.Any(char.IsControl)) return value;
            }
        }
        return null;
    }

    public static string PlainInline(ContainerInline? inline)
    {
        if (inline == null) return "";
        var builder = new StringBuilder();
        foreach (var child in inline.Descendants())
            switch (child)
            {
                case LiteralInline literal: builder.Append(literal.Content.ToString()); break;
                case CodeInline code: builder.Append(code.Content); break;
                case LineBreakInline: builder.Append(' '); break;
                case AutolinkInline auto: builder.Append(auto.Url); break;
            }
        return builder.ToString().Trim();
    }

    public static string Slug(string value)
    {
        var builder = new StringBuilder();
        var separator = false;
        foreach (var character in value.Normalize(NormalizationForm.FormC).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                if (separator && builder.Length > 0) builder.Append('-');
                builder.Append(character); separator = false;
            }
            else if (char.IsWhiteSpace(character) || character == '-') separator = true;
        }
        return builder.Length == 0 ? "section" : builder.ToString();
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
}
