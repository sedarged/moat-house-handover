# Access to SQLite Importer (Phase 5)

Phase 5 adds importer foundations for Access-to-SQLite migration:

- Access source reader for supported tables.
- SQLite staging import writer.
- Migration validator and issue severities.
- JSON/TXT migration reporting under `M:\Moat House\MoatHouse Handover\Migration\`.
- Dry-run and execute modes.
- Safe finalization: staging DB first, promote only on successful validation.

Access remains the active runtime provider after this phase.
SQLite is import target only in this phase; runtime repositories are not switched.

Next phase: Phase 6 backup/restore foundation.
