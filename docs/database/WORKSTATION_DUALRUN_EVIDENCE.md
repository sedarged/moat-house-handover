# Workstation Dual-run Evidence (Phase 9B)

Phase 9B adds a workstation evidence validator for latest dual-run JSON output in `M:\Moat House\MoatHouse Handover\Migration\DualRun\`.

Accepted evidence requires:
- Recommendation `ReadyForRuntimeSwitchCandidate`
- Failed count `0`
- Mismatch count `0`
- Access and SQLite paths recorded
- `ActiveRuntimeProvider` = `AccessLegacy`
- report recency within policy window

If missing/unreadable/not accepted/stale, runtime selection falls back to `AccessLegacy`.
