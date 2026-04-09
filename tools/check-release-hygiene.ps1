$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$propsPath = Join-Path $repoRoot "src/Directory.Build.props"

if (-not (Test-Path $propsPath)) {
    throw "Could not find src/Directory.Build.props at $propsPath"
}

[xml]$props = Get-Content $propsPath -Raw
$currentVersion = $props.Project.PropertyGroup.Version

if ([string]::IsNullOrWhiteSpace($currentVersion)) {
    throw "Could not read <Version> from src/Directory.Build.props"
}

$currentTfm = "net10.0"
$issues = New-Object System.Collections.Generic.List[string]

function Get-Matches {
    param(
        [string]$basePath,
        [string[]]$relativeGlobs,
        [string]$pattern
    )

    foreach ($glob in $relativeGlobs) {
        $fullPattern = Join-Path $basePath $glob
        foreach ($file in Get-ChildItem -Path $fullPattern -File -ErrorAction SilentlyContinue) {
            $content = Get-Content $file.FullName -Raw
            foreach ($m in [regex]::Matches($content, $pattern)) {
                [pscustomobject]@{
                    Path = $file.FullName
                    Value = $m.Value
                }
            }
        }
    }
}

$versionPattern = "\b\d+\.\d+\.\d+-(?:alpha|beta|rc)(?:[.-]?\d+)\b"
$tfmPattern = "\bnet\d+\.\d+\b"

$versionTargets = @(
    "examples/**/*.cs",
    "assets/**/*.svg"
)

$tfmTargets = @(
    "examples/**/*.cs",
    "assets/**/*.svg"
)

$versionMatches = Get-Matches -basePath $repoRoot -relativeGlobs $versionTargets -pattern $versionPattern
foreach ($match in $versionMatches) {
    if ($match.Value -ne $currentVersion) {
        $relative = [System.IO.Path]::GetRelativePath($repoRoot, $match.Path)
        $issues.Add("Version drift: $relative contains '$($match.Value)' but expected '$currentVersion'.")
    }
}

$tfmMatches = Get-Matches -basePath $repoRoot -relativeGlobs $tfmTargets -pattern $tfmPattern
foreach ($match in $tfmMatches) {
    if ($match.Value -ne $currentTfm) {
        $relative = [System.IO.Path]::GetRelativePath($repoRoot, $match.Path)
        $issues.Add("TFM drift: $relative contains '$($match.Value)' but expected '$currentTfm'.")
    }
}

if ($issues.Count -gt 0) {
    Write-Host "Release hygiene check failed:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host " - $issue" -ForegroundColor Red
    }
    exit 1
}

Write-Host "Release hygiene check passed for version '$currentVersion' and TFM '$currentTfm'." -ForegroundColor Green
