# Controlled SQLite Pilot Readiness (Phase 9B)

SQLite remains explicit opt-in only.

Pilot readiness checks:
- AccessLegacy fallback available
- SQLite DB and schema ready
- latest accepted dual-run evidence exists
- backup/migration/dual-run folders writable
- gate behaviour still enforces fallback
- no automatic migration/restore/provider switch

Readiness statuses:
- `ReadyForControlledPilot`
- `NotReady`
- `Blocked`
- `EvidenceMissing`

Next phase: Phase 10 controlled pilot/default-switch decision after accepted workstation evidence.


## Phase 9C update
Before any controlled pilot run, generate fresh workstation evidence using:
`\scripts\run-workstation-dualrun.ps1 -ShiftCode PM -ShiftDate 2026-05-01`

Even after accepted evidence, keep `AccessLegacy` as default unless an explicit Phase 10A pilot action requests SQLite.

Phase 10B prerequisite: app-lock diagnostics must report SQLite write guard readiness before any shared-drive pilot writes.
