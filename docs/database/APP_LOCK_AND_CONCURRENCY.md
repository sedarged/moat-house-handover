# App lock and shared SQLite concurrency (Phase 10B)

Phase 10B adds conservative app-level lock safety for SQLite on shared/network-backed roots.

- Lock file path: `Data/moat-house.write.lock` under the app-owned data root.
- Default root: `M:\Moat House\MoatHouse Handover\`.
- SQLite target: `Data/moat-house.db`.

## Lock payload
Human-readable JSON with:
- machine name
- user name
- process id
- app version
- data root
- sqlite path
- lock mode
- created timestamp (UTC)
- heartbeat timestamp (UTC)

No secrets are stored in the lock file.

## Behaviour
- Lock status checks are non-destructive and do not mutate lock files.
- Acquire uses `FileMode.CreateNew` + `FileShare.None` to avoid overwriting active locks.
- Active lock owned by another process is never overwritten/deleted.
- Stale lock detection uses a conservative default heartbeat threshold of 15 minutes.
- Stale/broken locks are reported and blocked for writes pending explicit admin/user action.
- AccessLegacy mode reports lock as not required/advisory.
- SQLite writes must pass the write guard and require current-process lock ownership.

## Scope
This phase establishes host/service/diagnostic foundations only.
Full UI-driven write lock interaction lands in Phase 10C+ with app shell/menu workflows.

This safety layer complements backups/restore; it does not replace backup policy.
