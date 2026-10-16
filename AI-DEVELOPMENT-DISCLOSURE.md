# AI development disclosure

## Project and scope

- **Project:** MarkVibe.
- **Repository:** [umbertotechnopreneur/MarkVibe](https://github.com/umbertotechnopreneur/MarkVibe).
- **Maintainer:** [Umberto Giacobbi](https://umbertogiacobbi.biz).
- **Disclosure date:** 2026-10-16.
- **Document atlas follow-up:** owner-approved semantic zoom, folder islands,
  neighborhood lenses, source-backed routes, preview, minimap and named views.
  Codex implemented the source changes and static checks; this follow-up has
  no bundle/build, automated-test or running-app acceptance evidence.
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
