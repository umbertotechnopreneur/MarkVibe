# Security and resource boundaries

The following boundaries describe the current source. Authorized checks on 2026-10-11 exercised path/scheme containment, bounded ZIP cases and a browser fixture with scripts, unsafe links and remote images. Those historical regressions cover their specific cases, not the later implementation or a complete security audit. The 2026-10-16 maintainability changes received source/static review only; no build or tests were run for that work. See [validation](validation.md) for recorded coverage and remaining acceptance.

## Untrusted document content

Markdig recognizes embedded HTML. The managed renderer hides comments and accepts only inert formatting tags, including `details` and `summary`, without any source attributes. Unsupported tags are displayed as text. The DOM adapter performs an independent DOMPurify allowlist pass, removes existing image sources and installs local asset URLs only after provider validation. Checkbox controls are disabled. Scripts, event attributes, embedded frames, objects and forms are not permitted.

External navigation allows explicit http, https and mailto URLs without credentials. External links open with noopener and noreferrer. Remote document images are not loaded. Document links, heading fragments and image references are decoded once in C#, then resolved inside the current workspace. File, data, JavaScript, scheme-relative and drive paths are blocked.

Mermaid configuration directives, front matter and click commands are rejected. Its library is imported lazily and rendered serially with strict security and HTML labels disabled. Its SVG is sanitized again, with active elements and external CSS/resource references rejected. A diagram failure leaves a source/error panel and the rest of the document available.

Generated Mermaid styles retain bounded static CSS rules scoped to that diagram's id. Animation/import/other at-rules are discarded; style properties are allowlisted and external URLs, escaped CSS and executable values are rejected. This preserves node fills and readable labels without admitting document-supplied theme configuration.

Current-page HTML/PDF preparation reuses the same HTML, image and Mermaid validation, with separate aggregate export bounds. Supported images become data URLs and diagrams become sanitized inline SVG. The standalone document embeds no script and adds a restrictive CSP. Blocked/missing resources become visible notices. Only safe external and same-document anchor links retain navigation; neighboring Markdown pages are not included. PDF uses a separate script-free sandboxed print frame, cleaned up after printing, cancellation or a bounded fallback. Neither exporter uploads content or writes the original workspace.

Local SVG assets use a separate, stricter profile that removes style, links, scripts, image references, filters, foreignObject and animation. Accepted raster files must have supported signatures and bounded header dimensions. Corrupt or unsupported images receive a useful error.

No Markdown content can select an interop method, load a library URL or supply a JavaScript callback. Ink and sticky-note editing, their codecs and their sidecar schemas are retired; annotation files are excluded from providers without deleting user files.

## Recent local paths

Desktop Open file and Open folder menus store up to twenty selected paths per
kind in `recent-locations.json` under the application data directory. Lists show
full paths alphabetically, with middle abbreviation only in the visible caption.
This local history is separate from automatic last-document restoration and
contains paths and last-open times, not document contents. Reopening an entry
uses the existing local workspace validation and never changes source documents.

## AI connections and OpenAI credentials


Settings retains the selected `api-key` or `codex-cli` provider separately from
credentials. The API connection is the default. Changing connection never
silently falls back to another provider, and obsolete/cancelled replies cannot
replace the captured document revision. Provider test actions are explicit and
their pass/fail dialogs describe what was actually checked.

The optional Settings validation action makes an authenticated HTTPS `GET` to
`https://api.openai.com/v1/models`. It sends only the candidate credential; no
workspace or document content is included. Validation is explicit, uses bounded
request deadlines and is cancelled when Settings closes. Opening Settings reads
credential status without validating a candidate. Normal application startup
automatically requests the account model list when a saved key is configured;
that request carries the credential but no document content and performs no
generation. Document or selection content and explicitly selected reference
documents are sent only through an explicit AI action, including its token-count
preflight. Collection membership and local file/token statistics make no API calls.

Each workspace collection retains at most 16 canonical Markdown paths. At send
time, the current editing target is excluded from references; each reference is
limited to 1 MiB UTF-8 and their combined source to 4 MiB. Duplicate or unsafe
paths are rejected. References are untrusted, read-only request content and do
not authorize editing those files. The captured current document or selection is
the only replacement target. Preflight counts include the reference messages.

The authenticated Windows bridge performs the request natively and saves an
accepted key as the user environment variable `OPENAI_API_KEY`, also updating the
current process. It does not fall back to WebView storage. Environment variables
can be read by other processes running as the same user.

The browser performs the request directly and saves an accepted key separately in
the site's `localStorage` under `markvibe.openai.apiKey`. Settings warns before validation that this isolated
browser storage is unencrypted and accessible to site scripts. Credential values
are never returned by status calls, logged by these handlers or included in
preferences, workspace sidecars or exports. API failure messages use fixed text
instead of reflecting remote response bodies. A failed validation preserves any
previously saved key. The check establishes authentication and model-list access;
model inference, account credit and quotas require separate checks.

The Windows Codex connection starts the installed CLI with structured arguments
and standard input, not a shell command containing the prompt. It uses the CLI's
existing login without returning credentials to the WebView, and does not load
the user's tool/plugin configuration for the editing session. The working
directory and response files are temporary; request sessions are ephemeral and
read-only. Input/output are bounded, and cancellation/deadline terminates the
child process tree. Documents are passed as captured text rather than granting
the selected workspace as the process working directory.

Read-only sandboxing is a write restriction, not complete OS isolation for
filesystem reads. The CLI is an external local program executing with the user's
identity; its own authentication/network policies remain applicable. A Test
request uses synthetic text and may consume account usage. No CLI inference or
credential test is run automatically as a development acceptance check.

The native `--smoke-test` profile intercepts the current `ai.*` bridge operations
before reaching credential, HTTP or CLI handlers. Its `ai.connection.status`
reports an unconfigured, unavailable provider; other AI operations fail explicitly.
The only editing commands are `ai.models`, `ai.prepare` and `ai.edit`, each with
an explicit provider. Retired `openai.*` and `codex.*` commands are not aliases.
This guard does not read the user's saved credential and prevents AI network
requests from that verification profile. It is separate from normal startup
behavior and does not establish live provider acceptance.

## Paths and enumeration

Workspace-relative paths are Unicode NFC and case-sensitive. Rooted paths, drive paths, controls, backslashes, noncanonical imported paths and paths escaping the root are rejected. URL percent encoding is handled once for links, never repeatedly for provider filenames.

Connected scans are cancellable and bounded by 10,000 visited entries, 1,024 directories and 16 directory levels. The .git, node_modules, bin, obj, .vs and __MACOSX segments are ignored. A relevant path collision is rejected instead of silently overwriting a resource.

## Limits

| Resource | Source boundary |
| --- | --- |
| Path length / segments | 1,024 characters / 64 segments |
| Relevant workspace entries / Markdown documents | 4,096 / 1,024 |
| Markdown file | 1 MiB UTF-8 |
| AI reference collection / combined reference source | 16 paths / 4 MiB UTF-8 per request, current editing target excluded |
| Aggregate metadata/link indexing input | 8 MiB source bytes per retained workspace |
| Parsed blocks / headings / document links | 2,048 / 512 / 4,096 |
| Rendered HTML | 3 MiB in UTF-16 code units, with bridge payload bounded separately |
| Bridge request JSON | 6 MiB in UTF-16 code units |
| Retained workspace contexts / selected models | 2 / 3 per context |
| Edited document sessions | 16 per retained workspace |
| Each document undo/redo stack | 16 entries and 4,194,304 UTF-16 code units |
| Source editor models | 3 |
| Visible map / projected edges | 120 nodes / 600 edges |
| Map sidecar / stored cards / absolute logical coordinates | 1 MiB / 4,096 / 100,000 |
| Diagram source / diagrams per page | 20,000 characters / 12 |
| Diagram source lines / configured edges / output elements | 400 / 160 / 4,096 |
| Image file / total live blobs | 8 MiB / 32 MiB |
| Image dimensions / pixels | 10,000 per dimension / 16,777,216 pixels |
| Aggregate leased image pixels | 33,554,432 estimated pixels |
| SVG file / elements | 256 KiB / 2,048 |
| Live image leases / concurrent image reads | 96 / 2 |
| ZIP input / directory | 32 MiB / 2 MiB |
| ZIP entries / declared expansion | 2,048 / 64 MiB |
| ZIP per-entry expansion / ratio | 8 MiB / 150:1 |

Images are lazy and leases have a DOM-host owner. Aggregate intrinsic pixel accounting limits retained images in addition to blob bytes; it is not an exact decoded-heap measurement. URLs are revoked on replacement, refresh, workspace release and host disposal. Pending reads reserve from the shared asset budget before creating URLs.

## ZIP policy

ZIP import first inspects the bounded end/central directory, validates every entry path (including ignored files), and checks local headers, names and nonoverlapping byte ranges. Multi-disk, ZIP64, encryption, Unix symlinks, duplicate paths and unsupported compression methods are rejected.

Only stored and raw-deflate entries are accepted. Non-ASCII filenames must explicitly be UTF-8. Entry content is read lazily in bounded compressed chunks. Actual output size and CRC are checked against central metadata; inflated output exceeding declared or per-file limits is rejected. No files are extracted to the PC filesystem.

## Local data and hosting

The Windows host serves only its staged app assets through a fixed HTTPS virtual origin. Native IPC checks the top-level sender URL, protocol version and per-launch capability; only named file/session/map/export, appearance/font and OpenAI operations are available. No COM host objects or shell proxy are exposed. The native service checks canonical relative paths, ignores generated directories, rejects alternate streams/reserved Windows names, and excludes reparse points and junctions. Only user-selected local roots are granted.

Native map writes compare the loaded sidecar revision and commit through a
temporary file with atomic replacement. This is optimistic external-change
detection, not cross-process compare-and-swap. Conflicts keep edits in memory and
preserve the on-disk original; export a backup or explicitly discard/reload.
Concurrent edits from other applications still require user reconciliation.

Map layouts accept only schema version 4 and are strictly validated in C# for
fields, canonical Markdown paths, paired bounded dimensions, finite coordinates,
16 colors and 24 local icons. Unknown fields, including legacy notes, are rejected.
Unsupported/invalid files are preserved. Connected browser folders open with read access; an explicit map
save click requests write permission. Browser writes check the revision before
opening and committing a temporary writable stream. These new map routes have
source/static review only; earlier security checks did not exercise them.

Edited Markdown lives in bounded managed document sessions and is saved through
an explicit copy action. Session state distinguishes unsaved work from an exported
copy; neither state implies that the original workspace file was replaced.
Source revisions and parsed-model cache entries have separate ownership so
eviction cannot discard an unsaved revision. Historical annotation designs are
retained as retired documentation, not active security boundaries.

Native PDF preparation uses a separate WebView2 with page scripts and web messaging disabled, a restrictive CSP and no external resource access. `PrintToPdfStreamAsync` writes through an atomic destination replacement after an OS save choice. Cancellation before commit preserves the previous destination; cancellation cannot undo an already completed commit. Debug smoke options require isolated directories and are rejected in Release.

Reading, map discovery, local statistics and export do not upload selected documents
or images. An explicit AI request is the exception: it sends the prompt, current
document or selection and collected reference text to OpenAI, including preflight.
The synthetic demo uses same-origin fetches. Versioned settings, a workspace
descriptor, a reading-state record and optionally a connected handle are stored
in browser storage; document contents are not persisted by workspace resumption.
Disabling remembering clears these entries and the stored handle. Storage failure
is handled as a session-only state. Historical browser regressions asserted no
external HTTP requests initiated by their document fixtures; they did not exercise
live AI sends or audit unrelated browser/runtime traffic.

Deployers must provide HTTPS, correct WebAssembly/module MIME types and complete local assets. A tested deployment-specific Content Security Policy is pending: OpenSilver interop and WebAssembly requirements must be evaluated before selecting script directives. No unverified CSP is presented as a security guarantee.

Archive, renderer and parser limits reduce exposure but are not a hard browser CPU timeout. Pathological inputs, cancellation latency and memory pressure still require adversarial browser validation before release.
