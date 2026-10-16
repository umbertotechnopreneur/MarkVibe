/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Modified with AI: OpenAI Codex; bounded local startup diagnostics; 2026-10-11
 * Human guidance: Umberto Giacobbi; diagnose startup and provide persistent logs
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarkVibe.Windows;

/// <summary>Writes bounded local diagnostics without document text, arguments, paths or credentials.</summary>
internal static class DesktopLog
{
    private const long MaximumFileBytes = 1024 * 1024;
    private const int MaximumFiles = 8;
    private static readonly object Gate = new();
    private static readonly string Session = $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Environment.ProcessId}";
    private static string? _directory;
    private static string? _file;
    private static int _part;

    /// <summary>Creates the log directory below the chosen application data directory.</summary>
    internal static void Initialize(string dataDirectory)
    {
        lock (Gate)
        {
            _directory = Path.Combine(dataDirectory, "logs");
        }
        Write("info", "application.start", new
        {
            version = typeof(App).Assembly.GetName().Version?.ToString(),
            runtime = Environment.Version.ToString(),
            architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
        });
    }

    /// <summary>Appends one JSON record and rotates only this logger's files; disk failures never stop the app.</summary>
    internal static void Write(string level, string eventName, object? details = null)
    {
        lock (Gate)
        {
            if (_directory == null)
            {
                return;
            }
            try
            {
                Directory.CreateDirectory(_directory);
                var record = JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, level, eventName, details });
                if (_file == null || (File.Exists(_file) && new FileInfo(_file).Length + Encoding.UTF8.GetByteCount(record) > MaximumFileBytes))
                {
                    _file = Path.Combine(_directory, $"markvibe-{Session}-{_part++}.jsonl");
                    File.WriteAllText(_file, "");
                    var ownedFiles = Directory.EnumerateFiles(_directory, "markvibe-*.jsonl")
                        .Where(path => Regex.IsMatch(Path.GetFileName(path), @"^markvibe-\d{8}T\d{9}Z-\d+-\d+\.jsonl$"))
                        .Select(path => new FileInfo(path))
                        .OrderByDescending(file => file.LastWriteTimeUtc)
                        .ToArray();
                    foreach (var oldFile in ownedFiles.Skip(MaximumFiles))
                    {
                        if (!string.Equals(oldFile.FullName, _file, StringComparison.OrdinalIgnoreCase))
                        {
                            oldFile.Delete();
                        }
                    }
                }
                File.AppendAllText(_file, record + "\n", Encoding.UTF8);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                Debug.WriteLine("MarkVibe local diagnostics could not be written.");
            }
        }
    }

    /// <summary>Records exception types and method names, deliberately excluding messages and source file paths.</summary>
    internal static void Error(string eventName, Exception error)
    {
        var failures = new List<object>();
        for (Exception? current = error; current != null && failures.Count < 5; current = current.InnerException)
        {
            var methods = new StackTrace(current, false).GetFrames()
                .Take(16)
                .Select(frame => frame.GetMethod())
                .Select(method => method?.DeclaringType?.FullName + "." + method?.Name)
                .ToArray();
            failures.Add(new { type = current.GetType().FullName, hresult = current.HResult, methods });
        }
        Write("error", eventName, failures);
    }

    /// <summary>Extracts diagnostic identifiers from runtime errors instead of persisting arbitrary console content.</summary>
    internal static void WebError(string eventName, string text)
    {
        // Console payloads can contain documents or API failures. Only known error
        // identifiers and a missing XAML resource name cross the logging boundary.
        var boundedText = text[..Math.Min(text.Length, 32768)];
        var types = Regex.Matches(boundedText, @"\b(?:[A-Za-z_][A-Za-z0-9_]*\.)*[A-Za-z_][A-Za-z0-9_]*(?:Exception|Error)\b")
            .Select(match => match.Value)
            .Distinct()
            .Take(8)
            .ToArray();
        var missingResource = Regex.Match(boundedText, @"Cannot find resource named '([A-Za-z_][A-Za-z0-9_.-]{0,79})'\.");
        Write("error", eventName, new
        {
            types,
            resource = missingResource.Success ? missingResource.Groups[1].Value : null
        });
    }
}
