/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared reader presentation operations; 2026-10-25
 * Human guidance: Umberto Giacobbi; menus and toolbars invoke services
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Text.Json;
using MarkVibe.Core;

namespace MarkVibe.Services;

/// <summary>Owns reader preference transformations and persistence independently of XAML.</summary>
public sealed class ReaderPresentationService
{
    private int _applyRevision;

    /// <summary>Exposes the application-owned reader model to presentation bindings.</summary>
    public ReaderPresentationState State { get; }

    /// <summary>Uses the shared reader model rather than retaining state inside a control.</summary>
    public ReaderPresentationService(ReaderPresentationState state) => State = state;

    /// <summary>Validates global preferences before publishing the new application snapshot.</summary>
    public void SetPreferences(Preferences preferences) => State.Preferences = preferences.Validated();

    /// <summary>Changes the source renderer preference without altering unrelated settings.</summary>
    public void SetSimpleSource(bool simple) => SetPreferences(State.Preferences with { SimpleSource = simple });

    /// <summary>Applies model preferences and rejects stale asynchronous platform responses.</summary>
    public async Task<JsonElement?> ApplyAsync(string readerId, string sourceId)
    {
        var revision = ++_applyRevision;
        var result = await BrowserServices.CallAsync<JsonElement>("preferences.apply", new
        {
            preferences = State.Preferences,
            readerPreferences = ResolvePreferences(),
            readerId,
            sourceId
        });
        if (revision != _applyRevision)
        {
            return null;
        }
        State.PreferencesPersisted = result.GetProperty("persisted").GetBoolean();
        return result;
    }

    /// <summary>Restores validated geometry before enabling subsequent persistence.</summary>
    public void RestoreLayout(ShellLayout layout)
    {
        State.Layout = layout.Validated();
        State.LayoutReady = true;
    }

    /// <summary>Changes only width, preserving every other saved pane preference.</summary>
    public void SetWidth(bool expanded) => State.Layout = State.Layout with { ReaderExpanded = expanded };

    /// <summary>Retains the requested assistant visibility across responsive layout changes.</summary>
    public void SetAiOpen(bool open) => State.Layout = State.Layout with { AiOpen = open };

    /// <summary>Retains the requested sidebar visibility independently of narrow-window constraints.</summary>
    public void SetSidebarOpen(bool open) => State.Layout = State.Layout with { SidebarOpen = open };

    /// <summary>Validates a user-resized sidebar width.</summary>
    public void SetSidebarWidth(double width) => State.Layout = (State.Layout with { SidebarWidth = width }).Validated();

    /// <summary>Validates a user-resized assistant width.</summary>
    public void SetAiWidth(double width) => State.Layout = (State.Layout with { AiWidth = width }).Validated();

    /// <summary>Validates the relative source width reported by the splitter.</summary>
    public void SetSourceRatio(double ratio) => State.Layout = (State.Layout with { SourceRatio = ratio }).Validated();

    /// <summary>Validates the width reported by the graph inspector.</summary>
    public void SetMapInspectorWidth(double width) => State.Layout = (State.Layout with { MapInspectorWidth = width }).Validated();

    /// <summary>Projects folder overrides over the global document preferences.</summary>
    public Preferences ResolvePreferences() => State.Appearance?.ApplyTo(State.Preferences) ?? State.Preferences;

    /// <summary>Applies and saves geometry without rerendering Markdown or diagrams.</summary>
    public Task<bool> SaveLayoutAsync() =>
        BrowserServices.CallAsync<bool>("layout.save", State.Layout);

    /// <summary>Restores an optional folder override over the user's global reading defaults.</summary>
    public async Task ReadAppearanceAsync(string workspaceId, CancellationToken cancellationToken)
    {
        State.WorkspaceId = workspaceId;
        State.Appearance = null;
        var revision = ++State.Revision;
        var result = await BrowserServices.CallAsync<JsonElement>(
            "reader.appearance.read", new { workspaceId }, cancellationToken);
        if (revision == State.Revision)
        {
            State.Appearance = result.ValueKind == JsonValueKind.Object
                ? Wire.Decode<ReaderAppearance>(result.GetRawText())
                : null;
        }
    }

    /// <summary>Validates a reading preference and persists its folder-specific snapshot.</summary>
    public async Task<ReaderAppearanceResult> ChangeAppearanceAsync(
        string workspaceId, string key, string value)
    {
        if (State.WorkspaceId != workspaceId)
        {
            throw new WorkspaceException("stale-workspace", "The active workspace changed. Reopen the menu and try again.");
        }
        State.Revision++;
        var current = ResolvePreferences();
        var preferences = key switch
        {
            "lightDocumentTheme" => current with { LightDocumentTheme = value },
            "darkDocumentTheme" => current with { DarkDocumentTheme = value },
            "lightCodeTheme" => current with { LightCodeTheme = value },
            "darkCodeTheme" => current with { DarkCodeTheme = value },
            "underlineLinks" => current with { UnderlineLinks = value == "true" },
            "documentFont" => current with { DocumentFont = value },
            "reset" => current,
            _ => throw new ArgumentException("Unknown reader preference.", nameof(key))
        };
        var appearance = key == "reset" ? null : ReaderAppearance.From(preferences.Validated());
        // Publish before persistence so a second change composes with this one, even while storage is pending.
        State.Appearance = appearance;
        var persisted = await BrowserServices.CallAsync<bool>("reader.appearance.save",
            new { workspaceId, appearance });
        return new(appearance, persisted);
    }
}
