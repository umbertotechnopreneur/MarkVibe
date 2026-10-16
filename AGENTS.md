# Repository Guidelines

## Project Structure & Module Organization

- `src/MarkVibe.Core/`: Markdown model, navigation, catalog and ink state.
- `src/MarkVibe/`: OpenSilver C#/XAML shell, Modern Theme, About and DOM hosts.
- `src/MarkVibe.Browser/`: WebAssembly bootstrap and local `wwwroot/` assets.
- `src/MarkVibe.Native/`: contained files, atomic sidecars and session persistence.
- `src/MarkVibe.Windows/`: WPF/WebView2, native pickers and direct PDF saving.
- `client/`: browser providers, sanitization, Monaco, Mermaid and rendering.
- `packaging/windows/` and `scripts/`: versioned MSIX/portable packaging.
- `tests/`, `schemas/`, `docs/`: checks, ink contract and architecture.

C# owns state and parsing. Never edit generated `wwwroot/js/`, `bin/` or `obj/`.

## Build, Test, and Development Commands

Use existing .NET SDK **10.0.401**, Node **>=22.12** and execution authorization:

```powershell
npm ci --ignore-scripts --no-audit --no-fund
npm run bundle
dotnet restore MarkVibe.slnx --locked-mode
dotnet build MarkVibe.slnx --no-restore
dotnet run --project src/MarkVibe.Browser --no-build --launch-profile MarkVibe
dotnet run --project src/MarkVibe.Windows --no-build
./scripts/package-windows.ps1 -Configuration Release
```

Packaging cleans repository `artifacts/`, increments the package version and creates x64/ARM64 MSIX, a bundle and portable ZIPs. Debug uses `-Configuration Debug`. It runs no tests. Outputs and temporary packaging files stay in repository `artifacts/`; never create them in Codex directories.

## Coding Style & Naming Conventions

Use English, four-space C#, two-space JavaScript and existing XAML indentation. Use PascalCase C# members, `_camelCase` fields and camelCase wire properties. Preserve nullable annotations, ES modules and LF. No formatter/linter is configured. Icons are local SVG symbols; retain accessible button labels.

Apply these readability rules to all new or changed code:

- Document every method/function with a useful comment explaining its purpose and any relevant contract. Prefer XML documentation in C# and an appropriate documentation comment in JavaScript or PowerShell.
- Add comments inside methods when the logic is not straightforward. Explain assumptions, ordering, invariants and the reason for a decision rather than restating each statement.
- Use descriptive variable and parameter names; avoid unclear abbreviations and single-letter names when their meaning is not immediately obvious.
- Prefer readable blocks and explicit intermediate steps. Avoid dense one-liners, multiple statements on one line and compact expressions that obscure control flow.
- Preserve a VibeWare header on maintained project source, script and markup files, using the exact `VBWR B` / `VBWR E` markers and the file format's comment syntax. Record only supported AI contributions and human guidance; use **MIT** for project code and retain the copyright holder from `LICENSE`.
- Keep generated output, vendored libraries and third-party theme assets under their existing notices. JSON and lockfiles remain valid data without comment headers. Keep the README footer and `AI-DEVELOPMENT-DISCLOSURE.md` aligned with the available evidence.

## Testing Guidelines

**Run tests only when the user explicitly requests them for the current task.** Earlier approvals do not override this. Suggest relevant checks without running them. Authorized options: core/native executable checks, `npm run test:client`, `test:browser`, `test:windows`. Use descriptive behavior names and synthetic fixtures; no coverage percentage is required. Installer acceptance is a requested manual installation; never install an app or certificate implicitly.

## Commit & Pull Request Guidelines

Use focused imperative subjects. Describe the problem, result and actual validation; link issues and UI screenshots when available. Update dependency locks. Record when tests were not requested.

## Agent Workflow

Ask permission before **each branch or worktree creation**. Keep active work in `.github/tasks/todo.md`, milestones in `archive.md` and findings in `lessons.md`. Respect read limits, preserve unrelated changes and require publication/SDK authorization. Reuse consent within its scope; keep secrets and private documents out of Git and artifacts.
