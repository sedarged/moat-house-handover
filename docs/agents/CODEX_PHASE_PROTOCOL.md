# Codex Phase Protocol

This repository uses phased PRs to keep Codex Web work reviewable and prevent fake progress.

## Core rule

```text
one task = one phase
one phase = one PR
one PR = one clear scope
```

## Phase states

Use these states in task notes and PR bodies:

| State | Meaning |
|---|---|
| Proposed | Phase is described but not started. |
| In progress | Branch/PR exists and work is ongoing. |
| Ready for review | Phase acceptance criteria are implemented and verification was run or honestly marked blocked. |
| Blocked | Work cannot proceed without a specific missing tool, permission, dependency, or decision. |
| Merged | PR was merged. |
| Superseded | Replaced by a newer phase/plan. |

## Before editing

Codex must identify:

- active branch
- base branch
- source-of-truth files read
- folder-level `AGENTS.md` files that apply
- relevant code/docs inspected
- exact files likely to change
- acceptance criteria
- verification commands
- known environment limits

## During editing

Codex must:

- stay inside phase scope
- keep PR diff reviewable
- use existing repo patterns
- update docs when behaviour changes
- avoid unrelated refactors
- avoid UI redesign unless explicitly requested
- avoid hidden Access-to-SQLite runtime switching
- avoid hardcoded demo values as production defaults

## If scope is too large

Do not create a tiny fake patch.

Report:

```text
PHASE NOT READY — BLOCKERS REMAIN

Inspected:
- ...

Reason this phase is too large/blocked:
- ...

Proposed smaller phase:
- ...
```

## PR body requirements

Every phase PR body must contain:

```markdown
## Summary

## Files changed

## What is included

## What is NOT included

## Verification

## Windows runtime status

## Remaining risks

## Next phase
```

The PR body must match the diff. If implementation changed contracts/services/schema/docs, the PR body must say so.

## Final task status

End with exactly one of:

- `PHASE READY FOR REVIEW`
- `PHASE NOT READY — BLOCKERS REMAIN`
- `NO CODE CHANGES MADE — BLOCKED`
- `NO CODE CHANGES MADE — ANALYSIS ONLY`

## Current planned migration phases

From `docs/ACCESS_TO_SQLITE_MIGRATION_PLAN.md`:

1. Phase 0 — ADR/source-of-truth update
2. Phase 1 — Access schema inventory and SQLite target schema mapping
3. Phase 2 — `M:\` AppPathService/data root service
4. Phase 3 — database provider boundary/repository interfaces
5. Phase 4 — SQLite bootstrapper/schema creation
6. Phase 5 — Access-to-SQLite importer
7. Phase 6 — backup/restore foundation
8. Phase 7 — SQLite repository implementations
9. Phase 8 — dual-run Access vs SQLite verification
10. Phase 9 — switch runtime default to SQLite
11. Phase 10 — remove Access/ACE from normal runtime
12. Phase 11 — installer/updater integration
13. Phase 12 — real Windows workstation UAT

Do not skip phases without an explicit source-of-truth/ADR update.
