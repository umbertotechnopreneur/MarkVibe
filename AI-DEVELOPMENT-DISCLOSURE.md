# AI development disclosure

## Text-only confirmation actions — 2026-10-25

During the authorized build/install/Git delivery, Umberto requested removal of
the icons from the shown Yes/No buttons. Codex set the existing HideIcon property
on both shared confirmation actions, retaining their localized text and behavior.
Release x64 0.1.0.33 was built from source, signed, installed and launched.
Generated markup, packaged assembly identity, Windows package status and process
path were checked. No tests or interactive GUI validation ran for this small edit.

## Release 0.1.0.32 delivery — 2026-10-25

On Umberto's subsequent installation and Git delivery request, Codex built
signed Release x64 packages from current source, trusted the local development
certificate, updated the existing MSIX installation and launched it. Packaged
startup/graph assets match source; Windows package status and the running
installed executable were verified. Graph traces remain local and ignored.
No new test suite or interactive native acceptance ran. Existing dependency
advisories remain. This delivery includes the dense graph, splash and subtle
hyperlink changes together with the previously delivered document services.

## Dense graph performance and startup presentation — 2026-10-25

Umberto requested further performance work on his real Markdown folder and a
cleaner splash with one extra second, dismissible by clicking. Codex profiled
the current 55-document / 506-link graph in isolated Debug Edge using DevTools
traces. It reduced dense-map decorative animation, batched geometry updates
and stabilized semantic detail during camera movement. Two final runs reduced
hover p95 from 100 to 33.3 ms; zoom improved from 66.6 to 33.3–50 ms.
The dated graph report records methodology, endpoint checks and limits.

The splash now has a wider responsive title and a separate startup state/service
with a DOM binding. Managed initialization signals readiness; clicks skip the
optional second without skipping initialization. Debug compilation and browser
startup during profiling passed. Splash visual/click acceptance, broad tests and
native validation were not performed. No installation or Git delivery ran;
the supplied documents and existing pending changes were preserved.

## Discreet hyperlink underlines — 2026-10-25

Umberto requested dotted or more discreet link underlines with additional space
below the text. Codex updated document CSS and the shared theme adapter to use
1px dots, a .25em offset and 55% of the link color in the reader and exports.
The existing option to hide underlines remains effective. Initial JavaScript/CSS
parsing and diff checks passed. On Umberto's subsequent build/install request,
Codex compiled Release x64 0.1.0.31 from current source, verified the new CSS in
the signed MSIX, updated the local installation and launched the installed app.
Windows package status and process path were verified. Existing NuGet warnings
remain; no tests, interactive GUI validation or Git delivery ran for this request.

## Local Release delivery — 2026-10-25

Umberto requested installation, commit and push and confirmed inclusion of all
pending app changes. Codex built Release x64 0.1.0.30 from repository source,
trusted the generated local development certificate, updated the existing MSIX
installation and launched its installed executable. Windows package status and
the process path were verified. Packaging also generated MSI and portable files;
those distributions were not installed. Existing NuGet vulnerability warnings
remain; dependencies were unchanged. No tests or GUI interaction ran.

## Document commands and model ownership — 2026-10-25

Umberto approved the proposed context commands and required an intermediate
object model with services below the UI. Codex added checked reader widths,
selection-prefilled find, source navigation, path/section/table/code copying,
grouped exports and collapsed appearance controls. Core contains the source
extraction and interaction models; App composes menu, search and reader services.
Global preferences, folder appearance and shell layout now use these services.
Existing workspace/navigation, AI, map and watch/deletion workflows still require
further separation; this change does not claim a complete application migration.

Static C# and JavaScript syntax, XAML XML, eight-language labels and whitespace
checks passed. No build, automated tests, installation, GUI interaction, new
dependency import or Git delivery ran for this change. Native clipboard, Excel
paste, visual search and context-menu acceptance remain unverified at runtime.

## Parent folder links — 2026-10-25

Umberto requested clickable `../` prefixes at both displayed workspace paths.
Codex added the two links and native parent opening through the existing guarded
workspace transition. Drive roots disable the links, and browser providers hide
them. Static C#/JavaScript syntax, XAML XML, translation and diff checks passed.
No build, tests, installation, GUI interaction or Git delivery ran.

## Optional Markdown association — 2026-10-25

Umberto requested a choice to associate Markdown files during installation.
Codex registered `.md` and `.markdown` handlers for MSI and MSIX, preserved quoted
document paths through `--open`, and added a localized Yes/No offer. Yes opens
Windows Default apps for final user confirmation; No preserves existing defaults.
The installer helper/interactive MSI invokes setup after installation. Other
installed routes offer it at first launch, remembering the answer across updates.
Portable/source and diagnostic modes skip setup. Static C#/PowerShell syntax,
JSON/translation coverage, XML and diff checks passed. Full SDK schema validation
was unavailable because the installed schema set references an undeclared
`rescap5:ImmediateRegistration` attribute. No build, tests, installation, GUI
interaction, dependency import or Git delivery ran.

Umberto also requested a Settings entry. Codex added separate effective-default
indicators for `.md` and `.markdown`, using Windows shell AppUserModelID/ProgID
queries, and an explicit setup action that remains available after declining the
installation offer. The UI refreshes on desktop activation, labels unavailable
queries and hides the action when both defaults match. C#/JavaScript syntax,
XAML XML, translation coverage and diff checks passed. Runtime/UI acceptance,
build and installation remain unperformed.

## Reader column width — 2026-10-24

Umberto requested normal and expanded reading columns to use horizontal space,
especially for wide tables. Codex added two localized toolbar actions, original
vector icons and a persisted shell-layout choice. Expanded mode removes the
reader width cap while retaining modest responsive margins and reading position.
Changing width does not rerender Markdown, Mermaid or themes. Static review
corrected toolbar responsiveness when AI/sidebar panes resize the document area.
C#/JavaScript/CSS syntax, XAML XML, translation structure and diff checks passed.
No application build, tests, installation, GUI interaction or Git delivery ran.

## Authorized local delivery and Git integration — 2026-10-23

Umberto requested a local Release build and installation before commit, push and
PR merge. Codex built current source as Release x64 0.1.0.29, trusted its generated
development certificate, updated the existing MSIX installation and verified the
running process at the installed package path. Profiling evidence was preserved.

A separate static review found a native-read race: a file could grow between
checking its size and allocating its buffer. Codex changed both operations to use
one captured length. Nine Native checks passed, including active-writer reads at
and above the size limit. No other actionable review findings remain. Existing
dependency advisories and native GUI/performance acceptance remain unresolved.
The owner authorized updating and merging existing PR #3; formal GitHub author
self-approval is not claimed. No installer, private document or raw trace is published.

## Authorized Debug browser graph profiling — 2026-10-23

Umberto requested browser Developer Tools profiling of the slow graph in Debug
and supplied a local Markdown folder. Codex reused the existing Browser project,
imported 32 Markdown files read-only in isolated headless Edge and recorded CDP
timeline/CPU traces after indexing. Camera batching, retained atlas elements and
removing SVG emphasis transitions reduced measured zoom p95 from 66.7 to 33.4 ms.
All 674 endpoint checks passed after each gesture; some short stalls remain.
The profile is browser evidence, not native desktop acceptance.

Codex added an explicit readable Debug bundle and guards that prevent its reuse
in Release. Twenty client checks, 22 Core checks, three targeted browser scenarios
and Debug source compilation passed. Browser checks exposed a startup race that
discarded Back while opening was finishing; Codex retained that history entry
until the provider was ready, and the immediate navigation regression passed.
No external dependency import, app installation, Git delivery or publication ran. Details and
validation limits are in `docs/graph-profile-2026-10-23.md`.

## Performance cleanup and authorized regression checks — 2026-10-23

Umberto requested a broader redundancy/performance review, preservation of
compatibility and relevant C# and JavaScript tests. Codex added catalog snapshot
and unchanged-source reuse, corrected source-view resource retention and removed
five unused private handlers without changing stored formats or bridge commands.
New native checks exposed a missing first-save data directory in recent history;
Codex fixed it. All 43 Core, Native and JavaScript checks passed. A synthetic
512-document comparison measured lower repeated-index time and allocation;
`docs/performance-review-2026-10-23.md` records the baseline and limits.
No GUI checks, new application delivery, external dependency imports or publication
ran. Installed Release remains 0.1.0.27 and later source changes await delivery.

## Recent menus, graph rendering and action colors — 2026-10-23

Umberto requested persistent recent file/folder paths, alphabetic display and
middle abbreviation; a single graph-card border; static performance review;
and colored action icons. Codex added separate bounded local histories, guarded
reopening, measured Unicode-safe path captions and complete accessible names.
Graph topology is reused until catalog metadata changes; drag/resize redraws
are batched per frame and unchanged connection curves keep their SVG attributes.
Selection uses one border. Shared action glyph colors adapt to light/dark themes
without recoloring VibeWare artwork or text labels. Validation is static only;
no runtime performance improvement or visual acceptance has been established.
C#/JavaScript/CSS syntax, XAML XML, eight-language translation structure and diff
checks passed. No build, tests, installation or publication ran for this request.

## Context menu copying — 2026-10-23

Umberto requested Copy and Copy entire file content in the Monaco and reader
context menus, retaining the existing Explorer action and excluding Paste.
Codex added selection capture before popup focus, shared copy commands and
eight-language captions. Full-file copying uses the open Markdown snapshot,
including local revisions, or loads the captured target through its provider.
JavaScript and C# syntax, XAML XML, translation structure and diff checks passed.
No build, tests, GUI interaction, installation or Git delivery was requested.

The owner subsequently requested compilation and installation. Current source,
including file monitoring and context copying, was built and packaged as Release
x64 0.1.0.27 with existing tools. Its development certificate was trusted and its
MSIX bundle updated the installed app from 0.1.0.26. Windows reports Status Ok;
the process was verified at the installed package path. Existing NuGet security
advisories remain. No tests, GUI acceptance, publication or Git delivery ran.

## Current-file change notifications — 2026-10-23

Umberto requested a Yes/No reload dialog when the current file changes outside
MarkVibe. Codex added a native file watcher with save debouncing, connected-folder
browser polling and navigation-owned notification identities. The managed shell
defers prompts during active operations, preserves the current copy after No or
read/parse failure, and warns before discarding local edits and undo history.
Reload uses the existing parser, provider containment and rendering paths.

This follow-up is source-only: syntax and diff checks, without builds, automated
suites, GUI interaction, packaging or installation. The installed application
remains Release x64 0.1.0.26. The owner has withheld approval and merge of this
new work. The prior PR #2 had already merged when that instruction arrived.
A synthetic Core case covers preserving other drafts and rejecting out-of-folder
reloads without source writes; it was added but not executed.

## Reader rendering and context themes — 2026-10-23

Umberto supplied rendering screenshots and requested theme selection from the
document context menu and a way to remove hyperlink underlines. Codex traced
visible HTML comments and disclosure tags to disabled HTML parsing, added
restricted formatting renderers and kept the independent browser sanitizer.
It added source navigation into disclosures, expanded their PDF contents and
connected context theme controls, installed monospace font overrides and link
decoration to per-folder preferences with global defaults and a reset command.
Font choices reuse the desktop host's installed-family inventory without downloads.
Mermaid rendering remains on its existing bounded, sanitized path.

Initial source-edit validation covered JavaScript syntax, XML/JSON structure
and diff checks. Two synthetic Core regression cases were added but not executed.
Compilation and installation were subsequently authorized as recorded below;
owner acceptance remains pending.

In a follow-up request, Umberto explicitly authorized Release compilation,
uninstalling the existing application, installation and artifact cleanup.
Release x64 0.1.0.26 was built from the current sources, packaged and signed;
the generated certificate was trusted through Windows elevation. Debug 0.1.0.25
was removed and the Release MSIX installed and launched. Windows reports Status
Ok and the display name MarkVibe; the running process is in its installed package.
Previous generated artifacts and temporary packaging files were removed. The
existing local user profile was retained. Existing transitive dependency advisories
remain; no tests, GUI acceptance, commits or publication were performed.

The owner then authorized Git delivery and requested remembering Release x64 as
the default. Codex updated the packaging parameter and build instructions and
recorded the preference. These default/instruction changes were syntax-reviewed;
they do not change the already installed 0.1.0.26 application binaries.

## Focused maintainability pass — 2026-10-17

Umberto requested concrete examples of duplicate/complex code, simpler ownership,
descriptive intermediate values, spacing and useful type/method/internal comments.
He explicitly rejected historical compatibility requirements. Codex reviewed the
AI bridge, browser request/storage paths, map codec/session and shared contracts,
implemented the focused changes and documented before/after examples in
`docs/maintainability-review-2026-10-17.md`. It also identified separate remaining
request-session, CLI process/protocol and native HTTP consolidation opportunities.

The current provider-explicit bridge replaces old command families without aliases;
browser HTTP/storage plumbing is shared, the map session has a dedicated file and
JSON validation has named phases. Secure-context UUID support is now required.
Bounds, cancellation, containment, strict map versioning and real platform checks
remain. This is a scoped maintainability review, not a complete dead-code or
security audit and not a claim that every file has been cleaned.

Bundling and final Debug x64 application compilation succeeded with zero errors
and the existing transitive dependency warnings. The smoke fixture was updated but
not executed. No suites, GUI checks, provider tests, inference, packaging, version
increment, installation or application launch occurred. Installed 0.1.0.25 remains
the earlier delivery and does not yet contain these source changes.

## Opaque desktop presentation — 2026-10-16

The owner supplied a new screenshot showing missing shell regions while moving
the document map in the installed 0.1.0.24 app. This disproves completion of the
earlier composed-host redraw repair. Codex replaced capture-to-WPF browser
presentation with direct HWND WebView2 hosting and made the browser, WPF surface
and native client backgrounds opaque. Native Acrylic availability is disabled
with a localized Settings explanation; the saved preference and browser modal
blur are retained. No GPU-disable flag or continuous forced redraw was added.
The screenshot does not by itself identify the exact runtime/driver failure;
native pan/zoom and modal acceptance remain with the owner.

Debug x64 0.1.0.25 compiled and packaged successfully, with the pre-existing
transitive dependency advisories still present. Its MSIX was installed and the
installed executable launched; Windows reports exact version, x64 and Status Ok.
MSI and portable outputs were also generated. No automated suites, GUI checks,
provider test or inference was run, and no functional resolution is claimed.

## Repository delivery and copy — 2026-10-16

With the owner's explicit branch and Git approval, Codex consolidated the 18
existing local commits into `First commit`, retained verified history/dirty-state
backups and release tags, protected `master`, and opened
[PR #1](https://github.com/umbertotechnopreneur/MarkVibe/pull/1) for the follow-up
changes. The PR remains open. No CI workflow or additional test was started.

At the owner's request, the complete repository and local Debug artifacts were
copied from `E:\Research\MarkVibe` to `E:\MarkVibe`, preserving the original.
The copy completed without reported failures; Git HEAD and selected file/package
hashes matched. The owner will reconnect the folder in Codex.

## Selectable AI connections — 2026-10-16

Umberto requested two explicit Settings choices: Codex CLI and OpenAI API key,
each with a Test button and a pass/fail window. Codex delegated managed/client
routing, Settings/localization and native transport review. The Windows adapter
uses the installed CLI and its existing login with ephemeral bounded requests,
disabled tools/plugins and cancellation that terminates the process tree. It
does not claim OS isolation for filesystem reads or API-equivalent CLI retention.
API remains the default; failures never silently select the other provider.

Debug x64 0.1.0.24 was bundled, compiled, signed, installed as MSIX and launched;
Windows reports Status Ok. MSI 0.1.24 and portable outputs were also generated.
No live provider Test action, inference, automated suite, check executable or
interactive acceptance was run. The owner performs those functional checks.

## Repository and branding preparation — 2026-10-16

Umberto requested a short README explaining the weekend-project origin, a document
map, MIT-compatible distribution notices, a real MSI alongside MSIX/portable,
three focused maintainability agents and PR-based delivery. He supplied the
VibeWare brand repository as the reference for the approved logo, source headers,
first-person About disclosure, footer and legal links. Codex used those templates;
the existing local logo matches the approved source byte for byte.

He also requested Visual Studio compilation/debugging and a future Store package.
After confirming that no Partner Center identity is available, he limited current
MSIX delivery to local Debug installation. The Store identity is not invented and
no Store submission or Release/ARM64 build is included in this delivery.

Delegated agents reviewed the C# exploration state, JavaScript graph effects and
Windows packaging. Their changes reduce repeated control/DOM reconstruction,
cancel effects tied to replaced geometry and clarify validation phases. The
license review identifies the bundled EPL-2.0 ELK module separately from MIT
application code and adds versioned source-availability notices. It does not
claim that all dependencies are MIT or that a security audit was performed.

Build, installation, Git delivery and owner acceptance are recorded only when
completed in the task archive. This preparation runs no automated suites or GUI
checks; the owner retains interactive acceptance.

The parent subsequently restored and compiled all seven solution projects in
Debug (zero errors; existing NU1900 cache warnings), then generated and signed
MSI, MSIX/bundle and portable 0.1.0.23 x64 outputs. The first MSI authoring attempt
exposed an incorrectly constructed RemoveFile row; it was corrected before clean
regeneration. Windows confirms installed MSIX version 0.1.0.23 and Status Ok,
and the installed executable was launched. MSI installation/upgrade/uninstall,
Visual Studio F5 and GUI acceptance remain unverified. No suite, check executable,
interactive input, screenshot, Release build or Store submission was performed.

The owner approved six further map improvements on 2026-10-16: lighter cards,
finite directional link effects, a relation lens, bounded multi-selection with
comparison and atomic AI reference collection, actual navigation trails and
catalog-backed attention highlights. Codex implemented managed state/contracts,
cached graph facts, accessible XAML controls and unfiltered renderer overlays.
No new rendering engine, inference request or source-file migration is involved.
GUI acceptance remains with the owner; delivery evidence belongs to the archive.
No interactive checks or automated suites were run for this follow-up.
Debug x64 0.1.0.22 subsequently bundled, compiled, signed and installed successfully;
Windows reports Status Ok and the installed executable was launched. This records
delivery and does not assert owner acceptance of the new interactions or effects.

## Project and scope

- **Project:** MarkVibe.
- **Repository:** [umbertotechnopreneur/MarkVibe](https://github.com/umbertotechnopreneur/MarkVibe).
- **Maintainer:** [Umberto Giacobbi](https://umbertogiacobbi.biz).
- **Disclosure date:** 2026-10-16.
- **Document atlas follow-up:** owner-approved semantic zoom, folder islands,
  neighborhood lenses, source-backed routes, preview, minimap and named views.
  Codex implemented the source changes and readability/UI follow-ups. Debug x64
  packaging and installation through 0.1.0.21 are recorded below and in the task
  archive. Automated suites were not requested; full functional acceptance
  remains with the owner and is distinct from successful delivery.
- **Applies to:** the 2026-10-11 source attribution, contributor guidance,
  readability revision, OpenAI settings and stateless Markdown AI editing,
  plus the 2026-10-16 local packaging, assistant-panel, map/settings and
  document-model/maintainability and readability-standard updates. Their
  evidence and historical validation are scoped separately below.
- **Project license:** [MIT](LICENSE), Copyright (c) 2026 Umberto Giacobbi.
  Dependencies and vendored assets retain their applicable licenses.

## Human intent

Umberto Giacobbi created MarkVibe to read local Markdown, navigate connected pages
and export useful HTML/PDF. Earlier handwritten/sticky-note requirements were
subsequently retired in favor of a simpler reader and optional Markdown AI editing.
The current requirements preserve workspace originals, retain local document
revisions, bound native file access and handle conflicting changes explicitly.

The following decisions remain under human direction:

- Product goals, priorities and acceptance criteria.
- The C#/OpenSilver architecture, browser rendering and Windows integration.
- Privacy, file safety, distribution and MIT licensing requirements.
- Execution, testing, installation and publication authorization.
- Readability: documentation for every class/type and method/function,
  explanations inside complex methods, descriptive names, deliberate blank lines
  and code that favors human understanding over compactness.

For this update, the maintainer requested the supplied VibeWare footer, an adapted
AI disclosure and source headers consistent with the repository's MIT license.
The maintainer then requested the same readability rules for existing code without
changing its behavior, plus explicit OpenAI key validation and host-specific storage.

## AI execution

For the scope above, OpenAI Codex:

- Replaced the floating assistant with a fixed right-hand pane and standard
  splitter; retained its open state and pane geometry in separate local layout
  settings. Simplified references and token estimates, moved explanations to blue
  information tooltips, and reused reference rows during prompt updates. Recorded
  the owner's standing authorization for local Debug certificate trust and
  installation, including closing the app and discarding unsaved in-app work.
  Delivery evidence for this update is recorded separately in the task archive.

- Implemented the maintainer's inline map toolbar/search, rounded view tabs,
  breadth-first Tree arrangement and optional animated depth based on managed
  connection distances, with shared graph indexing and bounded visual effects.
  Refined depth with up to twelve recessed levels combining graph and spatial
  distance, stronger selection and attenuation toward distant line endpoints.
- Refined the card appearance menu with a preview, color-selection rings and an
  icon gallery. Shared file context actions across navigation, map and document
  hosts, with contained native paths, standalone browser previews and a shared
  frosted confirmation before revision-checked Recycle Bin operations.
  Built/signed the 0.1.0.11 Debug x64 distribution without running tests. The
  requested local installation initially failed certificate trust; after explicit
  owner consent for the specific development certificate and installation,
  Windows registered version 0.1.0.11 with Status Ok. No functional tests ran.
- Added assistant usage guidance as displayed reviews, modal blur refinements,
  clear Pages/Outline hover and selected colors, Tree as
  the unsaved initial arrangement and directional document link sections.
  Built/signed 0.1.0.12 Debug x64 and launched its direct desktop preview. Tests
  and live inference were not run; installation of this new package requires
  authorization for its distinct development signing certificate.
- Following the owner's screenshot feedback, removed the OpenAI settings card
  and status badge. Moved blur onto the full-window overlay, encoding dimming in
  its background brush and separating content motion from backdrop animation.
  Built/signed 0.1.0.13 Debug x64 and launched the direct preview. No test suite,
  API validation or functional acceptance ran; the owner will test manually.
  After separate explicit owner consent for certificate
  E5C9ACC815B3399618A25C0B6F944C0E301216C1 in LocalMachine/TrustedPeople,
  verified the bundle checksum, trusted that certificate and installed the
  0.1.0.13 update. Windows registered X64 version 0.1.0.13 with Status Ok.
  Deployment confirmation does not establish functional acceptance.
- Inspected repository guidance, source inventory, licensing and existing records.
- Recorded the requested readability rules in `AGENTS.md` and `CONTRIBUTING.md`.
- Added VibeWare headers to maintained project source, scripts and markup, using
  each format's comment syntax and preserving the existing source body.
- Added the supplied VibeWare footer and a disclosure section to `README.md`.
- Adapted the disclosure template to the known project context and evidence.
- Reviewed header coverage, source preservation and local documentation links.
- Documented existing methods/functions, expanded dense blocks and long
  expressions, clarified local names and explained non-obvious persistence,
  indexing, anchoring and archive validation steps.
- Compared executable C#/PowerShell tokens and normalized JavaScript syntax trees,
  allowing only scoped local renames and equivalent control blocks.
- Implemented masked OpenAI key entry, an authenticated models-list request,
  cancellation/timeout handling and persistence after successful validation.
- Added Windows user-environment storage and disclosed insecure isolated browser
  storage, with credentials kept separate from preferences, workspaces and exports.
- Added OpenAI labels and messages to the shared nine-language UI catalog.

| Tool or service | Model or version, if known | Role in the project | Scope of use |
| --- | --- | --- | --- |
| OpenAI Codex | Model/version not recorded for this update | Attribution, readability, OpenAI settings, Markdown AI editing, documentation and static review | Maintained project sources, `AGENTS.md`, `CONTRIBUTING.md`, `README.md`, this disclosure and task records |

The source headers record the attribution and readability edits performed on 2026-10-11. Original
per-file authorship and earlier AI tool/model history have not been reconstructed;
the optional `Created with AI` line is omitted where creation provenance is unknown.

## Human review and responsibility

Umberto Giacobbi supplied the intent, templates, licensing decision and coding
requirements. He retains control over acceptance and publication and responsibility
for released work. A completed file-by-file human review or acceptance of this
working-tree update has not been recorded in this disclosure.

## Checks performed

These checks apply only to the attribution and documentation update:

- [x] Static review of header coverage, comment placement and documentation links.
- [x] Preservation of the pre-existing source body outside the added headers.
- [x] Confirmation of the existing project MIT notice and preservation of
  third-party attribution; this is a limited license check.
- [ ] Human code review.
- [ ] Build or compilation.
- [ ] Automated tests.
- [ ] Manual or exploratory app tests.
- [ ] Static analysis or linting of application behavior.
- [ ] Security review.
- [ ] Privacy or data-handling audit.
- [ ] Comprehensive dependency and license audit.
- [ ] Performance or resource checks.
- [ ] Accessibility review.

**Evidence:** [repository guidelines](AGENTS.md),
[contributor guide](CONTRIBUTING.md), [license](LICENSE),
[completed task records](.github/tasks/archive.md) and the working-tree diff.
Tests require an explicit request for the current task; none was made for the
2026-10-11 attribution update.

### 2026-10-16 local Windows packaging and assistant panel

At the maintainer's request, OpenAI Codex changed local packaging to default to
x64 Debug and recorded that local build requests include launching the new
desktop executable. Broader architectures and Release require explicit selection;
the manual CI workflow retains its explicit x64/ARM64 selection.

The maintainer separately authorized installing the missing WebAssembly workload
and requested tests for this task. Core (7), Native (5) and client (9) checks passed.
The WebAssembly tool-pack lock was aligned to 10.0.13. Build, signing and launch
outcomes, including existing NuGet security warnings, are recorded in the
[task archive](.github/tasks/archive.md). Packaging's `testsRun: false` describes
the packaging script itself; the checks above ran separately. No app/certificate
installation, OpenAI inference or publication was performed.

The maintainer then requested a simpler assistant panel. Codex moved the response
above the composer into its own scroll area, retained prompt/model/budget controls,
and made disclosure arrows point down when expanded and up when collapsed.
Assistant labels were added to the shared nine-language catalog; generated text
keeps its existing request-language behavior. Build and visual-inspection evidence
for this follow-up is recorded separately in the task archive.
The maintainer then requested an identity icon beneath the model selector; Codex
reused the existing sparkles vector and translated Markdown-assistant caption,
preserving the current colors. The owner explicitly authorized discarding the
previous instance's unfinished work before its replacement build and launch.

### 2026-10-16 readability standard and delegated cleanup

The maintainer requested human-readable code as the ongoing repository standard,
including XML documentation above C# classes and methods, explanations inside
complex methods, clear names and room between logical steps. Codex strengthened
`AGENTS.md` and `CONTRIBUTING.md` and aligned the engineering lessons.

A delegated Codex agent cleaned up `client/source.js`,
`client/document-selection.js` and `scripts/client-inputs.mjs`: expanded control-flow
blocks, wrapped long expressions/objects, separated logical phases and added
focused comments plus class documentation. Existing local names, public contracts
and concurrent file-context changes were retained.

Validation was limited to diff review, whitespace checks, syntax checks with the
existing Node 24.19.0 and identical before/after in-memory esbuild-normalized output
with identifier minification disabled for all three files. This is static evidence;
no build, client bundle, tests, app launch, installation or publication was performed
for this cleanup. Other agents' C# documentation work is outside this contribution.

### Historical repository records

The [local validation record](docs/validation.md) and
[2026-10-11 report](docs/validation-results-2026-10-11.md) describe earlier authorized
builds and core, client, browser, native and Windows checks, including visual PDF
inspection. Those checks preceded later source changes; the current Core, Native
and client checks are scoped in the 2026-10-16 section above.
The report's original ignored artifacts were removed during subsequent packaging.

[Windows packaging](docs/windows-packaging.md) and the
[implementation status](docs/implementation-status.md) separately record local
MSIX/portable build evidence and remaining runtime or installation acceptance.

## Known limitations and unverified areas

- The attribution task did not rebuild or exercise the app. Later compilation
  evidence is recorded below; runtime behavior remains unverified for this revision.
- The existing-code readability revision was checked statically; no application
  build, test suite or runtime validation was run for it, the new OpenAI settings
  or Markdown AI editing.
- No actual OpenAI key was accessed or used for this contribution. Live API,
  browser CORS/storage, user-environment persistence and dialog rendering remain
  unverified. The models-list check establishes authentication, not model inference.
- Full human review, original per-file AI provenance and a complete dependency,
  security or accessibility audit remain unrecorded for this scope.
- Physical pen/palm behavior, native dialogs at different DPI scales and packaged
  installation remain acceptance items in [active work](.github/tasks/todo.md).
- Runtime checks for later source changes remain scoped to their own task records.

## Readability and OpenAI credential settings — 2026-10-11

The readability stage reviewed 34 C# files, 27 JavaScript/modules and three
PowerShell scripts. Static comparisons preserved executable syntax while allowing
the stated local renames, split declarations and equivalent control blocks.
Concurrent sticky-note work was retained and compared against refreshed snapshots;
the authorized OpenAI feature was added after that readability checkpoint.

For the new feature, validation was limited to source inspection, syntax, native
type/API resolution without emitting an assembly, XAML control/event integration
and localization consistency. OpenAI's
[official model-list API reference](https://developers.openai.com/api/reference/resources/models/methods/list)
provided the authenticated request contract. No live key or API request, build,
test suite, app launch, packaging, installation or publication was performed.

## Stateless Markdown AI editing contribution — 2026-10-11

The maintainer requested a reusable key-aware composer, a dismissible Settings
banner, simple Markdown preview, model and reasoning selection without Ultra or
speed tiers, generous token/context bars, semantic cost categories, and an elevated
blurred waiting popup with a one-minute deadline. He clarified that each send
contains only the current prompt plus document/selection, and its structured edit
automatically replaces that target without conversation history.

OpenAI Codex implemented the managed contract and reusable DOM components,
selection/source-offset mapping, browser/native HTTPS, strict structured replies,
revision checks, cancellation, local undo, copy saving and annotation protection.
Two system messages describe MarkVibe and restrict it to Markdown editing/review,
including refusal of image generation, new books and unrelated bulk manuscripts.
Returned code is not executed; generation is not given tools. The model/pricing
catalog is retained as JSON with official sources, timestamp and model-specific
input/output budgets; the public downloader ran successfully on 2026-10-11.

Official OpenAI model, pricing, structured-output and token-counting documentation
informed the implementation. The local UI exposes relative cost labels rather
than currency quotes. Full-input preflight guards generation and marks individual
input shares estimated when exact attribution is unavailable. Usage keeps cached
and reasoning subsets from being counted twice. No subagent was used for this scope.

Validation is limited to JavaScript syntax, JSON/XAML structure, source-contract
inspection, installed Markdig renderer metadata and whitespace checks. No actual
OpenAI credential was read, inference request made, app launched, solution compiled,
bundle built, tests run, package installed or change published. Visual, API and
runtime acceptance remain in the task list. See [Markdown AI editing](docs/ai-editing.md).

## UI, theme and localization contribution — 2026-10-11

The maintainer requested colorful generated icons, borderless buttons with
tooltips, nine languages, Windows Acrylic, segmented navigation tabs, About
branding, responsive settings with two columns, and imported document/code themes
with their typography. OpenAI Codex implemented these changes. A delegated
`gpt-6-astra` agent designed and implemented the shared theme catalog and scoped
CSS adapter; it also refined comments and readability in its assigned files.
The built-in imagegen tool produced the toolbar icons, four sidebar proposals
and About artwork. Its underlying model/version was not exposed. The maintainer
selected the segmented-tab proposal and supplied the original VibeWare logo.

Imported third-party theme notices are preserved in
`THIRD-PARTY-NOTICES.txt`. Prompt records and image assets are in
`docs/design/` and the browser asset folders. The implementation reuses local
font stacks and strips external CSS/font requests from imported themes.

Validation for this contribution: client bundling and solution compilation with
existing Node 24.21.0 and .NET SDK 10.0.401 completed with zero warnings/errors.
No tests, app/browser launch, installation, packaging or publication were performed.
Visual layout, translation review, physical Acrylic appearance and themed
HTML/PDF output remain unverified; acceptance items are in the active task list.

## Disclosure statement

This project is developed with AI under human direction. This disclosure records
the known AI contribution, human guidance and completed checks for the stated scope.
Update it when contributions, tools, review or validation evidence materially change.

Learn more in the [VibeWare manifesto](https://umbertogiacobbi.biz/vibeware/manifesto).

## XAML application controls and simplified editor — 2026-10-11

Umberto required OpenSilver XAML/C# for every application control and explicitly
allowed specialized web renderers for Markdown, Monaco and Mermaid. He removed
pen, annotations and notes from scope. OpenAI Codex replaced DOM composer, flyout,
review, waiting overlay, source/map toolbars and map appearance UI with XAML
UserControls; moved icons to XAML geometry templates; and retired annotation UI,
managed sessions, native write endpoints and annotation exports. Existing user
sidecars were not removed. Fluent navigation/chevron geometry retains Microsoft
MIT attribution. The selected proposal 5 supplies the visual direction.

The native AI Assistant now has a draggable title bar and a compact mode that
retains the prompt. Its existing XAML surfaces use a CSS material fallback for
16-pixel backdrop blur and a system-accent shadow with 16-pixel elevation.
The title bar now shares the surrounding material rather than painting a separate
rectangle. One model/effort control opens a composer-width native popup; changing
reasoning effort updates its caption immediately without rebuilding the model list.
The flyout now covers the entire composer action row. Its native XAML slider uses
a larger rounded track with progressive accent saturation and a 160-millisecond
color transition. Keyboard guidance and an abbreviated token estimate share one
row; the compact panel removes its visible scrollbar and excess bottom space.

Validation for this revision: local client bundle and solution build with zero
warnings/errors; direct desktop inspection at normal and narrow widths, including
native flyouts, drag, compact mode and unsent-prompt closing protection.
Automated tests and OpenAI inference were not requested or run. API
request behavior, installation and publication are not certified by UI inspection.

## Cinematic document map and arrangements — 2026-10-16

Umberto requested a science-fiction-inspired map with a pure black background,
an application-accent selection shadow at elevation 16, wheel zoom, progressive
card entry and Star/Circle/Grid layout buttons. OpenAI Codex implemented the
renderer styling and gestures, native XAML toolbar and localized labels, and
managed deterministic graph arrangements. Delegated Codex agents contributed
the C# layout algorithms, toolbar/localization and an independent source review.

Arrangements rank distinct incoming/outgoing document neighbors, retain existing
card appearances and legacy metadata, and commit coordinates through the current
managed save boundary. Loaded layouts stay intact until an explicit arrangement.
The renderer respects reduced motion and keeps compact devices scrollable.

Validation for this contribution is limited to source review and static syntax,
markup and formatting checks. No build, bundle, automated tests, app launch,
installation or publication was requested or performed for these map changes.
The visual result and interaction/persistence behavior in the running app remain
unverified.

## Map interaction and settings follow-up — 2026-10-16

At Umberto's direction, OpenAI Codex added connection-hover emphasis, animated
single-click selection, double-click navigation, persistent resize grips, a
confirmed map reset, left-aligned icon controls and an optional monospace strip
of full-source document statistics. Removing a stale annotation callback repairs
the map-state render/autosave path. At this stage, card dimensions used a
version 3 layout contract that retained earlier layouts and legacy metadata;
the later maintainability revision below replaces that prerelease contract.

The same contribution adds shared frosted native dialogs with 300-millisecond
entry/exit blur, a clearer masked OpenAI key form, and one UI-font preference
whose Windows choices come from installed supported monospace families. Reader
and editor typography remain independently configured. Token counts are local
estimates; no additional document-to-AI attachment workflow is implemented.

Delegated Codex agents implemented managed layout/selection/statistics, toolbar
and dialog components, settings and font integration, with cross-review of the
source. Validation consists of static source review, JavaScript syntax checks,
JSON/XAML parsing and formatting checks. No build, bundle, automated tests, app
launch, API validation/inference, credential access, installation or publication
was performed for this follow-up. Runtime behavior remains unverified.

## Document model and maintainability — 2026-10-16

Umberto requested a multi-agent review focused on maintainability, duplicated
state, configuration mistakes and verification that is simple to operate. He
then authorized improving the unreleased model without backward compatibility.
OpenAI Codex delegated focused analysis and implementation across Core/session
ownership, UI/persistence cleanup and verification tooling while coordinating
the shell integration and cross-review.

The contribution moves edited source and bounded undo/redo history into Core
`DocumentSession`, owned by `DocumentCatalog`. Original, unsaved and exported
states are explicit; cache eviction does not replace a current revision with
the workspace original. The catalog retains at most 16 edited sessions and
three parsed models; each history stack is bounded by 16 entries and 4,194,304
UTF-16 code units. Metadata and statistics follow the effective revision.
Unused workspace-search contracts and retained search text were removed.

Map persistence now accepts schema 4 only, with no note collection or migrations
from earlier prerelease layouts. Retired note/ink schemas, the remaining note
codec and provider enumeration were removed without deleting user files.
Complete-catalog reconciliation removes orphaned map cards while preserving
current cards; reset rebuilds the current catalog from defaults.

The UI shares declared-child traversal, including OpenSilver UserControl content,
and retains explicit raw markers for document labels, tooltips and accessible
names. Icon event subscriptions are detached on unload. Core and browser settings
consume one supported-font JSON catalog, filtered by installed Windows families.
Map errors are surfaced through the managed notice path. Native smoke mode
blocks OpenAI bridge operations before credential access or HTTP work; normal
startup behavior with a configured key still loads the account model list.

Source review included JavaScript syntax, JSON/XAML structure, local OpenSilver
metadata inspection and formatting checks. The verification configuration and
fixtures were revised but not executed.

Umberto then requested map search with animated centering, folder grouping and a
document collection showing estimated AI input before sending. Codex implemented
these features with explicit read-only references, bounded collection size and
shared preflight accounting. No inference was invoked during development.

On the subsequent request to see the completed version, Codex bundled and built
**0.1.0.7 Debug x64**, created development-signed MSIX/bundle and portable outputs,
and launched the build executable directly. The live desktop window was observed;
existing NU1902/NU1903 dependency warnings remain. Test suites, app/certificate
installation and publication were not performed. Interaction acceptance and live
AI behavior remain unverified. Normal startup may load the account model list
when a key is configured; this is not an inference or credential-validation test.

Umberto subsequently requested nearly black cards with subtle gradients, cleaner
advanced statistics, inline icon/title headers and visible icons in the appearance
selector. Codex implemented these changes and corrected hidden-view restoration
after a startup screenshot exposed the issue. With explicit permission to discard
unfinished work and restart, Codex built and launched **0.1.0.8 Debug x64**.
The npm preparation now uses the selected Node runtime consistently. The existing
NuGet warnings remain; no test suite, live inference, installation or publication
was performed for this refinement.

## Docked assistant and interface follow-up — 2026-10-16

Umberto requested a fixed right-side AI pane with saved visibility and splitter
geometry, a closed first-run default, shorter labels and blue information
tooltips. Codex removed the floating/compact positioning path, separated layout
persistence from appearance preferences and avoided rebuilding unchanged reference
rows. Debug x64 0.1.0.15 was packaged, signed, installed and launched with the
owner's explicit development-certificate authorization.

The owner then requested a persistent map-inspector splitter, repair of settings
blur and stale theme labels, three agents focused on readable code, and semilight
interface fonts. Delegated agents implemented inspector layout, shared input
brushes and AI readability changes. Codex integrated these with XML documentation,
stable template-based modal blur and Semilight/Medium interface weights.

Debug x64 0.1.0.16 compiled and local UI inspection confirmed blur while Settings
remained open, light/dark/light caption refresh and a resized map inspector that
restored after process restart. The isolated diagnostics profile did not read
credentials or invoke OpenAI. Full test suites and live inference were not run;
later package/install evidence is recorded in the task archive.

The owner subsequently reported gray flashes while navigating the map. Codex
observed sidebar navigation replacing graph cards and restarting their entrance
animation, then retained the selection presentation and refreshed changed metadata
in place. The canvas stays opaque black, and equivalent planar scaling avoids
switching transform spaces during hover and selection. Focused inspection of
0.1.0.18 with 24 synthetic documents confirmed retained cards and no repeated
entrance animation during navigation after the initial presentation settled.
The Debug x64 package was signed, installed with the authorized certificate and
launched from its WindowsApps installation; Windows reports Status Ok and a
responding window. No test suite, live inference or publication ran.

The owner then showed persistent gray native redraw artifacts on card clicks,
including shell regions. The earlier retained-card observations remain valid
but did not prove resolution of that presentation failure. Codex investigated
the visible maximized Windows path using synthetic content, changed the visible
host to `WebView2CompositionControl`, bounded map painting and removed SVG filter
surfaces and minimap backdrop sampling. The standard hidden PDF host is retained.
No changes to drivers, runtime installation or user source documents were made.
Final delivery and the limits of native UI inspection belong to the task archive.

Debug x64 0.1.0.20 was then built, signed, installed and started successfully with
the required Windows Runtime projection included. The owner's exact gray failure
was not reproduced in the synthetic scenario, so resolution on his workspace
remains unconfirmed. He subsequently took ownership of GUI checks because visible
automation interrupted his work; Codex stopped those checks and recorded that
boundary in AGENTS.md. No test suite, inference or publication ran.

The next owner screenshots identified incomplete XAML map glyphs, failed card
double-click opening, clipped distance captions and inspector actions that looked
like plain text. Codex restored stroked geometry and missing musical/globe marks,
added path-based double activation, matched viewpoint input typography, and added
bordered icon actions with initially collapsed link sections and retained session
expansion. Route status has separate spacing; HTML/PDF symbols are redrawn.
Functional acceptance remains with the owner; packaging evidence is recorded in
the task archive. No interactive checks or test suites were requested or run for
this follow-up.
