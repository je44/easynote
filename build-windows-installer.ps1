param(
    [string]$Configuration = "Release",

    [string]$Version,

    [string]$OutputBaseFilename
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$isStandardRelease = $Configuration -ieq "Release"
$publishFolder = if ($isStandardRelease) { "app" } else { "custom" }
$publishDir = Join-Path $root (Join-Path "publish" $publishFolder)
$installerScript = Join-Path $root "installer.iss"

if (-not $PSBoundParameters.ContainsKey("Version")) {
    $Version = if ($isStandardRelease) { "1.0.0" } else { "1.0.0-custom" }
}

if (-not $PSBoundParameters.ContainsKey("OutputBaseFilename")) {
    $OutputBaseFilename = if ($isStandardRelease) { "EasyNoteSetup" } else { "EasyNoteSetup-custom" }
}

Write-Host "Publishing $Configuration build..."
dotnet publish (Join-Path $root "EasyNote\EasyNote.csproj") -c $Configuration -o $publishDir

$iscc = Get-Command ISCC -ErrorAction SilentlyContinue
if (-not $iscc) {
    $iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
}
if (-not $iscc) {
    $explicitPackager = $env:ISCC_EXE
    if ($explicitPackager -and (Test-Path $explicitPackager)) {
        $iscc = @{ Source = (Resolve-Path $explicitPackager).Path }
    }
}

if (-not $iscc) {
    Write-Host ""
    Write-Host "$Configuration publish completed."
    Write-Host "Packaging tool not found, so EXE packaging was skipped."
    Write-Host "Expected publish folder: $publishDir"
    exit 0
}

Write-Host ""
Write-Host "Building EXE package..."
& $iscc.Source "/DPublishDir=$publishDir" "/DMyAppVersion=$Version" "/DOutputBaseFilename=$OutputBaseFilename" $installerScript

Write-Host ""
Write-Host "Done."
Write-Host "Publish:   $publishDir"
Write-Host "Package:   $(Join-Path $root 'installer')"
