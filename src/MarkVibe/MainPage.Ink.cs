using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe;

public partial class MainPage
{
    private string _inkWorkspaceId = "";
    private bool _inkCanWrite;
    private readonly Dictionary<string, InkSession> _inkDrafts = new(StringComparer.Ordinal);

    private async Task LoadInkAsync(MarkdownModel model, Context context, CancellationToken cancellation)
    {
        _inkWorkspaceId = context.Provider.Info.Id; _inkCanWrite = context.Provider.Info.Capabilities.CanWriteSidecars;
        _inkRevision = _inkOriginal = _inkMessage = ""; _inkInvalid = false; _inkBusy = false;
        _ink = new(model);
        var key = _inkWorkspaceId + "\n" + model.Path;
        if (!_inkCanWrite && _inkDrafts.TryGetValue(key, out var draft) && draft.Model.Fingerprint == model.Fingerprint) { _ink = draft; return; }
        try
        {
            var stored = await BrowserServices.CallAsync<SidecarState>("ink.read", new { workspaceId = _inkWorkspaceId, path = model.Path }, cancellation);
            _inkRevision = stored.Revision; _inkOriginal = stored.Json ?? "";
            if (stored.Json != null) _ink = new(model, InkCodec.Read(stored.Json));
            if (_ink.ChangedDocument) _inkMessage = "Document changed · review ink before editing";
            if (_ink.Unresolved > 0) _inkMessage = $"{_ink.Unresolved} unresolved annotations · original sidecar preserved";
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error)
        {
            _inkInvalid = true;
            _inkMessage = error is WorkspaceException ? error.Message : "The annotation file could not be read. It has not been changed.";
        }
    }
    private Task RenderInkAsync()
    {
        var session = _ink;
        return BrowserServices.CallAsync("ink.render", new
        {
            hostId = ReaderHost.HostId, path = session?.Model.Path, workspaceId = _inkWorkspaceId,
            resolved = session?.Resolved, dirty = session?.Dirty == true, busy = _inkBusy,
            readOnly = _inkInvalid || session?.ChangedDocument == true, invalid = _inkInvalid,
            changed = session?.ChangedDocument == true, unresolved = session?.Unresolved ?? 0,
            canUndo = session?.CanUndo == true, canRedo = session?.CanRedo == true,
            hasInk = _inkOriginal.Length > 0 || session?.Value.Annotations.Count > 0, message = _inkMessage
        });
    }
    private async Task HandleInkAsync(JsonElement request)
    {
        await _inkGate.WaitAsync();
        try
        {
            var session = _ink;
            if (session == null || request.GetProperty("path").GetString() != session.Model.Path ||
                request.GetProperty("workspaceId").GetString() != _inkWorkspaceId) return;
            var action = request.GetProperty("action").GetString();
            _inkBusy = true; _inkMessage = "";
            try
            {
                if (_inkInvalid && action != "download") throw new WorkspaceException("invalid-ink", "The original annotation file is preserved. Export it before replacing or repairing it.");
                switch (action)
                {
                    case "stroke": session.Add(request.GetProperty("blockId").GetString() ?? "", Wire.Decode<InkStroke>(request.GetProperty("stroke").GetRawText())); break;
                    case "erase": session.Erase(request.GetProperty("annotationId").GetString() ?? "", request.GetProperty("strokeIndex").GetInt32()); break;
                    case "undo": session.Undo(); break;
                    case "redo": session.Redo(); break;
                    case "rebase": session.Rebase(); break;
                    case "save": await SaveInkAsync(); break;
                    case "discard":
                        if (_active != null && _document != null)
                        {
                            _inkDrafts.Remove(_inkWorkspaceId + "\n" + session.Model.Path);
                            await LoadInkAsync(_document, _active, CancellationToken.None);
                            session = _ink!;
                        }
                        break;
                    case "download":
                        await BrowserServices.CallAsync("ink.download", new { json = _inkInvalid ? _inkOriginal : InkCodec.Write(session.Value), filename = session.Model.Path.Split('/').Last() + ".ink.json" });
                        _inkMessage = "Annotation backup exported · existing sidecar unchanged";
                        break;
                    default: throw new WorkspaceException("unknown-action", "This ink action is unavailable.");
                }
                if (_inkCanWrite && session.Dirty && action != "download") await SaveInkAsync();
                if (!_inkCanWrite)
                {
                    var key = _inkWorkspaceId + "\n" + session.Model.Path;
                    if (_inkDrafts.Count >= 5 && !_inkDrafts.ContainsKey(key)) _inkDrafts.Remove(_inkDrafts.Keys.First());
                    _inkDrafts[key] = session;
                }
            }
            catch (Exception error) { _inkMessage = error is WorkspaceException ? error.Message : "Ink could not be saved. Your edits remain available; export a backup before reloading."; }
            finally { _inkBusy = false; await RenderInkAsync(); StatusText.Text = _inkMessage.Length > 0 ? _inkMessage : session.Dirty ? "Unsaved ink · Save ink to keep a copy" : "Ink saved separately · Markdown unchanged"; }
        }
        finally { _inkGate.Release(); }
    }
    private async Task SaveInkAsync()
    {
        if (_ink == null || !_ink.Dirty) return;
        if (_inkInvalid || _ink.ChangedDocument) throw new WorkspaceException("ink-conflict", "Review or export the annotations before saving.");
        var json = InkCodec.Write(_ink.Value);
        if (_inkCanWrite)
            _inkRevision = await BrowserServices.CallAsync<string>("ink.write", new { workspaceId = _inkWorkspaceId, path = _ink.Model.Path, documentHash = _ink.Model.Fingerprint, revision = _inkRevision, json });
        else
        {
            await BrowserServices.CallAsync("ink.download", new { json, filename = _ink.Model.Path.Split('/').Last() + ".ink.json" });
            _inkMessage = "Ink copy downloaded · keep it beside the Markdown";
        }
        _inkOriginal = json; _ink.Saved();
    }
    private async Task FlushInkAsync()
    {
        await BrowserServices.CallAsync("ink.flush");
        await _inkGate.WaitAsync();
        try
        {
            if (_ink?.Dirty != true) return;
            if (!_inkCanWrite) throw new WorkspaceException("ink-unsaved", "Save ink before changing documents to keep a sidecar copy.");
            await SaveInkAsync();
        }
        finally { _inkGate.Release(); }
    }
}
