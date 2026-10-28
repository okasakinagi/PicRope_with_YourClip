# 把原始角色图生成为可内嵌的 384×384 素材。
#
# 为什么要 384×384：上游 packaging\prepare-assets.ps1 会把 basket-*.png 统一缩放到
# 384×384（忽略长宽比）。素材本身就是 384×384 时，这一步等于无操作，角色不会被压扁。
#
# 用法：
#   .\make-clip-assets.ps1 -SourceDir "D:\我的图"          # 生成到 src\assets
#   .\make-clip-assets.ps1 -SourceDir "D:\我的图" -Local   # 生成到 personal\local-assets（不提交）
param(
    [string]$SourceDir,
    [string]$OutputDir,
    [switch]$Local,
    [int]$Canvas = 384,
    [int]$CharacterHeight = 176,
    [int]$CharacterTop = 68
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $SourceDir) { $SourceDir = Join-Path $PSScriptRoot 'images' }
if (-not $OutputDir) {
    $OutputDir = if ($Local) { Join-Path $root 'personal\local-assets' } else { Join-Path $root 'src\assets' }
}
if (-not (Test-Path -LiteralPath $SourceDir)) {
    throw "找不到图片来源目录：$SourceDir`n用 -SourceDir 指定放图的位置。"
}
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$files = Get-ChildItem -LiteralPath $SourceDir -Filter *.png | Where-Object { $_.Name -notlike '_*' } | Sort-Object Name
if ($files.Count -eq 0) { throw "目录里没有 PNG：$SourceDir" }

$index = 0
foreach ($file in $files) {
    $index++
    $source = [System.Drawing.Bitmap]::FromFile($file.FullName)
    try {
        $minX = $source.Width; $minY = $source.Height; $maxX = -1; $maxY = -1
        for ($y = 0; $y -lt $source.Height; $y += 2) {
            for ($x = 0; $x -lt $source.Width; $x += 2) {
                if ($source.GetPixel($x, $y).A -gt 16) {
                    if ($x -lt $minX) { $minX = $x }
                    if ($x -gt $maxX) { $maxX = $x }
                    if ($y -lt $minY) { $minY = $y }
                    if ($y -gt $maxY) { $maxY = $y }
                }
            }
        }
        if ($maxX -lt 0) { Write-Output ('跳过（全透明）：' + $file.Name); continue }

        $cropX = [math]::Max(0, $minX - 2); $cropY = [math]::Max(0, $minY - 2)
        $cropW = [math]::Min($source.Width - $cropX, $maxX - $cropX + 3)
        $cropH = [math]::Min($source.Height - $cropY, $maxY - $cropY + 3)
        $drawW = [int][math]::Round($CharacterHeight * $cropW / $cropH)
        if ($drawW -gt $Canvas) { $drawW = $Canvas }
        $drawX = [int](($Canvas - $drawW) / 2)

        $canvasImage = New-Object System.Drawing.Bitmap($Canvas, $Canvas, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($canvasImage)
            try {
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($source,
                    (New-Object System.Drawing.Rectangle($drawX, $CharacterTop, $drawW, $CharacterHeight)),
                    (New-Object System.Drawing.Rectangle($cropX, $cropY, $cropW, $cropH)),
                    [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally { $graphics.Dispose() }
            $destination = Join-Path $OutputDir ('basket-clip' + $index + '.png')
            $canvasImage.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
            Write-Output ('生成 ' + (Split-Path $destination -Leaf) + '  ←  ' + $file.Name + '  角色 ' + $drawW + 'x' + $CharacterHeight)
        }
        finally { $canvasImage.Dispose() }
    }
    finally { $source.Dispose() }
}
Write-Output ('共 ' + $index + ' 张 → ' + $OutputDir)

# 清掉上一次生成多出来的编号，避免换图后旧角色残留
$stale = Get-ChildItem -LiteralPath $OutputDir -Filter 'basket-clip*.png' |
    Where-Object { $_.BaseName -match '^basket-clip(\d+)$' -and [int]$Matches[1] -gt $index }
foreach ($file in $stale) {
    Remove-Item -LiteralPath $file.FullName -Force
    Write-Output ('清理残留：' + $file.Name)
}
