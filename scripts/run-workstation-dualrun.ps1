param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('AM','PM','NS')]
    [string]$ShiftCode,

    [Parameter(Mandatory = $true)]
    [DateTime]$ShiftDate,

    [string[]]$Departments = @('Injection', 'MetaPress', 'Slicing', 'Goods In & Despatch'),

    [string]$UserName = 'dualrun',

    [switch]$AllowNonWindows
)

$approvedRoot = 'M:\Moat House\MoatHouse Handover\'
$accessPath = 'M:\Moat House\MoatHouse Handover\Data\moat_handover_be.accdb'
$sqlitePath = 'M:\Moat House\MoatHouse Handover\Data\moat-house.db'
$reportRoot = 'M:\Moat House\MoatHouse Handover\Migration\DualRun\'

if (-not $IsWindows -and -not $AllowNonWindows) {
    Write-Host 'BLOCKED_ENVIRONMENT: This script only runs on Windows unless -AllowNonWindows is passed.' -ForegroundColor Yellow
    exit 2
}

Write-Host 'Running workstation dual-run evidence (non-destructive/read-only compare + report write only).'
Write-Host "Approved data root: $approvedRoot"
Write-Host "Access DB path: $accessPath"
Write-Host "SQLite DB path: $sqlitePath"
Write-Host "Report output folder: $reportRoot"
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
    '--user-name', $UserName
)

& dotnet @arguments
$exitCode = $LASTEXITCODE

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
