# Repository Guidelines

## Project Structure & Module Organization

- `src/MarkVibe.Core/`: Markdown model, navigation, catalog and ink state.
- `src/MarkVibe/`: OpenSilver C#/XAML shell, Modern Theme, About and DOM hosts.
- `src/MarkVibe.Browser/`: WebAssembly bootstrap and local `wwwroot/` assets.
- `src/MarkVibe.Native/`: contained files, atomic sidecars and session persistence.
- `src/MarkVibe.Windows/`: WPF/WebView2, native pickers and direct PDF saving.
- `client/`: browser providers, sanitization, Monaco, Mermaid and rendering.
- `packaging/windows/` and `scripts/`: versioned MSIX, MSI and portable packaging.
- `tests/`, `schemas/`, `docs/`: checks, ink contract and architecture.

C# owns state and parsing. Never edit generated `wwwroot/js/`, `bin/` or `obj/`.

## Build, Test, and Development Commands

Use existing .NET SDK **10.0.402**, Node **>=22.12** and execution authorization:

On this machine, a build request means the existing **MSIX** delivery,
**Debug, x64 only**, followed by launching the newly built desktop app. A request
for a real MSI also includes the Windows Installer artifact; do not call MSIX an
MSI or silently replace an installed distribution with a second installer type. The owner
confirmed x64 rather than 32-bit x86. Use Release or ARM64 only when explicitly
requested. For local Debug delivery, the owner has explicitly authorized closing
all MarkVibe instances, discarding unsaved in-app work, trusting the generated
MarkVibe development certificate in `LocalMachine/TrustedPeople`, and installing
or replacing the Debug package without another confirmation. Remove the previous
MarkVibe package if replacement requires it; do not delete source documents or
unrelated applications, data or certificates. Launch the installed Debug app after
delivery. Release installation and publication still require explicit authorization.
Run tests only when explicitly requested for the current task.

```powershell
npm ci --ignore-scripts --no-audit --no-fund
npm run bundle
dotnet restore MarkVibe.slnx --locked-mode
dotnet build MarkVibe.slnx --no-restore
dotnet run --project src/MarkVibe.Browser --no-build --launch-profile MarkVibe
dotnet run --project src/MarkVibe.Windows --no-build
./scripts/package-windows.ps1
# Explicit wider distribution only:
./scripts/package-windows.ps1 -Configuration Release -Architectures x64,arm64
```

`MarkVibe.sln` is the Visual Studio entry point; `.slnx` retains the same seven
projects for named command-line builds. Keep their project lists aligned. Shared
launch profiles start Desktop or Browser separately. Build compiles the check
projects but never executes them. Visual Studio prepares stale client assets before
the existing bundle guard; preserve design-time and command-line behavior. See
`docs/visual-studio.md` for native versus WebAssembly debugger ownership.

Packaging defaults to x64 Debug, cleans repository `artifacts/`, increments the package version and creates MSIX, a bundle, MSI installers and portable ZIPs for the selected architectures. It runs no tests and does not launch the app itself. For a direct preview, use `src/MarkVibe.Windows/bin/Debug/net10.0-windows10.0.19041.0/win-x64/MarkVibe.Windows.exe`; local Debug delivery ends with the installed app running. Outputs and temporary packaging files stay in repository `artifacts/`; never create them in Codex directories.

## VibeWare identity and publication

Follow the VibeWare `templates/APP-PUBLICATION.md`, `README-FOOTER.md` and
`SOURCE-CODE-HEADER.md` in the initiative's repository. MarkVibe participates in
the initiative; its creator and copyright holder is **Umberto Giacobbi**.

- Preserve the approved, unmodified `images/vibeware-logo.png`, its transparent
  canvas and aspect ratio. The header badge links to the manifesto and is
  keyboard accessible; it does not replace the application or taskbar icon.
- Use the exact tagline **Human intent, AI execution, and plenty of tokens** and
  canonical manifesto `https://umbertogiacobbi.biz/vibeware/manifesto`.
- Keep **About MarkVibe · Terms · Privacy** and **This is VibeWare · Manifesto**
  available without clipped links. Preserve footer space, show links for 30 seconds,
  then fade over 3 seconds; reveal on pointer/focus and keep visible while used.
- About identifies the installed version and actual purpose, uses the first-person
  AI-assistance responsibility statement, and retains links to MarkVibe on GitHub,
  VibeWare on GitHub, the personal site, Terms and Privacy plus an accessible Close.
- Keep VibeWare artwork free of recoloring, glow or other added effects. The brand
  does not establish performance, security, test results or third-party rights.
- Product data handling belongs in this repository. Shared Terms/Privacy pages,
  code licenses, local signing and Microsoft Store identity remain distinct.
- Partner Center identity is not yet available. Current MSIX delivery is local
  Debug only. A Store package requires its real Name, Publisher and display name,
  an explicitly requested Store configuration/version and separate publication
  authorization; see `docs/windows-store.md`.

## Coding Style & Naming Conventions

Use English, four-space C#, two-space JavaScript and existing XAML indentation. Use PascalCase C# members, `_camelCase` fields and camelCase wire properties. Preserve nullable annotations, ES modules and LF. No formatter/linter is configured. Icons are local SVG symbols; retain accessible button labels.

Apply these readability rules to all new or changed code, including future work
and existing code touched during maintenance. Code must be understandable to human
readers; minimizing line count is not a goal.

- Document every class/type and method/function with a useful comment explaining its
  purpose and relevant contract. Use XML documentation for C# type and member
  declarations and appropriate documentation comments in JavaScript or PowerShell.
- Add comments inside methods when the logic is not straightforward. Explain
  assumptions, ordering, invariants and the reason for a decision rather than restating
  each statement.
- Use descriptive variable and parameter names; avoid unclear abbreviations and
  single-letter names when their meaning is not immediately obvious.
- Separate logical phases with blank lines. Put distinct statements on separate lines,
  expand control-flow bodies and wrap long conditions, argument lists and initializers
  across readable, indented lines.
- Prefer straightforward control flow and small, cohesive methods. Use intermediate
  variables when their names clarify a complex expression; avoid redundant temporaries,
  dense ternaries and chains that obscure intent.
- Preserve behavior and public contracts during readability-only cleanup. Review
  comments, names, spacing and control flow together, and preserve concurrent changes
  from other contributors.
- Support only current application-owned schemas and bridge commands. Do not add
  migrations, obsolete aliases, old decoders or speculative compatibility layers
  unless the owner explicitly requests them. Remove unused historical paths when
  changing both ends of an internal contract. Keep malformed-input validation,
  cancellation, security boundaries and genuine platform capabilities: these are
  not backward compatibility. Never delete user files to retire a format.
- Preserve a VibeWare header on maintained project source, script and markup files,
  using the exact `VBWR B` / `VBWR E` markers and the file format's comment syntax.
  Record only supported AI contributions and human guidance; use **MIT** for project
  code and retain the copyright holder from `LICENSE`.
- Keep generated output, vendored libraries and third-party theme assets under their
  existing notices. JSON and lockfiles remain valid data without comment headers. Keep
  the README footer and `AI-DEVELOPMENT-DISCLOSURE.md` aligned with the available
  evidence.

## Testing Guidelines

**Run tests only when the user explicitly requests them for the current task.** Earlier approvals do not override this. Suggest relevant checks without running them. Authorized options: core/native executable checks, `npm run test:client`, `test:browser`, `test:windows`. Use descriptive behavior names and synthetic fixtures; no coverage percentage is required. Local Debug app/certificate installation is already authorized as described above; installation success does not establish functional acceptance.

The owner performs GUI acceptance. Do not drive the mouse or keyboard, focus,
maximize or move windows, capture the desktop, or run interactive app checks
unless explicitly requested. Requested Debug delivery still includes its authorized
build, installation and final launch; it does not authorize interactive validation.

## Commit & Pull Request Guidelines

Keep documentation short and easy to read. Use plain words, short sentences and
one or two concrete examples. Explain the result first; link to code for details.
Avoid repeating the same explanation across documents.

Use focused imperative subjects. Describe the problem, result and actual validation; link issues and UI screenshots when available. Update dependency locks. Record when tests were not requested.

Deliver future changes through pull requests into the protected default branch.
Do not push directly to it or rewrite published history as routine maintenance.
The owner's explicit initial-history consolidation is a one-time exception; retain
existing release tags and keep a recoverable history backup. Prefer a single
reviewable change per PR and squash merging when the repository permits it.

## Agent Workflow

Ask permission before **each branch or worktree creation**. Keep active work in `.github/tasks/todo.md`, milestones in `archive.md` and findings in `lessons.md`. Respect read limits, preserve unrelated changes and require publication/SDK authorization. Reuse consent within its scope; keep secrets and private documents out of Git and artifacts.
