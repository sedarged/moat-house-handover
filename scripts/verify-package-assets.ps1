$ErrorActionPreference = 'Stop'

$RootDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$PackageDir = if ($args.Count -gt 0) { $args[0] } else { (Join-Path $RootDir 'dist/local-host') }

if (-not (Test-Path -Path $PackageDir -PathType Container)) {
    Write-Error "[ERROR] Package directory not found: $PackageDir`nRun scripts/package-local.ps1 first."
}

$WebIndex = Join-Path $PackageDir 'webapp/index.html'
$RuntimeConfig = Join-Path $PackageDir 'config/runtime.config.json'

if (-not (Test-Path -Path $WebIndex -PathType Leaf)) {
    Write-Error "[ERROR] Missing packaged web asset: $WebIndex"
}

if (-not (Test-Path -Path $RuntimeConfig -PathType Leaf)) {
    Write-Error "[ERROR] Missing packaged runtime config: $RuntimeConfig"
}

Write-Host "[OK] Found packaged web asset: $WebIndex"
Write-Host "[OK] Found packaged runtime config: $RuntimeConfig"
Write-Host 'Packaged asset verification passed.'
