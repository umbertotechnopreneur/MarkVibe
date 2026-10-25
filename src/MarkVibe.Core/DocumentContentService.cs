/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; validated document context and clipboard content services; 2026-10-25
 * Human guidance: Umberto Giacobbi; Markdown parsing and command logic belong below the UI
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkVibe.Core;

/// <summary>Resolves menu context and prepares clipboard text from the authoritative Markdown snapshot.</summary>
public static class DocumentContentService
{
    /// <summary>Validates captured identities without reparsing or accepting stale DOM source positions.</summary>
    public static DocumentContextInfo Describe(MarkdownModel model, FileMenuTarget target)
    {
        if (target.IsFolder || target.Path != model.Path ||
            string.IsNullOrEmpty(target.Fingerprint) || target.Fingerprint != model.Fingerprint)
        {
            return new();
        }

        var element = string.IsNullOrEmpty(target.ElementId)
            ? null
            : model.Elements.FirstOrDefault(candidate => candidate.Id == target.ElementId);
        if (!string.IsNullOrEmpty(target.ElementId) && (element == null || !HasValidRange(model, element)))
        {
            return new();
        }

        var sourceContext = target.View == "source" && string.IsNullOrEmpty(target.ElementId);
        if (sourceContext)
        {
            if (target.SourceLine < 1 || target.SourceLine > model.LineCount)
            {
                return new();
            }
            // Source views report a line rather than DOM identifiers. Resolve it against C# parser metadata.
            element = FindElementAtLine(model, target.SourceLine);
        }

        var heading = string.IsNullOrEmpty(target.HeadingId)
            ? null
            : model.Headings.FirstOrDefault(candidate => candidate.Id == target.HeadingId);
        if (!string.IsNullOrEmpty(target.HeadingId) && heading == null)
        {
            return new();
        }

        var sourceLine = sourceContext || element == null
            ? target.SourceLine
            : LineAtOffset(model.Source, element.SourceStart);
        if (heading != null)
        {
            // A heading anchor cannot be combined with a captured element from another paragraph.
            if (element != null && (!element.Kind.StartsWith("heading-", StringComparison.Ordinal) ||
                sourceLine != heading.Line))
            {
                return new();
            }
            sourceLine = heading.Line;
        }
        else if (sourceContext || element?.Kind.StartsWith("heading-", StringComparison.Ordinal) == true)
        {
            heading = model.Headings.FirstOrDefault(candidate => candidate.Line == sourceLine);
        }

        if (sourceLine < 1 || sourceLine > model.LineCount)
        {
            return new();
        }

        var table = FindContainer(model, element, "table");
        var code = FindContainer(model, element, "code") ?? FindContainer(model, element, "diagram");
        return new(sourceLine, heading?.Id ?? "", table?.Id ?? "", code?.Id ?? "");
    }

    /// <summary>Creates a local Markdown section link that resolves correctly at every document directory depth.</summary>
    public static string CopySectionLink(MarkdownModel model, FileMenuTarget target)
    {
        var context = Describe(model, target);
        var heading = model.Headings.FirstOrDefault(candidate => candidate.Id == context.HeadingId);
        if (heading == null)
        {
            throw InvalidContext();
        }

        var label = heading.Text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace('\r', ' ').Replace('\n', ' ');
        // Links are document-relative. A workspace path here would repeat the current directory.
        return $"[{label}](#{Uri.EscapeDataString(heading.Id)})";
    }

    /// <summary>Copies the original table source, retaining alignment markers, escapes and formatting.</summary>
    public static string CopyTableMarkdown(MarkdownModel model, FileMenuTarget target)
    {
        var context = Describe(model, target);
        var element = RequireElement(model, context.TableElementId);
        return model.Source[element.SourceStart..element.SourceEnd];
    }

    /// <summary>Converts the selected table to quoted tab-separated cells suitable for spreadsheet paste.</summary>
    public static string CopyTableTsv(MarkdownModel model, FileMenuTarget target)
    {
        var context = Describe(model, target);
        var element = RequireElement(model, context.TableElementId);
        var document = Markdown.Parse(model.Source, MarkdownParser.CreatePipeline());
        var table = document.Descendants<Table>().FirstOrDefault(candidate => candidate.Span.Start == element.SourceStart);
        if (table == null)
        {
            throw InvalidContext();
        }

        var rows = new List<string>();
        foreach (var row in table.OfType<TableRow>())
        {
            var cells = new List<string>();
            foreach (var cell in row.OfType<TableCell>())
            {
                var text = new StringBuilder();
                AppendBlockText(cell, text);
                cells.Add(EscapeSpreadsheetCell(text.ToString()));
                for (var extraColumn = 1; extraColumn < cell.ColumnSpan; extraColumn++)
                {
                    cells.Add("");
                }
            }
            while (cells.Count < table.ColumnDefinitions.Count)
            {
                cells.Add("");
            }
            rows.Add(string.Join("\t", cells));
        }
        return string.Join("\r\n", rows);
    }

    /// <summary>Copies parsed code content without fences or container indentation, preserving code whitespace.</summary>
    public static string CopyCode(MarkdownModel model, FileMenuTarget target)
    {
        var context = Describe(model, target);
        var element = RequireElement(model, context.CodeElementId);
        var document = Markdown.Parse(model.Source, MarkdownParser.CreatePipeline());
        var code = document.Descendants<CodeBlock>().FirstOrDefault(candidate => candidate.Span.Start == element.SourceStart);
        if (code == null)
        {
            throw InvalidContext();
        }
        return code.Lines.ToString();
    }

    /// <summary>Finds the smallest parser-owned element overlapping the source line captured by an editor.</summary>
    private static MarkdownElement? FindElementAtLine(MarkdownModel model, int sourceLine)
    {
        var start = 0;
        for (var line = 1; line < sourceLine; line++)
        {
            var newline = model.Source.IndexOf('\n', start);
            if (newline < 0)
            {
                return null;
            }
            start = newline + 1;
        }
        var end = model.Source.IndexOf('\n', start);
        if (end < 0)
        {
            end = model.Source.Length;
        }

        return model.Elements
            .Where(candidate => HasValidRange(model, candidate) &&
                candidate.SourceStart <= end && candidate.SourceEnd > start)
            .OrderBy(candidate => candidate.SourceEnd - candidate.SourceStart)
            .FirstOrDefault();
    }

    /// <summary>Finds the nearest semantic container for a nested element such as a table cell.</summary>
    private static MarkdownElement? FindContainer(MarkdownModel model, MarkdownElement? element, string kind)
    {
        if (element == null)
        {
            return null;
        }
        return model.Elements
            .Where(candidate => candidate.Kind == kind && HasValidRange(model, candidate) &&
                candidate.SourceStart <= element.SourceStart && candidate.SourceEnd >= element.SourceEnd)
            .OrderBy(candidate => candidate.SourceEnd - candidate.SourceStart)
            .FirstOrDefault();
    }

    /// <summary>Resolves a verified semantic identifier and refuses missing or malformed source ranges.</summary>
    private static MarkdownElement RequireElement(MarkdownModel model, string elementId)
    {
        var element = model.Elements.FirstOrDefault(candidate => candidate.Id == elementId);
        if (element == null || !HasValidRange(model, element))
        {
            throw InvalidContext();
        }
        return element;
    }

    /// <summary>Checks the exclusive source range before any substring or position calculation.</summary>
    private static bool HasValidRange(MarkdownModel model, MarkdownElement element)
    {
        return element.SourceStart >= 0 && element.SourceEnd > element.SourceStart &&
            element.SourceEnd <= model.Source.Length;
    }

    /// <summary>Converts the parser's UTF-16 source offset into a one-based document line.</summary>
    private static int LineAtOffset(string source, int offset)
    {
        var line = 1;
        for (var index = 0; index < offset; index++)
        {
            if (source[index] == '\n')
            {
                line++;
            }
        }
        return line;
    }

    /// <summary>Extracts displayed cell text while retaining paragraph boundaries and inline code.</summary>
    private static void AppendBlockText(Block block, StringBuilder text)
    {
        if (block is ContainerBlock container)
        {
            foreach (var child in container)
            {
                if (text.Length > 0)
                {
                    text.Append('\n');
                }
                AppendBlockText(child, text);
            }
        }
        else if (block is LeafBlock leaf && leaf.Inline != null)
        {
            AppendInlineText(leaf.Inline, text);
        }
    }

    /// <summary>Copies rendered inline text rather than Markdown delimiters or raw HTML tags.</summary>
    private static void AppendInlineText(Inline inline, StringBuilder text)
    {
        switch (inline)
        {
            case LiteralInline literal:
                text.Append(literal.Content.ToString());
                break;
            case CodeInline code:
                text.Append(code.Content);
                break;
            case HtmlEntityInline entity:
                text.Append(entity.Transcoded.ToString());
                break;
            case AutolinkInline link:
                text.Append(link.Url);
                break;
            case LineBreakInline:
                text.Append('\n');
                break;
            case HtmlInline html when html.Tag.Trim().Equals("<br>", StringComparison.OrdinalIgnoreCase) ||
                html.Tag.Trim().Equals("<br/>", StringComparison.OrdinalIgnoreCase) ||
                html.Tag.Trim().Equals("<br />", StringComparison.OrdinalIgnoreCase):
                text.Append('\n');
                break;
            case ContainerInline container:
                for (var child = container.FirstChild; child != null; child = child.NextSibling)
                {
                    AppendInlineText(child, text);
                }
                break;
        }
    }

    /// <summary>Quotes TSV delimiters and forces formula-like cell values to paste as literal text.</summary>
    private static string EscapeSpreadsheetCell(string value)
    {
        var trimmed = value.AsSpan().TrimStart();
        if (!trimmed.IsEmpty && trimmed[0] is '=' or '+' or '-' or '@')
        {
            value = "'" + value;
        }
        if (value.IndexOfAny(['\t', '\r', '\n', '"']) >= 0)
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }
        return value;
    }

    /// <summary>Explains why an action cannot reuse a context captured from another document revision.</summary>
    private static WorkspaceException InvalidContext()
    {
        return new("document-context-stale", "The document changed. Open the context menu again.");
    }
}
