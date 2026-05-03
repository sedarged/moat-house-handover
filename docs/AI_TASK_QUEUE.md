# AI Task Queue

## Current priority
- Codex Web enablement and repository instruction alignment.

This preparation work must be merged before using Codex Web for the next implementation phase. It gives Codex one clear operating protocol, task template, environment guide, verification matrix, PR template, and issue template.

## Current enablement outputs
- Align `AGENTS.md`, `README.md`, and `BUILD_NOTES.md` with the current SQLite-target architecture.
- Add `docs/agents/CODEX_WEB_GUIDE.md`.
- Add `docs/agents/CODEX_TASK_TEMPLATE.md`.
- Add `docs/agents/CODEX_PHASE_PROTOCOL.md`.
- Add `docs/agents/VERIFICATION_MATRIX.md`.
- Add `docs/agents/CODEX_ENVIRONMENT.md`.
- Add `.github/PULL_REQUEST_TEMPLATE.md`.
- Add `.github/ISSUE_TEMPLATE/codex-task.yml`.

## Completed migration setup
- PR 22: Phase 0 — ADR/source-of-truth update for SQLite target and `M:\\` storage policy.
- PR 23: Phase 1 — Access schema inventory and SQLite target schema mapping.

## Current implementation PR
- Phase 4 — SQLite bootstrapper/schema creation (in progress in this PR).

## Next queued PR
- Phase 5 — Access-to-SQLite importer.

## Phase 2 follow-up review fixes applied
- unapproved `dataRoot` now fails validation (no warning-only behavior).
- required operational directories now verify write access using create/delete probe files.

## Phase 2 intended scope
- Create one host-side source of truth for operational paths.
- Use `M:\\Moat House\\MoatHouse Handover\\` as the primary live data root.
- Create/validate expected folders: Data, Attachments, Reports, Backups, Logs, Config, Imports, Migration.
- Add diagnostics-ready path validation.
- Do not silently fall back to `C:\\ProgramData`.
- Do not implement SQLite runtime repositories yet.
- Do not redesign UI.

## Guardrails for queued work
- Access remains legacy/current implementation until migration completes.
- Do not replace Access by hidden refactor. Access to SQLite migration must follow ADR-001 and phased PR sequence.
- Do not redesign UI while executing storage/database phases.
- No SQL Server, hosted backend server, cloud database, or SignalR dependency.

## Codex Web task rule
Use `docs/agents/CODEX_TASK_TEMPLATE.md` for future Codex Web implementation tasks.

For implementation tasks, Codex Web must use CODE MODE and must either:

1. produce a real branch/commit/PR, or
2. report `NO CODE CHANGES MADE — BLOCKED` with exact blocker.

Do not accept advice-only answers for implementation tasks unless the task explicitly says Ask/analysis only.

- Phase 5 importer/validator/reporting implementation complete in PR #29.
- Phase 6 backup/restore/rollback foundation in progress in this PR.


## Phase 6 progress
- Backup/restore/rollback foundation implemented host-side (service/model/diagnostic layer).
- Retention planning added as dry-run only (no default deletion).
- Next queued PR: Phase 7 — SQLite repository implementations.

## Phase 7 update
- Phase 7A complete: initial SQLite repository implementations (AuditLog, EmailProfile, Session, Department).
- Phase 7B complete: remaining SQLite repositories (Attachment, Budget, Preview).
- All repository interfaces now have SQLite implementations.
- AccessLegacy remains active runtime default provider.
- SQLite provider/runtime switch is not enabled yet.

## Next phase
- Phase 8 — dual-run Access vs SQLite verification (implemented) and evidence capture.
- Phase 9 — runtime default switch to SQLite only after Phase 8 verification evidence is accepted.

## Phase 9A update
- Guarded runtime provider selector and repository factory boundary implemented.
- AccessLegacy remains default provider; SQLite is explicit opt-in only with gate checks and fallback.
- Next queued: Phase 9B workstation evidence capture and controlled SQLite pilot.

- [x] Phase 9B: workstation evidence validator + controlled pilot readiness checks/docs


## Phase 9C update
- [x] Phase 9C: explicit workstation dual-run evidence runner (service + safe script + CLI/headless entry + docs).
- Next queued: Phase 10A controlled SQLite pilot run (AccessLegacy default remains unchanged until pilot decision).

## Phase 10A update
- [x] App-owned data root and first-run SQLite ownership foundation implemented.
- Next queued: Phase 10B — app-level lock/concurrency safety for shared SQLite.

## Phase 10B update
- [x] App-level lock and shared SQLite write-safety foundation.
- Next queued: Phase 10C — Main Menu + App Shell UI.


## Phase 10C update
- [x] Main Menu + App Shell UI implemented in webapp (home, nav shell, module placeholders, runtime status strip).

## Phase 10D update
- [x] AM/PM/Night placeholder routes replaced with real shift dashboard screens in the app shell.
- [x] Shift dashboards include shift-specific headers, major workflow cards, and runtime/lock/provider readiness panel.
- [x] Home shift cards route to AM/PM/Night dashboards with AM red, PM blue, NS green identity styling.
- Next queued: Phase 10E — Handover Session UI.

## Phase 10E update
- [x] Handover Session UI implemented with Create/Continue/Open entry from AM/PM/Night dashboards.
- [x] Session screen now shows shift/date/mode/status context and routes to Department Board, Budget, Attachments, Preview/Reports.
- Next queued: Phase 10F — Department Board UI.


## Phase 10F update
- [x] Department Status Board UI implemented with `departmentBoard` route and exact 13 handover areas.
- [x] Handover Session Department Board action now opens Department Status Board first.
- [x] Board uses allowed status set only: Completed, Incomplete, Not updated, Not running.
- [x] Board explicitly excludes labour allocation/support department list usage.
- Next queued: Phase 10G — Budget UI.

## Phase 10G update
- [x] Shift Labour Budget Summary UI implemented as the per-shift staff budget screen, not the 13-area handover status board.
- [x] Budget screen shows `BUDGET SUMMARY`, Date, Shift, Lines planned, Last updated, staff budget table, Summary panel, Comments panel, and bottom actions: Refresh, Edit, Save & Close, Print.
- [x] Budget rows use granular labour/staff areas such as Injection, MetaPress, Berks, Wilts, Further Processing, Brine operative, Rack cleaner / domestic, Goods In, Dry Goods, Supervisors, Admin, Cleaners, Stock controller, Training, Trolley Porter T1/T2, and Butchery.
- [x] UI fallback/dev seed is clearly separated from persisted host data; no fake production records are written by fallback rendering.
- Next queued: Phase 10H — Attachments UI.


## Phase 10H update
- [x] Attachments UI implemented with a real app-shell screen, session context strip, summary cards, handover-area filter, list/actions, add panel, and status panel.
- [x] Attachments entry points wired from Handover Session and Department Status Board.
- [x] Handover-area list uses Department Status Board areas + General Handover; budget-only labour areas are excluded from fallback filter list.
- [x] Upload/delete/preview actions remain capability-gated and honest when host wiring is unavailable.
- Next queued: Phase 10I — Preview / Reports UI.

## Phase 10I update
- [x] Preview / Reports UI implemented as the final review screen before export/send.
- [x] Preview shows session context, report readiness cards, Department Status preview, Budget Summary preview, Attachments preview, report actions, and report output/status panel.
- [x] Entry points are covered from Handover Session, Department Status Board, Budget Print, and Attachments Preview / Reports.
- [x] Report actions are honest: generated file paths are shown only when returned by the host/report service, and Continue to Send now opens Send / Email Review.
- Next queued: Phase 10K — Data Entry / Department Detail Editor UI.


## Phase 10J update
- [x] Send / Email Review UI implemented as a final confirmation screen in the app shell.
- [x] Preview / Reports Continue to Send now navigates to route `send`.
- [x] Screen includes context strip, readiness cards, recipients, subject/body preview, output list, validation panel, and navigation actions.
- [x] Send and draft actions are intentionally disabled/future where host send wiring is unavailable; UI states no email was sent.
- Next queued: Phase 10K — Data Entry / Department Detail Editor UI.


## Phase 10K update
- [x] Department Detail Editor UI implemented as a real supervisor/operator data-entry screen in the app shell route `departmentDetailEntry`.
- [x] Department Status Board `Open Department` now lands on a full editor with status selector, notes/issues/actions fields, metric fields for metric departments only, validation panel, and action bar.
- [x] Attachment summary/shortcut added, with safe fallback when attachment data is unavailable.
- [x] Save behaviour is honest: real save path when service responds; explicit no-write message when save is not wired.
- Next queued: Phase 10L — Admin / Diagnostics / Settings UI (or Phase 10K.1 Department Save Service / Data Contract Wiring if host save contract gaps remain).
