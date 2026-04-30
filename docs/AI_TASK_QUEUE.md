# AI Task Queue

## Current priority
- PR 23 (this PR): Phase 1 — Access schema inventory and SQLite target schema mapping.

## Phase 1 outputs
- `docs/database/CURRENT_ACCESS_SCHEMA.md`
- `docs/database/TARGET_SQLITE_SCHEMA.md`
- `docs/database/SCHEMA_MAPPING_ACCESS_TO_SQLITE.md`
- Update `docs/ACCESS_TO_SQLITE_MIGRATION_PLAN.md` to mark Phase 1 outputs and next PR.

## Next queued PR
- Phase 2 — `M:\` AppPathService/data root service.

## Phase 2 intended scope
- Create one host-side source of truth for operational paths.
- Use `M:\Moat House\MoatHouse Handover\` as the primary live data root.
- Create/validate expected folders: Data, Attachments, Reports, Backups, Logs, Config, Imports, Migration.
- Add diagnostics-ready path validation.
- Do not silently fall back to `C:\ProgramData`.
- Do not implement SQLite runtime repositories yet.
- Do not redesign UI.

## Guardrails for queued work
- Access remains legacy/current implementation until migration completes.
- Do not replace Access by hidden refactor. Access to SQLite migration must follow ADR-001 and phased PR sequence.
- Do not redesign UI while executing storage/database phases.
- No SQL Server, hosted backend server, cloud database, or SignalR dependency.
