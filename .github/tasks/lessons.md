# Engineering Lessons

Keep durable, repository-specific findings here; omit temporary commands and plans.

- Give internal AI commands one provider-explicit contract and change every caller
  and dispatcher together. Do not retain a second family of names as speculative
  compatibility. Reject unknown providers instead of silently using another key.
- A useful readability pass names domain predicates and phases, rather than
  introducing a temporary for every expression. Keep raw JSON validation separate
  from typed model invariants, and keep mutable session state out of codec files.
- Shared request plumbing must preserve policy differences. Credential validation
  and document editing have different deadlines/messages and save semantics;
  deduplication must not erase those contracts or weaken response bounds.

- A composition host is not inherently a redraw fix. WebView2CompositionControl
  adds graphics capture and WPF presentation; use it only when actual WPF overlays
  require that path. MarkVibe overlays are browser content, so direct opaque HWND
  hosting avoids that extra path. Keep the browser, WPF target and native client
  opaque together; native Acrylic and browser modal blur are separate decisions.
- Missing native shell regions in an owner screenshot invalidate earlier redraw
  acceptance. A successful build or a correct DOM background cannot establish
  that native presentation is fixed; keep owner interaction acceptance separate.

- A local CLI login status is not proof of a remote response. Keep the explicit
  synthetic connection test separate, explain its possible usage cost and never
  label static model IDs as verified account availability. API model-list access
  proves neither inference access nor quota. Preserve provider selection on failure.
- CLI read-only sandboxing restricts writes, not every filesystem read. Keep the
  selected workspace out of the process working directory, pass captured text on
  stdin, disable tools/plugins and reject unexpected tool events without claiming
  complete OS isolation. API store:false does not describe CLI retention.

- VibeWare branding follows its canonical publication templates: the approved
  floppy remains unchanged, identifies participation, and never replaces the app
  icon or actual creator. Keep first-person About disclosure and legal links
  available; a timed footer fades opacity while retaining space and focus access.
- An MIT application can include separately licensed components only while
  retaining their own obligations. The actual Mermaid bundle includes EPL-2.0
  elkjs 0.9.3: retain its complete license and versioned source-availability notice.
  Missing package.json license metadata is not proof that no license file exists.
- MSI compares three version fields. Preserve revision ordering explicitly and
  keep channel/architecture upgrade identities stable. An MSI build is not proof
  of installation, repair, upgrade, downgrade handling or uninstall acceptance.
- A Git bundle contains committed objects and refs, not dirty or untracked files.
  Preserve those separately before a history rewrite, keep backup outside the
  packaging cleanup boundary, and never force-update published release tags.

- Cache full-catalog attention facts by metadata revision and index completeness.
  Unindexed/failed metadata must not create isolation claims. First observation
  of a fingerprint establishes a baseline; it is not proof of a changed file.
- Useful link animation is bounded by duration and count, follows source-to-target
  geometry and stops without an idle frame loop. Cancel it when suspended, hidden,
  disposed or when reduced motion is selected. Keep temporary exploration state
  separate from layout autosave and document source.

- A transparent Path without a Stroke is invisible; filling an outline's outer
  shape can also hide its inner marks. Preserve inherited SVG fill/stroke rules
  when carrying graph symbols into XAML, including circle and ellipse geometry.
- Selection can change a card's descendant target or reparent the retained card
  before the second click. Recognize document activation by path, timestamp and
  pointer proximity as well as the browser's click count. Clear pending activation
  after gestures/session changes and emit navigation once from the click handler.

- A black browser screenshot and retained DOM nodes do not prove that a visible
  transparent Windows host repaints correctly. Check native presentation at the
  real DPI with the diagnostic window in front. Skip screen capture if it loses
  foreground focus, so unrelated windows are never included.
- WebView2CompositionControl needs the Windows Runtime .NET projection at startup.
  Target Windows 10 APIs explicitly and include their runtime assembly through the
  framework reference; compiling against a bare Windows target can still launch
  with FileNotFoundException. Keep the hidden PDF WebView independent.

- Local Debug delivery has standing owner authorization to close MarkVibe even
  with unsaved in-app work, trust that build's MarkVibe development certificate in
  LocalMachine/TrustedPeople, replace/install the package and launch it without
  another confirmation. Preserve source documents and unrelated data/certificates.
  This supersedes earlier per-certificate approval notes for local Debug only.
- Keep shell geometry in a separate versioned local setting. Save completed
  splitter gestures and explicit open/close choices; temporary responsive widths
  must not overwrite user preferences or rerender document content.
- OpenSilver's final DOM setup can replace Loaded-time classes and inline material
  styles. Give modal backdrop/surface elements stable template names and target
  those in CSS. Verify computed styles while the dialog remains open: a closing
  animation applying blur does not establish that the opening state has it.
- Treat map navigation as a selection update within the existing layout session.
  Recreating cards replays entry animations and loses pointer identity. Keep the
  canvas opaque and use centered 2D scale for the equivalent perspective size;
  switching transform spaces during hover forces avoidable compositor changes.
  A replacement workspace/session still requires a full presentation.

- Keep atlas lenses, collapsed folders and named camera views in managed,
  validated sidecar state. Camera-only pan/zoom updates existing minimap marks;
  semantic zoom and collapse must never rewrite document coordinates. Compute
  routes before the visible-card/edge cap and prioritize their real links.
- Defer saved camera restoration until rendering/fit has completed and the host
  has dimensions. Reuse folder-toggle DOM nodes across acknowledgments to retain
  keyboard focus; redirect collapsed external links to island bounds.

- Give OpenSilver ChildWindow's full-window Overlay its own backdrop blur at
  opacity 1, with translucency encoded in its background brush. Keep the portal
  root free of opacity/filter animations: an ancestor backdrop root limits the
  scene sampled by a child's filter. Animate the overlay blur and content surface
  separately. See MDN's backdrop-filter/backdrop-root documentation.
- Tree headers are composed controls: bind each caption/glyph foreground to the
  TreeViewItem and draw its background across the header row. Track header hover
  independently of descendants so nested folders do not highlight together.

- A self-signed MSIX can build/sign successfully while deployment fails because
  its fresh certificate is untrusted. Record actual installation state separately
  and obtain explicit consent before machine-wide TrustedPeople import when
  automatic approval requires it; never substitute user-store trust or bypasses.
- Compute map depth from the complete managed graph before projecting visible
  cards. Keep perspective separate from persisted coordinates, freeze resize
  magnification during a gesture and follow rendered card bounds for edge anchors.
- Graph distance alone puts many nearby documents on identical depth planes.
  Mix in bounded layout distance for visual separation, and attenuate connections
  by their farther endpoint so highlighted links do not flatten the background.
- A retained draft must be resolved before updating catalog metadata and cache.
  Replacing only the reader model after a disk read leaves map links/statistics
  describing the original document after cache eviction.
- Verify callers before classifying a defect as user-visible. The removed Core
  global search had test callers only; retained source/index logic was not evidence
  of an exposed search feature.
- Capture the workspace and document before asynchronous work and recheck them
  after its final await. Export completion marks the exact exported source; it
  must not mark a newer revision as saved. Keep source identity outside the LRU.
- Use one pre-release map contract. Reconcile persisted nodes against complete
  enumeration rather than the bounded content index or filtered visible graph.
  Dropping compatibility does not authorize deleting unsupported user sidecars.
- Actionable failures must open a visible notice; changing a TextBlock inside a
  collapsed container is not feedback. Keep dirty state and recovery actions.
- Use explicit localization keys for UI and a persistent raw-content marker for
  user labels. Match font traversal to OpenSilver's actual UserControl hierarchy.
- Existing bundle/esbuild files do not establish source or lockfile freshness.
  UI smoke scenarios must track current controls and interaction contracts.

- A stale undefined callback in `map.state` can retain new state while preventing
  both live rendering and subsequent autosave; switching views only masks the
  failure. Remove retired subsystem hooks rather than forcing a second render.
- Keep single-click map selection separate from opened-document navigation and
  preserve card DOM identity across selection updates for double-click handling.
  Persist resize dimensions atomically and derive graph spacing from real sizes.
- Native modal closing animations must retain backdrop hit interception and
  restore the open surface if a later subscriber cancels closure. Size the
  ContentRoot after OpenSilver expands the ChildWindow into its modal overlay.
- List installed UI fonts through the native host, preserve the saved choice
  during discovery, and apply typography only to XAML UI and map chrome.
  Statistics must use full source rather than truncated search snippets; label
  UTF-8 byte size and token estimates accurately.

- Arrange the complete document catalog in C# and commit all coordinates once.
  Count distinct incoming/outgoing neighbors for hub selection; use the same
  ranking before the visible-node cap so the central page cannot disappear.
  Preserve an eligible selected page and saved current-schema styles.
- Drain pending card edits before arranging and fit the camera only after the
  managed arrangement revision advances. Anchor wheel zoom to pointer coordinates,
  retain drag scale during gestures, and cancel entry effects for focus/reduced motion.

- Markdown AI edits are stateless: two fixed system messages, current target and
  current prompt only. Local undo and numeric usage counters must not become API
  history. Fence automatic replacements by path, provider session and revision.
- Model-list access does not supply prices or context limits. Retain a dated official
  public catalog and app-defined semantic cost labels; use explicit Standard service
  rather than inheriting a project's configured tier. Keep long-context rates separate.
- Count input before generation and reserve the model's documented output maximum
  inside the shared context. Prefix differences include framing/schema; label input
  shares estimated unless the full count agrees with actual API usage. Reasoning and
  cached tokens are subsets, not additional totals. Aborted usage may be unavailable.
- Use one deadline across preflight and generation, propagate cancellation across
  browser/native bridges, and reject late results before the managed source commit.
  Network work must not hold the native file/export semaphore.
- Rendered selections require exact literal source spans. Monaco and textareas may
  normalize CRLF/mixed line endings, so map their selection back to raw UTF-16 source.
  Unsupported or partially overlapping selections must not fall back to full-document
  editing. Drain/freeze annotation input before replacing source; protect its originals.

- OpenAI keys stay outside general preferences and workspace contracts. Validate
  explicitly through a fixed HTTPS endpoint before replacing a saved key; return
  presence only, map errors to fixed messages and cancel dialog-owned requests.
  Native WebView2 storage uses the Windows user environment; browser `localStorage`
  requires a visible warning about its lack of encryption and script access.
- For readability changes, compare syntax/tokens while allowing only scoped local
  renames and equivalent control blocks. Protect every write against concurrent
  edits, and retain the newer application behavior when another task changes a file.

- Every new or changed class/type and method/function needs a useful
  purpose/contract comment; use XML documentation for C# types and members.
  Explain non-obvious assumptions, ordering and invariants inside methods.
  Separate logical phases with blank lines, expand control-flow bodies and wrap
  long expressions. Favor descriptive names and useful intermediate steps without
  redundant temporaries; human readability takes priority over line count.
- VibeWare headers use exact `VBWR B` / `VBWR E` markers, the project's MIT notice
  and supported provenance only. Preserve XML declarations/HTML doctypes, omit
  headers from JSON/lockfiles and retain third-party assets' own license notices.
- Scope AI disclosures to recorded contributions and completed checks. Keep
  historical validation separate from the evidence for the current source update.

- A successful build does not prove startup, visible UI, native dialogs or hardware
  behavior. Record which layer was actually exercised; cold-launch desktop changes.
- Keep the assistant response in a bounded star row above the fixed composer.
  Reset its scroll only when reply text changes; budget/usage refreshes must not
  interrupt reading. Localized chrome and generated-response language are separate.
- Keep typed, bounded contracts across C#, JavaScript and native IPC. Check the
  sender origin and enforce file containment in the native service, beyond the UI.
- Persist and export through temporary files and atomic replacement. Preserve
  unknown formats and externally changed files; never turn a conflict into success.
- Cancel and dispose obsolete readers, workers, image leases and export views.
  OpenSilver DOM hosts must attach to a connected element before rendering.
- Use synthetic documents and isolated profiles for validation; private files,
  credentials and business notes do not belong in fixtures, screenshots or logs.
- Suggest checks relevant to the final change, but run tests only on a current,
  explicit user request. A previous validation approval is not continuing permission.
- When Markdown changes, repeated quotes cannot identify the original block safely.
  Require a unique quote and explicit review; keep ambiguous annotations unresolved.
- OpenSilver applies viewport layout asynchronously. Wait for actual bounds before
  asserting responsiveness; a completed browser resize does not imply managed layout completion.
- Queue completed ink strokes while native autosave is pending. Navigation/export
  must await the event queue; disabling drawing during each write loses fast strokes.
- Clean repository `artifacts/` before each requested Release/Debug distribution
  build, keep temporary packaging files there and remove them after packaging.
  Never create build/test output in Codex directories.
- Ask before each branch/worktree creation. Package creation, installation and
  publication are separate actions; record only the evidence actually obtained.
- Local build/MSI requests mean the existing MSIX format, x64 Debug and launching
  the newly built desktop executable. The owner confirmed x64, not 32-bit x86.
  Use Release or ARM64 only on explicit request; tests still require a current
  request, and app/certificate installation remains separate.
- `dotnet workload list` can report wasm-tools while its selected manifest
  references absent packs. Diagnose NETSDK1147 with the project's evaluated
  `MissingWorkloadPack` items; obtain SDK/workload-installation authorization
  before changing machine tooling, and retain the pinned SDK version.
- A reopened folder can retain its workspace id while replacing its provider and
  layout session. Suspend map input before draining edits, serialize replacement,
  and fence replies with session identity and generation. Release the write gate
  before navigation performs its own flush.
- Persistent map coordinates must come from the complete catalog, not a filtered
  or focused projection. Allocate new grid slots without moving existing cards.
- Browser folder write permission must be requested directly from an explicit
  user gesture. Download initiation does not prove the user's save completed.
- Reader sticky notes need source-backed element anchors/local offsets; raw page
  positions drift with responsive layout. Nested elements need explicit targets,
  and generated SVG/highlight nodes resolve to their source container. Strict map
  schemas require migration when adding notes; overlays isolate ink/pan input.

- Each source-backed note anchor retains its own document fingerprint. Saving
  another note must not make an old anchor appear to originate from changed text.
- Validate complete note/map JSON budgets before mutating undo history. A compact
  valid loaded file need not fit the serializer's indented representation until edited.
- Distinguish rejected note mutations from accepted edits whose autosave failed.
  Preserve action-owned fields as recovery drafts and deliver acknowledgments even
  when a newer presentation has already arrived.
- DOM-host replacement must retain unsent text, IME values and pending operations
  until drained or transferred to the same managed session. Explicit discard drops
  unsent drafts before reload; ordinary navigation must drain them first.

- Keep XAML chrome themes separate from CSS document themes. Share stable catalog
  IDs and translation keys, update existing semantic brushes, and scope imported
  CSS to the reader so it cannot recolor application controls or initiate font downloads.
- OpenSilver expands ChildWindow to its overlay dimensions after opening. Resize
  its named ContentRoot for responsive dialogs and retain the full modal overlay.
  Grid-based icon captions can wrap inside compact two-column action groups.
- Native Acrylic requires both a transparent WebView background and transparent
  shell layers. Keep reading/editor surfaces opaque and respect OS transparency,
  high contrast and unsupported Windows versions.

- Application chrome belongs in OpenSilver UserControls and shared ResourceDictionary
  styles. Only Markdown/Mermaid rendering, Monaco and graph drawing/gestures belong
  in DOM adapters. Interop may decorate an existing XAML surface for backdrop blur;
  it must not create replacement toolbars, prompts or flyout controls.
- OpenSilver Popup offsets use its placement target. Anchor model flyouts to their
  trigger with Placement.Top and screen bounds; renderer-reported global coordinates
  use Placement.Relative against the application root.
- Moving toolbar ownership changes renderer layout: preserve a flex source surface
  and a full-height map viewport so Monaco and graph fit do not collapse to zero.
- XAML prompt changes must synchronously update the nonvisual browser close guard.
  Debounced token estimates cannot be responsible for retaining an unsent draft.
- Retiring annotations must remove controls, mutation/session paths and export
  composition. Preserve already-existing map metadata and user sidecars without
  reviving the editor or rewriting original files.

- Reasoning-slider input must update the small choice state and labels directly.
  Full panel publication or clearing a model list on each value change destroys
  scroll/focus and wastes layout work. Cache catalog controls and refresh only
  when catalog contents or translated labels change.

- To overlay an entire composer with an OpenSilver popup, use the composer as its
  placement target and offset Top placement by the composer's measured height.
  Preserve native Slider template parts when changing track/thumb presentation.
  Hiding the panel ScrollViewer's scrollbar removes its reserved width while
  preserving scroll access to overflow content.
