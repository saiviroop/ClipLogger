# Generates src/ClipLogger.App/app.ico — a 256x256 clipboard glyph that matches
# TrayIconFactory.Create() scaled up 8x. Produces a PNG-payload .ico (Vista+).
# Run:  powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$S = 8                      # scale: 32px design -> 256px icon
$size = 32 * $S
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

function Rounded([int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
    $d = $r * 2
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

# clipboard body
$body = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45, 95, 170))
$bodyPath = Rounded (6*$S) (6*$S) (20*$S) (23*$S) (4*$S)
$g.FillPath($body, $bodyPath)

# white paper
$paper = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$g.FillRectangle($paper, (9*$S), (11*$S), (14*$S), (13*$S))

# top clip
$clip = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(205, 210, 220))
$clipPath = Rounded (12*$S) (3*$S) (8*$S) (6*$S) (2*$S)
$g.FillPath($clip, $clipPath)

# text lines
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(70, 120, 190)), (2*$S)
$g.DrawLine($pen, (11*$S), (15*$S), (21*$S), (15*$S))
$g.DrawLine($pen, (11*$S), (19*$S), (21*$S), (19*$S))
$g.DrawLine($pen, (11*$S), (23*$S), (18*$S), (23*$S))
$g.Dispose()

# encode bitmap as PNG
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $ms.ToArray()
$bmp.Dispose()

# wrap PNG in a single-image .ico container
$out = Join-Path $PSScriptRoot "..\src\ClipLogger.App\app.ico"
$fs = [System.IO.File]::Create((Resolve-Path -LiteralPath (Split-Path $out)).Path + "\app.ico")
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type = icon
$bw.Write([uint16]1)            # image count
$bw.Write([byte]0)              # width  (0 => 256)
$bw.Write([byte]0)              # height (0 => 256)
$bw.Write([byte]0)              # palette
$bw.Write([byte]0)              # reserved
$bw.Write([uint16]1)            # color planes
$bw.Write([uint16]32)           # bits per pixel
$bw.Write([uint32]$png.Length)  # size of PNG
$bw.Write([uint32]22)           # offset (6 + 16)
$bw.Write($png)
$bw.Flush(); $bw.Close()
Write-Host "Wrote app.ico ($($png.Length) bytes PNG payload)"
