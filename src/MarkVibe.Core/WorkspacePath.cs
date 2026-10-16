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
using System.Text;

namespace MarkVibe.Core;
public enum LinkKind
{
    Document,
    Asset,
    External,
    Blocked
}

public sealed record ResolvedLink(LinkKind Kind, string Path = "", string Anchor = "", string ExternalUrl = "");
public static class WorkspacePath
{
    private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "node_modules",
        "bin",
        "obj",
        ".vs",
        "__MACOSX"
    };
    /// <summary>Recognizes the supported Markdown filename extensions without case sensitivity.</summary>
    public static bool IsMarkdown(string path) => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".mdown", StringComparison.OrdinalIgnoreCase);
    /// <summary>Checks whether any workspace path segment belongs to an excluded directory.</summary>
    public static bool IsIgnored(string path) => path.Split('/').Any(Ignored.Contains);
    /// <summary>Canonicalizes a relative workspace path while rejecting unsafe names and traversal beyond the root.</summary>
    public static string Normalize(string path, bool imported = false)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path.Length > 1024 ||
            path.StartsWith('/') ||
            path.Contains('\\') ||
            path.Contains(':') ||
            path.Any(c => char.IsControl(c)))
        {
            throw new WorkspaceException("invalid-path", "This resource has an unsafe workspace path.");
        }

        var segments = new List<string>();
        foreach (var segment in path.Normalize(NormalizationForm.FormC).Split('/'))
        {
            if (segment is "" or ".")
            {
                if (imported)
                {
                    throw new WorkspaceException("invalid-path", "The imported path is not canonical.");
                }

                continue;
            }

            if (segment == "..")
            {
                if (imported || segments.Count == 0)
                {
                    throw new WorkspaceException("outside-workspace", "This link points outside the opened workspace.");
                }

                segments.RemoveAt(segments.Count - 1);
            }
            else
            {
                segments.Add(segment);
            }
        }

        if (segments.Count is 0 or > 64)
        {
            throw new WorkspaceException("invalid-path", "This resource has an unsafe workspace path.");
        }

        return string.Join("/", segments);
    }

    /// <summary>Validates percent escapes before decoding a URL component and rejects control characters.</summary>
    public static string DecodeUrlPart(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '%' &&
                (index + 2 >= value.Length ||
                !Uri.IsHexDigit(value[index + 1]) ||
                !Uri.IsHexDigit(value[index + 2])))
            {
                throw new WorkspaceException("invalid-url", "This link has invalid URL encoding.");
            }
        }

        var decoded = Uri.UnescapeDataString(value);
        if (decoded.Any(char.IsControl))
        {
            throw new WorkspaceException("invalid-url", "This link contains invalid characters.");
        }

        return decoded;
    }

    /// <summary>Classifies and resolves a document link while enforcing workspace containment and allowed external schemes.</summary>
    public static ResolvedLink Resolve(string sourcePath, string url)
    {
        try
        {
            sourcePath = Normalize(sourcePath);
            url = url.Trim();
            if (url.Length > 2048 || url.Any(char.IsControl) || url.Contains('\\') || url.StartsWith("//"))
            {
                return new(LinkKind.Blocked);
            }

            var schemeSeparator = url.IndexOf(':');
            var firstPathDelimiter = url.IndexOfAny(['/', '#', '?']);
            if (schemeSeparator >= 0 && (firstPathDelimiter < 0 || schemeSeparator < firstPathDelimiter))
            {
                // Uri treats a mailto recipient as UserInfo; it is not HTTP credentials.
                if (!Uri.TryCreate(url, UriKind.Absolute, out var external) ||
                    external.Scheme is not ("https" or "http" or "mailto") ||
                    (external.Scheme != "mailto" &&
                    (string.IsNullOrEmpty(external.Host) ||
                    !string.IsNullOrEmpty(external.UserInfo))))
                {
                    return new(LinkKind.Blocked);
                }

                return new(LinkKind.External, ExternalUrl: external.AbsoluteUri);
            }

            var anchorStart = url.IndexOf('#');
            var anchor = anchorStart >= 0 ? DecodeUrlPart(url[(anchorStart + 1)..]) : "";
            var path = anchorStart >= 0 ? url[..anchorStart] : url;
            var query = path.IndexOf('?');
            if (query >= 0)
            {
                path = path[..query];
            }

            if (anchor.Length > 256)
            {
                return new(LinkKind.Blocked);
            }

            if (path.Length == 0)
            {
                return new(LinkKind.Document, sourcePath, anchor);
            }

            path = DecodeUrlPart(path);
            if (path.StartsWith('/'))
            {
                return new(LinkKind.Blocked);
            }

            var sourceDirectorySeparator = sourcePath.LastIndexOf('/');
            var resolvedPath = Normalize((sourceDirectorySeparator < 0 ? "" : sourcePath[..(sourceDirectorySeparator + 1)]) + path);
            if (IsIgnored(resolvedPath))
            {
                return new(LinkKind.Blocked);
            }

            return new(IsMarkdown(resolvedPath) ? LinkKind.Document : LinkKind.Asset, resolvedPath, anchor);
        }
        catch (Exception error)when (error is WorkspaceException or UriFormatException or ArgumentException)
        {
            return new(LinkKind.Blocked);
        }
    }
}
