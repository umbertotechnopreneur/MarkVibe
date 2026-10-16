# Task Archive

Record completed behavior, validation scope and remaining limitations. Keep command
transcripts and temporary artifacts in dated validation reports, not this index.

## 2026-10-16 — Opaque direct presentation and Debug 0.1.0.25

- Treat the owner's screenshot of missing shell regions during map movement in
  0.1.0.24 as evidence that the earlier redraw change was insufficient. Replace
  visible WebView2CompositionControl with standard HWND-hosted WebView2. All app
  overlays are browser content, so WPF-over-browser composition is unnecessary.
- Remove full-client DWM glass and keep the WebView, WPF background and native
  composition target opaque from startup through theme changes. Report native
  Acrylic unavailable and explain it in all supported Settings languages; retain
  the saved preference and browser modal blur. Preserve accelerated rendering and
  existing map effects without forced-refresh timers or GPU-disable flags.
- Clean artifacts and build/sign Debug x64 0.1.0.25: MSI 0.1.25, MSIX/bundle and
  portable. Existing NU1902/NU1903 advisories for transitive Pkcs 8.0.1 and Xml
  8.0.4 remain; no dependency versions or SDKs were changed for this correction.
- Trust the matching development certificate
  31789C47C760EE203E260AF7601B72E44FA99D43, replace the MSIX and launch the installed
  app. Windows confirms 0.1.0.25, x64 and Status Ok. Record deployment separately
  in artifacts/INSTALL-INFO.json. No suites, GUI automation, desktop capture,
  provider tests or inference ran. Native pan/zoom and modal acceptance remains
  with the owner; the screenshot alone does not establish the driver/runtime cause.
- Maintain the fix in the copied repository `E:\MarkVibe`; the original checkout
  at `E:\Research\MarkVibe` remains the preserved pre-fix copy.

## 2026-10-16 — Initial consolidation, protected master, PR and repository copy

- Reuse the owner's explicit approval for `codex/prepare-public-repository`.
  Preserve all 18 original commits in verified Git bundles and the staged binary
  patch outside artifacts. Consolidate their original HEAD tree into root
  `861ed14` (`First commit`) and put the pending preparation/UI/provider changes
  in a single follow-up commit. Update remote master with an exact old-SHA lease;
  preserve the published `v0.1.0.2` tag and existing local checkpoint tag.
- Protect actual default branch `master`: require PRs, enforce administrators,
  resolve conversations, require linear history and disallow force pushes/deletion.
  Permit squash merging only. No required check was invented or workflow started;
  automated checks were not requested. The owner is the sole maintainer, so zero
  mandatory approving reviews avoids an impossible self-approval requirement.
- Open [PR #1](https://github.com/umbertotechnopreneur/MarkVibe/pull/1), assign it to
  `umbertotechnopreneur`, label it `enhancement` and `documentation`, and attach it
  to the Codex task. Leave it open for owner review; no PR merge is claimed.
- Copy the complete checkout, Git backups and Debug artifacts to the previously
  absent `E:\MarkVibe` using non-destructive recursive copy with junction exclusion.
  Preserve `E:\Research\MarkVibe`. The initial copy reported zero failures; verify
  matching Git HEAD and selected source, solution, package and index SHA-256 hashes.
  The owner will change the Codex project folder link; do not alter app metadata.

## 2026-10-16 — Selectable AI connections and Debug 0.1.0.24

- Add persisted OpenAI API key/Codex CLI radio selection, provider-specific status,
  one Test button per connection and localized pass/fail dialogs. Keep API as the
  default and reject unavailable or unknown providers without silent fallback.
  Cancel obsolete provider/edit operations before accepting a replacement result.
- API tests use model-list authentication and preserve an existing key on failure.
  Windows CLI tests request a tiny structured synthetic response through the
  installed CLI login; CLI editing receives the same captured target and explicit
  read-only references. Browser CLI support fails with an explicit desktop notice.
  Keep requests bounded and ephemeral, disable tools/plugins, reject tool events
  and terminate the process tree on cancellation or deadline. Read-only does not
  claim complete OS isolation for filesystem reads; CLI retention remains distinct
  from API store:false. Catalog IDs and token estimates do not prove CLI access.
- Rebuild/sign MSI 0.1.24, MSIX/bundle and portable 0.1.0.24 for Debug x64 after
  cleaning artifacts. Trust matching development certificate
  185EE4C6A8786F8CCD5A31D9A0771DF2FCC75F6B, install the MSIX and launch its installed
  executable. Windows reports exact version, x64 and Status Ok; record deployment
  separately in INSTALL-INFO.json. No provider Test action, inference, automated
  suite, check executable or GUI acceptance was executed. MSI installation and
  Visual Studio F5 remain unverified.
- The owner explicitly approved branch `codex/prepare-public-repository` and
  commit/push/PR delivery with initial-history consolidation. Create that branch
  without resetting the prepared work. Preserve recoverable history/dirty-state
  backups outside artifacts and retain published release tags.

## 2026-10-16 — Repository preparation, Visual Studio and Debug 0.1.0.23

- Rewrite README around the owner's weekend-project origin, concise capabilities
  and an illustrative Mermaid document map, without screenshots. Apply the supplied
  VibeWare templates: unchanged approved logo, accessible header badge, first-person
  About disclosure, canonical links and a layout-preserving 30-second/3-second
  footer that reveals on hover or keyboard focus. Localize new labels in all nine
  supported languages. Remove obsolete About claims about ink/annotations.
- Retain MIT application licensing and record actual dependency boundaries,
  including EPL-2.0 elkjs source availability, the chosen Apache-2.0 DOMPurify
  option and WebView2's SDK notice. Include bundled legal notices in client
  freshness detection. No claim that dependencies or trademarks become MIT.
- Use three agents for C# exploration, client graph effects and MSI packaging.
  Retain unchanged control previews, validate AI references against one catalog
  snapshot, cancel replaced SVG effects/stale selection geometry, reduce repeated
  DOM updates and remove unused or unreachable helpers. Preserve document data.
- Add native Windows Installer COM/makecab MSI generation with stable identities,
  per-user installation tables, retained user data and explicit version mapping.
  Correct the RemoveFile row found during the first actual packaging attempt;
  regenerate from a clean artifacts directory. Create and sign the MSI 0.1.23
  containing 908 payload files, plus MSIX/bundle and portable for app 0.1.0.23.
  Inspect MSI product/version metadata without installing it.
- Add a classic Visual Studio solution aligned with .slnx, Desktop/Browser launch
  profiles and VS-only client preparation. Restore and build all seven projects
  in Debug with zero errors and seven existing NU1900 cache warnings. Check
  executables compile but are not run. Visual Studio/F5 acceptance remains open.
- Install Debug x64 0.1.0.23 with development certificate
  58EA02A2C75389899AA13BB8D97B0CC8F6265039 in LocalMachine/TrustedPeople.
  Verify installed identity/version/architecture and Windows Status Ok, then launch
  the installed executable. Separate BUILD-INFO.json and INSTALL-INFO.json record
  generation and deployment. No interactive checks, suites or inference ran.
- The owner confirmed that Partner Center identity is not yet available and
  narrowed MSIX delivery to local Debug. Document future Store prerequisites;
  do not invent the identity, create a Store/Release build or submit anything.
  Git consolidation, protection and PR remain pending the required branch consent.

## 2026-10-16 — Useful map effects and Debug 0.1.0.22

- Implement all six owner-approved exploration ideas. Resting cards use simpler
  opaque surfaces, medium-weight titles and shorter excerpts; active cards reveal
  detail. Directional pulses run once for up to 24 adjacent links, using distinct
  incoming/outgoing colors, line styles and arrows. Hover/keyboard focus previews
  a relation lens without modifying saved positions.
- Add temporary C# exploration state, Ctrl/Cmd-click and Shift-drag selection,
  a bounded 12-document shelf, side-by-side catalog comparison and atomic AI
  reference batches. Validate the existing 16-reference/4 MiB limits before any
  collection membership changes. These actions invoke no inference.
- Record only successful navigation in a ten-visit trail. Chips locate documents;
  subdued dotted geometry joins consecutive visible visits. Preview/hover/centering
  do not add history. Calculate missing Markdown targets and isolation over the
  full catalog before projection; suppress isolation while metadata is incomplete.
  Changed documents mean observed fingerprint/local revision changes in the current
  session, not filesystem timestamps. Cache facts by catalog metadata revision.
- Keep source documents and sidecar schema unchanged. Finite effects respect
  reduced motion and cancel on suspension, visibility/session changes or disposal.
  No new blur, SVG filters, background engine or continuous animation loop is used.
- Bundle and compile Debug x64; sign/install version 0.1.0.22 with matching cert
  43E2B2B088F66242EE4206B723F230C1830C1CEE in LocalMachine/TrustedPeople.
  Windows confirms the version, x64 and Status Ok; launch the installed executable.
  BUILD-INFO.json and separate INSTALL-INFO.json record delivery. No automated
  suites, interactive checks, inference or publication ran. Existing NU1900
  vulnerability-cache warnings remain. Owner GUI acceptance is pending.
- Disregard the owner's retracted timeline/WebGL message and its associated Git
  request; no unrelated engine research, commit or push was performed.

## 2026-10-16 — Map inspector follow-up and Debug 0.1.0.21

- Restore the missing strokes on all 24 XAML map symbols, keep hollow geometry
  hollow, restore musical notes and globe meridians, and enlarge gallery choices
  to 22 px. Complete missing detail strokes on other reused interface glyphs and
  redraw HTML/PDF export symbols with distinct code/document silhouettes.
- Recognize the second document click by browser click count or the same path,
  nearby pointer coordinates and a 500 ms fallback window. Selection target changes
  no longer require a native dblclick event. Share activation with overview labels;
  retain single-click selection, Enter opening and gesture/session guards.
- Rename the distance caption, widen its selector from 120 to 200 px, and use
  explicit 12 px typography for viewpoint inputs and dropdown items. Give the
  view-name field 160 px so the larger matching watermark has space.
- Use bordered inspector buttons with local icons and shorter AI captions. Make
  incoming/outgoing sections initially collapsed, retaining their expansion while
  the inspector refreshes during the session. Separate route status from the next
  section with bottom spacing. Add XML/documentation comments to maintained code.
- Build/sign/install Debug x64 0.1.0.21 with the matching development certificate
  7BB013983A2D5379B421A83E104940F33A97AAD8 in LocalMachine/TrustedPeople. Windows
  confirms version, architecture and Status Ok; launch the installed executable.
  Repository artifacts include BUILD-INFO.json and separate INSTALL-INFO.json.
  No interactive checks or test suites ran; the owner performs GUI acceptance.
  No inference or publication ran. Existing NU1900 audit-cache warnings persist.

## 2026-10-16 — Composed Windows host and Debug 0.1.0.20

- A later owner screenshot showed gray native redraw corruption still present
  after 0.1.0.18. Retained cards and black DOM computed styles did not establish
  that native presentation was correct; the earlier acceptance boundary is limited.
- Replace the visible HWND-hosted browser with WebView2CompositionControl and keep
  the hidden PDF WebView unchanged. Target Windows 10 APIs explicitly and pin the
  existing 10.0.19041.57 reference pack so its Windows Runtime assembly is included.
  A startup check caught that missing assembly in the undelivered 0.1.0.19 package.
- Contain map painting within its opaque viewport; remove SVG drop-shadow filters
  and minimap backdrop blur while retaining shared modal material. Synthetic
  visible-window observation did not reproduce the owner's exact gray failure;
  its cause and resolution on the real workspace are not proven by those checks.
- Build/sign/install 0.1.0.20 Debug x64. Verify package Status Ok, exact certificate
  F26E7618CE8CC747858651927D35F18327CDA58B, the Windows Runtime assembly in the package,
  and successful startup with a responding WindowsApps process. No suite, inference
  or publication ran. NuGet audit-cache retrieval produced NU1900; earlier package
  advisories remain open.
- The owner requested that GUI checks be left to him because they interrupted
  active work. Stop interactive checks and record that rule in AGENTS.md. Owner
  acceptance remains in todo.md; no further window/input/capture operation is authorized
  without a new explicit request.

## 2026-10-16 — Stable map navigation, semilight UI and Debug 0.1.0.18

- Complete the requested three-agent pass: saved map-inspector splitter and
  documented content builders, shared theme brushes for existing input captions,
  and readable AI state/request/reference flows with XML documentation.
- Fix opening blur through stable template elements; inspect 16 px backdrop blur
  while Settings stays open. Inspect checkbox, menu and status colors through
  light/dark/light changes. Use Semilight (350) body text and Medium (500)
  emphasis, preserving caption bindings and document/editor typography.
- Resize the inspector from 290 to 400 px and observe restoration to 400 px after
  terminating and restarting the diagnostic desktop process. Retain the separate
  persisted AI, sidebar and source/preview layout implemented in 0.1.0.15.
- Investigate the owner's map flicker report. Sidebar navigation recreated every
  card and replayed entrance animations. Route it through retained selection,
  refresh changed metadata in place, and recognize replacement workspace sessions.
  Keep the map canvas opaque black and use equivalent planar depth scaling so
  hover/selection do not switch between 3D and 2D transforms.
- Inspect Debug 0.1.0.18 with 24 synthetic linked documents: sidebar navigation
  retains all 24 card elements, repeats no entrance animation once the initial
  presentation settles, and keeps the canvas black. Direct card selection also
  retains all cards. This focused observation does not establish universal GPU
  behavior or full atlas acceptance.
- Bundle/build/sign/install Debug x64 0.1.0.18 with existing tools. Verify current
  user package Status Ok, exact development certificate in LocalMachine/TrustedPeople,
  and a responding visible process running from that version's WindowsApps path.
  Save separate ignored artifacts/INSTALL-INFO.json. No test suite, inference,
  Git delivery or publication ran. Existing NU1902/NU1903 warnings remain tracked.

## 2026-10-16 — Docked assistant and Debug installation

- Replace the floating assistant with a fixed right-hand pane, standard splitter,
  closed first-run default and saved open state/width. Retain sidebar geometry and
  the source/preview ratio separately from appearance preferences.
- Collapse references into one disclosure row, keep the response above the composer,
  and show one next-request estimate. Move guidance/accounting to accessible blue
  information buttons; keep missing-context/input-limit warnings visible.
- Remove title dragging, compact mode, floating geometry and document spacer CSS;
  reuse unchanged reference rows instead of rebuilding them while typing.
- Build/sign Debug x64 0.1.0.15 using existing Node 24.19.0 and .NET SDK 10.0.402.
  Import that build's development certificate and update the current-user package.
  Windows reports version 0.1.0.15, Status Ok; its installed executable launched
  and displayed the reader. No test suite or live inference ran. Existing
  NU1902/NU1903 dependency advisories remain separate maintenance work.
- Record standing owner authorization for local Debug forced closure, discarding
  unsaved in-app work, certificate trust and installation in AGENTS.md. Source
  documents and unrelated data/certificates remain outside that authorization.
- The owner then reported missing settings blur while open and requested a saved
  map-inspector splitter, complete theme refresh and three-agent readability work.
  Those follow-ups are tracked separately; installation is not their acceptance.

## 2026-10-16 — Authorized 0.1.0.13 local installation

- Receive explicit owner consent for development certificate
  E5C9ACC815B3399618A25C0B6F944C0E301216C1, publisher CN=Umberto Giacobbi,
  in LocalMachine/TrustedPeople and installation of the 0.1.0.13 update.
- Verify the bundle against its build checksum and the certificate against the
  authorized thumbprint/publisher. Import that exact certificate through the
  prepared elevated helper, then install the bundle for the current Windows user.
- Windows confirms UmbertoGiacobbi.MarkVibe 0.1.0.13, X64, Status Ok. The existing
  direct desktop preview remains open. Record deployment in the ignored
  artifacts/installation-result-0.1.0.13.json without rewriting build checksums.
  No tests, API validation or functional acceptance were performed.

## 2026-10-16 — Document atlas implementation and local checkpoints

- Implement the seven owner-approved map improvements: semantic zoom and readable
  overview labels; collapsible colored folder islands; managed 0–3-link lenses;
  directed source excerpts and full-catalog shortest routes; document preview and
  section navigation; minimap and eight named viewpoints; floating map controls
  and finite arrangement transitions with reduced-motion support.
- Extend schema 4 with optional validated atlas settings, retaining existing card
  coordinates, strict parsing, revision-checked persistence and save/download
  recovery. Reset preserves saved viewpoints. Retain incoming/outgoing link-row
  work already present in the shared map files.
- Review JavaScript/C# syntax, XAML/JSON structure, localization array widths and
  whitespace. No bundle/build, tests, app launch, installation, inference or
  publication was requested. Runtime/visual acceptance remains in todo.md.
- The owner requested local checkpoint commits on the existing branch. Include
  existing local prerequisites in shared map files, leaving unrelated changes
  outside the checkpoints. These are work-in-progress checkpoints, not evidence
  of standalone build success.
  Model/persistence checkpoint: `84e44d5`. Interactive UI checkpoint: `3dbbedd`.

## 2026-10-16 - Human-readable code standard and delegated cleanup

- Strengthen `AGENTS.md` and `CONTRIBUTING.md` for every future change and maintained
  code: document classes/types and methods, use C# XML documentation, explain
  complex assumptions and invariants, choose descriptive names and separate
  logical phases with blank lines. Expand dense blocks and long expressions;
  use intermediate variables only when they help readers understand the code.
- Delegate a focused cleanup of `client/source.js`,
  `client/document-selection.js` and `scripts/client-inputs.mjs`. Add whitespace,
  explicit control-flow blocks, readable argument/object layouts and targeted
  source-selection/cache comments plus the `SourceHost` class documentation.
  Keep local names, public contracts and concurrent file-context work intact.
- Review the diff and pass `git diff --check`. The delegated agent checks syntax
  with existing Node 24.19.0 and compares in-memory esbuild-normalized output
  before/after with identifier minification disabled; all three comparisons match.
  These are static checks, not runtime acceptance. No build, bundle, tests,
  installation, branch/worktree creation or publication was performed for this task.

## 2026-10-16 — Sidebar feedback, modal blur and initial map tree

- Add shared Pages/Outline row feedback, contrasting selected text and distinct
  hover backgrounds; keep selection visible without focus and recolor immediately
  when the application appearance changes. Preserve caller-owned document labels.
- Blur the explicitly marked application surface behind ChildWindow popup portals,
  with 300 ms transitions, reduced-motion support and exact modal registration
  cleanup. Keep modal content sharp and retain nested/canceled-close behavior.
- Use Tree only for maps with no loaded layout; preserve saved positions and
  customization. Reset returns to Tree. Restyle incoming/outgoing links with
  direction arrows, target counts, document titles/icons and secondary paths.
- Add assistant capability/workflow help as displayed reviews, never automatic
  document replacements. No live inference was requested or invoked.
- Parse changed XAML/JSON and review whitespace. Build/sign **0.1.0.12 Debug x64**
  MSIX/bundle/portable and directly launch the desktop preview (observed window
  and process). Existing NU1902/NU1903 warnings remain. No test suite or functional
  UI exercise ran. The installed version remains 0.1.0.11 until the owner approves
  the newer package's distinct development certificate.

- Follow-up: the owner reports the Settings background still sharp and rejects
  the OpenAI card. Replace the card/badge with a flat settings section. Supersede
  the application-surface blur registration with a direct full-window Overlay
  blur, opacity 1 and translucent brush; keep backdrop and content animations
  separate. The previous source/build record is not visual acceptance of blur.
  Build/sign **0.1.0.13 Debug x64** successfully and launch the direct desktop
  preview. Keep the owner-requested functional testing separate: no suite or API
  validation runs. Retain existing NuGet warnings. Save a local source checkpoint
  without moving the active branch or consuming the real staging index.
  The recovery commit is `8fe938b6159fe5a842e21f1ce7e2e9593782bd3d`, retained by
  local tag `checkpoint/markvibe-0.1.0.13`. It captures current pending source work;
  it is a recovery snapshot rather than a published or merged delivery.

## 2026-10-16 — Appearance gallery and file context actions

- Replace the card icon dropdown with a visible responsive gallery, selected
  color rings, named choices and a preview. Reflect accepted managed state.
- Share Explorer, terminal, standalone browser preview and delete actions across
  map cards, navigation, reader/source and AI collection rows. Resolve native
  locations through the opened workspace and pass terminal directories as data.
- Reuse one frosted confirmation for map reset and exact-target deletion. Verify
  file bytes/metadata after confirmation, request one-file recycling through the
  Windows Shell and refresh catalog, retained drafts, map and AI collection.
  Disable external file actions in the isolated smoke profile.
- Parse XAML/JSON, check JavaScript syntax and whitespace, then build/sign
  **0.1.0.11 Debug x64** MSIX/bundle/portable. No test suites or live file actions
  were exercised; the owner explicitly reserved manual testing.
- The first installation attempt reports untrusted certificate 0x800B0109;
  automatic approval review rejects machine-wide trust without explicit consent.
  After the owner explicitly authorizes that security change and installation,
  trust certificate 3A2C63F5FF479F33571B27CEA8AE305DF9279C54 in
  LocalMachine/TrustedPeople and install the 0.1.0.11 bundle. Windows package
  registration reports version 0.1.0.11, x64, Status Ok. This confirms installation,
  not functional acceptance; no test suite or live file action was run.

## 2026-10-16 — Stronger graduated depth

- Combine full-catalog graph distance with logarithmic distance on the map, using
  up to twelve recessed levels so equal graph distances and disconnected pages
  still retain spatial variation. Keep saved coordinates unchanged.
- Increase selected-card magnification to 1.28 and strengthen its accent outline
  and glow. Fade whole cards and all lines toward their furthest endpoint,
  including lines from the selection. Hover retains readable foreground emphasis.
- Recompute planes after arrangement and completed moves while keeping active
  pointer gestures stable. Share selection scale with fitted bounds and animated
  search centering so enlarged cards remain inside the camera frame.
- Check JavaScript syntax and whitespace; build and development-sign
  **0.1.0.10 Debug x64** MSIX/bundle/portable and launch the build output directly.
  Open the map and inspect the current depth presentation visually. Existing
  NU1902/NU1903 warnings remain. No test suite, app/certificate installation,
  live inference or publication was performed; no unsaved work was force-discarded.

## 2026-10-16 — Inline map controls, tree layout and depth planes

- Move map controls onto the black canvas and keep the localized search watermark
  on the same row. Retain a visible search field while action captions compact
  and overflow can scroll. Give Read/Source/Split/Map/AI rounded selector surfaces.
- Share connection ranking and breadth-first traversal in Core DocumentGraph.
  Add Tree arrangement with cycle-safe levels, broad-level wrapping and a bounded
  grid fallback for pathological depth. Preserve existing card sizes and styles.
- Add switchable perspective planes based on shortest full-catalog distances
  from the selection. Bound shrinking/opacity, bring hovered or focused cards
  forward, follow animated card bounds with edges and honor reduced motion.
  Saved coordinates and document content remain independent of visual depth.
- Parse the changed XAML/JSON and check JavaScript syntax. Build and sign
  **0.1.0.9 Debug x64** MSIX/bundle/portable, then launch the build output directly.
  Inspect the rounded tabs and opened map visually; screenshots are under ignored
  `artifacts/`. Existing NU1902/NU1903 warnings remain. No test suite, installation,
  live inference or publication was performed.

## 2026-10-16 — Map surface refinement and startup restore

- Refine cards to a nearly black surface with two subtle static radial gradients.
  Keep the accent selection shadow and put the icon beside the document title.
  Replace clipped statistics labels with a monospace size/token row and compact
  icon counters for characters, words, lines and links, with full accessible labels.
- Use data-bound icon/caption templates in the appearance selector so the closed
  selection and dropdown rows show the actual local icon. Add a visible field label.
- Correct reader-only startup restoration and source/map navigation: render or
  restore only mounted views. A 0.1.0.7 screenshot exposed a hidden source host
  request; the fix also covers AI commits while the reader is collapsed.
- Resolve npm through the selected Node executable, avoiding a Windows npm shim
  silently using its older sibling runtime. The successful follow-up packaging
  used the existing Node 24 and .NET SDK 10.0.402.
- After the native unsaved-work guard blocked closing, obtain explicit permission
  to discard and restart. Build and development-sign **0.1.0.8 Debug x64**, then
  launch its build executable directly. Existing NU1902/NU1903 warnings remain;
  no test suite, live inference, installation or publication was performed.

## 2026-10-16 — Unified document model, map discovery and AI collection

- Implement the review follow-up with Core-owned original/current/exported source,
  bounded undo/redo, consistent catalog metadata after cache eviction and guards
  against stale asynchronous commits. Remove unused global search and legacy map
  schemas/notes without deleting user sidecars. Map schema 4 is the sole contract.
- Make map persistence failures visible, share UI traversal/font catalogs, preserve
  raw document labels and remove duplicated or retired presentation helpers.
- Add title/path map search across the complete catalog, cancellable animated
  centering and a folder arrangement with visual frames. Positions persist;
  folder-frame visibility belongs to the current workspace session.
- Add an explicit workspace AI collection with at most 16 paths, a 4 MiB reference
  limit, per-document estimates and a visible next-request total. Resolve current
  revisions before preflight, exclude the editing target from reference duplication
  and include references in the existing context limit. Only the open target can
  receive an edit result; selecting collection members sends no AI request.
- Add the simple verification menu, shared reports, current synthetic regression
  fixtures, locked Playwright and deterministic npm/bundle preparation. Native
  smoke mode cannot access saved AI keys or call OpenAI. Test suites were not run.
- On the owner's request to see this version, bundle, publish and development-sign
  **0.1.0.7 Debug x64** as MSIX, MSIX bundle and portable ZIP. Launch the build
  output directly and verify the live `MarkVibe` desktop window. Existing
  NU1902/NU1903 dependency warnings remain. No app/certificate installation or
  publication occurred. UI interactions and live inference remain unverified.

## 2026-10-16 — Maintainability and verification review

- Review the current working tree through three parallel Core/Native, UI/interop
  and tooling analyses, then reconcile findings and counterexamples against source.
- Record visible map-save failures, draft/catalog divergence, UI font/localization
  gaps, asynchronous reader-position risk, stale UI scenarios and build-input drift
  in `docs/maintainability-review-2026-10-16.md`. Global search is disconnected from
  the current UI; its truncation inconsistency is latent, not an exposed UI failure.
- Propose one current map schema, explicit state ownership and a simple local
  verification menu sharing a runner with manual CI. No refactor was implemented.
- Update only review/task documentation. No build, tests, bundle, app launch,
  credentials, API requests, installation, branch creation or publication.

## 2026-10-16 — Interactive map cards and application settings

- Repair map-state updates/autosave by removing the retired, undefined note
  callback. Add connection hover with a stable screen-space hit area, single-click
  selection, double-click opening, animated magnification and persistent resize
  grips. Update edge geometry with the visible card and preserve pending edits.
- Add left-aligned icon controls, confirmed Reset all and advanced full-source
  statistics in a monospace strip. Token counts remain local estimates. Version 3
  stores bounded dimensions; older layouts and legacy metadata remain supported.
  Large arrangements add circles/spokes instead of clamping overlapping cards.
- Add shared native frosted dialogs with 300 ms backdrop entry/exit and reduced
  motion, including safe canceled-close handling. Improve the masked OpenAI key
  form with explicit presence/validation state. Add one UI-font preference using
  installed supported Windows monospace families, independent of document fonts.
- Source reviewed across delegated agents. JavaScript syntax, JSON/XAML parsing,
  translation shape/duplicates and Git whitespace checks passed. No build, bundle,
  automated tests, app launch, credential access, API requests, installation or
  publication ran for this follow-up. Runtime/UI behavior is not yet verified.

## 2026-10-16 — Assistant identity below the model selector

- After the owner confirmed MarkVibe as the target, add the existing sparkles
  icon and the catalog-backed Markdown-assistant caption below the model selector.
  Preserve the current colors and the response/composer/budget arrangement.
- The owner explicitly approved closing the previous instance and discarding its
  unfinished work after the native close guard stopped the restart.
- Build and development-sign x64 Debug 0.1.0.6. Inspect the actual rebuilt
  WebView2 UI with an intercepted synthetic reply; the icon and Italian caption
  appear below the model selector and the response remains above the composer.
  Existing NU1902/NU1903 warnings remain. No installer/certificate was installed.
- The prior 21 client/Core/Native checks remain scoped to the earlier assistant
  update; the icon follow-up adds no tests and changes no request behavior.

## 2026-10-16 — Cinematic document map and automatic arrangements

- Add a pure black map surface, dark tinted cards, illuminated selected connections
  and an application-accent selection shadow with 16-pixel elevation/48-pixel blur.
- Zoom with the ordinary wheel around the pointer. Reveal cards progressively
  from the center, cap stagger at one second and respect reduced motion/focus.
  Preserve scrollable compact layouts and hold scale steady during dragging.
- Add localized native XAML Star/Circle/Grid commands. C# ranks distinct neighbors,
  prefers entry documents for ties, computes spaced layouts and saves one validated
  mutation. Preserve loaded positions until explicit arrangement, card appearance
  and legacy metadata. Keep the hub and eligible selection inside the 120-card cap.
- Evidence: source review including an independent delegated review, JavaScript
  syntax validation, XAML/JSON parsing and whitespace checks. No build, bundle,
  automated tests, runtime inspection, installation or publication was requested
  or performed for this change. Running-app appearance and behavior are unverified.

## 2026-10-16 — Scrollable assistant response and localized disclosures

- Put the assistant response above the composer in its own scroll viewer; keep
  the prompt, model/send controls and budget accessible. Preserve scroll position
  during state updates and keep the compact prompt height bounded.
- Retain dragging and 52-pixel compact mode. Both panel and budget chevrons point
  down when expanded and up when collapsed. Complete assistant labels in the
  shared nine-language catalog without translating document-owned reply text.
- After 10.0.401 disappeared from the machine, the owner approved the existing
  SDK 10.0.402 and the exact project-pin update. Build and development-sign only
  x64 Debug 0.1.0.5. Existing NU1902/NU1903 dependency advisories remain.
- Client (9), Core (7) and Native (5) checks passed again. Inspect the actual
  WebView2 UI with an isolated synthetic Markdown/profile and intercepted AI
  replies: response scrolling, fixed composer, budget disclosure, compact/restore
  and 1236x783/640x720 layouts. Prompt text survives compaction; the response
  keeps its reading position. No live inference or app/certificate installation.
- The interface already supports nine languages. Locale is not passed to the
  model request; generated-response language remains a separate behavior change.

## 2026-10-16 — Local x64 Debug packaging and launch

- Record the owner's local default: the existing MSIX format, x64 Debug and
  launch the newly built desktop executable. Release/ARM64 remain explicit.
  Add selectable packaging architectures and retain explicit x64/ARM64 CI input.
- After the owner approved wasm-tools installation, align the WebAssembly pack
  lock to 10.0.13. Use existing .NET 10.0.401, Node 24.19.0 and Windows SDK tools.
- Build and development-sign x64 Debug 0.1.0.4; leave its visible, responding
  desktop process running. File version is 0.1.0.4. Client (9), Core (7) and
  Native (5) checks passed before packaging. No app/certificate installation,
  OpenAI inference or publication was performed by this task.
- NuGet reported existing NU1902/NU1903 advisories for transitive
  System.Security.Cryptography.Pkcs 8.0.1 and Xml 8.0.4. Dependency remediation
  and obsolete ink-based browser/Windows smoke scenarios remain separate work.
- Release 0.1.0.3 completed before the owner's clarification; the subsequent
  requested Debug build replaced its artifacts through the documented cleanup.

## 2026-10-11 — Stateless Markdown AI editing and context/cost UI

- Add a reusable AI component gated by the saved OpenAI key, with a dismissible
  Settings banner, Markdown prompt preview, model popup and five reasoning stops.
  Send only the current prompt plus whole document or exact selection; automatically
  apply complete structured edits with revision checks and local undo, never history.
- Send separate system messages describing MarkVibe and enforcing Markdown-only
  work. Refuse images/media, new books and unrelated bulk manuscripts; grant no
  tools, explicitly use Standard service and disable stored Responses.
- Download the official public model/pricing snapshot to embedded JSON with dated
  sources, complete context/input/output limits, Standard prices and long-context
  multipliers. Expose semantic Cheap/Affordable/Prohibitive labels without prices.
- Preflight cumulative input tokens before generation. Use the full model context,
  reserve its maximum response/reasoning, block oversized or large unverified input,
  and expose context occupancy plus exact totals/labelled input estimates.
- Show a themed elevated document popup, dimmed 16 px backdrop blur, countdown,
  Cancel and a total 60-second deadline. Abort browser/native work and discard late
  replies; no automatic retry can repeat billed generation.
- Map rendered literal selections and raw Monaco/textarea ranges, including mixed
  line endings. Keep active gestures and pending notes safe during source commits,
  protect original sidecars, and save edited Markdown only through an explicit copy.
- Validation: public catalog download executed; JavaScript syntax, XML/JSON,
  localization structure/placeholders, source contracts, installed Markdig renderer
  metadata, headers/LF and whitespace inspected statically. No bundle/build, tests,
  live key access, inference, app/UI launch, installation or publication performed.
  Runtime acceptance remains in active work; prior dirty changes are preserved.

## 2026-10-11 — OpenAI API-key validation and host-specific storage

- Add a masked Settings field and localized Validate action. An authenticated
  `GET https://api.openai.com/v1/models` checks key/model-list access before saving;
  it sends no documents and generates no text. Inference and billing remain separate.
- In the authenticated WebView2 host, validate natively and save to the Windows
  user `OPENAI_API_KEY` environment variable, also updating the current process.
  The browser uses `markvibe.openai.apiKey` in this site's unencrypted `localStorage`
  and shows the requested insecure-storage warning before validation.
- Keep keys out of general preferences, status replies, sidecars and exports;
  use fixed error messages, preserve the saved key when validation fails, clear
  the password control after success and cancel requests when Settings closes.
- Static validation: C# syntax for DEBUG/release, native/core type and API
  resolution without emitting an assembly, JavaScript parsing, XAML controls/event
  handlers, 25 messages across the nine UI languages, source headers/LF and clean
  whitespace diff. No build, tests, live OpenAI call, app launch or publication.
  Runtime/CORS, persistence and visual acceptance remain in active work.

## 2026-10-11 — Readability applied to existing maintained code

- Review 34 C# files, 27 JavaScript/modules and three PowerShell scripts. Add
  purpose comments to methods/functions, explain complex steps, use clearer local
  names and expand dense statements and long expressions. Preserve public and
  wire names, document data, algorithms and third-party notices.
- Static comparison covered 331 named C# methods and 315 JavaScript functions,
  preserving executable tokens/syntax while allowing scoped local renames,
  split declarations and equivalent control blocks. PowerShell syntax and
  executable-token comparisons passed. No application code or test suite ran.
- Protect source writes against concurrent edits and retain newer sticky-note
  work. Keep review helpers/snapshots under ignored repository `artifacts/`.
  Update the VibeWare provenance and AI disclosure; add the authorized OpenAI
  behavior after the readability comparison checkpoint.

## 2026-10-11 — Reusable sticky-note editor and persistence

- Reuse one graph/reader note component with title, Markdown body, formatting
  actions, safe preview, dragging, keyboard movement, undo/redo and the shared
  16-color/24-icon appearance picker.
- Connect graph notes to root map JSON version 2 and reader notes to matching
  `<document path>.notes.json` sidecars. Support source-element/image anchors,
  responsive positioning, unresolved-note recovery and explicit reattachment.
- Add native and connected-browser saves with document/revision checks, explicit
  browser write grants, imported copies, conflict preservation and discard.
- Drain local drafts before navigation/export; retain pending detached editors,
  reject obsolete sessions and preserve rejected mutations as recoverable drafts.
- Include colored, formatted note boxes and self-contained icons in HTML/PDF.
- Checkpoints: map contract `6b7cb6e`, card UI `303902e`, note contracts `d077f81`.
  Source review, JavaScript syntax, schema/XML and whitespace checks only. Tests,
  build, bundle, app launch, installation and publication were not requested.

## 2026-10-11 — Sticky-note contracts and Markdown targets

- Add bounded C# note/anchor records, strict document-note JSON and shared undo/redo
  state. Validate complete file budgets before mutations without rejecting compact
  valid files during loading.
- Extend map JSON with version 2 notes while preserving version 1 until a note edit.
- Catalog nested Markdown source elements and conservatively resolve anchors;
  preserve existing ink identities and per-anchor document fingerprint evidence.
- Add contained native sidecar reads, document/revision-checked atomic writes and
  explicit note backup destinations that cannot replace Markdown.
- Astra source review and JSON/whitespace structure checks only. No tests, build,
  bundle, app launch, packaging, installation or publication for this milestone.

## 2026-10-11 — Persistent document-map card layout

- Store logical positions, 16 colors and 24 original local SVG icons per top-level
  workspace in the bounded, versioned `.markvibe-map.json` contract.
- Drag cards with live connections; add right-click/touch/keyboard appearance
  selection and keyboard movement. Filtering/focus preserve catalog coordinates.
- Native atomic autosave, connected-browser explicit write grant, imported-layout
  download and unsupported-file/revision conflict preservation.
- Drain pending edits before navigation; suspend map input during provider
  replacement and reject stale layout-session events/state.
- Checkpoint `6b7cb6e` records the core/native persistence contract. UI integration
  has source/Astra review, JavaScript syntax and JSON/SVG structure checks only.
  No tests, build, bundle, app launch, installation or publication for this task.

## 2026-10-11 — Reusable sticky-note feasibility

- Astra reviewed a shared C# note model and DOM editor for graph and Markdown,
  alongside existing paragraph ink. Proposal recorded in `docs/notes-design.md`.
- Reuse the graph appearance picker; notes have a separate title and a formatted
  Markdown body, rendered through the existing safe parsing boundary.
- Graph notes need a version 2 map schema/migration; reader notes need generic
  Markdown element anchors/local offsets and `<full document path>.notes.json`
  sidecars. Include headings, images and nested targets while preserving ink IDs.
- Input isolation, conflicts, close/navigation flushing and export inclusion are
  required integration work. Sticky notes are not implemented; no execution checks.

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

## 2026-10-11 — Readability, VibeWare attribution and AI disclosure

- Record useful method/function comments, explanations inside complex methods,
  descriptive names and readable code in `AGENTS.md` and `CONTRIBUTING.md`.
- Add MIT VibeWare headers with exact `VBWR B` / `VBWR E` markers to all 89
  maintained source, script and markup files. Record the attribution edit's known
  provenance; original per-file creation history remains unestablished.
- Preserve each pre-existing source body byte-for-byte, declaration placement,
  LF endings, generated output and third-party assets' existing notices.
- Replace the README footer with the exact supplied template; add a disclosure
  section and `AI-DEVELOPMENT-DISCLOSURE.md` with current and historical evidence.
- Static verification: headers/MIT/LF in 89/89 files, 25 XML documents parsed,
  3 PowerShell scripts parsed without execution, 23 local documentation links
  resolved, exact footer match and clean working/staged whitespace checks.
- No method/naming refactor, tests, build, bundle, app launch, packaging,
  installation or publication was requested or performed for this update.

## 2026-10-11 — Responsive dialogs, reader themes and multilingual Acrylic UI

- Replace the settings sidebar with a modal using two columns by default,
  compact two-column action grids and live reflow below 680 pixels.
- Centralize XAML resources by purpose: semantic brushes, typography, buttons,
  segmented navigation tabs and dialogs with system-accent elevation-16 shadows.
- Add local imagegen toolbar icons, the selected segmented sidebar, document
  toolbar placement, icon-only map zoom and a WorkTrail-inspired About layout.
  Use the owner's VibeWare logo and manifesto footer; keep Close text-only.
- Share translations between C# and JavaScript for English, Italian, French,
  Vietnamese, Chinese, Cantonese, Indonesian, Korean and Japanese. Prefer the
  system language while allowing a persisted override.
- Add native Windows Acrylic with solid fallbacks; keep document surfaces opaque.
- Import 15 preview and 22 code themes with notices, local font stacks, independent
  light/dark selections and matching standalone export presentation.
- Evidence: client bundle and solution build succeeded with zero warnings/errors.
  No tests, app launch, installation, packaging or publication were performed.
  Runtime layout, translation review, Acrylic and themed exports await requested
  visual acceptance. Checkpoints: assets 8eada89; shared themes/dialogs 7245e6e.

## 2026-10-11 — XAML application controls and annotation retirement

- Replace DOM application controls with OpenSilver UserControls: AI composer,
  model/effort flyout, review, waiting overlay, source/map toolbars and map appearance.
  Markdown/Mermaid, Monaco and graph drawing remain specialized browser renderers.
- Retire pen, annotations and sticky-note UI, managed editing sessions, native write
  endpoints and annotation exports. Preserve existing user sidecars and legacy map data.
- Use a native file TreeView, semantic document outline and reusable XAML vectors;
  Microsoft Fluent navigation/chevron geometry retains its MIT attribution.
- Apply selected proposal 5, Obsidian Focus. Add the draggable AI Assistant title bar,
  52-pixel compact mode and restored 260-pixel composer. Native flyouts stay on screen.
- Decorate existing XAML surfaces with 16-pixel CSS backdrop blur and an accent shadow
  with 16-pixel depth and 48-pixel blur; do not create replacement HTML controls.
- Evidence: client bundle and full solution build succeeded with zero warnings/errors.
  Direct desktop inspection covered 1236x783 and 640x720 layouts, two-column settings,
  About, native model/map popups, title dragging, compact/expand and prompt retention.
  Monaco filled 627 pixels; four map cards retained 260-pixel width. Specialized
  reader/source/map roots contained no application buttons.
- No automated tests, OpenAI inference, installation, packaging or publication ran.
  API request behavior and native file-picker acceptance remain separate follow-up work.
- Resource/vector foundation checkpoint: bcc3008. This migration is the next checkpoint.

## 2026-10-11 — Combined model selector and stable reasoning controls

- Remove the separate title-bar rectangle and divider; align its text with the
  composer inset and retain native drag, compact and close behavior.
- Combine model and reasoning effort into one XAML button. Open its native popup
  above the trigger with the same measured width as the composer and 16-pixel blur.
- Update effort captions immediately and commit only discrete effort changes.
  Do not republish the panel for slider input. Cache model controls across state
  updates; rebuild only when catalog or translated labels change.
- Evidence: full solution build passed with zero warnings/errors. Direct local
  desktop inspection verified a 346-pixel composer and popup at both normal and
  640x720 layouts. During an actual thumb drag to Maximum, the caption updated and
  the existing model controls retained their DOM identities. Restore Medium.
- No automated tests, OpenAI inference, installation or publication ran.
- Earlier migration checkpoint: 323d97b. Leave the updated desktop app running.

## 2026-10-11 — Composer overlay, animated effort and compact footer

- Anchor the model popup to the composer, aligning their bottom edges so it covers
  prompt, model/effort selector and Send. Retain composer width and backdrop blur.
- Add a shared native Slider template with its standard interaction parts, a
  28-pixel rounded track and 32-pixel thumb. Increase accent saturation across the
  existing five effort levels, with a 160-millisecond color animation; add no Ultra.
- Put keyboard guidance and the approximate token count (N TK) on the same row.
  Omit character counts, hide the panel scrollbar while retaining overflow scrolling,
  and reduce the collapsed-budget panel from 260 to 232 pixels.
- Evidence: full solution build passed with zero warnings/errors. Direct desktop
  inspection at normal and 640x720 sizes showed matching 346-pixel composer/popup
  widths and coverage of both action buttons. Guidance and count shared the same
  vertical position; panel scrollbar rectangles had zero dimensions.
- During native thumb dragging, intermediate RGB frames changed toward the accent
  over the short transition and the existing model controls retained their identities.
  Return the temporary reasoning choice to Medium and leave the desktop app running.
- No automated tests, OpenAI inference, installation, packaging or publication ran.
