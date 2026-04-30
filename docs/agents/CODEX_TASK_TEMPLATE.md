# Codex Task Template

Use this template when giving Codex Web an implementation task.

The goal is to force a clear Code Mode scope, real repository edits, honest verification, and a reviewable PR.

```text
MODE: CODE MODE

Repository:
sedarged/moat-house-handover

Base branch:
main

Task / PR title:
<short title>

Phase:
<phase number and name, e.g. Phase 2 — M:\ AppPathService/data root service>

Goal:
<one paragraph describing the outcome>

Context:
- Current architecture: WPF + WebView2.
- SQLite is the approved local database target.
- Access is legacy/current runtime until phased migration completes.
- Primary live data root is M:\Moat House\MoatHouse Handover\.
- Do not replace Access by hidden refactor.
- Do not redesign UI unless this task explicitly says UI work.

Required read order:
1. AGENTS.md
2. HANDOVER_APP_V2_SOURCE_OF_TRUTH.md
3. docs/agents/CODEX_WEB_GUIDE.md
4. docs/AI_TASK_QUEUE.md
5. <phase-specific docs>
6. nearest folder-level AGENTS.md if editing inside a scoped folder

Allowed changes:
- <explicit list of allowed files/folders/types>

Forbidden changes:
- <explicit list of forbidden files/folders/types>
- no unrelated refactors
- no UI redesign unless explicitly requested
- no hidden Access-to-SQLite runtime switch
- no SQL Server/backend/cloud/SignalR

Implementation requirements:
1. Inspect the repo before editing.
2. Identify existing patterns and use them.
3. Implement the requested phase fully within scope.
4. Do not satisfy an implementation phase with metadata-only or one-line edits.
5. If the task cannot be completed safely, stop and report BLOCKED with the exact reason and a smaller proposed phase.

Acceptance criteria:
- <specific, checkable criterion>
- <specific, checkable criterion>
- <specific, checkable criterion>

Verification commands:
- <command or “docs-only, inspect changed markdown/yaml”>
- <command>

PR requirements:
- Create/update one PR from a task branch.
- PR body must match the actual diff.
- Include Summary, Files changed, What is included, What is not included, Verification, Windows runtime status, Remaining risks, Next phase.
- If PR metadata is updated, re-read it and confirm it changed.

Final response must include:
- Files inspected
- Files changed
- Verification commands and results
- PR link
- Commit SHA
- Windows runtime status
- Final status exactly one of:
  - PHASE READY FOR REVIEW
  - PHASE NOT READY — BLOCKERS REMAIN
  - NO CODE CHANGES MADE — BLOCKED
  - NO CODE CHANGES MADE — ANALYSIS ONLY
```

## Notes

- Keep tasks short enough for one PR.
- For large work, ask Codex to produce a phase plan first in Ask mode, then run one phase in Code Mode.
- Do not ask for “finish everything” unless you intentionally want a broad final pass and can review a large PR.
