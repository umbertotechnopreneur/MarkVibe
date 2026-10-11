# Task Archive

Record completed behavior, validation scope and remaining limitations. Keep command
transcripts and temporary artifacts in dated validation reports, not this index.

## 2026-10-11 — Browser foundation and contributor guide

- OpenSilver Modern Theme, system/light/dark settings and document-position memory.
- Local Markdown workspaces, sanitized reader, Monaco source, document map and
  standalone HTML/browser-print PDF export.
- Contributor guide with pinned commands, architecture and authorization boundaries.
- Validation: bundle, locked restore/build, 7 core checks, 9 client checks and 17
  browser checks at root and subpath passed. Prepared PDF pages were inspected.
  Native Windows hosting and physical pen input were not covered by those checks.

## 2026-10-11 — Windows host, direct PDF and paragraph ink

- Reuse the OpenSilver app in WPF/WebView2 without a development server; add native
  contained files, remembered document and direct HTML/PDF saves.
- Add logical ink bands with pressure samples, highlighter, eraser, undo/redo,
  sidecar autosave, changed-document review, conflict preservation and recovery.
- Queue consecutive strokes during autosave; await them before navigation/export.
  Clear stale native resumption metadata when selecting a browser workspace.
- Adapt contributor/task/lesson practices from the three reference repositories.
- Validation: locked restore/build with zero warnings/errors; core 7/7, client 9/9,
  native service 10/10, Windows 11/11 and browser 19/19 at root and subpath. Six native
  PDF pages were visually inspected. Physical pen and OS picker interaction remain open.

## 2026-10-11 — Icons, About, licensing and Windows distribution

- Add shared colored local SVG symbols across the shell, source/map and ink buttons;
  retain labels and use a compact narrow toolbar.
- Adapt the reference About structure with author/version, first-person product
  story, MIT, repository and VibeWare manifesto links.
- Approve MIT for Umberto Giacobbi; add a first-person README and original SVG banner.
- Produce Release 0.1.0.2 x64/ARM64 self-contained portable ZIPs, development-signed
  MSIX files and architecture-selecting bundle; clean previous artifacts and private staging.
- Add manual GitHub artifact workflow and preserve current test/branch/worktree rules.
- Evidence: bundle, locked restore, both RID publishes, MakeAppx and signing succeeded.
  No tests, app launch or installation were performed for this request.
