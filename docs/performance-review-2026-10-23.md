<!-- VBWR B
Project: MarkVibe | Creator: Umberto Giacobbi
Created with AI: OpenAI Codex; measured catalog cleanup and regression review; 2026-10-23
Human guidance: Umberto Giacobbi; make the application leaner, preserve compatibility and run relevant C# and JavaScript tests
Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
VBWR E -->

# Performance review — 2026-10-23

The catalog now avoids repeated sorting and unchanged Markdown parsing. Source
view cleanup releases unused Monaco models and detached fallback documents.
Five private UI handlers with no source, XAML or reflection callers were removed.
Existing saved formats, bridge commands, provider capabilities and supported
Markdown features are unchanged.

## Implemented changes

- `DocumentCatalog.Cards` reuses a read-only sorted snapshot until metadata changes.
  Old snapshots retain their values after indexing, editing or reseeding.
- Indexing still reads the provider, applies local revisions and checks its byte
  budget. An existing source fingerprint allows reuse of successful metadata.
  Changed source updates cards, graph facts and any retained parsed model.
  Failed-index placeholders are tracked separately so restoring identical source
  clears the error. Opening unchanged content no longer invalidates graph topology.
- Card construction avoids splitting source into lines when there are no link excerpts.
- Source-model eviction continues past active models to dispose unused ones.
  Replacing an editor clears references to detached textarea/line views and
  releases a partially created editor before falling back after an error.
- Removed only `ImportFolder`, `OpenZip`, `ReopenFolder`, `AddFiles` and
  `RefreshWorkspace` private event wrappers. Their provider operations and saved
  workspace modes remain supported.
- Native recent-path storage creates its data folder before the first atomic save.
  The new native tests exposed this missing initialization.

The preceding graph changes also reuse topology, coalesce drag/resize work into
one frame and retain unchanged SVG curves. New synthetic checks cover frame
coalescing, final-coordinate flushing, cancellation and visible/hit-path agreement.

## Measurements

A temporary Release .NET 10 harness compared the catalog at commit
`bab3f8ee89e02f8fff70c3a9b9d2f3373e10c47a` with the current source in the same process.
It used 512 generated Markdown files totaling 1,369,554 UTF-8 bytes, in-memory
reads, two warm-up indexing passes, five paired samples and disabled tiered JIT.
Values below are medians. Allocation values are bytes allocated during the
operation, not retained memory or process working set.

| Workload | Before | After | Allocations before → after |
| --- | ---: | ---: | ---: |
| Reindex the unchanged, already seeded catalog | 121.93 ms | 1.43 ms | 127.86 MB → 1.65 MB |
| Read the sorted card collection 2,000 times | 80.92 ms | 0.021 ms | 29.35 MB → about 1.7 KB |

These results isolate repeated catalog work. They do not measure first-open time,
disk access, WebAssembly execution, UI responsiveness or graph frame rate. A fresh
`Seed` still resets metadata and requires indexing; the unchanged-index result
must not be presented as a full folder-refresh or startup speedup.

Local harness, input baseline and raw samples are under
`artifacts/review-2026-10-23/benchmark/` and `catalog-benchmark.json`.

## Validation and remaining opportunities

- 22 Core checks, 8 Native checks and 13 JavaScript checks passed: 43 total.
  Coverage includes existing path/ZIP boundaries, drafts, undo/redo, exports,
  stale revisions and map JSON, plus cache invalidation, error recovery, recents
  persistence/cancellation and source-resource cleanup.
- C# test projects compiled in Release. Missing test restore assets were restored
  from the existing local NuGet cache; no external dependencies were introduced.
- Changed C# syntax, XAML XML and Git whitespace checks passed. JavaScript host
  tests bundle real host methods with synthetic browser/Monaco boundaries; they
  do not validate real Monaco integration or desktop rendering.
- Monaco and Mermaid are already loaded on demand. Their feature sets and language
  support were retained. Removing bundled capabilities just to reduce bytes would
  change functionality; dependency changes were outside this cleanup.
- Subsequent browser profiling measured the graph and identified repeated style
  invalidation and SVG transitions as major costs. See
  [the graph profile](graph-profile-2026-10-23.md) for the corrections and limits.

This review initially ran without GUI checks or delivery. The owner's later
request built and installed Release x64 0.1.0.29 containing these changes, and
authorized Git integration through PR #3. Native performance and GUI acceptance
remain separate from that installation.
