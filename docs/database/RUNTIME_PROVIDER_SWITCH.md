# Runtime Provider Switch (Phase 9A)

Phase 9A adds a guarded runtime provider selector and repository factory boundary.

- Default runtime provider remains `AccessLegacy`.
- `SQLite` can be requested only through explicit config (`runtimeProvider`) or `MOAT_HOUSE_RUNTIME_PROVIDER` override.
- SQLite request is gated by sqlite file/schema/repository checks and dual-run evidence.
- Missing/failed gate falls back to `AccessLegacy` with runtime status + diagnostics warning.
- No automatic migration/restore/rollback is run by provider selection.

Next:
- Phase 9B: workstation dual-run evidence capture and controlled SQLite pilot.
- Phase 10: remove Access from normal runtime only after accepted evidence.


## Phase 9B update
- Added workstation dual-run evidence validation and controlled SQLite pilot readiness checks.
- AccessLegacy remains default safe provider; SQLite remains explicit opt-in with gated accepted evidence.


## Phase 9C update
A dedicated workstation evidence runner is now available (`--dualrun-evidence` and `scripts/run-workstation-dualrun.ps1`).
This does not change runtime provider selection behavior: default remains `AccessLegacy`, and SQLite remains explicit opt-in with gate/fallback.

Phase 10B adds advisory/required app-lock status reporting: lock is advisory in AccessLegacy and required for SQLite writes.
