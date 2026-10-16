<!-- VBWR B
Project: MarkVibe
Repository: https://github.com/umbertotechnopreneur/MarkVibe
Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
VibeWare: Human intent, AI execution, and plenty of tokens
Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
Created with AI: OpenAI Codex; focused maintainability review and concrete before/after examples; 2026-10-17
Human guidance: Umberto Giacobbi; remove duplication, improve readability and require no historical compatibility
Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
Copyright (c) 2026 Umberto Giacobbi
License: MIT (see LICENSE)
VBWR E -->

# Maintainability review — 17 October 2026

This pass reviews application-owned AI dispatch, browser requests and storage,
map persistence/session state, and shared managed contracts. It implements the
focused changes below, rather than claiming every project file has been audited.
Generated output, vendored libraries and third-party notices are not rewritten.

## Findings and implemented changes

| Finding | Concrete change | Relevant source |
| --- | --- | --- |
| Duplicate AI protocol families across three layers | Replace eight `openai.*`/`codex.*` commands with three provider-explicit edit commands; retain the existing two connection commands. No obsolete aliases remain. | [client dispatch](../client/main.js), [AI adapter](../client/openai.js), [managed caller](../src/MarkVibe/MainPage.Ai.cs) |
| Unused public credential operations after Settings gained provider-specific tests | Remove `openai.status` and `openai.validate` bridge handlers. Keep browser credential functions private and native credentials behind `AiConnectionService`. | [native AI dispatch](../src/MarkVibe.Windows/DesktopWindow.Ai.cs) |
| AI argument decoding mixed into desktop file/printing dispatch | Move the current AI operations into a documented partial file; decode the provider and captured request once. Keep authentication, cancellation and smoke isolation before dispatch. | [DesktopWindow.Ai.cs](../src/MarkVibe.Windows/DesktopWindow.Ai.cs) |
| Duplicate browser fetch, timeout and error handling for key validation | Use `sendOpenAi` for both validation and edits, retaining validation-specific timeout/messages. Validation now also uses the bounded response reader. | [openai.js](../client/openai.js) |
| Stream cancellation can obscure a request deadline | Classify caller cancellation first, the request deadline second, then other failures; a stream-level WorkspaceError must not hide a timeout. This is a source-level correction, not a claimed runtime reproduction. | [sendOpenAi](../client/openai.js) |
| Repeated version-envelope/storage handling | Share bounded `readStoredValues` and `saveStoredValues`; keep preference and layout normalization separate. | [settings.js](../client/settings.js) |
| JSON codec and mutable session combined in one file | Move the existing public session type into its own file without introducing an adapter or renaming its contract. | [MapLayout.cs](../src/MarkVibe.Core/MapLayout.cs), [MapLayoutSession.cs](../src/MarkVibe.Core/MapLayoutSession.cs) |
| Long mixed validation expressions | Separate position, paired dimensions, finite bounded sizes and supported appearance. Extract raw JSON atlas/dimension checks and card-overlap geometry. | [map codec](../src/MarkVibe.Core/MapLayout.cs), [session geometry](../src/MarkVibe.Core/MapLayoutSession.cs) |
| Repeated JSON string parsing | Deserialize the layout from the already parsed `JsonElement`; retain duplicate-property checks and exact schema validation. | [MapLayoutCodec.Read](../src/MarkVibe.Core/MapLayout.cs) |
| Dense nested browser calls and repeated argument encoding | Introduce named request/response phases and a shared argument encoder. | [BrowserServices.cs](../src/MarkVibe/BrowserServices.cs) |
| Missing purpose/contract documentation | Add useful XML documentation for shared types, browser operations, map records/session and extracted helpers. Expand the native model reader and token-count checks. | [Contracts.cs](../src/MarkVibe.Core/Contracts.cs), [MarkdownAiClient.cs](../src/MarkVibe.Windows/MarkdownAiClient.cs) |
| Old-browser ID compatibility branch | Require the supported secure-context UUID API instead of maintaining a time/Math.random alternative. | [path.js](../client/path.js) |

## Example: one internal AI protocol

Previously the managed caller chose a command family:

```csharp
return (provider == AiProviders.CodexCli ? "codex." : "openai.") + operation;
```

That choice was repeated in JavaScript exports and native switch cases. Status
and validation operations were still publicly exposed even though Settings had
already switched to connection-specific operations. These were actual active or
unused internal routes, not evidence of a separate format migration system.

The current caller sends the provider explicitly:

```csharp
var exchange = await BrowserServices.CallAsync<MarkdownAiExchange>(
    "ai.edit",
    new { provider, request, payload = MarkdownAi.Payload(request), prefixTokens },
    cancellation.Token);
```

The browser forwards only provider, request and prefix counts to the Windows
host. Native code reconstructs the API payload and selects the provider through
`AiConnectionService`. The browser implementation still uses its managed payload.
Unknown providers fail rather than choosing another credential or billing path.
Old commands are rejected; both ends of the internal contract change together.

## Example: explicit request/response phases

Before, a complete round trip occupied one nested expression:

```csharp
public static async Task<T> CallAsync<T>(string operation, object? arguments = null, CancellationToken cancellationToken = default) => Wire.Decode<T>(await Runtime.CallAsync(operation, arguments == null ? "{}" : Wire.Encode(arguments), cancellationToken));
```

After, each intermediate value explains its role:

```csharp
public static async Task<T> CallAsync<T>(
    string operation,
    object? arguments = null,
    CancellationToken cancellationToken = default)
{
    var requestJson = EncodeArguments(arguments);
    var responseJson = await Runtime.CallAsync(operation, requestJson, cancellationToken);
    return Wire.Decode<T>(responseJson);
}
```

The response-free overload shares the argument encoder but does not introduce
typed JSON decoding where the old implementation deliberately ignored a result.

## Example: geometry rules with names

The old card validator combined null checks, finite coordinates, coordinate
limits, paired nullable dimensions, dimension limits and style membership in one
condition. The same rejection contract now has separate phases:

```csharp
var validPosition = double.IsFinite(card.X)
    && double.IsFinite(card.Y)
    && Math.Abs(card.X) <= MaximumCoordinate
    && Math.Abs(card.Y) <= MaximumCoordinate;
var pairedDimensions = card.Width.HasValue == card.Height.HasValue;
var validDimensions = IsValidDimension(card.Width, MinimumCardWidth, MaximumCardWidth)
    && IsValidDimension(card.Height, MinimumCardHeight, MaximumCardHeight);

if (!validPosition || !pairedDimensions || !validDimensions)
{
    throw Invalid();
}
```

Raw JSON checks still distinguish omission from explicit null. Named overlap
bounds preserve the twelve-unit placement gap. Comments explain those invariants
instead of merely describing the next statement.

## Example: remove a compatibility branch

Before:

```javascript
export const makeId = () => globalThis.crypto?.randomUUID?.()
  ?? "w-" + Math.random().toString(36).slice(2) + Date.now().toString(36);
```

After:

```javascript
export const makeId = () => globalThis.crypto.randomUUID();
```

Supported browser hosting must provide a secure context, such as HTTPS or
localhost, and the UUID API. No alternative old-browser identity path remains.
Existing workspace IDs are opaque identifiers and are not rewritten or deleted.

## Compatibility policy and boundaries

The owner explicitly requires no historical compatibility. `AGENTS.md` now says
to support only current application-owned schemas and commands, without old
aliases, migrations or decoders unless explicitly requested. This pass does not
invent a historical migration that was not found: map schema 4 and version-1
storage envelopes already reject unsupported versions.

Defaults for absent/corrupt preferences, bounds on untrusted JSON, file containment,
cancellation, current CLI capability checks and the compact/plain-text source view
remain. They protect current operation or implement real host capabilities; they
are not historical-format support. Retiring a format never authorizes deleting
the user's files. The native and browser security boundaries retain independent
validation rather than trusting an earlier process to have checked input.

## Remaining structural opportunities

These are observed maintenance candidates, not confirmed functional defects or
completed changes in this pass:

1. [SubmitMarkdownAiAsync](../src/MarkVibe/MainPage.Ai.cs) still coordinates request
   capture, references, generation, previews and UI state. A follow-up could give
   a request-session object responsibility for its captured provider/generation,
   target revision and cancellation. Keep final revision checks and commit locking
   in the catalog/shell; merely moving a long method would not establish ownership.
2. [CodexCliClient](../src/MarkVibe.Windows/CodexCliClient.cs) combines executable
   discovery, capability probing, process lifetime and JSONL event parsing. A small
   process runner and a separate protocol accumulator would clarify those contracts.
   Preserve output bounds, process-tree termination, unexpected-tool rejection and
   the distinction between a local login check and a remote response.
3. Native [key validation](../src/MarkVibe.Windows/OpenAiKeySettings.cs) and
   [Markdown HTTP transport](../src/MarkVibe.Windows/MarkdownAiClient.cs) still
   repeat request/error plumbing. A shared transport could remove this duplication,
   but must preserve their different 20-second/4 MiB and 60-second/8 MiB policies,
   sanitized messages and validation-before-save ordering. The browser equivalent
   was consolidated in this pass; the native transport was not.

## Validation and delivery limits

- `npm run bundle` succeeded, validating the new JavaScript exports/imports and
  preparing generated client assets through the existing build script.
- Debug x64 compilation of `MarkVibe.Windows` and its application dependencies
  succeeded after the final source change: zero errors, eighteen existing
  NU1902/NU1903 warnings for transitive Pkcs 8.0.1 and Xml 8.0.4.
- The existing Windows smoke fixture was updated to use `ai.connection.status`;
  it was not executed. No new passing test result is claimed.
- No suites, GUI checks, screenshots, live provider tests or inference ran.
  No packaging, version increment, certificate change, installation or app launch
  belongs to this pass. The installed 0.1.0.25 predates these source changes.
- Source-level changes do not prove end-to-end editing, cancellation or map-save
  acceptance. Relevant owner checks are both providers, invalid provider rejection,
  cancellation during preparation/edit, strict map read/write and saved settings.
