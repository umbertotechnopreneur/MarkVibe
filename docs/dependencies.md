# Dependency and compatibility decisions

Direct versions below are pinned in source. The 2026-10-16 license review used
the installed npm/NuGet package metadata, lockfiles and actual bundle input graph.
Historical execution evidence is recorded separately in the validation and task
archive documents; a source/license review is not a security or runtime test.

| Dependency | Pin | Purpose | Upstream licensing context |
| --- | --- | --- | --- |
| .NET SDK | 10.0.402 | C# 14 / .NET 10 build selection; owner-approved patch update on 2026-10-16 | .NET licensing; review distribution components |
| OpenSilver | 3.3.3 | C#/XAML DOM application library | MIT upstream |
| OpenSilver.Themes.Modern | 3.3.0 | Official control templates and light/dark palettes | MIT upstream |
| OpenSilver.WebAssembly | 3.3.3 | Matching browser runtime with net10.0 target | MIT upstream |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.10 | Browser host | MIT upstream |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 10.0.10 | Local development server | MIT upstream, development-only |
| Markdig | 1.4.0 | Managed Markdown AST and HTML | BSD-2-Clause |
| System.Security.Cryptography.Xml | 8.0.4 | Patch vulnerable 8.0.2 transitive dependency | .NET MIT upstream |
| Microsoft.Web.WebView2 | 1.0.4258.31 | Windows WPF host and direct PDF API | SDK LICENSE.txt: permissive three-clause terms; separate installed runtime |
| DOMPurify | 3.4.16 | Independent DOM/SVG sanitation | Apache-2.0 option selected from MPL-2.0 OR Apache-2.0 |
| Monaco editor | 0.57.0 | Lazy read-only source editor | MIT |
| Mermaid | 12.1.0 | Strict diagrams | MIT |
| elkjs (via Mermaid) | 0.9.3 | Included diagram layout module | EPL-2.0; retained license and versioned source availability |
| fflate | 0.8.3 | Bounded lazy ZIP deflate | MIT |
| highlight.js | 11.12.0 | Rendered code highlighting | BSD-3-Clause |
| esbuild | 0.28.2 | Local client bundling | MIT, development-only |

NuGet references use exact range syntax; npm dependencies use exact version strings. global.json disables SDK roll-forward. npm and NuGet lockfiles are present and NuGet locked restore passes. Fresh npm installation was not repeated during this validation; use `npm ci --ignore-scripts --no-audit --no-fund` for a later authorized clean reproduction.

Modern Theme has its own release numbering: 3.3.0 requires OpenSilver >=3.3.0 and
was compiled with the existing OpenSilver 3.3.3 pin. The XML override replaced
the earlier vulnerable 8.0.2 dependency, but subsequent builds reported other
NU1902/NU1903 advisories for Pkcs 8.0.1 and Xml 8.0.4. Those remain tracked in
the active task list; do not read historical clean restore output as a current
security assessment. No dependency upgrade is included in this review.

The local default Node 20.19.0 is below the bundle requirement. Validation used the already installed cached Node 24.19.0 through a process-local PATH change. No Node installation or global configuration change was made.

The bundle script retains legal-comment files, emits a build manifest and copies package-level notices for modules included by the actual build graph. Distribution includes the approved MIT application license and available NuGet notices. MarkVibe's MIT license cannot replace dependency obligations.

## Application license and included components

My application code remains MIT. Most resolved components use MIT, BSD, ISC,
Apache-2.0, NCSA or public-domain terms; the included ELK module remains EPL-2.0.
It is not relabeled MIT. [EPL-2.0 section 3](https://www.eclipse.org/legal/epl/epl-v20.html)
requires retaining its license and telling recipients how to obtain its source.
The [distribution notices](../THIRD-PARTY-NOTICES.txt) provide the elkjs 0.9.3
source archive, its build instructions, the ELK baseline and the upstream change
identified by the [release notes](https://github.com/kieler/elkjs/releases/tag/0.9.3).
esbuild retains external legal comments and copies its complete license.

DOMPurify's Apache-2.0 alternative is selected explicitly. The transitive khroma
2.1.0 package has no license field in package.json but includes an MIT `license`
file; the bundle copies that file. Do not infer that an absent metadata field
means absent license terms.

WebView2's NuGet SDK license is copied with the Windows payload. The Evergreen
Runtime is an installed prerequisite, not a bundled browser redistribution.
OpenAI logos remain subject to their brand terms, and theme assets retain NCSA,
MIT and CC0 notices. The approved VibeWare logo is unchanged MIT-covered artwork
from the same copyright holder; trademark rights remain distinct. This review
documents redistribution boundaries, not blanket ownership of third-party code.

## Why this architecture

The Windows host pins WebView2 1.0.4258.31, the stable SDK verified on NuGet on 2026-10-11. Local checks used the already installed runtime 154.0.4258.62. The SDK package was restored; no SDK/workload or WebView2 Runtime installation was performed. Host operations follow [WebView2 security guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security) and the official [PDF printing APIs](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/print).

OpenSilver provides the requested DOM-based C#/XAML runtime. Its matching WASM package's net10.0 metadata supports selecting .NET 10 at source level. Markdig is managed code with a shared AST; it avoids independent JavaScript interpretations of headings and links.

Monaco and Mermaid are heavy optional dependencies and are dynamically imported. Browser documents and images are lazy. The design is a static site with browser file capabilities, so publishing the complete generated static directory does not require a document-processing service.

## Primary sources

- [OpenSilver 3.3.3](https://www.nuget.org/packages/OpenSilver/3.3.3)
- [Modern Theme 3.3.0](https://www.nuget.org/packages/OpenSilver.Themes.Modern/3.3.0)
- [Official theme handling](https://doc.opensilver.net/documentation/how-to-topics/theme-handling.html)
- [OpenSilver.WebAssembly 3.3.3 and target frameworks](https://www.nuget.org/packages/OpenSilver.WebAssembly/3.3.3)
- [Markdig 1.4.0](https://www.nuget.org/packages/Markdig/1.4.0)
- [WebAssembly host package 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly/10.0.10)
- [DevServer package 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly.DevServer/10.0.10)
- [DOMPurify 3.4.16](https://github.com/cure53/DOMPurify/releases/tag/3.4.16)
- [Monaco 0.57.0](https://github.com/microsoft/monaco-editor/releases/tag/v0.57.0)
- [Mermaid 12.1.0](https://github.com/mermaid-js/mermaid/releases/tag/mermaid@12.1.0)
- [fflate 0.8.3](https://github.com/101arrowz/fflate/releases/tag/v0.8.3)
- [highlight.js 11.12.0](https://github.com/highlightjs/highlight.js/releases/tag/11.12.0)
- [esbuild 0.28.2](https://github.com/evanw/esbuild/releases/tag/0.28.2)
