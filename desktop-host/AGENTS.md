# desktop-host AGENTS

Scope: `desktop-host/`

- Runtime host changes must preserve the current modern Moat House dark/orange WebView2 UI design and workflow visuals unless explicitly requested otherwise.
- SQLite is the approved target database. Access is legacy/current until migration completion.
- Do not replace Access by hidden refactor. Access to SQLite migration must follow `docs/decisions/ADR-001-local-sqlite-database.md` and `docs/ACCESS_TO_SQLITE_MIGRATION_PLAN.md`.
- Primary live data root is `M:\Moat House\MoatHouse Handover\`.
- No SQL Server, no hosted backend server, no cloud database, and no SignalR dependency.
