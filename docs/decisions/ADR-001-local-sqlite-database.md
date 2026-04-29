# ADR-001: Approve SQLite local database target

## Status
Approved

## Date
2026-04-29

## Context
MOAT HOUSE HANDOVER v2 currently runs as a local-first Windows desktop app with a WPF host, WebView2 frontend, and Access-oriented backend implementation. This ADR approves the target architecture direction without changing runtime code in this PR.

## Decision
The approved target architecture is:

- Windows desktop app
- WPF / .NET desktop host
- WebView2 frontend
- current modern dark/orange Moat House UI preserved
- SQLite local database as the approved target database
- no SQL Server for now
- no backend server
- no cloud database
- no SignalR/server dependency
- no Electron rewrite
- no browser-only deployment

Access is explicitly treated as legacy/current implementation until the phased migration is complete.

## Storage policy reference
Primary live data root is:

`M:\Moat House\MoatHouse Handover\`

Target SQLite database path:

`M:\Moat House\MoatHouse Handover\Data\moat-house.db`

## SQLite mode caution for shared/network paths
Because the primary data root may be on `M:` and may resolve to shared/network storage, SQLite WAL mode must **not** be assumed as the default. Initial runtime defaults must use conservative SQLite settings. WAL may be enabled later only when the database path is confirmed to be local non-network storage.

## Consequences
- Migration work must be phased and explicit.
- No hidden repository/provider refactor is allowed outside planned migration PRs.
- UI design remains locked during storage/database migration work unless explicitly requested.
- No server/cloud architecture drift is allowed during migration.

## Implementation sequencing
Follow `docs/ACCESS_TO_SQLITE_MIGRATION_PLAN.md` for the mandatory phase order.
