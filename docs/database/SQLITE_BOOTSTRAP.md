# SQLite Bootstrap Foundation (Phase 4)

## Scope
Phase 4 adds a SQLite target bootstrap foundation only.

- Creates/opens target DB at `M:\Moat House\MoatHouse Handover\Data\moat-house.db`.
- Applies conservative shared-drive-safe pragmas:
  - `PRAGMA foreign_keys = ON;`
  - `PRAGMA journal_mode = DELETE;`
  - `PRAGMA busy_timeout = 5000;`
- Creates target schema tables and indexes from `docs/database/TARGET_SQLITE_SCHEMA.md`.
- Creates `tblSchemaMigrations` with marker `001_initial_sqlite_schema`.
- Seeds required readiness data: `tblDepartments` (13 handover departments), `tblShiftRules`, and `tblConfig` path keys.
- Adds diagnostics checks for SQLite target readiness.

## Explicit non-goals
- Access remains active runtime provider.
- No runtime provider switch to SQLite.
- No SQLite runtime repositories.
- No Access-to-SQLite importer in this phase.
- No backup/restore foundation.
- No UI redesign.

## Idempotency and safety
Bootstrap uses `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`, and `INSERT OR IGNORE` for seed/migration markers so repeated runs are non-destructive.

## Runtime behaviour
Startup runs Access bootstrap as active runtime setup, then runs SQLite bootstrap as readiness-only setup. If SQLite bootstrap fails, runtime provider still remains `AccessLegacy`; diagnostics reports the SQLite failure details for admin follow-up.

## Next phase
Phase 5 — Access-to-SQLite importer.
