<!-- VBWR B
Project: MarkVibe | Creator: Umberto Giacobbi
Created with AI: OpenAI Codex; measured Debug browser graph optimization; 2026-10-23
Human guidance: Umberto Giacobbi; reproduce graph lag with DevTools and local Markdown
Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
VBWR E -->

# Debug graph profile — 2026-10-23

The graph performs less repeated style and compositor work during navigation.
On the owner's supplied folder, zoom's 95th-percentile frame interval fell from
66.7 ms to 33.4 ms. Some 59–78 ms main-thread tasks remain; this is an improvement,
not a claim of uniformly smooth rendering or native desktop acceptance.

## Method and measurements

The existing Browser project served the actual C#/OpenSilver application in Debug.
An isolated headless Edge 155.0.4283.62 session imported the supplied folder read-only:
32 Markdown documents, 337 connections and 2,577 graph DOM elements. The viewport
was 1,600 × 1,000, with a 1,078 × 855 graph. Remote HTTP requests were blocked.
No document content or credentials were uploaded or added to tracked reports.

The harness waited for `map.indexComplete`, then captured the same idle, pan,
32-step wheel zoom and card-hover workloads through the Chrome DevTools Protocol.
Traces contain timeline/layout/paint events, CPU samples, long tasks and animation
frame intervals. Measurements include tracing overhead and normal OS scheduling.
Each row below compares one complete baseline with the final verified run; these
are not multi-run medians. Two repeat runs measured zoom p95 at 33.4 and 33.3 ms.

| Measured work | Before | After |
| --- | ---: | ---: |
| Zoom layout passes | 190 | 36 |
| Zoom style recalculation time | 864.7 ms | 62.4 ms |
| Zoom frame interval, p95 | 66.7 ms | 33.4 ms |
| Zoom frames exceeding 32 ms | 35 / 171 | 13 / 151 |
| Zoom longest main-thread task | 208 ms | 78 ms |
| Hover frame interval, p95 | 33.3 ms | 16.8 ms |
| Pan frame interval, p95 | 16.8 ms | 16.8 ms |
| Idle layout/style passes and long tasks | 0 | 0 |

The worst intermediate zoom stalls were dominated by browser `Layerize` and
`Commit`, rather than Markdown parsing. Removing repeated writes helped, but
removing the connection opacity/stroke transitions produced the larger frame-time
improvement. An experimental `will-change: transform` layer did not improve the
result and is not included.

A separate generated stress folder rendered 120 cards and 600 connections
(7,598 graph elements). It measured zoom p95 33.4 ms, hover p95 33.5 ms and pan
p95 16.8 ms, with no long tasks during those captured scenarios. All 1,200
connection endpoints passed the same alignment checks. This has no paired
baseline and is not a substitute for the owner's larger real workspace.
Its evidence is under `artifacts/graph-profile-2026-10-23/stress/`.

Local evidence is in `artifacts/graph-profile-2026-10-23/before-ready/` and
`after-verified/`: `summary.json`, per-scenario `.trace.json` and `.cpuprofile`.
`after-final/` contains the second successful final implementation measurement.
`confirmed-ready/` confirms the completed Debug build and explicit opening
readiness, with the same endpoint checks and zoom p95 at 33.3 ms.
Earlier experiments and incomplete imports are not used as the baseline.

## Changes

- Coalesce pan/zoom drawing into one animation frame. Read viewport dimensions
  before writing styles. Cancel deferred work on suspension, replacement and disposal.
- Scope changing hit-target widths to the SVG, update them only when scale changes,
  and defer them during camera gestures. Arrowheads and hit targets return when
  the gesture settles; connections remain visible throughout. Hover magnification
  resumes under the stationary pointer after movement ends.
- Keep final connection colors and opacity, without their repeated dissolves.
  Card magnification, keyboard focus, routes and directed links remain available.
- Reuse minimap rectangles and semantic-zoom labels. Group folder members once
  per draw and avoid unchanged marker, title, label and SVG attribute writes.
- Avoid connection animation and lens recomputation when hovering the already
  selected card. Preserve managed state ownership and existing saved schemas.
- Add `bundle:debug` with readable JavaScript and linked source maps. Release
  guards reject a Debug bundle or source maps, and packaging keeps its normal bundle.

## Reproduce locally

Use the repository's existing .NET 10.0.402 and Node >=22.12. New external imports
still require the documented targeted first-import scan; this work added none.

```powershell
npm run bundle:debug
dotnet build src/MarkVibe.Browser/MarkVibe.Browser.csproj -c Debug --no-restore
dotnet run --project src/MarkVibe.Browser/MarkVibe.Browser.csproj -c Debug --no-build --no-restore --launch-profile MarkVibe
```

With that server running, use a second terminal:

```powershell
node scripts/profile-graph.mjs --folder "<markdown-folder>" --output artifacts/graph-profile
```

The profiler requires an explicit Debug bundle, accepts only a loopback server,
blocks external HTTP and closes its
isolated browser on completion. `--headed` is available for explicitly authorized
visible debugging. Raw traces can be inspected in browser Developer Tools.
The browser shell is at `http://localhost:5080`; it has its own browser session
and does not replace the installed Release application.

## Validation boundary

The final real-folder run reported no browser errors. After each measured scenario,
all 674 connection endpoints stayed aligned within 0.004 screen pixels; arrowheads
and pointer targets were restored and no camera gesture remained active.
Twenty JavaScript checks cover geometry batching, minimap identity, lens behavior,
gesture cancellation/suspension and Debug/Release bundle separation.

All 22 Core checks passed in Debug. Three targeted browser scenarios passed:
immediate Back/Forward; grid arrangement, selection, zoom, advanced view and
double-click opening; card resize, JSON export and both cancel/confirm reset paths.
These runs reported no JavaScript/HTTP errors or external requests. The initial
broader browser run was interrupted after failures; the full 18-scenario suite
was not completed. Relevant obsolete toolbar/dialog selectors were updated
without removing their behavior assertions.

The browser checks also exposed a real startup race: the first page was visible
while provider opening was still finishing, and Back was discarded by the busy
guard. The shell now retains the latest requested history entry and restores it
after opening finishes. Open file/folder controls remain disabled until that
same boundary completes. The immediate Back regression passes without adding a
delay. Resize tests and the profiling harness wait for explicit control/graph readiness
and managed acknowledgments rather than fixed startup sleeps.

The final Debug Browser build passed with zero errors and 18 existing
NU1902/NU1903 dependency-advisory warnings. Its bundle passed Debug validation
and was rejected by Release validation as intended. Build and check logs remain
under the same dated artifact folder; `browser-delivery-checks/results.json`
contains the three successful browser results on the completed Debug build.
The profiling phase did not install or publish an application. The owner's later
delivery request built and installed Release x64 0.1.0.29, with Release bundle
validation passing. A separate review also corrected the native read-size race
and passed all nine Native checks. The installed process was verified; no native
performance measurement or GUI acceptance was performed during delivery.
These measurements do not cover native WebView2, first-open latency,
the larger workspace shown in earlier screenshots, or all graph arrangements.
