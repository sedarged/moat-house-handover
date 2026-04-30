# Access to SQLite Migration Plan

This plan defines the controlled phased migration from the current Access-based runtime implementation to the approved SQLite target.

## Guardrails
- Migration must follow ADR-001.
- Do not replace Access by hidden refactor.
- Do not redesign UI during migration phases.
- No SQL Server, hosted backend server, cloud database, or SignalR dependency is introduced.
- Primary live data root remains `M:\Moat House\MoatHouse Handover\`.
- SQLite WAL mode must not be assumed for the `M:` path because it may be shared/network-backed.

## Phased sequence
- **Phase 0 — ADR/source-of-truth update** — complete in PR #22.
- **Phase 1 — Access schema inventory and SQLite target schema mapping** — this PR.
- **Phase 2 — M:\ AppPathService/data root service**
- **Phase 3 — database provider boundary/repository interfaces**
- **Phase 4 — SQLite bootstrapper/schema creation**
- **Phase 5 — Access-to-SQLite importer**
- **Phase 6 — backup/restore foundation**
- **Phase 7 — SQLite repository implementations**
- **Phase 8 — dual-run Access vs SQLite verification**
- **Phase 9 — switch runtime default to SQLite**
- **Phase 10 — remove Access/ACE from normal runtime**
- **Phase 11 — installer/updater integration**
- **Phase 12 — real Windows workstation UAT**

## Phase 1 outputs

Phase 1 is documentation/schema mapping work only.

It creates:

- `docs/database/CURRENT_ACCESS_SCHEMA.md`
- `docs/database/TARGET_SQLITE_SCHEMA.md`
- `docs/database/SCHEMA_MAPPING_ACCESS_TO_SQLITE.md`

Phase 1 records:

- current Access tables, columns, indexes, seed values, and repository usage
- target SQLite table definitions and conservative shared-drive settings
- Access-to-SQLite type mapping
- query syntax mapping
- config path transformation from old `C:/MOAT-Handover/shared/...` values to ADR-001 `M:\Moat House\MoatHouse Handover\...` values
- known alignment issues to fix later, especially current 4-department seed versus 13-department source-of-truth list

## Next PR

The next PR after Phase 1 is **Phase 2: M:\ AppPathService/data root service**.

Phase 2 should introduce a single host-side source of truth for operational paths, diagnostics for the `M:` storage root, and no silent fallback to `C:\ProgramData`.

Phase 2 must not implement SQLite runtime repositories yet and must not redesign the UI.
