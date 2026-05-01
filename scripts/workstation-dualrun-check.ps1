param([switch]$DryRun)
$root = 'M:\Moat House\MoatHouse Handover'
Write-Host 'Phase 9B workstation dual-run checklist (read-only by default)'
Write-Host "Data root: $root"
$paths = @("$root\Data", "$root\Backups", "$root\Migration\DualRun")
foreach ($p in $paths) { Write-Host ("{0}: {1}" -f $p, (Test-Path $p)) }
Write-Host 'Runtime default must remain AccessLegacy.'
Write-Host 'Request SQLite only after accepted dual-run report and gate pass.'
Write-Host 'No migration/restore/provider switch is executed by this script.'
if ($DryRun) { Write-Host 'DryRun flag provided: still non-destructive.' }
