# App-owned data root

The Moat House Handover app owns the operational data root. Default root:
`M:\Moat House\MoatHouse Handover\`

First-run initialization creates (if missing):
- `Data/`
- `Attachments/`
- `Reports/`
- `Backups/`
- `Migration/`
- `Migration/DualRun/`
- `Logs/`
- `Config/`

SQLite target DB path: `Data/moat-house.db`.
AccessLegacy fallback/import path: `Data/moat_handover_be.accdb`.

First-run behavior is non-destructive:
- creates folders safely
- bootstraps SQLite schema if DB is missing
- does not overwrite existing SQLite DB
- does not auto-import Access
- does not auto-switch runtime provider
- does not auto-restore backups

Next phases:
- Phase 10B: shared SQLite lock/concurrency safety
- Phase 10C: Main Menu + App Shell UI

Phase 10B adds app-level shared SQLite lock safety using `Data/moat-house.write.lock` for conservative write ownership checks.

Phase 10B enforcement note: SQLite mutating repository methods are guarded by AppWriteGuard when SQLite is effective provider; AccessLegacy is unchanged.
