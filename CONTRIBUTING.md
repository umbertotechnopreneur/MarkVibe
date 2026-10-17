# Contributing

I welcome focused improvements to MarkVibe. Read [AGENTS.md](AGENTS.md) and the
current [implementation status](docs/implementation-status.md) before changing it.

Keep code, comments and UI text in English. Use four spaces in C#, two in JavaScript
and the existing XAML conventions. C# owns Markdown parsing, navigation and state;
JavaScript owns DOM rendering, browser capabilities and sanitation. Dispose obsolete
readers, editors, workers, image leases and callbacks. Preserve local file boundaries
and external-change conflicts.

Write code for human readers. Document every class/type and method/function with a
useful purpose and contract comment. Use XML documentation for C# type and member
declarations and appropriate documentation comments in JavaScript or PowerShell.
Explain non-obvious assumptions, ordering, invariants and decisions inside methods;
avoid comments that merely repeat the code.

Choose descriptive variable and parameter names. Separate logical phases with blank
lines, put distinct statements on separate lines and expand control-flow bodies.
Wrap long conditions, argument lists and initializers across readable, indented lines.
Prefer straightforward control flow and small, cohesive methods. Introduce intermediate
variables when their names clarify complex expressions; avoid redundant temporaries,
dense ternaries and chains that hide intent. Minimizing line count is not a goal.

Apply this standard to all future changes and to existing code touched during
maintenance. Preserve behavior and public contracts during readability-only cleanup,
and preserve concurrent changes from other contributors.

Preserve the VibeWare source headers with the exact `VBWR B` / `VBWR E` markers,
MIT notice and supported AI provenance. Keep generated files and third-party assets
under their own notices; add no comment headers to JSON or lockfiles. Update the
[AI development disclosure](AI-DEVELOPMENT-DISCLOSURE.md) when contributions or
validation evidence change.

Deliver changes through pull requests into the protected default branch. Keep one
reviewable change per PR and use a focused imperative subject. Describe the problem and result, update relevant
documentation and dependency locks, and report exactly what was compiled or tried.
Keep private documents, credentials and temporary build output out of source control.

For coding agents, ask before each branch or worktree creation. Run tests only when
I explicitly request them for the current task; suggest a small relevant check when
useful. An older test approval is not continuing permission. Do not install an app,
certificate, SDK or workload implicitly. The owner's documented local Debug MSIX
delivery authorization in AGENTS.md includes closing MarkVibe, trusting its own
development certificate, replacement installation and final launch; it does not
authorize interactive acceptance or installation of a second distribution type.
Build/package requests authorize their
necessary local generation and restore steps, within the existing environment.

Before each requested Release or Debug distribution build, clean repository
`artifacts/` through `scripts/package-windows.ps1`. Keep distribution files and
temporary packaging data there, with final files only after success. Create no
output folders in Codex directories. The package script does not run tests, create
branches/worktrees or publish releases. Manual GitHub packaging uses the same script.

Track actionable work in `.github/tasks/todo.md`, completed milestones in `archive.md`
and durable findings in `lessons.md`. A publication request is separate from a build.
Application code is [MIT licensed](LICENSE); preserve third-party license notices.

Use the approved VibeWare logo and the exact tagline from the initiative's
templates. Keep first-person authorship, the manifesto, About and legal links
consistent with the publication rules recorded in [AGENTS.md](AGENTS.md). A brand
label is not evidence that a release passed functional or security checks.

— Umberto Giacobbi · [VibeWare](https://umbertogiacobbi.biz/vibeware/manifesto)
