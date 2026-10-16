/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Modified with AI: OpenAI Codex; WebView runtime and navigation diagnostics; 2026-10-11
 * Human guidance: Umberto Giacobbi; diagnose startup and provide persistent logs
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace MarkVibe.Windows;

/// <summary>Captures runtime failures through the host without exposing a remote debugging port.</summary>
internal sealed class WebViewDiagnostics
{
    private readonly List<CoreWebView2DevToolsProtocolEventReceiver> _receivers = new();

    /// <summary>Registers before navigation so failures during Blazor and OpenSilver startup are included.</summary>
    internal async Task AttachAsync(CoreWebView2 core)
    {
        core.NavigationCompleted += (_, args) => DesktopLog.Write(
            args.IsSuccess || args.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled ? "info" : "error", "webview.navigation",
            new { success = args.IsSuccess, status = args.WebErrorStatus.ToString(), httpStatus = args.HttpStatusCode });
        core.ProcessFailed += (_, args) => DesktopLog.Write("error", "webview.processFailed",
            new { kind = args.ProcessFailedKind.ToString(), reason = args.Reason.ToString(), exitCode = args.ExitCode });
        try
        {
            var consoleReceiver = core.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled");
            consoleReceiver.DevToolsProtocolEventReceived += ConsoleReceived;
            _receivers.Add(consoleReceiver);
            var exceptionReceiver = core.GetDevToolsProtocolEventReceiver("Runtime.exceptionThrown");
            exceptionReceiver.DevToolsProtocolEventReceived += ExceptionReceived;
            _receivers.Add(exceptionReceiver);
            await core.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");
            DesktopLog.Write("info", "webview.diagnosticsReady", new { runtime = core.Environment.BrowserVersionString });
        }
        catch (Exception error)
        {
            // A diagnostic capability failure must not prevent reading documents.
            DesktopLog.Error("webview.diagnosticsUnavailable", error);
        }
    }

    /// <summary>Passes only console errors through the structured identifier filter.</summary>
    private static void ConsoleReceived(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs args)
    {
        using var message = JsonDocument.Parse(args.ParameterObjectAsJson);
        var root = message.RootElement;
        if (root.GetProperty("type").GetString() != "error")
        {
            return;
        }
        var text = string.Join("\n", root.GetProperty("args").EnumerateArray()
            .Where(argument => argument.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String)
            .Select(argument => argument.GetProperty("value").GetString()));
        DesktopLog.WebError("webview.consoleError", text);
    }

    /// <summary>Captures unhandled JavaScript failures without dumping remote objects or event arguments.</summary>
    private static void ExceptionReceived(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs args)
    {
        using var message = JsonDocument.Parse(args.ParameterObjectAsJson);
        var details = message.RootElement.GetProperty("exceptionDetails");
        var description = "JavaScriptError";
        if (details.TryGetProperty("exception", out var exception) &&
            exception.TryGetProperty("description", out var value) && value.ValueKind == JsonValueKind.String)
        {
            description = value.GetString() ?? description;
        }
        DesktopLog.WebError("webview.unhandledException", description);
    }
}
