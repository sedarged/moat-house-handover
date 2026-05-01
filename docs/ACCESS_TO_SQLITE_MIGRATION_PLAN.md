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
- **Phase 1 — Access schema inventory and SQLite target schema mapping** — complete in PR #23.
- **Phase 2 — M:\ AppPathService/data root service** — complete in PR #25.
- **Phase 3 — database provider boundary/repository interfaces** — complete in PR #27.
- **Phase 4 — SQLite bootstrapper/schema creation**
- **Phase 5 — Access-to-SQLite importer**
- **Phase 6 — backup/restore foundation**
- **Phase 7 — SQLite repository implementations** — completed in two PR phases: 7A (AuditLog/EmailProfile/Session/Department) and 7B (Attachment/Budget/Preview).
- **Phase 8 — dual-run Access vs SQLite verification** (next).
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

## Phase 5 update
Importer, validator, report writer, and staging promotion logic added. Access remains active runtime provider.

## Phase 6 update
Backup/restore/rollback host-side safety foundation added, including manifest/checksum validation, staged restore planning, pre-restore backup enforcement, rollback readiness checks, and retention dry-run planning. Access remains active runtime provider.


## Phase 7A update
SQLite repository infrastructure and first repositories (AuditLog, EmailProfile, Session, Department) implemented while AccessLegacy remains default runtime.
Phase 7B will add Attachment, Budget, and Preview SQLite repositories.


## Phase 8 update
Dual-run Access vs SQLite verification harness added for read-only parity checks across repository outputs, with diagnostics readiness checks and dual-run reports under Migration/DualRun. AccessLegacy remains active runtime provider.

## Phase 9 next
Prepare guarded runtime provider switch controls with explicit fallback safety and mismatch gate enforcement.

## Phase 9A update
Phase 9A introduces guarded provider selection and repository factory wiring. AccessLegacy remains default runtime. SQLite opt-in is gated and falls back safely when checks/evidence are incomplete.


## Phase 9B update
- Added workstation evidence capture + controlled pilot readiness layer. Phase 10 is controlled pilot/default-switch only after accepted evidence.


## Phase 9C update
Phase 9C adds explicit workstation dual-run evidence execution/validation tooling (runner service + script/CLI).
No runtime default switch occurs in Phase 9C.
Next phase remains Phase 10A controlled SQLite pilot run after accepted workstation evidence.
