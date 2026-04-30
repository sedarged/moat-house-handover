# Access to SQLite Migration Plan

This plan defines the controlled phased migration from the current Access-based runtime implementation to the approved SQLite target.

## Guardrails
- Migration must follow ADR-001.
- Do not replace Access by hidden refactor.
- Do not redesign UI during migration phases.
- No SQL Server, hosted backend server, cloud database, or SignalR dependency is introduced.

## Phased sequence
- **Phase 0 — ADR/source-of-truth update**
- **Phase 1 — Access schema inventory and SQLite target schema mapping**
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

## Next PR
The next PR after this ADR/doc PR is **Phase 1: Access schema inventory and SQLite target schema mapping**.

This next PR is documentation/design mapping work and is not SQLite runtime implementation.
