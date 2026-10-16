/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; local Markdown document statistics; 2026-10-16
 * Human guidance: Umberto Giacobbi; optional map details for document size, words, characters and estimated tokens
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Globalization;
using System.Text;

namespace MarkVibe.Core;

/// <summary>Describes the full decoded Markdown source; byte size is its UTF-8 representation, not a filesystem allocation.</summary>
public sealed record DocumentStatistics(int SourceBytes, int Characters, int Words, int EstimatedTokens, int Lines, int OutgoingLinks)
{
    /// <summary>Counts Unicode scalar characters and letter/number word groups without retaining source text or making provider requests.</summary>
    public static DocumentStatistics FromModel(MarkdownModel model)
    {
        var sourceBytes = Encoding.UTF8.GetByteCount(model.Source);
        var characters = 0;
        var words = 0;
        var insideWord = false;
        foreach (var character in model.Source.EnumerateRunes())
        {
            characters++;
            if (Rune.IsLetterOrDigit(character))
            {
                if (!insideWord)
                {
                    words++;
                }

                insideWord = true;
                continue;
            }

            // Combining marks belong to the preceding word; punctuation and whitespace end it.
            var category = Rune.GetUnicodeCategory(character);
            if (category is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark))
            {
                insideWord = false;
            }
        }

        var outgoingLinks = model.Links.Select(link => link.Path).Distinct(StringComparer.Ordinal).Count();
        return new(sourceBytes, characters, words, EstimateTokens(sourceBytes), model.LineCount, outgoingLinks);
    }

    /// <summary>Shares the app's rounded UTF-8 byte/4 heuristic; it is an estimate, never a tokenizer or provider usage count.</summary>
    internal static int EstimateTokens(int sourceBytes) => (sourceBytes + 3) / 4;
}
