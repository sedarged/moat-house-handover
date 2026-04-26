$ErrorActionPreference = 'Stop'

$RootDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$HostProject = Join-Path $RootDir 'desktop-host/MoatHouseHandover.Host.csproj'
$DistDir = Join-Path $RootDir 'dist/local-host'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error '[ERROR] Missing required tool: dotnet'
}

if (-not (Test-Path -Path $HostProject -PathType Leaf)) {
    Write-Error "[ERROR] Host project not found: $HostProject"
}

if (Test-Path -Path $DistDir) {
    Remove-Item -Path $DistDir -Recurse -Force
}
New-Item -Path $DistDir -ItemType Directory | Out-Null

Write-Host "Publishing desktop host package to $DistDir"
dotnet publish $HostProject -c Release -o $DistDir -p:EnableWindowsTargeting=true

Write-Host "Local package created: $DistDir"
