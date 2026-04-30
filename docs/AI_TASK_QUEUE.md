# AI Task Queue

## Current priority
- PR 22 (this PR): ADR/source-of-truth approval for SQLite target + M: storage policy (docs only).

## Next queued implementation PR
- Phase 1 — Access schema inventory and SQLite target schema mapping.

## Guardrails for queued work
- Access remains legacy/current implementation until migration completes.
- Do not replace Access by hidden refactor. Access to SQLite migration must follow ADR-001 and phased PR sequence.
- Do not redesign UI while executing storage/database phases.
- No SQL Server, hosted backend server, cloud database, or SignalR dependency.
