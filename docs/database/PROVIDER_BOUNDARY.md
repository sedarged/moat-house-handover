# Provider Boundary (Phase 3)

Phase 3 introduces a host-side provider/repository boundary while keeping Access/OleDb as the active runtime implementation.

## Current runtime provider
- Active provider: `AccessLegacy`.
- Active runtime database path: `M:\Moat House\MoatHouse Handover\Data\moat_handover_be.accdb` (resolved via startup path services).

## Future provider target
- Approved future provider: SQLite.
- Planned target database path: `M:\Moat House\MoatHouse Handover\Data\moat-house.db`.
- SQLite runtime repositories are **not** implemented in Phase 3.

## Boundary introduced
- `IDataProvider` + `DatabaseProviderInfo` for provider diagnostics metadata.
- Repository interfaces for current persistence boundaries:
  - `ISessionRepository`
  - `IDepartmentRepository`
  - `IAttachmentRepository`
  - `IBudgetRepository`
  - `IPreviewRepository`
  - `IAuditLogRepository`
  - `IEmailProfileRepository`
- Existing Access/OleDb repositories implement these interfaces directly.

## Non-goals in Phase 3
- No SQLite dependency/package changes.
- No SQLite bootstrap/schema creation.
- No Access-to-SQLite importer.
- No runtime provider switch.

## Next phases
- Phase 4: SQLite bootstrapper/schema creation.
- Phase 5: Access-to-SQLite importer.
