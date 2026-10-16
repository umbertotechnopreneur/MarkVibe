@echo off
rem VBWR B
rem Project: MarkVibe | Creator: Umberto Giacobbi
rem Created with AI: OpenAI Codex; accessible verification launcher; 2026-10-16
rem Human guidance: simple checks without packaging or installation
rem Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
rem VBWR E
where pwsh.exe >nul 2>nul
if errorlevel 1 (
  echo PowerShell 7 non e' disponibile. Aggiungi al PATH una copia gia' installata.
  pause
  exit /b 2
)
pwsh.exe -NoProfile -File "%~dp0scripts\check.ps1" -OpenReport
set "checkResult=%errorlevel%"
pause
exit /b %checkResult%
