# Windows distribution

`scripts/package-windows.ps1` creates self-contained Windows 11 x64 and ARM64
packages. The desktop .NET runtime is included; WebView2 uses the installed Evergreen
Runtime. No SDK, workload, app or certificate is installed by packaging.

## Requested Release or Debug build

Use PowerShell 7, existing Node >=22.12, .NET SDK 10.0.401 and Windows SDK tools
`makeappx.exe`/`signtool.exe`. The script discovers tools already installed on the machine.

```powershell
./scripts/package-windows.ps1 -Configuration Release
./scripts/package-windows.ps1 -Configuration Debug
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

Outputs: x64/ARM64 portable ZIPs, x64/ARM64 MSIX files, one architecture-selecting
MSIX bundle, public development certificate, installation helper, README,
`BUILD-INFO.json` and `SHA256SUMS.txt`. No tests, installation, branch/worktree creation,
commit or publication are performed by this script.

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
and native session. `--data-dir` overrides it. Ink remains beside the selected Markdown.
The portable directory can be moved together with `data/`; remembered document paths
still refer to their original absolute locations.

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
