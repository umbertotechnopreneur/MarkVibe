/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; current-document event bridge; 2026-10-23
 * Human guidance: Umberto Giacobbi; prompt before reloading externally changed files
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using MarkVibe.Core;

namespace MarkVibe.Windows;

internal sealed partial class DesktopWindow
{
    /// <summary>Posts file metadata only to the initialized trusted application page on the owning UI thread.</summary>
    private void NotifyDocumentChange(object notification)
    {
        if (!_closing && _initialized && _view.CoreWebView2 != null && MainPage(_view.CoreWebView2.Source))
        {
            _view.CoreWebView2.PostWebMessageAsJson(Wire.Encode(notification));
        }
    }
}
