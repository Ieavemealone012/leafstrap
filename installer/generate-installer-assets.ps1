$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$installerRoot = $PSScriptRoot
$windowsRoot = Split-Path $installerRoot -Parent
$assetsRoot = Join-Path $installerRoot "assets"
$iconPath = Join-Path $windowsRoot "Bloxstrap\app.ico"
$licensePath = Join-Path $windowsRoot "LICENSE"

New-Item -ItemType Directory -Path $assetsRoot -Force | Out-Null

function ConvertTo-RtfText([string] $text) {
    $builder = [System.Text.StringBuilder]::new()
    foreach ($character in $text.ToCharArray()) {
        $code = [int] $character
        switch ($character) {
            '\' { [void] $builder.Append('\\') }
            '{'  { [void] $builder.Append('\{') }
            '}'  { [void] $builder.Append('\}') }
            "`r" { }
            "`n" { [void] $builder.Append("\par`r`n") }
            default {
                if ($code -gt 127) {
                    if ($code -gt 32767) { $code -= 65536 }
                    [void] $builder.Append("\u$code?")
                }
                else {
                    [void] $builder.Append($character)
                }
            }
        }
    }
    return $builder.ToString()
}

$licenseText = Get-Content -LiteralPath $licensePath -Raw
$rtfBody = ConvertTo-RtfText $licenseText
$rtf = "{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\fs18 $rtfBody}"
[System.IO.File]::WriteAllText((Join-Path $assetsRoot "License.rtf"), $rtf, [System.Text.Encoding]::ASCII)

$sourceIcon = [System.Drawing.Icon]::new($iconPath, 256, 256)
$leafIcon = $sourceIcon.ToBitmap()

function New-InstallerBitmap([string] $path, [int] $width, [int] $height, [bool] $large) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([System.Drawing.Color]::White)

    if ($large) {
        $accentBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(232, 247, 234))
        $graphics.FillRectangle($accentBrush, 0, 0, 164, $height)
        $graphics.DrawImage($leafIcon, 34, 82, 96, 96)
        $accentBrush.Dispose()
    }
    else {
        $accentBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(31, 168, 74))
        $graphics.FillRectangle($accentBrush, 0, 0, 7, $height)
        $graphics.DrawImage($leafIcon, $width - 54, 7, 44, 44)
        $accentBrush.Dispose()
    }

    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $graphics.Dispose()
    $bitmap.Dispose()
}

New-InstallerBitmap (Join-Path $assetsRoot "Banner.bmp") 493 58 $false
New-InstallerBitmap (Join-Path $assetsRoot "Dialog.bmp") 493 312 $true

$leafIcon.Dispose()
$sourceIcon.Dispose()
