# Access to SQLite Importer (Phase 5)

## Scope
Phase 5 adds importer, validation, and report foundations only.

Access remains the active runtime provider. SQLite is migration target only.

## Modes
- **DryRun**: reads Access, imports to staging SQLite, validates, writes JSON/TXT report, does not promote target.
- **Execute**: does DryRun pipeline, and if validation has no errors then backs up existing target DB and promotes staging DB to target.

## Paths
- Source Access: `M:\Moat House\MoatHouse Handover\Data\moat_handover_be.accdb`
- Target SQLite: `M:\Moat House\MoatHouse Handover\Data\moat-house.db`
- Staging SQLite: `M:\Moat House\MoatHouse Handover\Migration\moat-house.importing.db` (caller-defined)
- Reports folder: `M:\Moat House\MoatHouse Handover\Migration\`

## Staging strategy
- Always bootstrap a fresh staging DB first.
- Seeded Phase 4 tables (`tblDepartments`, `tblShiftRules`, `tblConfig`) are explicitly cleared in staging before importing Access source equivalents.
- Validation runs on staging DB.
- Execute mode promotes only if validation has no errors.

## Validation checks
- required SQLite tables exist
- `tblSchemaMigrations` contains `001_initial_sqlite_schema`
- per-table source/import accounting consistency
- failed/skipped rows produce issues
- FK/orphan checks for handover/budget/attachment relationships
- shift rules include `AM`, `PM`, `NS`
- departments count includes source-of-truth baseline (13 active)
- budget variance mismatch count is reported
- config path policy does not retain `C:/MOAT-Handover/shared...`
- journal mode is not WAL

## Reports
Each run writes:
- `migration_YYYY-MM-DD_HH-mm-ss.json`
- `migration_YYYY-MM-DD_HH-mm-ss.txt`

Reports include:
- source/target/staging/report paths
- mode, actor, started/finished timestamps
- table row counts (source/imported/skipped/failed)
- validation issues and severities
- config transform summary
- orphan counts
- budget variance mismatches
- journal mode
- backup path (if execute promotion runs)
- target promoted true/false
- final status and next action

## Safety boundaries
- Never modifies or deletes original Access DB.
- No runtime provider switch to SQLite.
- No SQLite runtime repositories for app screens yet.
- No UI redesign.
- No backup/restore framework beyond pre-promotion backup.

## Environment limits
Cloud/Linux checks can validate build/package/static behavior only.
Real Access ACE/OLEDB runtime migration execution requires Windows workstation verification.

## Next phase
Phase 6 — backup/restore foundation.
