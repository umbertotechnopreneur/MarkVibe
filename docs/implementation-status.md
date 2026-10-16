# Implementation status

Status date: **2026-10-16**. This page describes the current source, including the
document-model cleanup, map discovery and explicit AI reference collection.
Source/static review is recorded separately from execution evidence; earlier
successful runs do not validate this revision.
Application code remains MIT licensed by Umberto Giacobbi.

## Current implementation

- OpenSilver C#/XAML application controls, Modern Theme and a .NET 10 WebAssembly
  host, with specialized browser renderers for Markdown, source and the map.
- Local files, folders and ZIPs; safe relative links, images and diagrams;
  remembered workspace/document position; read-only Source and Split views.
- A metadata/link catalog with bounded incremental indexing. No workspace-wide
  full-text search UI or retained `SearchText` model exists. Map search matches
  titles and paths and animates the camera to a selected result. Source Find
  searches the active document only.
- `DocumentSession` owns original/current/exported source and revision state in
  Core. Reader, Source, map metadata, export and close guards resolve the same
  effective revision. Parsed-cache eviction no longer changes that revision.
- Up to 16 edited sessions per workspace, with separate undo/redo stacks bounded
  to 16 entries and 4,194,304 UTF-16 code units each; three parsed models per catalog.
  Refresh retains an unsaved revision when its backing file disappears.
- Optional stateless Markdown AI editing of the current document or selection,
  explicit copy saving, local undo/redo, request cancellation and context estimates.
  Workspace originals are not overwritten by applying an AI result.
- An explicit per-workspace AI collection of up to 16 canonical document paths,
  with local token estimates before sending. Reference source is bounded to 4 MiB
  per request, excludes the current editing target and is included in preflight.
  Adding documents or showing statistics does not call OpenAI.
- A black document map with selection/hover emphasis, wheel zoom, progressive
  entry, Star/Circle/Grid/Tree and folder-group arrangements, optional animated
  connection-depth planes, inline toolbar search, resize grips, confirmed reset and optional
  full-source statistics. Single click selects; double-click opens a document.
- The owner-approved document atlas adds semantic zoom with readable overview
  labels, collapsible colored islands, 0–3-link lenses, source connection excerpts,
  full-catalog shortest routes, document preview/section navigation, a minimap
  and eight named camera viewpoints. Floating controls and arrangement transitions
  retain keyboard and reduced-motion support. This follow-up has source/static
  validation only; its build, tests and running-app behavior remain unverified.
- Map schema **4** only: document card coordinates, colors, icons and optional
  paired dimensions, plus optional bounded atlas settings. Reconciliation and reset remove orphaned card entries using
  the complete catalog. Older map formats are unsupported and preserved on disk.
- Native revision-checked atomic map writes and connected-browser saves after
  explicit write permission; imports and ZIPs export a separate layout copy.
- Independent UI font selection from a shared catalog filtered to installed
  Windows families, live language changes and shared frosted dialogs. Declared
  UI traversal is shared; raw document labels remain outside translation.
- Masked OpenAI key replacement with validation before storage. Normal startup
  loads the model list when a key is configured, without sending documents.
  The isolated native smoke profile blocks AI routes and does not read saved keys.
- Local HTML/PDF export with validated embedded images and diagrams; a Windows
  WPF/WebView2 host, native pickers and direct PDF saving.
- Windows packaging scripts for MSIX/portable outputs. Local packaging defaults
  to Debug x64; Release and additional architectures require explicit selection.

Pen, ink and sticky-note editing are retired. Their codecs/schemas and provider
enumeration are removed. Existing user annotation files are neither imported nor
deleted. [Ink](ink-design.md) and [sticky-note](notes-design.md) documents remain
historical descriptions, not current capabilities.

## Historical evidence

Earlier repository records include reader/source/browser checks, native file and
PDF checks, and x64/ARM64 distribution artifacts. Their exact scope, dates and
counts belong to [validation](validation.md), the dated validation reports and
the [task archive](../.github/tasks/archive.md). Those records include features
that were subsequently retired and must not be presented as acceptance of the
current source or installed package.

The connected-handle browser checks use synthetic origin-private files and a
persisted FileSystemDirectoryHandle. They cover browser APIs rather than physical
filesystem pickers or OS permission dialogs. Narrow desktop browser viewports
are not evidence from a mobile device.

## Remaining acceptance

A Debug x64 package and launch have been requested. Their actual result will be
recorded in the [task archive](../.github/tasks/archive.md); no success is implied
here. Automated tests and live AI requests are not part of that build request.
Functional acceptance remains needed for document revision consistency, reference
context accounting, map search/grouping/persistence, theme/font/language changes,
dialog lifecycle and the simplified verification entry points.
Installation, hardware/ARM64 behavior, accessibility, native picker/revocation
behavior, production browser hosting and sustained resource use require their
own acceptance. No live OpenAI inference or credential validation was performed
for the maintainability work.

Possible future work includes workspace full-text search, offline browser shell caching,
and macOS/Linux wrappers. These are not implemented capabilities.

## Handoff boundaries

The repository is [umbertotechnopreneur/MarkVibe](https://github.com/umbertotechnopreneur/MarkVibe).
Microsoft Store submission and production browser deployment remain separate
requests. Packaging, installation and publication are distinct actions; package
creation alone is not evidence that an installed app or public release works.

Ask before each branch/worktree creation. Run tests only on an explicit request
for the current task. A local build request uses Debug x64 and launches the new
desktop output; broader packaging requires explicit selection. Do not install
SDKs, workloads, apps or certificates implicitly. See [distribution](windows-packaging.md).
