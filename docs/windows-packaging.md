# Windows distribution

`scripts/package-windows.ps1` defaults to self-contained Windows 11 **x64 Debug**
packages. Release and ARM64 require an explicit selection. The desktop .NET runtime is included; WebView2 uses the installed Evergreen
Runtime. No SDK, workload, app or certificate is installed by packaging.

## Requested Release or Debug build

Use PowerShell 7, existing Node >=22.12, .NET SDK 10.0.402 and Windows SDK tools
`makeappx.exe`/`signtool.exe`. The script discovers tools already installed on the machine.

```powershell
./scripts/package-windows.ps1
# Explicit wider distribution:
./scripts/package-windows.ps1 -Configuration Release -Architectures x64,arm64
# Launch the local Debug build after packaging:
Start-Process ./src/MarkVibe.Windows/bin/Debug/net10.0-windows10.0.19041.0/win-x64/MarkVibe.Windows.exe
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

Outputs: portable ZIPs, MSI installers and MSIX files for the selected architectures, one
MSIX bundle, public development certificate, installation helper, README,
`BUILD-INFO.json` and `SHA256SUMS.txt`. No tests, installation, branch/worktree creation,
commit or publication are performed by this script. For local Debug delivery,
the owner has authorized closing MarkVibe, discarding unsaved in-app work,
trusting the generated MarkVibe certificate in LocalMachine/TrustedPeople and
installing/replacing the Debug package without another confirmation. Launch the
installed app afterward. This does not authorize deleting source documents or
unrelated data/certificates. Release installation and publication remain separate.
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
msiexec.exe /i "MarkVibe-<version>-debug-win-x64.msi"
```

MSI upgrades replace the same channel/architecture; uninstall removes installed
payload files and shortcuts, retaining the separate user profile and source
documents. MSI and MSIX are separate products and can coexist. Local Debug
delivery on this workstation continues to replace the existing MSIX; generating
an MSI does not establish its installation, upgrade or uninstall acceptance.
Do not install a second distribution implicitly.

The native authoring script and table definitions live under `packaging/windows/`.
Keep component identities stable across upgrades and never add wildcard deletion
of user data to the uninstall tables.

The visible desktop browser uses `WebView2CompositionControl`; the isolated PDF
host keeps the standard hidden WebView2 control. Both use the same authenticated
bridge and resource boundaries. The visible control participates in WPF painting
instead of presenting a separate child HWND. Its native Acrylic background and
the in-app modal blur remain separate materials. See Microsoft's
[WPF WebView2 hosting documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/platforms/wpf).
The composition control may have lower frame rates than HWND hosting; inspect
selection, scrolling, window resizing and modal opening in a visible window
when changing this path. Browser-only captures cannot prove native redraw behavior.
The Windows project targets `net10.0-windows10.0.19041.0` and pins the existing
Windows SDK .NET reference pack `10.0.19041.57`. A bare `net10.0-windows` target
defaults to Windows 7 APIs and does not deploy the Windows Runtime assembly that
composition hosting loads at startup. The MSIX's Windows 11 minimum is unchanged.

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
