param(
    [string]$Configuration = "Release",

    [string]$Version,

    [string]$OutputBaseFilename,

    [string]$PublishDir,

    [switch]$UseReleaseMetadata
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$useExistingPublishDir = $PSBoundParameters.ContainsKey("PublishDir") -and -not [string]::IsNullOrWhiteSpace($PublishDir)
$isStandardRelease = $Configuration -ieq "Release"
$useOfficialMetadata = $UseReleaseMetadata.IsPresent -or $isStandardRelease
$runtimeIdentifier = "win-x64"
$publishFolder = if ($isStandardRelease) { "app" } else { "custom" }
$publishDir = if ($useExistingPublishDir) {
    (Resolve-Path $PublishDir).Path
} else {
    Join-Path $root (Join-Path "publish" $publishFolder)
}
$installerScript = Join-Path $root "installer.iss"

if (-not $PSBoundParameters.ContainsKey("Version")) {
    $Version = if ($useOfficialMetadata) { "1.0.0" } else { "1.0.0-custom" }
}

if (-not $PSBoundParameters.ContainsKey("OutputBaseFilename")) {
    $OutputBaseFilename = if ($useOfficialMetadata) { "EasyNoteSetup" } else { "EasyNoteSetup-custom" }
}

if ($useExistingPublishDir) {
    if (-not (Test-Path $publishDir)) {
        throw "Publish directory not found: $publishDir"
    }

    Write-Host "Using existing publish directory: $publishDir"
} else {
    $publishRoot = Join-Path $root "publish"
    if (Test-Path $publishDir) {
        $resolvedPublishDir = (Resolve-Path $publishDir).Path
        $resolvedPublishRoot = (Resolve-Path $publishRoot).Path

        if (-not $resolvedPublishDir.StartsWith($resolvedPublishRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to delete publish directory outside workspace publish root: $resolvedPublishDir"
        }

        Remove-Item -LiteralPath $resolvedPublishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    Write-Host "Publishing $Configuration build for $runtimeIdentifier (self-contained)..."
    dotnet publish (Join-Path $root "EasyNote\EasyNote.csproj") `
        -c $Configuration `
        -r $runtimeIdentifier `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir
}

$iscc = Get-Command ISCC -ErrorAction SilentlyContinue
if (-not $iscc) {
    $iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
}
if (-not $iscc) {
    $candidatePackagers = @(
        $env:ISCC_EXE,
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in $candidatePackagers) {
        if (Test-Path $candidate) {
            $iscc = @{ Source = (Resolve-Path $candidate).Path }
            break
        }
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
