param([Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
[System.IO.Directory]::CreateDirectory($Destination) | Out-Null
foreach ($asset in @(@('Square44x44Logo.png',44), @('Square150x150Logo.png',150), @('StoreLogo.png',50), @('AppIcon.png',256))) {
    $size = [int]$asset[1]
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $background = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#525ce8'))
    $foreground = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $shape = [System.Drawing.Drawing2D.GraphicsPath]::new()
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $scale = $size / 64.0
        $graphics.ScaleTransform($scale, $scale)
        $shape.AddArc(0,0,36,36,180,90); $shape.AddArc(28,0,36,36,270,90)
        $shape.AddArc(28,28,36,36,0,90); $shape.AddArc(0,28,36,36,90,90); $shape.CloseFigure()
        $graphics.FillPath($background, $shape)
        # Same original M geometry as wwwroot/favicon.svg.
        $points = [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new(14,46), [System.Drawing.PointF]::new(14,18),
            [System.Drawing.PointF]::new(22,18), [System.Drawing.PointF]::new(32,34),
            [System.Drawing.PointF]::new(42,18), [System.Drawing.PointF]::new(50,18),
            [System.Drawing.PointF]::new(50,46), [System.Drawing.PointF]::new(42,46),
            [System.Drawing.PointF]::new(42,31), [System.Drawing.PointF]::new(32,46),
            [System.Drawing.PointF]::new(22,31), [System.Drawing.PointF]::new(22,46))
        $graphics.FillPolygon($foreground, $points)
        $bitmap.Save((Join-Path $Destination $asset[0]), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $shape.Dispose(); $foreground.Dispose(); $background.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
}
$png = [System.IO.File]::ReadAllBytes((Join-Path $Destination 'AppIcon.png'))
$stream = [System.IO.File]::Create((Join-Path $Destination 'MarkVibe.ico'))
$writer = [System.IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
    $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$png.Length); $writer.Write([uint32]22)
    $writer.Write($png)
} finally { $writer.Dispose() }
