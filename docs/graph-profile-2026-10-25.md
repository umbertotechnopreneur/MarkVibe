# Dense graph performance — 2026-10-25

The current real workspace produced **55 documents and 506 directed links**.
The optimized renderer retains all of them. Hover frame p95 fell from 100 ms
to 33.3 ms in two final runs. Zoom improved, but still has slower frames.

## Scope and measurements

The owner requested profiling against a supplied local Markdown workspace in an
isolated Debug browser. Its Markdown files were imported read-only into a fresh
headless Edge 156.0.4314.42 context. External HTTP requests were blocked.
The viewport was 1600 × 1000, with a 1078 × 855 graph and 4,203 graph elements.
All runs used the same folder, gestures, renderer size and DevTools collection.

| Scenario | Before p95 | Final run 1 p95 | Final run 2 p95 |
| --- | ---: | ---: | ---: |
| Idle | 16.8 ms | 16.8 ms | 16.8 ms |
| Pan | 33.4 ms | 16.8 ms | 16.8 ms |
| Zoom | 66.6 ms | 33.3 ms | 50.0 ms |
| Hover | 100.0 ms | 33.3 ms | 33.3 ms |

Hover frames exceeding 32 ms fell from 55/64 to 11/144 and 10/148.
Zoom frames exceeding 32 ms fell from 36/134 to 9/135 and 14/138.
The baseline zoom had a 112 ms long task; neither final run recorded a task
over the browser's 50 ms long-task threshold. Idle retained zero layout,
style-recalculation and paint work in all runs.

These are one baseline and two final exploratory runs, not a controlled
benchmark or a native WebView2 frame-rate guarantee. Capture overhead and
machine load affect timing. The older October 23 report used a smaller folder
and a different Edge version, so its figures are not this comparison's baseline.

## What consumed the time

1. Hover magnification interpolated for 420 ms. For another 460 ms the renderer
   repeatedly measured every card and updated affected SVG curves. Moving over
   neighboring cards restarted this work. CPU samples prominently included
   `getBoundingClientRect`, `setAttribute` and `updateEdges`.
2. Those geometry changes also forced SVG painting, layer construction and
   GPU work. Hover paint time was 218.0 ms in the baseline versus 38.5–41.1 ms
   in the final runs; `Layerize` was 341.1 ms versus 34.2–115.0 ms. Removing
   JavaScript alone would not address this browser rendering cost.
3. Zoom changed overview title sizes and detail classes while the camera was
   moving. This invalidated text layout and styles during the gesture. Zoom
   style recalculation fell from 148.4 ms to 40.1–44.3 ms after stabilizing the
   presentation during movement.

These are trace totals over each gesture scenario. Categories can overlap
and run on different threads; do not add them as independent elapsed time.
The profiler now includes Layerize, Commit, RasterTask and GPUTask in its summary.

## Implementation

- At 250 or more rendered links, cards retain their selected/hovered size,
  depth, color, borders, content and relation emphasis, but change size
  immediately. Dense cards omit blurred shadows and decorative transitions.
- Dense-map geometry refreshes coalesce into one animation frame for adjacent
  leave/enter/focus events. There is no continuing magnification loop.
- During dense-map pan/zoom, the semantic detail level and title size remain
  stable. They update when the existing gesture settling timer completes
  (180 ms after the last input). Camera coordinates, overview label positions,
  percentage and minimap remain live.
- Smaller graphs retain their existing presentation. No document/link limit,
  graph parsing, persisted model, service contract or source document changed.

The existing profiling assertions passed for all scenarios: 1,012 connection
endpoints had less than 0.001 px settled error, arrows and hit targets returned,
and the browser reported no uncaught errors. These checks do not cover every
layout, selection, folder collapse, resize, accessibility or native interaction.

## Repeat the profile

Use the installed SDK 10.0.402 and Node >=22.12, with the existing dependencies.
Do not install dependencies or run a test suite just to profile this graph.

```powershell
npm run bundle:debug
dotnet build src/MarkVibe.Browser/MarkVibe.Browser.csproj -c Debug --no-restore
dotnet run --project src/MarkVibe.Browser -c Debug --no-build --no-restore --launch-profile MarkVibe
```

In another terminal:

```powershell
node scripts/profile-graph.mjs --folder 'E:\YourMarkdownWorkspace\docs' --output artifacts/graph-profile/local-run
```

Use distinct output directories for sequential runs, never concurrent profiles.
The script rejects non-loopback URLs and a Release bundle. `--url` can select
another loopback port; `--headed` shows the isolated profiling browser. It closes
that browser on completion or failure. Stop the development server afterwards.

Local evidence for this session is in ignored
`artifacts/graph-profile-2026-10-25/`: `before-1`, intermediate `after-1`, and
final `after-2` / `after-3`. Packaging clears `artifacts/`; retain any needed raw
traces privately before packaging. Do not commit traces or private documents.

## Use Developer Tools interactively

Open `http://localhost:5080` in a separate Edge profile while the Debug server
is running. Open the folder, switch to Map and allow indexing to finish.
Press F12, select **Performance**, record one gesture, then stop recording.
Use **Sources** with the linked source maps to inspect `client/map.js` and
`client/map-atlas.js`. In Performance, import a saved `*.trace.json`; import
`*.cpuprofile` in the JavaScript Profiler to inspect sampled JavaScript stacks.
The scripted importer blocks external requests; an ordinary manual browser
session does not inherit that interception. Keep documents and traces local.

Compare p95 frame time, long tasks, layout, paint and compositor work, with
the same graph and gesture. A low JavaScript total does not prove smooth GPU
rendering. Closing Developer Tools while recording through CDP avoids the cost
of painting the DevTools panels themselves.

This is an isolated Debug browser facility. It does not enable Developer Tools
in installed Release or replace the installed package. After profiling,
`npm run bundle` restores optimized Release client assets before a Release build.

## Validation boundary

The Debug Browser project compiled successfully with existing NU1902/NU1903
dependency advisories. JavaScript/CSS/JSON parsing and diff checks passed.
The isolated server was stopped and the optimized Release client bundle was
regenerated with its build guard passing. This does not build or install a new
Release package. No dependencies were changed. No broad C#/JavaScript
test suite, native GUI acceptance, installation, commit or push ran for this work.
Remaining zoom variation and native behavior require separate measurement;
this report does not claim the whole application is now allocation-free or fully
optimized.

On the subsequent delivery request, Release x64 0.1.0.32 was built from source,
installed over 0.1.0.31 and launched. Package contents, installed status and
process path were checked. Raw traces were retained across packaging cleanup.
Installation does not extend the Debug measurements to native GUI acceptance.
