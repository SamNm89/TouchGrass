# Requires ImageMagick installed and 'magick' available in PATH

Get-ChildItem -Filter *.png | ForEach-Object {
    $baseName = $_.BaseName
    $inputPath = $_.FullName
    $outputIco = "$baseName.ico"

    # Create a single .ico with only 512x512 resolution
    magick convert "$inputPath" -resize 512x512 "$outputIco"

    Write-Host "Created 512x512 icon: $outputIco"
}
