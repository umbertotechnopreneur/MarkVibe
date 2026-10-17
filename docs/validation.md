# Verification

Double-click `Verifica-MarkVibe.cmd` on Windows and choose a profile. The same menu is available with `npm run check`. These commands **prepare and run checks**; use them only when verification is explicitly requested. A request to build or package alone does not authorize running them.

| Profile | Checks |
| --- | --- |
| Rapida | Core document revisions, navigation, map contracts, native file persistence, client paths and ZIP boundaries |
| InterfacciaWindows | Debug x64 desktop startup, native file containment, map selection/resize/autosave/conflicts, settings, HTML/PDF and restart |
| Completa | Rapida and InterfacciaWindows, plus browser navigation, source views, providers, map interaction, preferences, export and content boundaries |

The runner writes a standalone `report.html`, `results.json`, stage logs and any UI screenshots under `artifacts/checks/<UTC time>-<process id>/`. **Passed** means that stage completed successfully; **Failed** identifies a problem; **NotRun** identifies a dependent stage or missing prerequisite that was not checked. Exit codes are 0 for success, 1 for failure and 2 for incomplete verification. There is no coverage-percentage claim.

## Prerequisites and isolation

Use an existing PowerShell 7, Node >=22.12 and the exact .NET SDK in `global.json`. UI profiles require Windows, installed Microsoft Edge and WebView2 Runtime. Missing tools produce an actionable report; the runner never installs an SDK, browser, app or certificate.

The runner restores JavaScript packages from the committed npm lock with lifecycle scripts disabled and restores the selected .NET projects in locked mode. Playwright is a pinned development dependency, so no machine-specific module path is needed. It uses installed Edge and does not download browsers.

Windows UI checks build Debug x64 and explicitly pass the resulting executable to an isolated smoke process. The profile, document fixtures and export destinations are synthetic. Smoke mode reports no configured OpenAI key and rejects AI operations before reading user credentials. Browser checks start with a fresh profile and no saved key. No API calls, packaging, version increment, installation or publication belong to verification.

The existing packaging command still cleans the entire repository `artifacts/` directory by owner policy, including old check evidence. Keep any required reports before requesting another package.

## Command-line and manual CI

```powershell
./scripts/check.ps1 -Profile Rapida
./scripts/check.ps1 -Profile InterfacciaWindows
./scripts/check.ps1 -Profile Completa -OpenReport
```

The manually dispatched **Verify MarkVibe** workflow uses these same profiles. Rapida runs on Windows and Linux; interface profiles run on Windows. It uploads reports even after failure. The packaging workflow remains separate and runs no tests.

For targeted diagnosis, `npm run test:client`, `npm run test:browser` and `npm run test:windows` remain available. Browser checks require an already prepared running host; `MARKVIBE_BASE_URL` selects it. The Windows harness accepts `MARKVIBE_WINDOWS_EXECUTABLE` and defaults to `src/MarkVibe.Windows/bin/Debug/net10.0-windows10.0.19041.0/win-x64/MarkVibe.Windows.exe`. The menu handles these details automatically.

## Build freshness

Both packaging and verification restore npm dependencies deterministically. `scripts/prepare-client.ps1` provides the explicit dependency-and-bundle step for a normal build:

```powershell
./scripts/prepare-client.ps1
dotnet restore MarkVibe.slnx --locked-mode
dotnet build MarkVibe.slnx --no-restore
```

The browser project checks a fingerprint of client sources, shared JSON catalogs, scripts and dependency locks before building. Missing, stale or incomplete bundles stop the build with preparation instructions. This guard only reads files; it does not silently install or bundle anything.

## Evidence boundaries

The updated runner and scenarios were authored on 2026-10-16. They have **not been executed as part of this cleanup**, and no new passing results are claimed.

Historical checks from 2026-10-11 are recorded in [the historical validation report](validation-results-2026-10-11.md). Those results preceded retired ink controls, the current map interactions and document revision changes. They are not evidence that the current application passes these scenarios.

UI automation uses synthetic documents and software pointer events. Physical pen/touch, OS picker choices, printer output, installation, ARM64 hardware, accessibility certification and production deployment remain separate acceptance work.
