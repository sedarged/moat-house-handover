$ErrorActionPreference = 'Stop'

$RootDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$HostProject = Join-Path $RootDir 'desktop-host/MoatHouseHandover.Host.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error '[ERROR] Missing required tool: dotnet'
}

if (-not (Test-Path -Path $HostProject -PathType Leaf)) {
    Write-Error "[ERROR] Host project not found: $HostProject"
}

Write-Host 'Building desktop host with Windows targeting enabled'
dotnet build $HostProject -c Release -p:EnableWindowsTargeting=true

Write-Host 'Host build completed.'
