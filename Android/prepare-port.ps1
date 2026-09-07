$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$utf8 = [Text.UTF8Encoding]::new($false)
# Android transport is maintained separately for incremental synchronization.
# Preserve the original help and credits text, changing only platform-specific statements.
$about = [IO.File]::ReadAllText((Join-Path $sourceRoot 'AcercaDeForm.cs'))
$assets = Join-Path $PSScriptRoot 'Assets'
[IO.Directory]::CreateDirectory($assets) | Out-Null
foreach ($item in @(@{Name='description.txt'; Start='                Text = "Esta aplicación'; End='            };'}, @{Name='help.txt'; Start='"EXPLICACIÓN DE FUNCIONES'; End='            };'})) {
    $start = $about.IndexOf($item.Start)
    $end = $about.IndexOf($item.End, $start)
    $part = $about.Substring($start, $end-$start)
    $value = ([regex]::Matches($part, '"((?:\\.|[^"\\])*)"') | ForEach-Object { [regex]::Unescape($_.Groups[1].Value) }) -join ''
    $value = $value.Replace('Desarrollada para Windows 10/11 32/64 bits · .NET Framework 4.7.2','Adaptada para Android · .NET para Android')
    $value = $value.Replace('   - La primera vez descarga Chromium (unos 150 MB) – puede tardar.','   - Utiliza la conexión a Internet del celular.')
    [IO.File]::WriteAllText((Join-Path $assets $item.Name), $value, $utf8)
}
# Extract the existing PNG representation from the original ICO without redrawing it.
$icon = [IO.File]::ReadAllBytes((Join-Path $sourceRoot 'BalotoApp.ico'))
$entries = [BitConverter]::ToUInt16($icon,4)
$found = $false
for ($i=0; $i -lt $entries; $i++) {
    $entry=6+16*$i
    $size=[BitConverter]::ToInt32($icon,$entry+8)
    $offset=[BitConverter]::ToInt32($icon,$entry+12)
    if ($icon[$offset] -eq 137 -and $icon[$offset+1] -eq 80) {
        $drawable=Join-Path $PSScriptRoot 'Resources\drawable'
        [IO.Directory]::CreateDirectory($drawable) | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $drawable 'app_icon.png'),$icon[$offset..($offset+$size-1)])
        $found=$true
        break
    }
}
if (-not $found) {
    Add-Type -AssemblyName System.Drawing
    $drawable=Join-Path $PSScriptRoot 'Resources\drawable'
    [IO.Directory]::CreateDirectory($drawable) | Out-Null
    $originalIcon=[Drawing.Icon]::new((Join-Path $sourceRoot 'BalotoApp.ico'),256,256)
    $bitmap=$originalIcon.ToBitmap()
    $bitmap.Save((Join-Path $drawable 'app_icon.png'),[Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    $originalIcon.Dispose()
}
Write-Output 'Original text and icon prepared; incremental transport preserved.'
