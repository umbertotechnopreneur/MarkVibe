# VBWR B
#
# Project: MarkVibe
# Repository: https://github.com/umbertotechnopreneur/MarkVibe
# Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
#
# VibeWare: Human intent, AI execution, and plenty of tokens
# Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
#
# Modified with AI: OpenAI Codex; readability revision; 2026-10-11
# Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
# Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
#
# Copyright (c) 2026 Umberto Giacobbi
# License: MIT (see LICENSE)
#
# VBWR E

<#
.SYNOPSIS
Install the adjacent MSIX bundle; trust its development certificate only when the explicit switch is supplied.
#>

param([switch]$TrustDevelopmentCertificate)
$ErrorActionPreference = 'Stop'
$msixBundles = @(
    Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.msixbundle' -File)
if ($msixBundles.Count -ne 1) {

    throw 'Keep this script beside exactly one MarkVibe MSIX bundle.'
}
if ($TrustDevelopmentCertificate) {
    $principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Open PowerShell as administrator to trust the development certificate, then run this script again.'
    }
    Import-Certificate -FilePath (Join-Path $PSScriptRoot 'MarkVibe-Development.cer') -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
}
Add-AppxPackage -Path $msixBundles[0].FullName
Write-Host 'MarkVibe installed for the current Windows user. Open it from Start.'
