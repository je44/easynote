param(
    [string]$Configuration = "Release",

    [string]$Version,

    [string]$OutputBaseFilename,

    [string]$PublishDir,

    [switch]$UseReleaseMetadata,

    [switch]$SkipInstaller
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
$portableDir = Join-Path $root (Join-Path "publish" "portable")
$portableZipPath = Join-Path $root (Join-Path "publish" "EasyNote-portable-win-x64.zip")
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

if (Test-Path $portableDir) {
    $resolvedPortableDir = (Resolve-Path $portableDir).Path
    $resolvedRoot = (Resolve-Path $root).Path

    if (-not $resolvedPortableDir.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to delete portable directory outside workspace root: $resolvedPortableDir"
    }

    Remove-Item -LiteralPath $resolvedPortableDir -Recurse -Force
}

New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $portableDir -Recurse -Force
Get-ChildItem -Path $portableDir -Include "*.pdb", "*.xml" -Recurse -File | Remove-Item -Force
New-Item -ItemType Directory -Path (Join-Path $portableDir "data") -Force | Out-Null
New-Item -ItemType File -Path (Join-Path $portableDir "portable.marker") -Force | Out-Null

if (Test-Path $portableZipPath) {
    Remove-Item -LiteralPath $portableZipPath -Force
}

Compress-Archive -Path (Join-Path $portableDir "*") -DestinationPath $portableZipPath -CompressionLevel Optimal

$shouldBuildInstaller = -not $SkipInstaller.IsPresent

if ($shouldBuildInstaller) {
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
        Write-Host "Portable package created successfully."
        Write-Host "Packaging tool not found, so installer EXE packaging was skipped."
        Write-Host "Publish folder:  $publishDir"
        Write-Host "Portable dir:   $portableDir"
        Write-Host "Portable zip:   $portableZipPath"
        exit 0
    }

    Write-Host ""
    Write-Host "Building EXE package..."
    & $iscc.Source "/DPublishDir=$publishDir" "/DMyAppVersion=$Version" "/DOutputBaseFilename=$OutputBaseFilename" $installerScript
    if ($LASTEXITCODE -ne 0) {
        throw "Installer packaging failed with exit code $LASTEXITCODE"
    }
}

Write-Host ""
Write-Host "Done."
Write-Host "Publish:        $publishDir"
Write-Host "Portable dir:   $portableDir"
Write-Host "Portable zip:   $portableZipPath"
if ($shouldBuildInstaller) {
    Write-Host "Installer dir:  $(Join-Path $root 'installer')"
} else {
    Write-Host "Installer dir:  skipped"
}
