# Windows distribution

`scripts/package-windows.ps1` defaults to self-contained Windows 11 **x64 Release**
packages. Debug and ARM64 require an explicit selection. The desktop .NET runtime is included; WebView2 uses the installed Evergreen
Runtime. No SDK, workload, app or certificate is installed by packaging.

## Requested Release or Debug build

Use PowerShell 7, existing Node >=22.12, .NET SDK 10.0.402 and Windows SDK tools
`makeappx.exe`/`signtool.exe`. The script discovers tools already installed on the machine.

```powershell
./scripts/package-windows.ps1
# Explicit wider distribution:
./scripts/package-windows.ps1 -Configuration Release -Architectures x64,arm64
# Launch the local Release build after packaging:
Start-Process ./src/MarkVibe.Windows/bin/Release/net10.0-windows10.0.19041.0/win-x64/MarkVibe.Windows.exe
```

Run one requested configuration at a time: each invocation **deletes the repository's
entire `artifacts/` directory before building**, including previous packages and old
validation fixtures. The deletion checks the absolute output boundary and removes
links without following them. Final distribution files remain there; temporary staging
and private signing material are removed. No Codex output directory is created.

`packaging/windows/version.json` stores the last successful four-part version.
Default packaging increments its final component, across both configurations.
`-Version 0.1.1.0` selects a greater explicit version. Version metadata advances only
after packaging succeeds. Build settings include version, configuration and UTC timestamp;
the About window displays the version and timestamp.

Packaged applications also contain `app/build-info.json`. The splash reads this
before managed startup and shows version/configuration/architecture, the build
machine's name and its local build date/time, UTC offset and timezone identifier.
The local date/time text uses the build host's culture and timezone; opening the
package on another machine does not convert it. Unpackaged development hosts
without that file omit the metadata instead of inventing compilation facts.

Outputs: portable ZIPs, MSI installers and MSIX files for the selected architectures, one
MSIX bundle, public development certificate, installation helper, README,
`BUILD-INFO.json` and `SHA256SUMS.txt`. No tests, installation, branch/worktree creation,
commit or publication are performed by this script. For local Release x64 delivery,
the owner has authorized closing MarkVibe, discarding unsaved in-app work,
trusting the generated MarkVibe certificate in LocalMachine/TrustedPeople and
installing/replacing the Release package without another confirmation. Remove an
old Debug installation when replacing it. Launch the
installed app afterward. This does not authorize deleting source documents or
unrelated data/certificates. Publication remains separate.
Run tests only when the owner explicitly requests them for the current task.

## Install the development bundle

Keep the bundle, `MarkVibe-Development.cer` and installation helper together.
Open PowerShell **as administrator** in that directory and run:

```powershell
./Install-MarkVibe.ps1 -TrustDevelopmentCertificate
```

This explicitly trusts the supplied certificate in LocalMachine/TrustedPeople and
installs the bundle for the current Windows user. Open MarkVibe from Start.
The bundle chooses x64 or ARM64. Once the certificate is trusted, double-clicking
the bundle is another installation route. Each build gets a fresh development
certificate; trust the CER supplied with that build. The private key is not distributed.
See Microsoft's [MSIX signing and certificate trust guidance](https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide).

## Markdown file association

The MSIX helper and a full/reduced-UI MSI installation offer to use MarkVibe for
`.md` and `.markdown`. Choosing **Yes** opens Windows Default apps, where the
user selects MarkVibe for each extension. **No** keeps the existing defaults.
The response is remembered per Windows user and distribution, including updates.
The same choice remains available in Settings, alongside the effective default
status for each extension, even after answering No during installation.

Double-clicked MSIX installations and quiet MSI installations offer the same
choice at the first normal app launch. For an unattended MSIX helper run, use
`-SkipFileAssociationPrompt`. Portable and source builds do not register file
types or display this request. Repair and uninstall do not display it.

Windows controls the final default selection; the installer only registers an
available handler. See Microsoft's [Default Apps setup](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-default-apps-settings).

## Portable

Extract the matching ZIP completely into a writable folder; run `MarkVibe.Windows.exe`.
`portable.flag` selects a sibling `data/` directory for the WebView2 profile, settings
and native session. `--data-dir` overrides it. Existing annotation sidecars are left
untouched; the application no longer edits ink or sticky notes.
The portable directory can be moved together with `data/`; remembered document paths
still refer to their original absolute locations.

## MSI installer

The same packaging command creates a real Windows Installer `.msi` for each
selected architecture, in addition to MSIX and portable. It uses Windows Installer
COM and the existing Windows `makecab.exe`; no additional packaging tool or EULA
acceptance is required. The MSI includes the self-contained app and licenses,
installs for the current user under LocalAppData, and creates a Start menu shortcut.
WebView2 Runtime remains a prerequisite. It is not installed by the MSI.

Debug/Release and x64/ARM64 have separate stable upgrade identities. The MSI
compares three version fields, so application version `major.minor.build.revision`
maps to `major.minor.(build * 1000 + revision)`; for example, `0.1.0.23` becomes
`0.1.23`. Packaging rejects revisions above 999 or values beyond MSI's numeric
limits before cleaning artifacts. The four-part app version remains in build
metadata and the application's About window.

Open the MSI normally or use Windows Installer's standard command line:

```powershell
msiexec.exe /i "MarkVibe-<version>-release-win-x64.msi"
```

MSI upgrades replace the same channel/architecture; uninstall removes installed
payload files and shortcuts, retaining the separate user profile and source
documents. MSI and MSIX are separate products and can coexist. Local Release x64
delivery on this workstation continues to replace the existing MSIX; generating
an MSI does not establish its installation, upgrade or uninstall acceptance.
Do not install a second distribution implicitly.

The native authoring script and table definitions live under `packaging/windows/`.
Keep component identities stable across upgrades and never add wildcard deletion
of user data to the uninstall tables.

The visible desktop browser uses the standard HWND-hosted `WebView2` with opaque
browser, WPF and native client backgrounds. Native Acrylic is unavailable and
Settings explains why; the saved preference is retained. All application dialogs,
menus and tooltips live inside the browser, so they do not need WPF-over-browser
composition. The isolated PDF host remains hidden with its existing boundaries.

This removes the transparent capture-to-WPF presentation path after the owner
reported missing shell regions during map movement in 0.1.0.24. Microsoft documents
that `WebView2CompositionControl` adds a graphics-capture step and can have lower
frame rates than standard hosting; see its
[composition-control specification](https://github.com/MicrosoftEdge/WebView2Feedback/blob/main/specs/WPF_WebView2CompositionControl.md).
In-app modal blur remains a separate browser effect. No GPU-disable flag, redraw
polling loop or document-map animation removal is introduced. Owner acceptance of
pan/zoom, selection, resize, themes and modal opening remains necessary;
browser-only captures cannot prove native redraw behavior.
The Windows project targets `net10.0-windows10.0.19041.0` and pins the existing
Windows SDK .NET reference pack `10.0.19041.57`. A bare `net10.0-windows` target
defaults to Windows 7 APIs and does not deploy the Windows Runtime assembly that
the former composition host loaded at startup. The compatible Windows target and
MSIX's Windows 11 minimum are retained; no SDK/package update is needed for this fix.

## GitHub and Microsoft Store

The manual `windows-packages.yml` workflow accepts configuration and four-part
version, builds both architectures on one Windows runner, and uploads `artifacts/*`.
It contains no test or automatic-release step. Branch/worktree creation and publishing
remain separate, explicitly authorized operations.

For a future Store submission, use Partner Center's reserved package identity and
publisher. The local development identity is configurable through `-IdentityName`
and `-Publisher`; `-Unsigned` prepares files for later signing. Store distribution is
signed by Microsoft. See [manual desktop MSIX components](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion).

Installer acceptance means explicitly installing the generated package and trying
the app. Packaging success is not an installation result. Automated test execution
requires a separate current request.
