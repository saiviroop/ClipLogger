# Generates web/social assets for the ClipLogger site into docs/assets/.
# Reuses the clipboard glyph from make-icon.ps1 (blue body, white paper, gray clip).
# Run:  powershell -ExecutionPolicy Bypass -File tools\make-web-assets.ps1
Add-Type -AssemblyName System.Drawing

$outDir = Join-Path $PSScriptRoot "..\docs\assets"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$outDir = (Resolve-Path -LiteralPath $outDir).Path

$cBody = [System.Drawing.Color]::FromArgb(45, 95, 170)    # #2D5FAA
$cClip = [System.Drawing.Color]::FromArgb(205, 210, 220)  # #CDD2DC
$cLine = [System.Drawing.Color]::FromArgb(70, 120, 190)   # #4678BE

function Rounded([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $d = $r * 2
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

# Draw the 32x32-design glyph at offset (ox,oy) scaled by s onto graphics $g.
function Draw-Glyph($g, [single]$ox, [single]$oy, [single]$s) {
    $body = New-Object System.Drawing.SolidBrush $cBody
    $g.FillPath($body, (Rounded ($ox+6*$s) ($oy+6*$s) (20*$s) (23*$s) (4*$s)))
    $paper = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $g.FillRectangle($paper, ($ox+9*$s), ($oy+11*$s), (14*$s), (13*$s))
    $clip = New-Object System.Drawing.SolidBrush $cClip
    $g.FillPath($clip, (Rounded ($ox+12*$s) ($oy+3*$s) (8*$s) (6*$s) (2*$s)))
    $pen = New-Object System.Drawing.Pen $cLine, (2*$s)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($pen, ($ox+11*$s), ($oy+15*$s), ($ox+21*$s), ($oy+15*$s))
    $g.DrawLine($pen, ($ox+11*$s), ($oy+19*$s), ($ox+21*$s), ($oy+19*$s))
    $g.DrawLine($pen, ($ox+11*$s), ($oy+23*$s), ($ox+18*$s), ($oy+23*$s))
}

function New-Graphics($bmp) {
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    return $g
}

# --- icon-512.png (transparent) ---
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = New-Graphics $bmp
$g.Clear([System.Drawing.Color]::Transparent)
Draw-Glyph $g 0 0 16
$g.Dispose()
$bmp.Save((Join-Path $outDir "icon-512.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# --- apple-touch-icon.png 180x180 (opaque, light bg, padded) ---
$bmp = New-Object System.Drawing.Bitmap(180, 180)
$g = New-Graphics $bmp
$g.Clear([System.Drawing.Color]::FromArgb(244, 246, 251))
Draw-Glyph $g 18 18 4.2
$g.Dispose()
$bmp.Save((Join-Path $outDir "apple-touch-icon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# --- og.png 1200x630 (social / AI preview card) ---
$W = 1200; $H = 630
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = New-Graphics $bmp
$rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H)
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, `
    [System.Drawing.Color]::FromArgb(15, 31, 58), `
    [System.Drawing.Color]::FromArgb(20, 50, 94), `
    [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
$g.FillRectangle($grad, $rect)

# glyph on the left
Draw-Glyph $g 70 150 9

$white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$muted = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(178, 196, 224))
$accent = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(120, 170, 240))
$fTitle = New-Object System.Drawing.Font("Segoe UI", 78, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fTag = New-Object System.Drawing.Font("Segoe UI", 36, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$fMeta = New-Object System.Drawing.Font("Segoe UI Semibold", 28, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)

$tx = 430
$g.DrawString("ClipLogger", $fTitle, $white, $tx, 175)
$g.DrawString("Capture selected text to a timestamped", $fTag, $muted, $tx, 290)
$g.DrawString("log file - one hotkey, no clutter.", $fTag, $muted, $tx, 338)
$g.DrawString("Free   |   Windows   |   Open source", $fMeta, $accent, $tx, 420)

$g.Dispose()
$bmp.Save((Join-Path $outDir "og.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Host "Wrote icon-512.png, apple-touch-icon.png, og.png to $outDir"
