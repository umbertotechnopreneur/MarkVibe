# VBWR B
# Project: MarkVibe | Creator: Umberto Giacobbi
# Created with AI: OpenAI Codex; reproducible client preparation; 2026-10-16
# Human guidance: shared dependency preparation without stale installed packages
# Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
# VBWR E
<# .SYNOPSIS
Restore the exact npm lock and bundle local assets; never download browsers or run tests.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'npm-command.ps1')
Push-Location -LiteralPath $repositoryRoot
try {
    Invoke-ProjectNpm -Arguments @('ci', '--ignore-scripts', '--no-audit', '--no-fund')
    Invoke-ProjectNpm -Arguments @('run', 'bundle')
} finally {
    Pop-Location
}
