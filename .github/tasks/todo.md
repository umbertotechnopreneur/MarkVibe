# Active work

Keep only open, actionable tasks here. State an observable completion condition;
reuse existing authorization and identify any remaining owner checkpoint.

- [ ] Compile and owner-check current-file change notification: watch the opened native file, debounce
  editor saves, prompt Yes/No and reload only after acceptance. Preserve the
  current snapshot on No or read failure; reject obsolete navigation events and
  warn before discarding local revisions. Support connected browser folders by
  polling their existing read grant. No tests, build or installation requested
  for this follow-up. C# and JavaScript syntax, translation structure and diff
  checks passed. A synthetic Core reload case was added but not executed.
  Installed Release x64 0.1.0.26 does not include this follow-up. Do not approve
  or merge its future pull request yet.

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
