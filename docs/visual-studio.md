<!-- VBWR B
  Project: MarkVibe | Creator: Umberto Giacobbi
  Created with AI: OpenAI Codex; Visual Studio solution and debugger guidance; 2026-10-16
  Human guidance: compile the complete app and debug desktop and browser code
  Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
VBWR E -->

# Build and debug in Visual Studio

Open **`MarkVibe.sln`**, select **Debug / Any CPU**, then choose **MarkVibe Desktop**
in the startup dropdown. Press **F5** to build and debug the Windows host. The
desktop runtime defaults to **win-x64**, even though the managed solution platform
is Any CPU. This runs a development executable; it does not install a package.

`MarkVibe.slnx` contains the same seven projects and remains the solution used by
the repository's command-line scripts. Keep both project lists aligned when adding
or removing a project. Always name the intended solution in command-line commands.

## Existing prerequisites

- **Visual Studio 2026 (18.x)** with C#/.NET desktop support. Browser debugging also
  uses its ASP.NET/web tooling and a supported Edge or Chrome browser. Microsoft
  supports targeting .NET 10 starting with VS 18.0. The 10.0.4xx SDK band ships with
  VS 18.9; an updated VS 2026 is the preferred pairing. This is not a claim that
  18.9 is the minimum supported version. See Microsoft's
  [SDK and Visual Studio support matrix](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- **.NET SDK 10.0.402**, pinned without roll-forward in `global.json`. Select an
  existing installation with that exact SDK; do not silently retarget the projects.
- **Node.js >=22.12**, npm and **PowerShell 7 (`pwsh`)** available in the environment
  inherited by Visual Studio. Check `node --version` in its terminal. If an older
  Node takes precedence, reopen Visual Studio from an environment whose PATH selects
  the existing supported Node installation.
- **WebView2 Runtime** for the desktop host. Windows SDK reference packages and
  OpenSilver's build tools are declared in the projects and restored through NuGet.
  No simulator project or machine-wide OpenSilver extension is needed by this
  solution's build targets.

This repository change installs no SDK, workload, browser, extension or runtime.

## What Build Solution includes

| Project | Purpose |
| --- | --- |
| `MarkVibe.Windows` | WPF executable, WebView2 host, desktop file dialogs and printing |
| `MarkVibe.Native` | Native file access and persisted workspace/session state |
| `MarkVibe.Browser` | Blazor WebAssembly bootstrap, OpenSilver runtime and local client assets |
| `MarkVibe` | Shared OpenSilver C#/XAML application and controls |
| `MarkVibe.Core` | Markdown, catalog, navigation and map state |
| `MarkVibe.Core.Checks` | Core executable checks, compiled but never run by Build Solution |
| `MarkVibe.Native.Checks` | Native executable checks, compiled but never run by Build Solution |

On a normal Visual Studio build, `PrepareVisualStudioClient` checks the existing
client fingerprint before `RequireClientBundle`. If dependencies or generated
assets are missing or stale, it calls `scripts/prepare-client.ps1`: locked `npm ci`
without install scripts, followed by the local bundle. An unchanged bundle is
reused. Both hosts enter MSBuild even when C# has not changed, so edits to external
JavaScript inputs cannot bypass this check or leave old desktop assets staged. Design-time evaluation
does not restore npm packages or bundle assets.

The desktop project references the Browser build and stages its static assets
under the executable's `app/` directory. It serves those assets inside WebView2;
the desktop launch does not require a separate web development server.

The command-line build keeps its explicit client preparation:

```powershell
pwsh -NoProfile -File scripts/prepare-client.ps1
dotnet restore MarkVibe.sln --locked-mode
dotnet build MarkVibe.sln -c Debug --no-restore
```

These commands compile the checks projects without executing them. Build Solution
also does not run the JavaScript/browser/Windows suites, create MSI/MSIX packages,
clean distribution artifacts or install the app. Distribution builds continue to
use [the packaging script](windows-packaging.md).

## Choose the process that owns the code

**MarkVibe Desktop** starts `MarkVibe.Windows` under the managed debugger. Use it
for breakpoints in the WPF host, WebView2 integration, native file operations and
other .NET code executed by that process. The OpenSilver UI runs as WebAssembly
inside WebView2; starting the WPF debugger alone does not attach to that runtime.

**MarkVibe Browser** starts the `MarkVibe Debug` profile of `MarkVibe.Browser` at
`http://localhost:5080`. It opens a browser and configures the Blazor debugging
proxy through `inspectUri`. Use it for C# breakpoints in the shared OpenSilver UI
and Core code executed in WebAssembly. Wait for the debugger to attach before
triggering the relevant action. Browser storage and file access use the browser
providers, so this profile does not reproduce Windows-only file dialogs or printing.
See Microsoft's [Blazor debugging guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/debug?view=aspnetcore-10.0)
and OpenSilver's [browser debugging instructions](https://doc.opensilver.net/documentation/how-to-topics/debug-in-browser.html).

The original `MarkVibe` browser profile remains available for command-line use and
does not open a browser automatically. Do not start both browser profiles at once
on the same port.

If the shared startup profiles are not shown, use **Configure Startup Projects**
on the solution. Enable multi-project launch profiles in Visual Studio's options
if that installation requires it. Alternatively, set `MarkVibe.Windows` as the
single startup project. For browser debugging, set `MarkVibe.Browser` as startup
and select **MarkVibe Debug** in its launch dropdown. The two entries in
`MarkVibe.slnLaunch` each start one host; they do not start the executable checks.
See [Microsoft's launch-profile documentation](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-set-multiple-startup-projects).

## Verification boundary

The solution lists, configuration mappings, JSON/XML syntax and target conditions
were reviewed statically. The parent then restored and compiled all seven projects
through `MarkVibe.sln -c Debug` successfully, with desktop assets staged. Existing
NU1900 vulnerability-cache warnings remain. The package build uses the same shared
project graph. Visual Studio and F5 were not launched, and no check executable or
test suite was run. The owner checks native and WebAssembly breakpoint attachment
and the Visual Studio-only client preparation path.
