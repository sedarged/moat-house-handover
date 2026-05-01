# Backup / Restore / Rollback Foundation (Phase 6)

Access remains the active runtime provider after Phase 6.

## Paths
- Backup root: `M:\Moat House\MoatHouse Handover\Backups`
- Migration root: `M:\Moat House\MoatHouse Handover\Migration`
- Access DB: `M:\Moat House\MoatHouse Handover\Data\moat_handover_be.accdb`
- SQLite target: `M:\Moat House\MoatHouse Handover\Data\moat-house.db`

## Backup structure
`Backups/YYYY-MM-DD_HH-mm-ss_<kind>/` with:
- `manifest.json`
- `summary.txt`
- `Data/*`
- `Migration/*` (latest reports optionally)

Manifest stores included files, size, and SHA256.

## Restore modes
- `ValidateOnly`: validates backup folder + manifest integrity only.
- `RestoreToStaging`: copies files into safe staging output.
- `RestoreToLive`: requires successful validation and creates `PreRestore` backup first.

## Rollback support
- PreMigration backups are recognized and can be validated/restored using normal restore planner/service.
- Rollback can target staging or live target path.

## Retention
- Phase 6 includes dry-run retention planning only.
- Keep-all is default safety posture.
- No automatic destructive cleanup by default.

## Not implemented yet
- No end-user UI for backup/restore/rollback.
- No runtime provider switch to SQLite.
- No unattended scheduled backup execution.

## Windows limitations
Cloud/Linux checks cannot prove shared `M:` permissions, ACE/OLEDB provider runtime behavior, or WPF/WebView2 interactive flows.

Phase 10B note: lock/concurrency checks protect active writes but do not replace backup/restore safeguards.
