param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('AM','PM','NS')]
    [string]$ShiftCode,

    [Parameter(Mandatory = $true)]
    [DateTime]$ShiftDate,

    [string[]]$Departments = @('Injection', 'MetaPress', 'Slicing', 'Goods In & Despatch'),

    [string]$UserName = 'dualrun',

    [string]$DataRoot = 'M:\Moat House\MoatHouse Handover\',

    [switch]$AllowNonWindows
)

$approvedRoot = $DataRoot
if (-not $approvedRoot.EndsWith('\')) { $approvedRoot = $approvedRoot + '\' }
$accessPath = Join-Path $approvedRoot 'Data\moat_handover_be.accdb'
$sqlitePath = Join-Path $approvedRoot 'Data\moat-house.db'
$reportRoot = Join-Path $approvedRoot 'Migration\DualRun'

$isWindowsRuntime = $env:OS -eq 'Windows_NT' -or [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

if (-not $isWindowsRuntime -and -not $AllowNonWindows) {
    Write-Host 'BLOCKED_ENVIRONMENT: This script only runs on Windows unless -AllowNonWindows is passed.' -ForegroundColor Yellow
    exit 2
}

Write-Host 'Running workstation dual-run evidence (non-destructive/read-only compare + report write only).'
Write-Host "Moat House Handover app data root: $approvedRoot"
Write-Host "Access DB path: $accessPath"
Write-Host "SQLite DB path: $sqlitePath"
Write-Host "Report output folder: $reportRoot"
Write-Host "AccessLegacy exists: $(Test-Path $accessPath)"
Write-Host "SQLite exists: $(Test-Path $sqlitePath)"
Write-Host "Reports folder exists: $(Test-Path (Join-Path $approvedRoot 'Reports'))"
Write-Host "Reports folder writable: $([bool](New-Item -ItemType Directory -Path (Join-Path $approvedRoot 'Reports') -Force -ErrorAction SilentlyContinue))"
Write-Host "ShiftCode: $ShiftCode"
Write-Host "ShiftDate: $($ShiftDate.ToString('yyyy-MM-dd'))"
Write-Host "Departments: $($Departments -join ', ')"

if ($approvedRoot -ne 'M:\Moat House\MoatHouse Handover\') {
    Write-Host 'WARNING: data root is not the approved M: root.' -ForegroundColor Yellow
}

$projectPath = Join-Path $PSScriptRoot '..\desktop-host\MoatHouseHandover.Host.csproj'
$departmentArg = $Departments -join ','
$arguments = @(
    'run', '--project', $projectPath, '--',
    '--dualrun-evidence',
    '--shift-code', $ShiftCode,
    '--shift-date', $ShiftDate.ToString('yyyy-MM-dd'),
    '--departments', $departmentArg,
    '--user-name', $UserName,
    '--data-root', $approvedRoot
)

& dotnet @arguments
$exitCode = $LASTEXITCODE

$latestJson = Get-ChildItem -Path $reportRoot -Filter 'dualrun_*.json' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1
$latestTxt = Get-ChildItem -Path $reportRoot -Filter 'dualrun_*.txt' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -ne $latestJson) {
    Write-Host "Latest JSON report: $($latestJson.FullName)"
} else {
    Write-Host 'Latest JSON report: (not found)'
}

if ($null -ne $latestTxt) {
    Write-Host "Latest TXT report: $($latestTxt.FullName)"
} else {
    Write-Host 'Latest TXT report: (not found)'
}

if ($exitCode -eq 0) {
    Write-Host 'ACCEPTED_EVIDENCE_READY' -ForegroundColor Green
    exit 0
}
if ($exitCode -eq 1) {
    Write-Host 'NOT_READY_MISMATCH_FOUND' -ForegroundColor Yellow
    exit 1
}
if ($exitCode -eq 2) {
    Write-Host 'BLOCKED_ENVIRONMENT' -ForegroundColor Yellow
    exit 2
}

Write-Host 'RUN_FAILED' -ForegroundColor Red
exit 3
