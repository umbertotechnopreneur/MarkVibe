<!-- VBWR B

  Project: MarkVibe
  Repository: https://github.com/umbertotechnopreneur/MarkVibe
  Creator: Umberto Giacobbi | https://umbertogiacobbi.biz

  VibeWare: Human intent, AI execution, and plenty of tokens
  Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto

  Created with AI: OpenAI Codex; Markdown AI contract and validation scope; 2026-10-11
  Human guidance: Umberto Giacobbi; stateless scoped edits, semantic costs and a one-minute deadline
  Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md

  Copyright (c) 2026 Umberto Giacobbi
  License: MIT (see LICENSE)

VBWR E -->

# Markdown AI editing

Use **AI** beside the view controls. Without a saved OpenAI key, the component
shows a dismissible activation banner linked to Settings. A configured key exposes
the plain-text XAML prompt composer, account-available models in a native Popup, and five
reasoning levels: Low, Medium, High, Very high and Maximum. There are no speed tiers
or Ultra reasoning option. Enter submits; Shift+Enter inserts a line break.

Select document text before moving focus to the prompt to edit that selection.
The managed request captures the selection and document fingerprint before sending.
Reader text is mapped only where an exact literal source span exists. Unsupported or partially overlapping selections disable submission
and direct the user to Source; they never silently become whole-document edits.
Monaco and the plain-source fallback map raw UTF-16 offsets, including CRLF and
mixed line endings. Generated diagram/image text is not a Markdown source range.

## One request, one replacement

Each send contains two fixed system messages, the current document/selection,
and the current editing instruction. The application message identifies MarkVibe,
its creator, purpose, views and exports, explains why the user supplied the prompt,
and states that edit text will replace the supplied source. The policy message
limits work to Markdown editing/review and rejects image/media generation, new
books, fifty-page files and unrelated bulk output. Existing long documents can be
revised or shortened. Document instructions are content, not application authority.

Responses use a strict JSON schema with `kind`, `markdown` and `summary`. A complete
`edit` is parsed and automatically replaces the captured document or selection;
a `review` is displayed. The component grants no tools, sets `tool_choice: none`
and `store: false`, explicitly selects Standard service, sends no conversation/history or previous response ID, and
never executes returned code. Reviews appear in a read-only XAML text control. Edited documents use the existing
managed Markdown parser and browser sanitizer. Refused, truncated, malformed, obsolete or cancelled
responses cannot replace source. See [OpenAI structured outputs](https://developers.openai.com/api/docs/guides/structured-outputs).

An elevated popup covers the document during token preflight and generation,
with a theme-aware dimmed backdrop, 16 px backdrop blur, progress and Stop.
The composer, model flyout, usage expander, review and waiting surface are OpenSilver
UserControls; JavaScript owns only rendering, selection and provider adapters.
AI Assistant can be dragged by its transparent title bar and compacted without
clearing the prompt. A single model/effort selector opens a composer-width native
flyout covering the complete prompt and action row. Its native slider has a rounded,
28-pixel track; accent saturation increases across the five existing levels with
a 160-millisecond color transition. Reasoning changes update the label immediately and retain
the model controls and their scroll position; only catalog or language changes
rebuild that list.
Keyboard guidance and the approximate prompt count share a compact footer row
(`≈ N TK`). The character count is omitted. The collapsed-budget panel is 232
pixels high, and its content can scroll without a visible panel scrollbar.
The entire network operation has a **60-second deadline**, not a fresh minute
for each HTTP call. Cancellation propagates into browser/native HTTPS. A late reply
is discarded before the source commit. No automatic retry repeats billed generation.

## Catalog, cost and context

The embedded [model snapshot](../src/MarkVibe.Core/AI/openai-models.json) retains
the retrieval timestamp, sources, Standard text prices, long-context multipliers,
supported reasoning levels, full context, maximum input and maximum output.
It was downloaded from official documentation on 2026-10-11. Account availability
comes separately from `GET /v1/models`; that endpoint does not supply this pricing
and context catalog. Refresh its public facts explicitly with:

```powershell
node scripts/update-openai-models.mjs
```

The downloader uses only official public Markdown documentation, no credential
or private files. It replaces JSON only after all downloads and required fields
succeed. Pricing is a dated snapshot and may change, including promotional rates.
The app does not silently download catalog changes during a request.

Model badges show **Cheap** in green, **Affordable** in amber, and **Prohibitive**
in red. These are application-defined relative Standard output-price categories:
up to 5 USD per million output tokens, up to 20, and above 20 respectively.
Only the categories appear in the UI. The documented long-input multiplier can
change the badge. Actual usage depends on input, output, reasoning and caching;
the badges are not account-specific invoices. [Official OpenAI pricing](https://developers.openai.com/api/docs/pricing).

The seven retained models currently document 1,050,000 context tokens, at most
922,000 input tokens and 128,000 output tokens. MarkVibe reserves the documented
maximum output for response plus reasoning and uses the remaining full input
capacity. It does not shrink context to a small conversational budget. The app's
existing **1 MiB document**, block/headings and **16,000-character prompt** limits
still apply independently. [GPT-6.1 Sol limits](https://developers.openai.com/api/docs/models/gpt-6.1-sol).

Before generation, three cumulative input counts measure system/schema, then
document/selection, then prompt. Exact full-input counts enforce the model limit;
oversized requests receive a clear message recommending a smaller selection or
shorter prompt. If counting is unavailable, a conservative UTF-8 byte bound permits
only requests that fit; a large unverified request is blocked. Partial counts may
still establish the total while leaving the individual shares estimated.

The context bar uses the model's full capacity as its scale and displays input
shares, response reservation, free space and occupied percentage. The token bar
separately shows System, Prompt, Document, Response and Reasoning, with exact API
usage totals after completion and explicit approximation markers otherwise.
Reasoning is a subset of output and cached input a subset of input; neither is
added twice. Prefix attribution is exact only when the full preflight count agrees
with actual input usage. Session counters retain numbers, not conversational text.
[OpenAI token counting](https://developers.openai.com/api/docs/guides/token-counting).

## Local drafts and original files

Edits replace the in-memory document and update its reader, source, headings,
search and map metadata. Local Undo/Redo requires no API call and is never added
to the next request. Revision/session checks prevent an old response from modifying
another page. Active graph-card gestures prevent a commit; document renderer input
freezes briefly during the managed replacement. Ink and sticky notes are retired.

**Save Markdown copy** opens an explicit native destination or starts a browser download. The native
picker prevents replacement of the current original file. Browser download initiation
does not prove completion and retains the unsaved-change warning. **Restore original**
discards local Markdown edits after confirmation. Closing or replacing a workspace
protects unsaved drafts and requests. Draft sessions are limited to sixteen pages;
restored or explicitly saved sessions can expire without discarding unsaved source.
Original files and annotation sidecars are not silently rewritten by an AI reply.

## Validation for this contribution

The official public catalog downloader was executed successfully. The current client
bundle and full solution build succeeded with zero warnings and errors. Direct local
inspection checked XAML controls and specialized renderer boundaries. Automated tests,
inference, live key validation, installation and publication were not run. Network
cancellation, selections, undo and saving still require their own functional acceptance.
See [active work](../.github/tasks/todo.md).
