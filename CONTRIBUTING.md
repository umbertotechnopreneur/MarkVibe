# Contributing

I welcome focused improvements to MarkVibe. Read [AGENTS.md](AGENTS.md) and the
current [implementation status](docs/implementation-status.md) before changing it.

Keep code, comments and UI text in English. Use four spaces in C#, two in JavaScript
and the existing XAML conventions. C# owns Markdown parsing, navigation and ink;
JavaScript owns DOM rendering, browser capabilities and sanitation. Dispose obsolete
readers, editors, workers, image leases and callbacks. Preserve local file boundaries
and external-change conflicts.

Document every method/function with a useful comment about its purpose and contract.
Use XML documentation in C# and appropriate documentation comments in JavaScript or
PowerShell. Add comments inside methods when assumptions, ordering or decisions need
explanation. Choose descriptive variable and parameter names, use readable blocks
and intermediate steps, and avoid dense one-liners or multiple statements on a line.
Apply these rules to all new or changed code.

Preserve the VibeWare source headers with the exact `VBWR B` / `VBWR E` markers,
MIT notice and supported AI provenance. Keep generated files and third-party assets
under their own notices; add no comment headers to JSON or lockfiles. Update the
[AI development disclosure](AI-DEVELOPMENT-DISCLOSURE.md) when contributions or
validation evidence change.

Make one reviewable change at a time. Describe the problem and result, update relevant
documentation and dependency locks, and report exactly what was compiled or tried.
Keep private documents, credentials and temporary build output out of source control.

For coding agents, ask before each branch or worktree creation. Run tests only when
I explicitly request them for the current task; suggest a small relevant check when
useful. An older test approval is not continuing permission. Do not install an app,
certificate, SDK or workload implicitly. Build/package requests authorize their
necessary local generation and restore steps, within the existing environment.

Before each requested Release or Debug distribution build, clean repository
`artifacts/` through `scripts/package-windows.ps1`. Keep distribution files and
temporary packaging data there, with final files only after success. Create no
output folders in Codex directories. The package script does not run tests, create
branches/worktrees or publish releases. Manual GitHub packaging uses the same script.

Track actionable work in `.github/tasks/todo.md`, completed milestones in `archive.md`
and durable findings in `lessons.md`. A publication request is separate from a build.
Application code is [MIT licensed](LICENSE); preserve third-party license notices.

— Umberto Giacobbi · [VibeWare](https://umbertogiacobbi.biz/vibeware/manifesto)
