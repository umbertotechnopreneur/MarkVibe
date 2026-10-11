param([switch]$TrustDevelopmentCertificate)
$ErrorActionPreference = 'Stop'
$bundle = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.msixbundle' -File)
if ($bundle.Count -ne 1) { throw 'Keep this script beside exactly one MarkVibe MSIX bundle.' }
if ($TrustDevelopmentCertificate) {
    $principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Open PowerShell as administrator to trust the development certificate, then run this script again.'
    }
    Import-Certificate -FilePath (Join-Path $PSScriptRoot 'MarkVibe-Development.cer') -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
}
Add-AppxPackage -Path $bundle[0].FullName
Write-Host 'MarkVibe installed for the current Windows user. Open it from Start.'
