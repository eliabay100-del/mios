# Generates assets\logo.png - the MIOS system logo (red gear + wrench + black tire).
# Original artwork drawn with GDI+ so it is watermark-free and scales crisply.
Add-Type -AssemblyName System.Drawing

$size = 512
$bmp = New-Object System.Drawing.Bitmap -ArgumentList $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)

$red   = [System.Drawing.Color]::FromArgb(255, 213, 27, 34)
$redDk = [System.Drawing.Color]::FromArgb(255, 176, 18, 24)
$black = [System.Drawing.Color]::FromArgb(255, 28, 28, 28)
$white = [System.Drawing.Color]::White
$trans = [System.Drawing.Color]::Transparent

function SB([System.Drawing.Color]$c) { New-Object System.Drawing.SolidBrush -ArgumentList $c }
function PT([double]$x, [double]$y) { New-Object System.Drawing.PointF -ArgumentList ([float]$x), ([float]$y) }

# ---------------------------------------------------------------- GEAR (back)
$cx = 168.0; $cy = 186.0
$rOuter = 150.0; $rRoot = 112.0; $rHole = 60.0
$teeth = 10
$step = 2 * [Math]::PI / $teeth
$pts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
for ($i = 0; $i -lt $teeth; $i++) {
    $a = $i * $step
    foreach ($pair in @(@($rRoot, 0.00), @($rOuter, 0.15), @($rOuter, 0.40), @($rRoot, 0.55))) {
        $r = [double]$pair[0]; $ang = $a + ([double]$pair[1]) * $step
        $pts.Add((PT ($cx + $r * [Math]::Cos($ang)) ($cy + $r * [Math]::Sin($ang))))
    }
}
$gearPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$gearPath.AddPolygon($pts.ToArray())
$g.FillPath((SB $red), $gearPath)
# punch the gear hub so the wrench/background shows through
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.FillEllipse((SB $trans), ([float]($cx - $rHole)), ([float]($cy - $rHole)), ([float](2 * $rHole)), ([float](2 * $rHole)))
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver

# ----------------------------------------------------------------- TIRE (mid)
$tx = 338.0; $ty = 338.0
$rTire = 150.0; $rRim = 104.0
$g.FillEllipse((SB $black), ([float]($tx - $rTire)), ([float]($ty - $rTire)), ([float](2 * $rTire)), ([float](2 * $rTire)))
# subtle tread hatching
$penTread = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::FromArgb(255, 58, 58, 58)), ([float]16)
$penTread.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
$rT = $rTire - 20
$g.DrawEllipse($penTread, ([float]($tx - $rT)), ([float]($ty - $rT)), ([float](2 * $rT)), ([float](2 * $rT)))
# silver rim
$g.FillEllipse((SB $white), ([float]($tx - $rRim)), ([float]($ty - $rRim)), ([float](2 * $rRim)), ([float](2 * $rRim)))
# spokes
$penSpoke = New-Object System.Drawing.Pen -ArgumentList $black, ([float]26)
$penSpoke.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$penSpoke.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
for ($k = 0; $k -lt 5; $k++) {
    $ang = -[Math]::PI / 2 + $k * 2 * [Math]::PI / 5
    $sx = $tx + 52 * [Math]::Cos($ang); $sy = $ty + 52 * [Math]::Sin($ang)
    $g.DrawLine($penSpoke, ([float]$tx), ([float]$ty), ([float]$sx), ([float]$sy))
}
# hub
$g.FillEllipse((SB $black), ([float]($tx - 34)), ([float]($ty - 34)), 68, 68)
$g.FillEllipse((SB $white), ([float]($tx - 14)), ([float]($ty - 14)), 28, 28)

# ----------------------------------------------------------------- WRENCH (front)
$ringX = 112.0; $ringY = 424.0
$headX = 392.0; $headY = 128.0
$rRing = 40.0; $rHead = 52.0

$penShaft = New-Object System.Drawing.Pen -ArgumentList $red, ([float]50)
$penShaft.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$penShaft.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($penShaft, ([float]$ringX), ([float]$ringY), ([float]$headX), ([float]$headY))

# box (ring) end
$g.FillEllipse((SB $red), ([float]($ringX - $rRing)), ([float]($ringY - $rRing)), ([float](2 * $rRing)), ([float](2 * $rRing)))
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.FillEllipse((SB $trans), ([float]($ringX - 18)), ([float]($ringY - 18)), 36, 36)
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
$g.FillEllipse((SB $red), ([float]($ringX - 24)), ([float]($ringY - 24)), 48, 48)
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.FillEllipse((SB $trans), ([float]($ringX - 18)), ([float]($ringY - 18)), 36, 36)
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver

# open (jaw) end
$g.FillEllipse((SB $red), ([float]($headX - $rHead)), ([float]($headY - $rHead)), ([float](2 * $rHead)), ([float](2 * $rHead)))
$state = $g.Save()
$g.TranslateTransform([float]$headX, [float]$headY)
$g.RotateTransform(-47)
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$jaw = New-Object System.Drawing.Drawing2D.GraphicsPath
$jaw.FillMode = [System.Drawing.Drawing2D.FillMode]::Winding
$jaw.AddRectangle((New-Object System.Drawing.RectangleF -ArgumentList ([float]4), ([float]-17), ([float]90), ([float]34)))
$jaw.AddEllipse((New-Object System.Drawing.RectangleF -ArgumentList ([float]-14), ([float]-17), ([float]34), ([float]34)))
$g.FillPath((SB $trans), $jaw)
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
$g.Restore($state)

$out = Join-Path $PSScriptRoot 'logo.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)

# Also emit a multi-size .ico (PNG-compressed entries) so windows/taskbar use the mark.
$icoOut = Join-Path $PSScriptRoot 'logo.ico'
$sizes = @(256, 128, 64, 48, 32, 16)
$pngs = @()
foreach ($s in $sizes) {
    $b = New-Object System.Drawing.Bitmap -ArgumentList $s, $s
    $bg = [System.Drawing.Graphics]::FromImage($b)
    $bg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $bg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $bg.Clear([System.Drawing.Color]::Transparent)
    $bg.DrawImage($bmp, (New-Object System.Drawing.Rectangle -ArgumentList 0, 0, $s, $s))
    $bg.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    $ms.Dispose(); $b.Dispose()
}
$fs = [System.IO.File]::Create($icoOut)
$bw = New-Object System.IO.BinaryWriter -ArgumentList $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $bw.Write([Byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([Byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$pngs[$i].Length)
    $bw.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush(); $bw.Dispose(); $fs.Dispose()

$g.Dispose(); $bmp.Dispose()
Write-Host "Wrote $out"
Write-Host "Wrote $icoOut"
