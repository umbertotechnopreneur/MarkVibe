# VBWR B
# Project: MarkVibe | Creator: Umberto Giacobbi
# Created with AI: OpenAI Codex; manual verification profiles and readable reports; 2026-10-16
# Human guidance: simple checks, isolated fixtures, no API calls or packaging
# Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
# VBWR E
<# .SYNOPSIS
Run an explicitly selected verification profile and write HTML/JSON evidence.
.DESCRIPTION
Rapida checks Core, native file services and client contracts. InterfacciaWindows
builds and checks the desktop UI. Completa includes both and the browser UI.
This script never installs an SDK/browser, packages, increments versions or calls AI.
#>
[CmdletBinding()]
param(
    [ValidateSet('Rapida', 'InterfacciaWindows', 'Completa')][string]$Profile,
    [switch]$OpenReport
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $Profile) {
    Write-Host 'Verifica MarkVibe'
    Write-Host '1. Rapida - documenti, salvataggio e regole locali'
    Write-Host '2. Interfaccia Windows - apertura, mappa e impostazioni'
    Write-Host '3. Completa - tutti i controlli, incluso il browser'
    $selection = Read-Host 'Scegli 1, 2 o 3 (Invio = Rapida)'
    $Profile = switch ($selection) {
        '2' { 'InterfacciaWindows' }
        '3' { 'Completa' }
        { $_ -in '', '1' } { 'Rapida' }
        default { throw 'Scelta non valida. Riavvia e scegli 1, 2 o 3.' }
    }
}
$reportDirectory = Join-Path $repositoryRoot ('artifacts/checks/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + $PID)
[IO.Directory]::CreateDirectory($reportDirectory) | Out-Null
$results = [Collections.Generic.List[object]]::new()
$browserHost = $null
$environmentBefore = @{}
. (Join-Path $PSScriptRoot 'npm-command.ps1')

<# .SYNOPSIS
Retain a human-readable stage outcome without treating a missing prerequisite as success.
#>
function Add-CheckResult([string]$Name, [string]$Status, [string]$Detail, [string]$Log = '') {
    $results.Add([pscustomobject]@{ name = $Name; status = $Status; detail = $Detail; log = $Log })
    Write-Host "[$Status] $Name - $Detail"
}

<# .SYNOPSIS
Execute one stage, retain its output and return whether dependent stages can proceed.
#>
function Invoke-CheckStep([string]$Name, [scriptblock]$Action) {
    $logName = 'step-' + ($results.Count + 1) + '.log'
    try {
        & $Action 2>&1 | Tee-Object -FilePath (Join-Path $reportDirectory $logName) | Out-Host
        Add-CheckResult $Name 'Passed' 'Completato.' $logName
        return $true
    } catch {
        Add-Content -LiteralPath (Join-Path $reportDirectory $logName) -Value $_.Exception.Message
        Add-CheckResult $Name 'Failed' $_.Exception.Message $logName
        return $false
    }
}

<# .SYNOPSIS
Run a native command and stop its stage at a nonzero exit code.
#>
function Invoke-CheckCommand([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command ha restituito il codice $LASTEXITCODE. Consulta il log." }
}

<# .SYNOPSIS
Set only process-local harness options and remember their original values.
#>
function Set-CheckEnvironment([string]$Name, [string]$Value) {
    if (-not $environmentBefore.ContainsKey($Name)) {
        $environmentBefore[$Name] = [Environment]::GetEnvironmentVariable($Name, 'Process')
    }
    [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
}

<# .SYNOPSIS
Reserve an available loopback port briefly for the local browser host.
#>
function Get-AvailablePort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    } finally {
        $listener.Stop()
    }
}

<# .SYNOPSIS
Detect the installed Evergreen WebView2 runtime using read-only installer metadata.
#>
function Test-WebViewRuntime {
    if (-not $IsWindows) { return $false }
    foreach ($registryRoot in @('HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients', 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients', 'HKCU:\SOFTWARE\Microsoft\EdgeUpdate\Clients')) {
        if (-not (Test-Path -LiteralPath $registryRoot)) { continue }
        foreach ($client in Get-ChildItem -LiteralPath $registryRoot -ErrorAction SilentlyContinue) {
            $metadata = Get-ItemProperty -LiteralPath $client.PSPath -ErrorAction SilentlyContinue
            if ($metadata.name -match 'WebView2' -and $metadata.pv -and $metadata.pv -ne '0.0.0.0') { return $true }
        }
    }
    return $false
}

<# .SYNOPSIS
Write a standalone report with escaped messages and links to retained stage evidence.
#>
function Write-CheckReport {
    $payload = [ordered]@{ profile = $Profile; finishedAtUtc = [DateTime]::UtcNow.ToString('o'); results = @($results.ToArray()) }
    $payload | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $reportDirectory 'results.json') -Encoding utf8
    $rows = foreach ($result in $results) {
        $name = [Net.WebUtility]::HtmlEncode($result.name)
        $detail = [Net.WebUtility]::HtmlEncode($result.detail)
        $link = if ($result.log) { '<a href="' + $result.log + '">Dettagli</a>' } else { '' }
        '<tr><td>' + $name + '</td><td class="' + $result.status + '">' + $result.status + '</td><td>' + $detail + '</td><td>' + $link + '</td></tr>'
    }
    $html = @"
<!doctype html><html lang="it"><meta charset="utf-8"><title>Verifica MarkVibe</title>
<style>body{font:16px system-ui;margin:3rem;max-width:1100px;background:#11151c;color:#e8eaf0}table{border-collapse:collapse;width:100%}td,th{text-align:left;padding:14px;border-bottom:1px solid #39404e}a{color:#afa4ff}.Passed{color:#8ee0b4}.Failed{color:#ff9d9d}.NotRun{color:#f0ce86}p{line-height:1.6}</style>
<h1>Verifica MarkVibe · $Profile</h1><p>Passed: superato · Failed: problema rilevato · NotRun: non eseguito.<br>Questi risultati riguardano solo i controlli elencati. Nessuna installazione dell'applicazione, SDK o browser, pubblicazione o richiesta AI.</p>
<table><thead><tr><th>Controllo</th><th>Esito</th><th>Descrizione</th><th>Log</th></tr></thead><tbody>$($rows -join "`n")</tbody></table>
<p>Gli scenari e gli screenshot delle interfacce sono nelle sottocartelle <a href="browser/results.json">browser</a> e <a href="windows/results.json">windows</a>, quando eseguiti.</p></html>
"@
    $html | Set-Content -LiteralPath (Join-Path $reportDirectory 'report.html') -Encoding utf8
}

Push-Location -LiteralPath $repositoryRoot
try {
    $prerequisites = Invoke-CheckStep 'Strumenti disponibili' {
        if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Node non trovato. Aggiungi al PATH una versione gia installata >=22.12.' }
        $nodeVersion = (& node --version).TrimStart('v')
        if ([version]$nodeVersion -lt [version]'22.12.0') { throw "Node $nodeVersion non supportato: seleziona una versione >=22.12." }
        if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK non trovato. Seleziona il percorso di uno SDK gia installato.' }
        $sdkVersion = (Get-Content global.json -Raw | ConvertFrom-Json).sdk.version
        if (-not ((& dotnet --list-sdks) -match ('^' + [regex]::Escape($sdkVersion) + '\s'))) { throw ".NET SDK $sdkVersion richiesto da global.json non disponibile." }
        Write-Host "Node $nodeVersion; .NET SDK $sdkVersion."
    }
    if ($prerequisites) {
        $dependenciesReady = Invoke-CheckStep 'Dipendenze JavaScript dal lock' {
            Invoke-ProjectNpm -Arguments @('ci', '--ignore-scripts', '--no-audit', '--no-fund')
        }
        if ($Profile -ne 'InterfacciaWindows') {
            Set-CheckEnvironment 'MARKVIBE_NATIVE_ARTIFACTS_DIR' (Join-Path $reportDirectory 'native-files')
            foreach ($projectName in @('MarkVibe.Core.Checks', 'MarkVibe.Native.Checks')) {
                $project = "tests/$projectName/$projectName.csproj"
                $checkName = if ($projectName -eq 'MarkVibe.Core.Checks') { 'Documenti, revisioni e mappa' } else { 'File, salvataggi e conflitti' }
                $built = Invoke-CheckStep "Preparazione: $checkName" {
                    Invoke-CheckCommand 'dotnet' @('restore', $project, '--locked-mode')
                    Invoke-CheckCommand 'dotnet' @('build', $project, '--no-restore', '-c', 'Debug')
                }
                if ($built) {
                    $null = Invoke-CheckStep $checkName { Invoke-CheckCommand 'dotnet' @('run', '--project', $project, '--no-build', '-c', 'Debug') }
                } else { Add-CheckResult $checkName 'NotRun' 'Preparazione non riuscita.' }
            }
            if ($dependenciesReady) {
                $null = Invoke-CheckStep 'Regole del client' { Invoke-ProjectNpm -Arguments @('run', 'test:client') }
            } else { Add-CheckResult 'Regole del client' 'NotRun' 'Dipendenze non disponibili.' }
        }
        if ($Profile -ne 'Rapida') {
            $edgePaths = @("${env:ProgramFiles(x86)}/Microsoft/Edge/Application/msedge.exe", "$env:ProgramFiles/Microsoft/Edge/Application/msedge.exe", "$env:LOCALAPPDATA/Microsoft/Edge/Application/msedge.exe")
            $edgeAvailable = $IsWindows -and @($edgePaths | Where-Object { Test-Path -LiteralPath $_ }).Count -gt 0
            if (-not $edgeAvailable -or -not $dependenciesReady -or -not (Test-WebViewRuntime)) {
                Add-CheckResult 'Interfacce' 'NotRun' 'Servono Windows, Microsoft Edge, il runtime WebView2 gia installato e le dipendenze JavaScript. Non e stato installato alcun browser/runtime.'
            } else {
                $built = Invoke-CheckStep 'Preparazione interfacce Debug x64' {
                    Invoke-ProjectNpm -Arguments @('run', 'bundle')
                    Invoke-CheckCommand 'dotnet' @('restore', 'src/MarkVibe.Windows/MarkVibe.Windows.csproj', '--locked-mode', '-r', 'win-x64')
                    Invoke-CheckCommand 'dotnet' @('build', 'src/MarkVibe.Windows/MarkVibe.Windows.csproj', '--no-restore', '-c', 'Debug', '-r', 'win-x64')
                }
                if ($built) {
                    Set-CheckEnvironment 'MARKVIBE_WINDOWS_EXECUTABLE' (Join-Path $repositoryRoot 'src/MarkVibe.Windows/bin/Debug/net10.0-windows10.0.19041.0/win-x64/MarkVibe.Windows.exe')
                    Set-CheckEnvironment 'MARKVIBE_WINDOWS_ARTIFACTS_DIR' (Join-Path $reportDirectory 'windows')
                    Set-CheckEnvironment 'MARKVIBE_WINDOWS_DEBUG_PORT' ([string](Get-AvailablePort))
                    $null = Invoke-CheckStep 'Interfaccia Windows' { Invoke-ProjectNpm -Arguments @('run', 'test:windows') }
                    if ($Profile -eq 'Completa') {
                        $null = Invoke-CheckStep 'Interfaccia browser' {
                            $baseUrl = 'http://127.0.0.1:' + (Get-AvailablePort) + '/'
                            $startInfo = [Diagnostics.ProcessStartInfo]::new('dotnet')
                            $startInfo.WorkingDirectory = $repositoryRoot
                            $startInfo.UseShellExecute = $false
                            $startInfo.CreateNoWindow = $true
                            foreach ($argument in @('run', '--project', 'src/MarkVibe.Browser', '--no-build', '--no-launch-profile', '--', '--urls', $baseUrl)) { $startInfo.ArgumentList.Add($argument) }
                            $script:browserHost = [Diagnostics.Process]::Start($startInfo)
                            $available = $false
                            for ($attempt = 0; $attempt -lt 60; $attempt++) {
                                if ($browserHost.HasExited) { throw 'Il server browser si e fermato prima di essere disponibile.' }
                                try { $null = Invoke-WebRequest -Uri $baseUrl -TimeoutSec 2; $available = $true; break } catch { Start-Sleep -Milliseconds 500 }
                            }
                            if (-not $available) { throw 'Il server browser non risponde entro il tempo previsto.' }
                            Set-CheckEnvironment 'MARKVIBE_BASE_URL' $baseUrl
                            Set-CheckEnvironment 'MARKVIBE_ARTIFACTS_DIR' (Join-Path $reportDirectory 'browser')
                            Invoke-ProjectNpm -Arguments @('run', 'test:browser')
                        }
                    }
                } else { Add-CheckResult 'Interfacce' 'NotRun' 'Build non riuscita; nessuna vecchia applicazione e stata avviata.' }
            }
        }
    } else { Add-CheckResult 'Controlli richiesti' 'NotRun' 'Completa i prerequisiti indicati e ripeti la verifica.' }
} catch {
    Add-CheckResult 'Esecuzione' 'Failed' $_.Exception.Message
} finally {
    if ($browserHost -and -not $browserHost.HasExited) { $browserHost.Kill($true); $browserHost.WaitForExit() }
    foreach ($name in $environmentBefore.Keys) { [Environment]::SetEnvironmentVariable($name, $environmentBefore[$name], 'Process') }
    Pop-Location
    Write-CheckReport
}
Write-Host "Report: $(Join-Path $reportDirectory 'report.html')"
if ($OpenReport -and $IsWindows) { Start-Process -FilePath (Join-Path $reportDirectory 'report.html') -WindowStyle Hidden }
if (@($results | Where-Object status -eq 'Failed').Count) { exit 1 }
if (@($results | Where-Object status -eq 'NotRun').Count) { exit 2 }
exit 0
