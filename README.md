# MarkVibe

![MarkVibe — Read. Edit. Keep it yours.](docs/assets/markvibe-banner.svg)

**A local Markdown reader and editor with optional AI editing and portable HTML/PDF export.**

[Download Windows packages](https://github.com/umbertotechnopreneur/MarkVibe/releases) ·
[MIT license](LICENSE) · [My website](https://umbertogiacobbi.biz) ·
[VibeWare manifesto](https://umbertogiacobbi.biz/vibeware/manifesto)

## Why I created it

I wanted a simple local workflow: open documentation, follow connected pages,
inspect its Markdown, revise it with an optional AI assistant and export a useful
HTML or PDF. MarkVibe uses the same document rendering for reading and exporting,
with local images and diagrams included in the result.

## What I built

| Capability | What you can do |
| --- | --- |
| Local documents | Open Markdown files, folders or documentation ZIPs; follow relative links and images |
| Four views | Read, inspect read-only source with Monaco, compare in Split, or explore the document map |
| HTML and PDF | Export the current page with embedded images and diagrams; Windows saves PDF directly |
| Appearance | Use OpenSilver Modern Theme in Light, Dark or System mode, reading styles and zoom |
| UI font | Choose an installed supported monospace font on Windows while keeping document typography separate |
| Document map | Explore with semantic zoom, collapsible folder islands, neighborhood lenses, source-backed connections, shortest routes, document previews, a minimap and saved viewpoints |
| Pick up later | Remember the last document, path and reading position; turn remembering off in settings |
| OpenAI key | Validate an API key from Settings and save it in the Windows user environment or isolated browser storage |
| Markdown AI | Revise the current document or selection with a stateless prompt, model/reasoning controls, semantic costs and context/token bars |
| AI collection | Select reference documents per workspace, inspect their estimated context, and include them in the next explicit AI request |

Reading and navigation run locally. An explicit AI send shares the
current prompt, document or selection, and explicitly collected reference documents directly with OpenAI. Each request has
no conversational memory. The Source pane remains read-only; AI results replace
the current document revision, with local undo/redo and an explicit Save Markdown copy action.
Exports cover the current page. Edited Markdown is saved as an explicit copy.
The managed document session keeps that revision independent of the parsed-model
cache, and distinguishes original, unsaved and exported content. There is no
workspace-wide full-text search. Map search matches document titles and paths;
Source Find searches the open document.

## Get started on Windows

I provide Windows 11 packages for **x64 and ARM64**:

- **MSIX bundle:** chooses the architecture automatically and adds MarkVibe to Start.
- **Portable ZIP:** extract the matching architecture to a writable folder and open
  `MarkVibe.Windows.exe`. Settings and the last document stay in its `data/` folder.

Both include the .NET runtime. Microsoft Edge WebView2 Runtime is required.
Initial packages are development-signed: use their supplied public certificate and
installation instructions. Store publication is a later decision.
See [Windows packaging and installation](docs/windows-packaging.md).

Open a file or folder, then use Read, Source or Split.
Use the document toolbar to export HTML or PDF.
The About window links to my website, this repository, the license and VibeWare.

In **Settings → OpenAI**, paste a key and choose **Validate**. MarkVibe sends an
authenticated request to [OpenAI's model-list endpoint](https://developers.openai.com/api/reference/resources/models/methods/list)
and saves the key only after a successful response. The check sends no documents
and generates no text; inference availability and billing are separate checks.
Windows/WebView2 stores it in the user environment variable `OPENAI_API_KEY`.
The browser uses this site's `localStorage`, with a visible warning that this
isolated storage is unencrypted and can be read by site scripts. Keys are kept
separate from preferences, workspaces and exported documents.
When a saved key is configured, normal startup automatically requests the account
model list. That request sends no documents and generates no text.

Choose **AI**, select text if desired, and write an instruction such as “make it
shorter” or “make it professional”. A complete edit automatically replaces the
selected Markdown or current document. The document shows a waiting popup with
Cancel and a one-minute deadline. Model badges indicate relative cost without
displaying currency prices; context and token bars distinguish measured counts
from estimates. See [Markdown AI editing](docs/ai-editing.md) for the retained
official model catalog, request limits, draft saving and validation scope.

Use the map's AI-context button or **AI collection → Add current document** to
collect references. The collection retains up to 16 paths in the current workspace;
reference content is limited to 4 MiB per request. The open document remains the
editing target and is excluded from the reference payload and reference-token total.
Local estimates make no API calls. References are sent only with the next explicit
request, whose token preflight includes them before generation.

The browser build also imports files/folders/ZIPs.
Browser PDF uses its print dialog; imported documents may need reselection after a
restart. I have not packaged macOS or Linux desktop wrappers.

Application controls are OpenSilver XAML/C# UserControls and resource dictionaries.
Browser adapters are limited to Markdown rendering, Monaco, Mermaid, graph drawing,
files, clipboard and export. There is no Vue application. Pen, ink annotations and
sticky-note editing have been removed. Existing annotation files are left untouched.
The current map format is schema **4**, containing document cards only. Earlier
prerelease map formats are unsupported and preserved on disk; there is no migration
layer. See [map layout and recovery](docs/map-layout.md).

## Build from source

Use .NET SDK **10.0.402**, Node **22.12 or later**, and an existing Windows SDK for MSIX:

```powershell
npm ci --ignore-scripts --no-audit --no-fund
npm run bundle
dotnet restore MarkVibe.slnx --locked-mode
dotnet build MarkVibe.slnx --no-restore
dotnet run --project src/MarkVibe.Windows --no-build
```

For the browser, run `dotnet run --project src/MarkVibe.Browser --no-build --launch-profile MarkVibe`
and open [localhost:5080](http://localhost:5080/).

To create distribution files:

```powershell
./scripts/package-windows.ps1
# Explicit wider distribution:
./scripts/package-windows.ps1 -Configuration Release -Architectures x64,arm64
```

Each packaging build cleans repository `artifacts/`, advances the four-part version,
and defaults to x64 Debug. I launch the new desktop executable after a local build
request. Packaging does not run tests or install anything.
The manual **Package MarkVibe for Windows** GitHub workflow uploads the same
files as downloadable build artifacts. It does not publish a release automatically.

I use C#/OpenSilver for the application, Markdig for Markdown, Monaco for source,
Mermaid for diagrams and WPF/WebView2 for Windows. See [architecture](docs/architecture.md),
[settings](docs/settings.md) and [contributing](CONTRIBUTING.md).
Earlier local checks are recorded in [validation](docs/validation.md); they are
distinct from installation or hardware acceptance of each new package.

## Free software

I am **Umberto Giacobbi**, and I release MarkVibe under the **MIT license**. You may
use it for any purpose, change it, share it or sell it, while keeping the copyright
and license notice. Dependency licenses remain applicable.

## AI development disclosure

I develop MarkVibe with AI under my direction. I define the product goals,
architecture, privacy and licensing requirements, and remain responsible for the
code I accept and release. The repository requires useful method comments,
explanations inside complex methods, descriptive names and readable code.

The [AI development disclosure](AI-DEVELOPMENT-DISCLOSURE.md) records the known
contributions, human guidance, completed checks and unverified areas. The 2026-10-11 XAML UI migration was bundled and built locally and inspected in
the running desktop app. Automated tests and inference were not requested or run.
Earlier static and runtime validation is recorded separately in that disclosure.
The 2026-10-16 map update adds a black canvas, accent elevation, wheel zoom,
progressive card entry and Star/Circle/Grid arrangements. It has received source
and static syntax review only; its build and runtime behavior remain unverified.
The map follow-up adds hover/selection, persistent card sizes, confirmed reset
and a monospace statistics strip. Settings gain frosted dialogs, a clearer key
form and an installed UI-font selector that leaves document fonts unchanged.
These follow-up changes also have static validation only.
The subsequent maintainability work introduces one authoritative document session,
removes legacy annotation/schema and unused global-search paths, shares UI
traversal and font catalogs, and preserves document labels during language changes.
Delegated agents contributed analysis and implementation. This revision has
source/static checks only: no build, tests or running-app verification were
performed for that work. Historical validation above remains scoped to its date.

The follow-up adds map title/path search with animated centering, folder grouping
and an explicit AI reference collection. A Debug x64 package and launch have been
requested; their actual outcome belongs to the [task archive](.github/tasks/archive.md).
This paragraph does not claim build success or functional acceptance. Automated
tests and live AI calls remain separate from that requested build and launch.

The document atlas follow-up adds progressive zoom detail, readable overview
labels, collapsible colored islands, one-to-three-link neighborhood lenses,
source excerpts, complete-catalog shortest routes, a document inspector, minimap
and eight named viewpoints. Codex implemented these owner-approved changes and
performed source/static checks only. Their bundle, build, tests and running-app
appearance remain unverified; see [map behavior and validation](docs/map-layout.md).

---

<p align="right">
  <a href="https://umbertogiacobbi.biz/vibeware/manifesto">
    <img src="https://raw.githubusercontent.com/umbertotechnopreneur/VibeWare/main/Branding/vibeware-logo.png" alt="VibeWare floppy logo" width="160">
  </a><br>
  <strong>VibeWare</strong><br>
  <em>Human intent, AI execution, and plenty of tokens</em>
</p>

### What is VibeWare?

I coined the term [VibeWare](https://umbertogiacobbi.biz) and started an open initiative for developers who build with AI and care about the craft. I challenge the assumption that vibe coding is synonymous with low-quality code, while openly acknowledging the weaknesses and risks of AI-generated software: experienced developers must guide the process, test carefully, check security and take responsibility for the result. With this footer, I acknowledge that I developed this software with AI and ask you to judge the quality of the work.

[**Read the VibeWare manifesto**](https://umbertogiacobbi.biz/vibeware/manifesto)

*You are welcome to explore, adapt and reuse the open-source VibeWare materials under the project license — my contribution to the developer community.*
