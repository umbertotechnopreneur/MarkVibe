# VBWR B
#
# Project: MarkVibe
# Repository: https://github.com/umbertotechnopreneur/MarkVibe
# Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
#
# VibeWare: Human intent, AI execution, and plenty of tokens
# Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
#
# Modified with AI: OpenAI Codex; Debug and architecture defaults; 2026-10-16
# Human guidance: Umberto Giacobbi; local x64 Debug builds and explicit wider packaging
# Modified with AI: OpenAI Codex; real MSI distribution using native Windows authoring; 2026-10-16
# Human guidance: Umberto Giacobbi; an MSI installer alongside MSIX and portable files
# Modified with AI: OpenAI Codex; default local delivery to Release x64; 2026-10-23
# Human guidance: Umberto Giacobbi; retire Debug deliveries and remember Release x64
# Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
#
# Copyright (c) 2026 Umberto Giacobbi
# License: MIT (see LICENSE)
#
# VBWR E

<#
.SYNOPSIS
Build the requested Windows distribution using existing tools, with temporary data contained in repository artifacts.
#>

# Builds distribution files only. Never runs tests, installs packages/certificates,
# creates branches/worktrees, or publishes anything.
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')][string]$Configuration = 'Release',
    [ValidateNotNullOrEmpty()][ValidateSet('x64', 'arm64')][string[]]$Architectures = @('x64'),
    [string]$Version,
    [switch]$Unsigned,
    [string]$IdentityName,
    [string]$Publisher
)
$ErrorActionPreference = 'Stop'
# Keep the first occurrence of each requested architecture so it is published once.
$Architectures = @($Architectures | Select-Object -Unique)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$workRoot = Join-Path $artifactsRoot '.work'
$windowsPackaging = Join-Path $repositoryRoot 'packaging/windows'
$versionFile = Join-Path $windowsPackaging 'version.json'
$packageMetadata = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
$previousVersion = [version]$packageMetadata.version
if (-not $Version) {
    $Version = if ($previousVersion.Revision -lt 999) {
        '{0}.{1}.{2}.{3}' -f $previousVersion.Major, $previousVersion.Minor, $previousVersion.Build, ($previousVersion.Revision + 1)
    } else {

        '{0}.{1}.{2}.0' -f $previousVersion.Major, $previousVersion.Minor, ($previousVersion.Build + 1)
    }
}
if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {

    throw 'Version must have four numeric components, for example 0.1.0.1.'
}
$packageVersion = [version]$Version
if ($packageVersion -le $previousVersion -or @(
        $packageVersion.Major, $packageVersion.Minor, $packageVersion.Build, $packageVersion.Revision | Where-Object {
            $_ -gt 65535 }).Count) {
    throw "Use a version greater than $previousVersion, with each component at most 65535."
}
# Validate MSI's stricter version range before cleaning the previous artifacts.
$msiProductVersion = & (Join-Path $windowsPackaging 'New-MsiPackage.ps1') `
    -Version $packageVersion -ValidateVersionOnly
if (-not $IdentityName) {

    $IdentityName = $packageMetadata.identityName
}
if (-not $Publisher) {

    $Publisher = $packageMetadata.publisher
}
<#
.SYNOPSIS
Run a build tool, retain relevant output and stop packaging immediately on a nonzero exit code.
#>
function Invoke-BuildTool([string]$File, [string[]]$Arguments) {
    & $File @Arguments | Where-Object {

        -not [string]::IsNullOrWhiteSpace($_) -and $_ -notmatch '^Processing .+ as a payload file\.'
    }
    if ($LASTEXITCODE -ne 0) {

        throw "$File failed with exit code $LASTEXITCODE. Packaging stopped."
    }
}
<#
.SYNOPSIS
Remove only verified repository artifact directories, deleting reparse-point links before recursion.
#>
function Remove-BuildDirectory([string]$Directory) {
    $target = [System.IO.Path]::GetFullPath($Directory)
    if ($target -ne $artifactsRoot -and -not $target.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a directory outside $artifactsRoot."
    }
    if (-not (Test-Path -LiteralPath $target)) {

        return
    }
    if ((Get-Item -LiteralPath $target -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) {

        throw 'The build output root must not be a link.'
    }
    # Remove links themselves before recursion, without traversing their targets.
    foreach ($link in Get-ChildItem -LiteralPath $target -Directory -Recurse -Force -Attributes ReparsePoint) {
        [System.IO.Directory]::Delete($link.FullName, $false)
    }
    Remove-Item -LiteralPath $target -Recurse -Force
}
<#
.SYNOPSIS
Find a tool on PATH or in the latest existing Windows SDK without installing additional components.
#>
function Find-SdkTool([string]$Name) {
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {

        return $command.Source
    }
    $kitRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
    foreach ($sdkVersionDirectory in Get-ChildItem -LiteralPath $kitRoot -Directory | Where-Object {
            $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } | Sort-Object {
            [version]$_.Name } -Descending) {
        $candidate = Join-Path $sdkVersionDirectory.FullName "x64/$Name"
        if (Test-Path -LiteralPath $candidate) {

            return $candidate
        }
    }
    throw "$Name is unavailable. Use an existing Windows SDK; this script does not install one."
}
$makeAppx = Find-SdkTool 'makeappx.exe'
$signTool = if (-not $Unsigned) {

    Find-SdkTool 'signtool.exe'
}
$nodeVersion = (& node --version).TrimStart('v')
if ($LASTEXITCODE -ne 0 -or [version]$nodeVersion -lt [version]'22.12.0') {

    throw 'Use an existing Node >=22.12 in this PowerShell session.'
}
$builtAt = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$channel = $Configuration.ToLowerInvariant()
$namePrefix = "MarkVibe-$Version-$channel"
Push-Location -LiteralPath $repositoryRoot
try {
    Write-Host "Cleaning $artifactsRoot before $Configuration $Version."
    Remove-BuildDirectory $artifactsRoot
    [System.IO.Directory]::CreateDirectory($workRoot) | Out-Null
    # A pre-existing esbuild directory does not prove that the npm lock was restored.
    & (Join-Path $PSScriptRoot 'prepare-client.ps1')
    Invoke-BuildTool 'dotnet' @(
        'restore', 'src/MarkVibe.Windows/MarkVibe.Windows.csproj', '--locked-mode')
    $icons = Join-Path $workRoot 'icons'
    & (Join-Path $windowsPackaging 'New-PackageAssets.ps1') -Destination $icons
    $signingFile = Join-Path $workRoot 'development.pfx'
    $signingPassword = [Guid]::NewGuid().ToString('N')
    if (-not $Unsigned) {
        $signingKey = [System.Security.Cryptography.RSA]::Create(2048)
        try {
            $certificateRequest = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new($Publisher, $signingKey, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
            $certificateRequest.CertificateExtensions.Add([System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false, $false, 0, $true))
            $certificateRequest.CertificateExtensions.Add([System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new([System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature, $true))
            $enhancedKeyUsages = [System.Security.Cryptography.OidCollection]::new()

            $enhancedKeyUsages.Add([System.Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.3')) | Out-Null
            $certificateRequest.CertificateExtensions.Add([System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($enhancedKeyUsages, $true))
            $certificateRequest.CertificateExtensions.Add([System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]::new($certificateRequest.PublicKey, $false))
            $certificate = $certificateRequest.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5), [DateTimeOffset]::UtcNow.AddYears(1))
            try {
                [System.IO.File]::WriteAllBytes($signingFile, $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $signingPassword))
                [System.IO.File]::WriteAllBytes((Join-Path $artifactsRoot 'MarkVibe-Development.cer'), $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
                $thumbprint = $certificate.Thumbprint
            } finally {

                $certificate.Dispose()
            }
        } finally {

            $signingKey.Dispose()
        }
    }
    $bundleInput = Join-Path $workRoot 'bundle'
    [System.IO.Directory]::CreateDirectory($bundleInput) | Out-Null
    foreach ($architecture in $Architectures) {
        $runtimeIdentifier = "win-$architecture"
        $payload = Join-Path $workRoot $runtimeIdentifier
        Invoke-BuildTool 'dotnet' @(

            'publish',
            'src/MarkVibe.Windows/MarkVibe.Windows.csproj',
            '-c',
            $Configuration,
            '-r',
            $runtimeIdentifier,
            '--self-contained',
            'true',
            '--no-restore',
            '-o',
            $payload,
            "-p:AssemblyVersion=$Version",
            "-p:FileVersion=$Version",
            "-p:InformationalVersion=$Version-$channel",
            "-p:MarkVibeBuildTime=$builtAt",
            "-p:ApplicationIcon=$(Join-Path $icons 'MarkVibe.ico')")
        if (-not (Test-Path -LiteralPath (Join-Path $payload 'MarkVibe.Windows.exe')) -or -not (Test-Path -LiteralPath (Join-Path $payload 'app/index.html'))) {

            throw 'The desktop publish output is incomplete.'
        }
        Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $payload
        $notices = Join-Path $payload 'THIRD_PARTY_NOTICES.txt'
        Copy-Item -LiteralPath (Join-Path $payload 'app/js/THIRD_PARTY_NOTICES.txt') -Destination $notices
        $nugetRoot = if ($env:NUGET_PACKAGES) {

            $env:NUGET_PACKAGES
        } else {

            Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages'
        }
        $packageIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($project in @(
                'MarkVibe.Core', 'MarkVibe.Native', 'MarkVibe', 'MarkVibe.Browser', 'MarkVibe.Windows')) {
            $dependencyLock = Get-Content -LiteralPath (Join-Path $repositoryRoot "src/$project/packages.lock.json") -Raw | ConvertFrom-Json
            foreach ($framework in $dependencyLock.dependencies.PSObject.Properties) {
                foreach ($dependency in $framework.Value.PSObject.Properties) {
                    if ($dependency.Value.type -in @(
                            'Direct', 'Transitive')) {

                        $packageIds.Add($dependency.Name.ToLowerInvariant() + '/' + $dependency.Value.resolved) | Out-Null
                    }
                }
            }
        }
        foreach ($runtimeFamily in @(
                'microsoft.netcore.app.runtime.', 'microsoft.windowsdesktop.app.runtime.')) {
            $runtimeFolder = Join-Path $nugetRoot ($runtimeFamily + $runtimeIdentifier)
            foreach ($runtimeVersion in Get-ChildItem -LiteralPath $runtimeFolder -Directory) {

                $packageIds.Add($runtimeFamily + $runtimeIdentifier + '/' + $runtimeVersion.Name) | Out-Null
            }
        }
        foreach ($package in ($packageIds | Sort-Object)) {
            $packageFolder = Join-Path $nugetRoot $package
            foreach ($notice in Get-ChildItem -LiteralPath $packageFolder -File | Where-Object {
                    $_.Name -match '^(LICEN[CS]E|NOTICE|COPYING|THIRD.PARTY.NOTICES)([._-].*)?$' }) {
                Add-Content -LiteralPath $notices -Value ("`n--- " + $package + ' / ' + $notice.Name + " ---`n" + [System.IO.File]::ReadAllText($notice.FullName)) -Encoding utf8
            }
        }
        Add-Content -LiteralPath $notices -Value "`n.NET runtime: https://github.com/dotnet/runtime/blob/main/LICENSE.TXT`n.NET WPF: https://github.com/dotnet/wpf/blob/main/LICENSE.TXT`nWebView2 Runtime: https://developer.microsoft.com/microsoft-edge/webview2/`n" -Encoding utf8
        $portableMarker = Join-Path $payload 'portable.flag'
        Set-Content -LiteralPath $portableMarker -Value 'Keep settings and WebView2 data beside this executable.' -Encoding utf8NoBOM
        Set-Content -LiteralPath (Join-Path $payload 'PORTABLE-README.txt') -Value "MarkVibe $Version ($Configuration, $architecture)`nExtract the entire ZIP to a writable folder, then open MarkVibe.Windows.exe.`n.NET is included. Windows 11 and Microsoft Edge WebView2 Runtime are required.`nSettings and last document stay in the data folder. Source documents and previous sidecars are left untouched.`nhttps://github.com/umbertotechnopreneur/MarkVibe`nCopyright (c) 2026 Umberto Giacobbi. MIT. VibeWare." -Encoding utf8NoBOM
        [System.IO.Compression.ZipFile]::CreateFromDirectory($payload, (Join-Path $artifactsRoot "$namePrefix-$runtimeIdentifier-portable.zip"), [System.IO.Compression.CompressionLevel]::Optimal, $false)
        Remove-Item -LiteralPath $portableMarker, (Join-Path $payload 'PORTABLE-README.txt') -Force
        $msiPath = Join-Path $artifactsRoot "$namePrefix-$runtimeIdentifier.msi"
        & (Join-Path $windowsPackaging 'New-MsiPackage.ps1') `
            -PayloadDirectory $payload `
            -OutputPath $msiPath `
            -WorkDirectory (Join-Path $workRoot "msi-$architecture") `
            -Version $packageVersion `
            -Configuration $Configuration `
            -Architecture $architecture `
            -IconPath (Join-Path $icons 'MarkVibe.ico')

        if (-not $Unsigned) {
            Invoke-BuildTool $signTool @(
                'sign', '/fd', 'SHA256', '/f', $signingFile, '/p', $signingPassword, $msiPath)
        }

        $assetsFolder = Join-Path $payload 'Assets'

        [System.IO.Directory]::CreateDirectory($assetsFolder) | Out-Null
        foreach ($assetName in @(
                'Square44x44Logo.png', 'Square150x150Logo.png', 'StoreLogo.png')) {

            Copy-Item -LiteralPath (Join-Path $icons $assetName) -Destination $assetsFolder
        }
        [xml]$manifest = Get-Content -LiteralPath (Join-Path $windowsPackaging 'AppxManifest.xml') -Raw
        $manifest.Package.Identity.SetAttribute('Name', $IdentityName)

        $manifest.Package.Identity.SetAttribute('Publisher', $Publisher)
        $manifest.Package.Identity.SetAttribute('Version', $Version)

        $manifest.Package.Identity.SetAttribute('ProcessorArchitecture', $architecture)
        $manifest.Package.Properties.PublisherDisplayName = $packageMetadata.publisherDisplayName
        if ($Configuration -eq 'Debug') {
            $manifest.Package.Properties.DisplayName = 'MarkVibe (Debug)'
            $visualElements = $manifest.GetElementsByTagName('VisualElements', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')[0]
            $visualElements.SetAttribute('DisplayName', 'MarkVibe (Debug)')
        }
        $manifest.Save((Join-Path $payload 'AppxManifest.xml'))
        $packagePath = Join-Path $artifactsRoot "$namePrefix-$runtimeIdentifier.msix"
        Invoke-BuildTool $makeAppx @(
            'pack', '/d', $payload, '/p', $packagePath, '/o')
        if (-not $Unsigned) {

            Invoke-BuildTool $signTool @(
                'sign', '/fd', 'SHA256', '/f', $signingFile, '/p', $signingPassword, $packagePath)
        }
        Copy-Item -LiteralPath $packagePath -Destination $bundleInput
        Remove-BuildDirectory $payload
    }
    $bundle = Join-Path $artifactsRoot "$namePrefix.msixbundle"
    Invoke-BuildTool $makeAppx @(
        'bundle', '/d', $bundleInput, '/p', $bundle, '/bv', $Version, '/o')
    if (-not $Unsigned) {
        Invoke-BuildTool $signTool @(
            'sign', '/fd', 'SHA256', '/f', $signingFile, '/p', $signingPassword, $bundle)
        Copy-Item -LiteralPath (Join-Path $windowsPackaging 'Install-MarkVibe.ps1') -Destination $artifactsRoot
    }
    $buildInfo = [ordered]@{
        version=$Version

        configuration=$Configuration

        builtAtUtc=$builtAt

        architectures=$Architectures

        packageFormats=@('msi', 'msix', 'portable')

        msiProductVersion=$msiProductVersion

        selfContained=$true

        signed=(-not $Unsigned)

        certificateThumbprint=$thumbprint

        identityName=$IdentityName

        publisher=$Publisher

        githubCommit=$env:GITHUB_SHA

        testsRun=$false

        installationTried=$false
    }
    $buildInfo | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifactsRoot 'BUILD-INFO.json') -Encoding utf8NoBOM
    $readme = @"
MarkVibe $Version - $Configuration

MSI: choose the matching .msi for a conventional per-user Windows installation.
Double-click it, or run msiexec /i "<matching-file>.msi" /qb for basic progress UI.
The app is installed under LocalAppData/Programs, with a Start menu shortcut.
Remove it through Installed apps. Documents and profile data are retained.
MSI and MSIX are separate installations; choose one installer format.
MSI version $msiProductVersion maps application version $Version without losing its revision.

MSIX: bundle contains $($Architectures -join ', '). For this development-signed build,
open PowerShell as administrator and run .\Install-MarkVibe.ps1 -TrustDevelopmentCertificate.
Then open MarkVibe from Start. The public CER is included; the private key is not distributed.
Each signed build uses a fresh development certificate; trust its supplied CER for installation.

PORTABLE: choose the matching ZIP, extract everything to a writable folder,
and open MarkVibe.Windows.exe. Profile/settings stay in its data folder.

All formats include .NET. Microsoft Edge WebView2 Runtime and Windows 11 are required.
Packaging does not install the app/certificate or run tests. BUILD-INFO.json records this.
Unsigned MSIX builds are intended for later signing/Store preparation, not double-click installation.

Created by Umberto Giacobbi. MIT license. VibeWare.
https://github.com/umbertotechnopreneur/MarkVibe
https://umbertogiacobbi.biz/vibeware/manifesto
"@
    Set-Content -LiteralPath (Join-Path $artifactsRoot 'README.txt') -Value $readme -Encoding utf8NoBOM
    Remove-BuildDirectory $workRoot
    $hashes = foreach ($file in Get-ChildItem -LiteralPath $artifactsRoot -File | Sort-Object Name) {

        (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $file.Name
    }
    Set-Content -LiteralPath (Join-Path $artifactsRoot 'SHA256SUMS.txt') -Value $hashes -Encoding utf8NoBOM
    $packageMetadata.version = $Version
    $packageMetadata | ConvertTo-Json | Set-Content -LiteralPath $versionFile -Encoding utf8NoBOM
    Write-Host "Created $Version $Configuration $($Architectures -join '/') MSI, MSIX and portable files in $artifactsRoot. Tests were not run by packaging."
} finally {
    if (Test-Path -LiteralPath $workRoot) {

        Remove-BuildDirectory $workRoot
    }
    Pop-Location
}
