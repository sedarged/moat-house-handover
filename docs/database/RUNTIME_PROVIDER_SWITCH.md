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
