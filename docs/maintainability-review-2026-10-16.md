<!-- VBWR B
Project: MarkVibe
Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
Created with AI: OpenAI Codex; source review with three parallel specialist analyses; 2026-10-16
Human guidance: Umberto Giacobbi; prioritize maintainability, remove unnecessary compatibility and simplify checks
Copyright (c) 2026 Umberto Giacobbi
License: MIT (see LICENSE)
VBWR E -->

# MarkVibe maintainability review

## Focused follow-up for repository preparation

A later three-agent pass reviewed exploration state, graph effects and Windows
packaging. It retains unchanged selection/trail/comparison controls, validates
AI batches against one current catalog snapshot, cancels effects before replacing
their SVG, cancels selection geometry after camera/viewport changes and avoids
repeated DOM badge updates. An unused argument, single-call wrapper and unreachable
edge branch were removed. The branding update extracts shared publication links
and a small lifecycle-owned footer instead of adding more coordination to MainPage.

The Windows packaging pass adds native MSI authoring with stable upgrade identities
and explicit three-field version mapping. Existing Windows Installer COM and
makecab supply the build tools. These changes received source/syntax review;
package and solution compilation results belong to the task archive. No automated
suite or interactive acceptance was requested for this pass.

## Earlier architectural review

The highest value changes are to give document drafts and derived metadata one
owner, make failed map saves visible, and repair the verification workflow. Several
duplications already cause inconsistent behavior. The existing Core, browser adapter
and native host boundaries provide a useful foundation for targeted refactoring.

This review covers the current working tree on 16 October 2026, including pending
map and settings changes. Three parallel reviews covered Core/Native, UI/interop,
and configuration/testing; findings below were reconciled against source. No build,
test, bundle, application launch or API request was performed. Runtime scenarios
are acceptance checks to implement and run later, not claimed reproductions.
The initial review changed documentation only. The owner subsequently authorized
the implementation below, emphasizing one document model and no legacy-format
compatibility. The findings after this section preserve the source observations
that motivated that work; references to removed methods describe the review baseline.

## Implementation following the review

| Area | Implemented result |
| --- | --- |
| Document revisions | `DocumentSession` owns original, current and exported source plus bounded undo/redo. The catalog derives reader, map and statistics from that same revision, independently of its three-model cache. |
| Asynchronous state | Revision fingerprints reject obsolete commits. Workspace transitions, refresh and exported-copy completion retain their original context across awaits. Reader theme work restores position only for the same renderer revision and document. |
| Map persistence | A visible inline notice reports failed saves. The sole supported layout contract is schema 4 with document nodes; retired note models and schemas are removed. Reconciliation prunes absent paths from the complete catalog, and reset rebuilds current nodes. Existing unsupported files are preserved. |
| UI ownership | Shared declared-child traversal handles OpenSilver UserControl content. Raw document labels survive language changes. Icon hover handlers detach correctly; UI-font and language catalogs are shared. |
| Unused paths | Removed disconnected global search, retained search-source strings, unversioned preference fallback, annotation enumeration and empty or duplicated presentation/download helpers. |
| Verification setup | A double-click menu and a shared PowerShell runner provide quick, Windows UI and complete profiles, with HTML/JSON results. UI fixtures are updated for current map behavior and Playwright is a locked development dependency. Native smoke mode blocks OpenAI credential/API operations. |
| Build inputs | One preparation script restores locked npm dependencies before bundling. A source/lock fingerprint and output inventory let ordinary .NET builds reject missing or stale client assets. |
| Documentation | Current architecture, settings, security and implementation status distinguish source behavior from historical runtime evidence. |

These changes received source review and syntax/format validation. Following the
owner's request to see the completed functions, the current source was bundled
and packaged through **0.1.0.8 Debug x64**, then launched directly. Existing NuGet
NU1902/NU1903 dependency warnings remain. The new runner and regression cases have
not been executed; compilation and a live window do not establish interaction or
AI-inference acceptance. No application/certificate installation or publication
was performed. See the task archive for the exact delivery scope.

The follow-up also adds title/path map search with animated centering, a folder
arrangement, and an explicit AI reference collection with estimated totals before
sending. The editing target remains separate from its read-only references.

Remaining architectural opportunities are extracting the large page/map
coordinators, replacing anonymous interop payloads with named contracts, and
aligning the independent asset/path validation policies. They are deliberately
separate from the completed state-ownership and duplication fixes.

## Defects and configuration gaps

### High priority Map save errors can remain hidden

When a layout write conflicts with an external edit or loses filesystem access,
[HandleMapAsync](E:/Research/MarkVibe/src/MarkVibe/MainPage.Map.cs:276) catches the
exception and records `state.Message`, then writes `StatusText.Text`. The containing
[StatusNotice](E:/Research/MarkVibe/src/MarkVibe/MainPage.xaml:121) starts collapsed;
that path does not show it. The managed map controls ignore the message, and
[MapHost.updateStatus](E:/Research/MarkVibe/client/map.js:294) is empty.

The dirty state and close protection remain, but the immediate failure and recovery
instructions can be invisible. Route failures through one visible notice presenter,
retain the dirty edit and offer retry, copy download and explicit discard. Routine
success updates can remain unobtrusive. Acceptance: externally change a synthetic
sidecar, move a card, and verify that the conflict is visible and neither version is
silently discarded.

### High priority UI checks target a retired interface

The Windows harness waits for
[`.ink-toolbar`](E:/Research/MarkVibe/tests/windows/smoke.mjs:133) before running
its scenarios, although the current application no longer creates it. The browser
suite still expects a DOM `.map-summary`, an `Open ...` card label and navigation
on [single click](E:/Research/MarkVibe/tests/browser/smoke.mjs:246). The map now
uses a managed summary, selection on click and navigation on double click.
Both suites also contain retired annotation scenarios.

These checks cannot serve as acceptance criteria for the current product. Remove
retired cases and update interactions before adding broad new coverage. Preserve
useful document, navigation, sanitation and export cases. This is a source mismatch;
the suites were not executed during the review.

### Medium priority Returning to a draft restores old map metadata

After editing document A, opening B, C and D can evict A from the three-document
cache. Returning to A makes
[DocumentCatalog.OpenAsync](E:/Research/MarkVibe/src/MarkVibe.Core/DocumentCatalog.cs:154)
read the original file and update its title, introduction, links and statistics.
[Navigation](E:/Research/MarkVibe/src/MarkVibe/MainPage.xaml.cs:390) then substitutes
the retained draft for display, but
[MarkdownDraftForNavigation](E:/Research/MarkVibe/src/MarkVibe/MainPage.Ai.cs:671)
does not update the catalog or cache.

The reader can show the edited document while the map shows original metadata.
Resolve the effective document, including any draft, before updating all derived
views. Cache eviction must not change document meaning. Acceptance: edit A's title
and links, navigate through three other documents, return, and compare reader,
outline and map against the same revision.

### Lower priority Global search is disconnected and internally inconsistent

[IndexAsync](E:/Research/MarkVibe/src/MarkVibe.Core/DocumentCatalog.cs:106) indexes
the full source within the aggregate budget. In contrast,
[UpdateDraft](E:/Research/MarkVibe/src/MarkVibe.Core/DocumentCatalog.cs:180) keeps only
12,000 characters. A direct Core search loses matches beyond that point after an
unrelated edit; undo/restore also use that update path. However, `Search` currently
has callers only in Core checks, not in the application. Both application map
calls also pass an empty query. This is a latent inconsistency and a disconnected
feature, not a currently exposed global-search failure.

Decide whether global search belongs in the next release. If it does, connect its
UI and centralize catalog projection and index budgeting; verify long-document
search through edit and undo. Otherwise remove `SearchText`, `SearchResult`, the
unused search path and unused query plumbing rather than retaining full source
strings for an unavailable feature. Keep the metadata/graph index independently.
Pages excluded by the documented aggregate index budget are an intentional limit.

### Medium priority Font updates stop at UserControl boundaries

[AppTypography.Walk](E:/Research/MarkVibe/src/MarkVibe/Controls/AppTypography.cs:34)
visits `ContentControl.Content` but not `UserControl.Content`. OpenSilver 3.3.3's
UserControl derives from Control rather than ContentControl. Inherited font changes
can reach some content, but locally assigned values such as
[DocumentTitle](E:/Research/MarkVibe/src/MarkVibe/MainPage.xaml:42) and
[SourcePath](E:/Research/MarkVibe/src/MarkVibe/Controls/MonacoEditorView.xaml:22)
remain fixed to Consolas.

Prefer a shared application font resource or explicit binding. If traversal remains,
include declared UserControl content while protecting template glyph fonts and
document renderers. Acceptance: change to another installed font and inspect shell,
sidebar, source toolbar and newly opened dialogs; document/editor typography must
remain independent. The inheritance contract was inspected; visual behavior was
not exercised.

### Medium priority Localization can translate document owned labels

[AppLocalization](E:/Research/MarkVibe/src/MarkVibe/Controls/AppLocalization.cs:112)
infers translation keys from any recognized TextBlock text, excluding only specific
DataContext types and names.
[CreateTreeNode](E:/Research/MarkVibe/src/MarkVibe/MainPage.Navigation.cs:194) creates
folder and outline captions without that exemption; incoming map link titles have
the same issue. A folder named `Source` or heading named `Settings` can therefore
change its displayed name when switching language.

Use explicit translation keys and a durable raw/user-content marker. `SetRaw`
currently clears a remembered key without preventing later inference. Acceptance:
switch languages with folder names and document headings deliberately matching UI
labels, and verify that only application labels change.

### Medium priority Theme work can restore an old document position

[applyPreferences](E:/Research/MarkVibe/client/main.js:239) captures the reader's
position, awaits native appearance/font work and then restores it. Its guard checks
only preference revision.
[MarkdownHost.retheme](E:/Research/MarkVibe/client/markdown.js:367) independently
captures and restores position around asynchronous diagram rendering, again without
checking the original document at the final restore.

Navigation while this work is pending can apply document A's position to document
B. Capture document/workspace identity, renderer revision and article before the
first await; require all of them to match before restoring. Have one owner for
position preservation. This asynchronous scenario is inferred from the control
flow and needs a deliberately delayed rendering fixture for runtime confirmation.

### Medium priority Build inputs can diverge from source and lockfiles

[package-windows.ps1](E:/Research/MarkVibe/scripts/package-windows.ps1:148) runs
`npm ci` only if `node_modules/esbuild` is absent. An updated lockfile can therefore
be packaged using old installed dependencies. Separately, the
[Browser project](E:/Research/MarkVibe/src/MarkVibe.Browser/MarkVibe.Browser.csproj:35)
checks only whether the client bundle exists, so an ordinary .NET build can reuse
JavaScript from before a client source change. Packaging does rebundle, but still
has the dependency freshness issue.

Use one preparation path that verifies installed dependencies against the lock and
bundles current source. Record a source/lock fingerprint in the bundle manifest or
declare proper incremental inputs/outputs. A stale bundle should be rebuilt or
produce a useful failure. Replace the project's `npm install` suggestion with the
repository's deterministic installation procedure.

### Medium priority Test setup depends on machine specific state

Both UI harnesses load Playwright via `MARKVIBE_PLAYWRIGHT_PATH` or an undeclared
`playwright` dependency. It is absent from package.json and package-lock.json.
This was a documented local setup, but a fresh clone plus `npm ci` cannot reproduce
it. The [Windows harness](E:/Research/MarkVibe/tests/windows/smoke.mjs:32) also
hard-codes an executable outside the current `win-x64` output directory, risking a
missing executable or testing an older build.

Declare and lock the test dependency, detect existing Edge/WebView2 prerequisites,
and pass the actual produced executable path from the runner. Keep missing
prerequisites distinct from failed product checks. No dependency installation or
browser download should happen silently.

### Lower priority Reset retains orphaned map cards

[EnsureDocuments](E:/Research/MarkVibe/src/MarkVibe.Core/MapLayout.cs:280) retains
saved paths that no longer exist and adds current paths. After enough renames or
reorganizations, those entries can exhaust the 4,096-node persistence limit even
with a small current workspace.
[Reset](E:/Research/MarkVibe/src/MarkVibe.Core/MapLayout.cs:354) retains them too.

After a complete enumeration, distinguish current and orphaned entries. Reset can
rebuild from the current catalog; any temporary retention of absent cards should
have an explicit bounded policy. Do not prune from a filtered or partial projection.

## Maintainability changes

| Area | Current source of complexity | Focused change |
| --- | --- | --- |
| Document state | MainPage drafts, catalog cards and LRU cache independently hold document versions | A DocumentSession owning effective source and draft history; one catalog projection method |
| UI state | MainPage has roughly 2,390 lines across partial files with shared mutable fields | Extract workspace/document and map coordination into small classes; keep XAML event wiring in the page |
| Map rendering | The 1,203-line map adapter owns camera, gestures, card rendering, edges and optimistic edits | Separate camera, interaction and rendering helpers with explicit lifecycle ownership |
| Interop | String operations, anonymous payloads and repeated JSON property access | Named request/state records and domain dispatchers; explicit failure when a required view is unavailable |
| Preferences | Defaults in Contracts.cs, settings.js and main.js; font and language catalogs repeated | One shared catalog/envelope, following the existing shared reader-theme catalog pattern |
| UI decoration | Separate tree walkers for localization, icons and typography | Explicit resource bindings and translation keys; share declared-child enumeration only where necessary |

Splitting a class into more partial files alone does not resolve ownership. Avoid
introducing a general event bus, plugin framework or new UI framework for this work.

### Remove pre release compatibility deliberately

New maps still start at schema 1 and upgrade to schema 3 when resized. The codec
supports three schema variants and retains legacy notes through `StickyNotes.cs`,
although annotation editing was retired. Standardize on one current map contract
and remove unsupported legacy branches, the unused notes model and retired schemas
and fixtures. Stop enumerating ink/notes files as supported application resources.
Existing files on disk can simply remain untouched; removing compatibility does
not require deleting user files or implementing a migration system.

Also remove support for unversioned preference objects if it has no current use.
Keep malformed/unsupported-file handling and explicit reset/recovery behavior.

### Remove duplication that has no independent responsibility

- `DocumentMapView.DisplayAsync` and `DisplaySelectionAsync` repeat sidebar,
  summary and toolbar preparation. Share preparation and keep their distinct
  renderer commands, preserving selection DOM identity and the camera.
- `downloadSidecar` and `downloadMarkdown` repeat Blob/link lifecycle code.
  A helper can take MIME type and filename without changing completion semantics.
- Remove empty `MapHost.updateStatus`, unused `rememberTimer`, retired sidecar
  state declarations and comments that describe removed annotation behavior.
- Make `IconLabel.OnLoaded` event attachment idempotent or detach named handlers;
  it currently adds new anonymous mouse handlers on every load.
- Preserve operation/session identifiers in diagnostics. `emit` currently swallows
  every rejected managed invocation. Record safe error categories and operation
  names without document contents, prompts or credentials.

### Keep independent validation boundaries

Native filesystem checks and browser sanitation have independent responsibilities;
they should not disappear under a general duplication cleanup. Share declarative
limits and supported extensions, then verify equivalent acceptance at each boundary.
For example, Core rejects all `char.IsControl` characters while JavaScript path
validation rejects C0 controls and DEL only; U+0085 can pass one layer and fail the
next. JS also classifies PDF/TXT as assets while the native provider lists images.
Make these policy differences explicit or align them.

Preserve the centralized Markdown AST, bounded resources, atomic native writes,
sidecar revision checks, lazy Monaco/Mermaid loading and separate document/UI themes.

## Simple checks for everyday use

The implemented entry point is a double-clickable
[Verifica-MarkVibe.cmd](E:/Research/MarkVibe/Verifica-MarkVibe.cmd), backed by one
[scripts/check.ps1](E:/Research/MarkVibe/scripts/check.ps1) runner. The same runner
serves local use and manual CI without changing package versions or creating an
installer. The requirements below explain its design; current commands and
prerequisites are maintained in `docs/validation.md`.

| Menu choice | Behavior | Result shown to the user |
| --- | --- | --- |
| Rapida | Check prerequisites, prepare current inputs, run focused Core/Native and client checks | Documents, catalog consistency, file safety and saved-state results |
| Interfaccia Windows | Build Debug x64 through the common preparation path and exercise synthetic document/map/settings flows | A short result list with screenshots for failures |
| Completa | Combine the quick and Windows profiles with browser coverage | One consolidated report, with each stage clearly marked |

Provide a single configuration with sensible defaults, automatic free-port
selection, repository-relative paths and an isolated test profile. Discover the
already installed SDK, supported Node, Edge and WebView2. Explain missing
prerequisites in normal language and stop dependent stages. Use four explicit
states: passed, failed and not run; explain checks outside the selected profile.
Never turn a skipped check into
a success. Keep HTML results, JSON details, screenshots and logs together under
ignored repository artifacts.

The Core and Native executable scenarios remain; a new
test framework is not required for this improvement. The runner invokes
them explicitly because they are console checks rather than `dotnet test` projects.
Playwright should be a locked development dependency, with existing-browser use
separate from any explicitly requested setup/install action.

Add a manual verification workflow separate from packaging, reusing the same
profiles. Windows comes first, then Linux-compatible Core/client/browser checks.
The packaging workflow remains separate and intentionally runs no tests. The new
verification workflow is also manually dispatched.

Prioritize behavioral regressions that protect real state:

1. Draft navigation after cache eviction, catalog consistency, undo and restore;
   long-document search only if that feature is retained and connected.
2. Map conflict/failure feedback, retry, persisted resize, reset and orphan cleanup.
3. First-entry toolbar actions, single/double click, zoom anchoring and filter-safe
   arrangements, with bounds and overlap checks for varied card sizes.
4. UI font isolation, raw document labels, delayed theme work and dialog lifecycle.
5. AI cancellation, late replies, selection replacement and context limits using
   synthetic transport replies, without real API keys or billed requests.

Use concise behavior names such as “La mappa conserva le dimensioni dopo la
riapertura”. A failure should explain the expected behavior and what happened;
stack traces belong in the attached details.

## Documentation and delivery order

Current-status documentation needs one authoritative page. `implementation-status.md`
still describes ink/sticky notes and read-only Markdown, while architecture records
their retirement and AI editing exists. Keep historical validation results clearly
dated rather than treating them as current acceptance evidence.

`security.md` says startup makes no automatic API check, but MainPage starts
`RefreshMarkdownAiKeyAsync`, which requests the account model list when a saved key
exists. That request does not send documents. Document the actual behavior or
change the trigger deliberately.

The dependency page records an older no-warning result while the task list records
NU1902/NU1903 findings against still-pinned dependencies. Reconcile that history and
perform a fresh advisory/restore check when dependency maintenance is executed;
this review did not establish current advisory status. The packaging guide also
names `windows-packages.yml`, whereas the actual file is `package-windows.yml`.

Recommended sequence:

1. Repair map failure feedback and effective document/catalog ownership. Correct
   font/localization and asynchronous position handling; decide the scope of search.
2. Repair existing UI scenarios and build freshness, then introduce the simple
   runner and focused regressions. Use that verification path throughout cleanup.
3. Remove legacy schema/annotation code, consolidate catalogs and small duplicated
   helpers, and extract controllers where ownership is clear. Align current docs.

Keep packaging, installation, publication and SDK changes separate from these
checks. Packaging currently clears all artifacts by explicit repository policy;
if report retention is introduced, revise that policy and script together rather
than relying on reports surviving packaging.
