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
- PR 22: Phase 0 — ADR/source-of-truth update for SQLite target and `M:\` storage policy.
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
- Use `M:\Moat House\MoatHouse Handover\` as the primary live data root.
- Create/validate expected folders: Data, Attachments, Reports, Backups, Logs, Config, Imports, Migration.
- Add diagnostics-ready path validation.
- Do not silently fall back to `C:\ProgramData`.
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


## Phase 7A update
SQLite repository infrastructure and first repositories (AuditLog, EmailProfile, Session, Department) implemented while AccessLegacy remains default runtime.
Phase 7B will add Attachment, Budget, and Preview SQLite repositories.
