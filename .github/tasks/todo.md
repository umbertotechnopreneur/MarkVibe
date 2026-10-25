# Active work

Keep only open, actionable tasks here. State an observable completion condition;
reuse existing authorization and identify any remaining owner checkpoint.

Current local delivery: Release x64 MSIX **0.1.0.32**, built from the current
source, installed and launched on 2026-10-25. This delivery includes dotted link
underlines, document services, expanded reading, Markdown associations and parent
links, dense graph optimizations and the dismissible splash. Windows package
status and installed process path were verified. No tests
or GUI interaction ran for this delivery; historical static-only notes below
describe implementation checkpoints, not the current installation state.
Changes explicitly marked source-only await a later delivery.

- [ ] Owner-check dense graph presentation and the dismissible splash in
  installed Release 0.1.0.32. Dense maps retain relations with immediate magnification;
  the splash stays one second after readiness, or skips that delay on a click.
  Debug browser profiling passed on 55 documents / 506 links; see
  `docs/graph-profile-2026-10-25.md`. Splash click/keyboard and localized appearance
  still need GUI acceptance. Signed packaging, installation and launch passed.

- [ ] Owner-check subtle hyperlink underlines in installed Release x64 0.1.0.32:
  dotted 1px decoration, .25em offset and 55% of the link color, including imported
  document themes and exports. Verify dark/light appearances, wrapped links,
  visited/focused links and the underline-off preference. Source compilation,
  signed packaging, installation and launch passed; the MSIX contains the new
  link CSS. No automated tests or interactive GUI validation ran.

- [ ] Owner-check document context commands after a requested build: checked
  Column/Expanded column, Ctrl+F with selection and next/previous, source location,
  native/relative file path, local section anchor, table Markdown/Excel TSV,
  code-only copy, PDF/HTML export and collapsed Appearance. Verify reader, Monaco,
  plain source and split panes, workspace changes and stale-menu rejection.
  Models and services own menu/search/preferences/layout state. Static checks
  passed; no build, tests, installation, GUI checks or Git delivery ran.

- [ ] Complete the application-wide model/service separation described in
  `docs/architecture.md`. Workspace/navigation, AI, map persistence and
  watch/deletion transitions still have MainPage ownership. Their state and
  operation lifetimes must move into models/services; moving fields alone is
  insufficient. The context-command work does not claim this migration complete.

- [ ] Owner-check `../` before the header path and sidebar folder name after a
  requested build. Both links open the native workspace's parent through existing
  draft/map/history guards. They disable at drive roots and during opening;
  browser providers hide them. Static C#/JavaScript syntax, XAML XML,
  translations and diff checks passed. No build, tests, installation or GUI checks ran.

- [ ] Owner-check the optional Markdown association offer after a requested build.
  Source registers `.md` and `.markdown` for MSIX/MSI and opens documents with
  `--open`. Yes opens Windows Default apps; No preserves existing defaults.
  The response persists across updates. Check installer/first-launch timing,
  double-click opening, quiet installation, repair/uninstall and portable behavior.
  Settings now includes effective default indicators for each extension and
  an explicit Windows setup action, refreshed when the desktop app regains focus.
  Verify separate/unknown states and changing defaults outside MarkVibe.
  Static C#/JavaScript syntax, JSON/translation and XAML/XML checks passed; no build, tests,
  installation, GUI interaction or Git delivery requested.

- [ ] Owner-check reader toolbar Column / Expanded column after a requested build.
  Expanded mode fills the reader with responsive 16–32px side margins; the choice
  persists with shell layout and preserves the source-backed reading position.
  Verify tables, all document styles, reopening and split/AI/sidebar resizing.
  Source implementation and static review are complete; C#/JavaScript/CSS syntax,
  XAML XML, eight-language JSON and diff checks passed. No build, tests,
  installation, GUI interaction or Git delivery requested for this change.

- [ ] Owner-check the profiled graph in installed Release x64 0.1.0.29. On the
  supplied folder (32 documents, 337 connections), zoom p95 fell from 66.7 to
  33.4 ms and style work from 864.7 to 62.4 ms. Endpoint and restored-gesture
  checks passed. Some short stalls remain; native WebView2 and the larger
  screenshot workspace remain unmeasured. See `docs/graph-profile-2026-10-23.md`.
  Debug compilation and 22 Core, 20 client and three browser checks passed.
  Immediate Back during startup now restores its queued destination. The
  installed Release now includes these changes; native acceptance is pending.

- [ ] Owner-check the source performance cleanup in installed Release 0.1.0.29.
  Review `docs/performance-review-2026-10-23.md`: 43 authorized Core,
  Native and JavaScript checks passed; synthetic catalog timings are recorded.
  First-open and native UI responsiveness remain unmeasured. The subsequent
  Debug browser graph profile is recorded separately above.

- [ ] Owner-check graph cards with a single selection border and semantic colors
  for toolbar/action icons, including PDF, code, Settings and About. Static graph
  optimization reuses topology by catalog metadata revision, batches drag/resize
  drawing per frame and avoids unchanged SVG curve writes. The subsequent
  authorized review passed 43 automated checks, including graph geometry cases.
  C#/JavaScript/CSS syntax, XAML XML and diff checks passed; installed Release
  is now 0.1.0.29. Functional and performance acceptance remain with the owner.

- [ ] Owner-check recent file/folder dropdowns on the desktop Open buttons:
  separate local lists retain the twenty most recently selected paths per kind,
  deduplicate case-insensitively and display alphabetically. Browse remains
  available above each list. Long paths use font-measured middle abbreviation,
  with complete tooltips and accessible names. Existing workspace guards apply
  to reopening, and missing entries show an error. Native persistence, bounds,
  cancellation and malformed-history tests passed in the subsequent review.
  Included in installed Release 0.1.0.29. C#/JavaScript syntax, XAML XML,
  eight-language translation structure and diff checks passed.

- [ ] Owner-check shared context menu copy actions in Monaco, the simple source
  view and the document reader: Copy uses the selection captured before popup
  focus; Copy entire file content uses the current Markdown snapshot, including
  local revisions, or reads the target document. Existing Explorer reveal stays
  available for native files. Copy is disabled without a selection. No paste was
  added. Installed and launched Release x64 MSIX 0.1.0.29 after the owner's
  build/install request. JavaScript/C# syntax, XAML XML, eight-language JSON and
  diff checks passed. Native GUI acceptance remains with the owner.

- [ ] Owner-check current-file change notification: watch the opened native file, debounce
  editor saves, prompt Yes/No and reload only after acceptance. Preserve the
  current snapshot on No or read failure; reject obsolete navigation events and
  warn before discarding local revisions. Support connected browser folders by
  polling their existing read grant. Included in the owner's subsequently
  requested Release x64 0.1.0.27 build/install. C# and JavaScript syntax, translation structure and diff
  checks passed. The subsequent Core reload check passed. Installed Release
  0.1.0.29 also fixes the concurrent-read allocation bound; nine Native checks
  passed. Native GUI acceptance remains pending. The owner's 2026-10-23 request
  authorizes reviewing and merging PR #3 with the performance work.

- [ ] Owner-check installed Release x64 0.1.0.26 and the 2026-10-23 reader correction: HTML comments
  stay hidden, `details`/`summary` expand correctly, heading navigation reveals
  collapsed sections and PDF includes their contents. Right-click offers the
  same four theme catalogs as Settings, an installed monospace font override
  and a hyperlink-underline switch, saved per opened folder. Global Settings
  stays the fallback; resetting the folder removes its override. Check local
  and browser workspaces, menu placement, folder persistence
  and Mermaid within disclosures. Owner-requested Release source compilation,
  packaging, certificate trust and MSIX installation succeeded after cleaning
  artifacts and uninstalling Debug 0.1.0.25. Windows reports x64, Status Ok and
  display name MarkVibe; the installed Release process is running. Existing
  NU1902/NU1903 dependency advisories remain. No tests or GUI checks ran.
  See artifacts/BUILD-INFO.json and INSTALL-INFO.json locally. Release x64 is now
  the owner's default delivery configuration; Debug was retired.

- [ ] Owner review/functional acceptance of the 2026-10-17 maintainability pass:
  provider-explicit `ai.*` commands without old aliases, shared browser HTTP/storage
  handling, split map codec/session, named validation phases and documented types.
  See `docs/maintainability-review-2026-10-17.md` for before/after examples and
  remaining architectural candidates. Bundle and final Debug x64 source compilation
  passed; no tests or GUI checks ran. Release 0.1.0.26 now includes this source
  pass after its authorized packaging and installation. The existing smoke fixture is updated
  but not executed. Future changes do not require historical compatibility.

- [ ] Owner acceptance of installed Debug x64 0.1.0.25 and the opaque desktop
  presentation repair: the 0.1.0.24
  screenshot shows missing shell regions while panning, despite the previous
  composed-host change. Use direct HWND WebView2, opaque host/browser backgrounds
  and no full-client DWM glass. Disable native Acrylic availability with localized
  explanation; retain the preference and browser modal blur. Confirm map pan/zoom,
  selection, resizing, themes and modals manually. Build/sign/MSIX installation
  and final launch succeeded; Windows reports version 0.1.0.25, x64 and Status Ok.
  No GUI checks, suites, provider tests or inference ran. Existing NU1902/NU1903
  transitive dependency advisories remain outside this rendering correction.

- [ ] Owner acceptance of installed Debug x64 0.1.0.24: persisted Codex CLI/API
  radio selection, separate Test buttons and explicit pass/fail dialogs. API tests
  check model-list authentication; CLI tests request a tiny synthetic response and
  can consume usage. Neither test was executed during development. Verify real
  connection results, cancellation and edits manually. The Windows CLI uses its
  existing login; the browser reports that CLI integration requires the desktop.
  Build, signing, MSIX installation and final launch succeeded; Windows reports
  version 0.1.0.24, x64 and Status Ok. No suites or interactive checks ran.

- [ ] Owner updates the Codex project folder link to `E:\MarkVibe`. The complete
  repository, Git history/backups and Debug artifacts were copied there from
  `E:\Research\MarkVibe`; the original is preserved. Use the new folder for future
  work after the owner reconnects it, rather than editing both copies independently.

- [ ] Owner acceptance of installed Debug x64 0.1.0.23: updated VibeWare header,
  About/disclosure, timed hover/focus footer, map-effect lifecycle and retained
  exploration controls. Windows confirms x64 version 0.1.0.23 and Status Ok.
  MSI 0.1.23 and portable outputs were generated; MSI installation/upgrade/uninstall
  and Visual Studio F5/breakpoint attachment remain unverified. All seven solution
  projects compile in Debug, including check projects without executing them.

- [ ] Owner acceptance of installed Debug x64 0.1.0.22: lighter cards, finite
  directional link pulses, relation lens, up to 12 selected documents with
  comparison/atomic AI collection, actual navigation trail and cached attention
  facts. Ctrl/Cmd-click or Shift-drag selects; toolbar controls expose the same
  actions. Build/sign/install completed, Windows reports Status Ok and the installed
  executable was launched. No automated suites or interactive checks ran. The
  timeline/WebGL message was retracted; no related Git delivery/research ran.

- [ ] Owner acceptance of installed Debug x64 0.1.0.21: repaired map icons and
  document double activation, wider distance selector with matching view-name
  typography, bordered inspector actions, collapsible incoming/outgoing links,
  route spacing and clearer HTML/PDF export symbols. Build/sign/install and final
  launch are complete; Windows reports Status Ok. No interactive checks, test
  suites, inference or publication ran. The owner checks appearance/navigation.

- [ ] Owner acceptance of the gray redraw repair introduced in Debug 0.1.0.20
  and retained in installed 0.1.0.21. The owner reports an improvement.
  The visible host uses WPF composition, map painting is contained, and SVG filters
  and minimap backdrop blur are removed. Build, installation and startup succeeded;
  resolution on the owner's actual workspace remains unconfirmed. The owner now
  performs all GUI checks: no window manipulation, input automation or desktop
  capture unless explicitly requested. No test suite or publication requested.

- [ ] When execution/acceptance is explicitly requested, validate the implemented
  document atlas: semantic zoom and overview labels, island collapse/reopen,
  0–3-link lenses, source excerpts and full-catalog shortest routes, inspector,
  minimap and eight saved viewpoints. Include sidecar conflicts/reopen, search
  expansion, compact preview, both themes and reduced motion. Debug 0.1.0.18
  includes the atlas and was installed/launched. Focused UI inspection covered
  inspector persistence and navigation retention; broader interaction acceptance,
  automated suites and publication remain separate. Local checkpoint commits are authorized.

- [ ] When execution is explicitly requested, run the new `Verifica-MarkVibe.cmd`
  profiles against the unified DocumentSession model and schema-4 map. Verify
  cache eviction, stale replies, undo/redo/export, first-entry map controls,
  visible save failures, raw document labels and UI-font isolation. Review
  `docs/maintainability-review-2026-10-16.md` for the implemented cleanup and
  remaining architectural opportunities. Source/static validation and the
  0.1.0.10 Debug x64 build/direct launch are complete; runner execution and
  functional runtime acceptance remain unverified.

- [ ] When dependency maintenance is requested, resolve the existing NU1902/NU1903
  warnings for transitive System.Security.Cryptography.Pkcs 8.0.1 and Xml 8.0.4.
  The current x64 Debug package builds; no advisory remediation was requested.

- [ ] When execution/validation is explicitly requested, bundle/build Markdown AI
  editing and check document/selection replacement, CRLF/Unicode source ranges,
  strict replies, missing/rejected keys, model availability, context preflight,
  cost categories, both themes and the elevated 16 px blur popup. Verify its total
  60-second deadline, Stop/navigation/key-change cancellation and late replies.
  Check local undo, copy saves, drafts/close protection and original sidecars with
  synthetic files. Source/static work and public catalog download are complete;
  current bundle/build and UI inspection are complete; automated tests, inference and publication were not requested.

- [ ] When execution/validation is requested, bundle/build the OpenAI key settings
  and check masked input, successful/failed authentication, cancellation/timeout,
  preservation of existing keys, Windows user-environment persistence and browser
  storage warnings/failures. Verify translations and narrow dialog layouts.
  Current bundle/build and local dialog inspection are complete; automated tests and live key validation were not requested.

- [ ] When functional validation is explicitly requested, check persistent map
  save/reopen, card dragging, appearance, filtering and folder permission/conflicts.
  Include the 2026-10-16 black canvas, accent selection elevation, wheel anchor,
  staggered/reduced-motion entry and Star/Circle/Grid/Tree arrangements, including
  tied/disconnected graphs and the 120-card cap. The 0.1.0.9 Debug x64 package
  compiled and its map was opened for visual inspection. Automated tests and
  broader interaction acceptance remain pending. Ink and notes stay retired.
  The follow-up also needs requested runtime acceptance of connection hover,
  click/double-click, resizing and schema-4 reopen, confirmed reset, metadata strip,
  immediate arrangement/autosave, inline search and the black-canvas toolbar.
  Include connection-depth transitions during selection/hover/resizing, compact
  views, reduced motion and full-catalog distances beyond the 120-card projection.
  The 0.1.0.10 preview adds twelve graduated spatial/graph levels, stronger
  selection and far-endpoint line attenuation; its map was visually inspected.

- [ ] When validation is requested, check frosted modal open/close/cancel and
  reduced motion, the OpenAI credential form and the installed UI-font selector.
  Confirm delayed font discovery preserves preferences and UI changes leave
  reader/editor/export typography intact. These changes have static review only;
  no build, tests, credential access or live API validation was performed.

- [ ] On the user's request, inspect the refreshed UI at wide and narrow widths:
  settings must start in two columns and reflow while resizing; check all nine
  languages, tooltips, rounded view tabs, About and system-accent dialog shadows.
  Verify native Acrylic with Windows transparency enabled/disabled and inspect
  selected document fonts plus matching HTML/PDF exports. Bundle/build succeeded;
  current local wide/narrow inspection is recorded in archive.md; OS/device, translation and export acceptance remain separate.

- [ ] Exercise native file/folder/save dialogs and multiple desktop DPI scales with
  synthetic files; automated exports currently use explicit smoke destinations.
- [ ] Owner manual acceptance of the installed 0.1.0.13 Debug x64 package remains
  separate from deployment. Windows confirms version 0.1.0.13 and Status Ok after
  explicit certificate/installation consent. No test suite or functional checks
  were run; the owner reserved testing.
- [ ] Before a Microsoft Store submission, use the reserved Partner Center identity
  and publisher and obtain separate Store-publication authorization.

Move completed milestones to `archive.md`; put reusable engineering findings in
`lessons.md`. Keep logs, screenshots and disposable fixtures under ignored `artifacts/`.
