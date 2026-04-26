$ErrorActionPreference = 'Stop'

$RootDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$WebAppDir = Join-Path $RootDir 'webapp'

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error '[ERROR] Missing required tool: node'
}

$IndexPath = Join-Path $WebAppDir 'index.html'
if (-not (Test-Path -Path $IndexPath -PathType Leaf)) {
    Write-Error "[ERROR] Missing web entrypoint: $IndexPath"
}

Write-Host "[OK] Found web entrypoint: $IndexPath"

$JsDir = Join-Path $WebAppDir 'js'
$JsFiles = Get-ChildItem -Path $JsDir -Filter '*.js' -File -Recurse | Sort-Object FullName

if (-not $JsFiles -or $JsFiles.Count -eq 0) {
    Write-Error "[ERROR] No JavaScript files found under $JsDir"
}

Write-Host 'Running syntax checks with node --check'
foreach ($File in $JsFiles) {
    node --check $File.FullName | Out-Null
    Write-Host "[OK] $($File.FullName)"
}

Write-Host 'Web checks completed.'
