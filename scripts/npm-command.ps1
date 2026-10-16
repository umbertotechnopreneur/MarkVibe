# VBWR B
# Project: MarkVibe | Creator: Umberto Giacobbi
# Created with AI: OpenAI Codex; npm uses the selected Node executable; 2026-10-16
# Human guidance: avoid the Windows npm shim silently selecting an older sibling Node
# Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
# VBWR E

<# .SYNOPSIS
Run npm with the Node selected on PATH and fail immediately when prerequisites or npm fail.
#>
function Invoke-ProjectNpm([string[]]$Arguments) {
    $selectedNode = @(Get-Command node -CommandType Application -ErrorAction Stop)[0].Source
    $versionText = (& $selectedNode --version).TrimStart('v')
    if ($LASTEXITCODE -ne 0 -or [version]$versionText -lt [version]'22.12.0') {
        throw "Node $versionText non supportato. Seleziona nel PATH una versione gia installata >=22.12."
    }

    if ($IsWindows) {
        $npmShim = @(Get-Command npm.cmd -CommandType Application -ErrorAction Stop)[0].Source
        $npmCli = Join-Path (Split-Path -Parent $npmShim) 'node_modules/npm/bin/npm-cli.js'
        if (-not (Test-Path -LiteralPath $npmCli -PathType Leaf)) {
            throw "npm-cli.js non trovato accanto a $npmShim. Seleziona un'installazione npm completa nel PATH."
        }
        # npm.cmd can prefer node.exe in its own directory even when PATH selects a newer Node.
        & $selectedNode $npmCli @Arguments
    } else {
        $npmExecutable = @(Get-Command npm -CommandType Application -ErrorAction Stop)[0].Source
        & $npmExecutable @Arguments
    }

    if ($LASTEXITCODE -ne 0) {
        throw "npm ha restituito il codice $LASTEXITCODE. La preparazione si e fermata."
    }
}
