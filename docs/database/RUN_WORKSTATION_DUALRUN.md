# Run Workstation Dual-run Evidence (Phase 9C)

Use this on a real Windows workstation with `M:\Moat House\MoatHouse Handover\` available.

## Command

```powershell
.\scripts\run-workstation-dualrun.ps1 -ShiftCode PM -ShiftDate 2026-05-01
```

## Optional parameters

- `-Departments "Injection","MetaPress","Slicing","Goods In & Despatch"`
- `-UserName dualrun`
- `-AllowNonWindows` (debug only)

## Host CLI equivalent

```powershell
.\MoatHouseHandover.Host.exe --dualrun-evidence --shift-code PM --shift-date 2026-05-01 --departments Injection,MetaPress,Slicing --user-name dualrun
```

## Outputs

Writes reports under:

`M:\Moat House\MoatHouse Handover\Migration\DualRun\`

- `dualrun_YYYY-MM-DD_HH-mm-ss.json`
- `dualrun_YYYY-MM-DD_HH-mm-ss.txt`

## Status meanings

- `ACCEPTED_EVIDENCE_READY`: latest report validates as accepted.
- `NOT_READY_MISMATCH_FOUND`: report generated but contains mismatches/failures or non-accepted recommendation.
- `BLOCKED_ENVIRONMENT`: missing Windows/M: prerequisites or runner blocked.
- `RUN_FAILED`: unexpected failure.

## Safety

- AccessLegacy remains default runtime provider.
- No automatic runtime provider switch.
- No migration execute.
- No restore.
- No email sending.
