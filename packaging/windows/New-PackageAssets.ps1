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
Generate the original MarkVibe package logos and a PNG-backed Windows icon at the requested destination.
#>

param([Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
[System.IO.Directory]::CreateDirectory($Destination) | Out-Null
foreach ($assetSpec in @(
        @(
            'Square44x44Logo.png', 44), @(
            'Square150x150Logo.png', 150), @(
            'StoreLogo.png', 50), @(
            'AppIcon.png', 256))) {
    $pixelSize = [int]$assetSpec[1]
    $bitmap = [System.Drawing.Bitmap]::new($pixelSize, $pixelSize)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $background = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#525ce8'))
    $foreground = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $backgroundShape = [System.Drawing.Drawing2D.GraphicsPath]::new()
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $scale = $pixelSize / 64.0
        $graphics.ScaleTransform($scale, $scale)
        $backgroundShape.AddArc(0, 0, 36, 36, 180, 90)

        $backgroundShape.AddArc(28, 0, 36, 36, 270, 90)
        $backgroundShape.AddArc(28, 28, 36, 36, 0, 90)

        $backgroundShape.AddArc(0, 28, 36, 36, 90, 90)

        $backgroundShape.CloseFigure()
        $graphics.FillPath($background, $backgroundShape)
        # Same original M geometry as wwwroot/favicon.svg.
        $letterPoints = [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new(14, 46),
            [System.Drawing.PointF]::new(14, 18),
            [System.Drawing.PointF]::new(22, 18),
            [System.Drawing.PointF]::new(32, 34),
            [System.Drawing.PointF]::new(42, 18),
            [System.Drawing.PointF]::new(50, 18),
            [System.Drawing.PointF]::new(50, 46),
            [System.Drawing.PointF]::new(42, 46),
            [System.Drawing.PointF]::new(42, 31),
            [System.Drawing.PointF]::new(32, 46),
            [System.Drawing.PointF]::new(22, 31),
            [System.Drawing.PointF]::new(22, 46))
        $graphics.FillPolygon($foreground, $letterPoints)
        $bitmap.Save((Join-Path $Destination $assetSpec[0]), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {

        $backgroundShape.Dispose()

        $foreground.Dispose()

        $background.Dispose()

        $graphics.Dispose()

        $bitmap.Dispose()
    }
}
$iconPngBytes = [System.IO.File]::ReadAllBytes((Join-Path $Destination 'AppIcon.png'))
$iconStream = [System.IO.File]::Create((Join-Path $Destination 'MarkVibe.ico'))
$iconWriter = [System.IO.BinaryWriter]::new($iconStream)
try {
    $iconWriter.Write([uint16]0)

    $iconWriter.Write([uint16]1)

    $iconWriter.Write([uint16]1)
    $iconWriter.Write([byte]0)

    $iconWriter.Write([byte]0)

    $iconWriter.Write([byte]0)

    $iconWriter.Write([byte]0)
    $iconWriter.Write([uint16]1)

    $iconWriter.Write([uint16]32)

    $iconWriter.Write([uint32]$iconPngBytes.Length)

    $iconWriter.Write([uint32]22)
    $iconWriter.Write($iconPngBytes)
} finally {

    $iconWriter.Dispose()
}
