# Dual-run Access vs SQLite Verification (Phase 8)

Phase 8 adds a host-side dual-run verification harness that compares AccessLegacy repository read payloads against SQLite repository read payloads for the same session/shift inputs.

## Runtime posture
- AccessLegacy remains the active default runtime provider.
- SQLite repositories are verification-only in this phase.
- No runtime provider switch is enabled in Phase 8.

## Safety boundaries
- Dual-run verification is read-only by default.
- Mutating methods are skipped and reported (for example `CreateBlankSession`, `SaveDepartment`, `LoadBudget` when it could create missing records, `SaveBudget`, attachment add/remove operations).
- No migration execute/promotion, restore, or rollback actions are run by dual-run comparison.

## Report output
Expected production report root:

`M:\Moat House\MoatHouse Handover\Migration\DualRun\`

Each run writes:
- `dualrun_YYYY-MM-DD_HH-mm-ss.json`
- `dualrun_YYYY-MM-DD_HH-mm-ss.txt`

Reports include source/target paths, comparison scope, status counts, mismatches/warnings, and recommendation.
Reports also record the approved SQLite data root separately from the dual-run report output folder.

## Normalization behaviour
- List/array ordering is preserved by default so ordering mismatches are detectable for contract-sensitive payloads (department summaries, attachments, budget rows, preview data).
- No global collection sorting is applied.

## Diagnostics
Diagnostics now exposes dual-run readiness checks:
- `dualrun.access_db.exists`
- `dualrun.sqlite_db.exists`
- `dualrun.repositories.available`
- `dualrun.report_folder.write`
- `dualrun.runtime_default_accesslegacy`
- `dualrun.last_report.exists`
- `dualrun.ready_for_manual_verification`

## Decision rule for next phase
Any unresolved mismatches block runtime switch work. Phase 9 is runtime switch preparation with guarded toggle and rollout safety checks.

## Phase 9A follow-on
Dual-run report existence is now consumed as a runtime provider gate for SQLite opt-in selection. Missing evidence triggers AccessLegacy fallback by default.


## Phase 9B update
- Latest dual-run report is now validated for acceptance/staleness before controlled pilot readiness and SQLite runtime gate decisions.
