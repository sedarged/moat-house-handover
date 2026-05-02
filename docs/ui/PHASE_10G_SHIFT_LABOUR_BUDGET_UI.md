# Phase 10G — Shift Labour Budget UI

Phase 10G adds the shift labour/staff budget summary screen.

## Purpose

The Budget screen is a per-shift labour budget view. It is not the 13-area Department Status Board. It tracks staffing requirements and usage for the selected shift/session.

## Route

- `budgetMenu`
- legacy alias `budget` remains available.

Entry points:

- Handover Session → Budget
- Department Status Board → Budget
- shell/sidebar Budget route when available

## Screen structure

The Budget screen follows the Budget Summary reference layout:

- Orange Moat House header
- `BUDGET SUMMARY` title
- Top info strip with Date, Shift, Lines planned, Last updated
- Main labour budget table
- Right Summary panel
- Comments panel
- Bottom action bar

## Table columns

- Department
- Budget Staff
- Staff Used
- Reason

## Budget labour areas

The fallback/dev seed uses the labour budget areas from the reference screen:

- Injection
- MetaPress
- Berks
- Wilts
- Further Processing
- Brine operative
- Rack cleaner / domestic
- Goods In
- Dry Goods
- Supervisors
- Admin
- Cleaners
- Stock controller
- Training
- Trolley Porter T1/T2
- Butchery

This list is intentionally different from the 13-area handover status board because labour budgeting is more granular than handover status.

## Summary panel

The Summary panel shows:

- Date
- Shift
- Lines planned
- Total staff required
- Total number of staff used
- Total staff on register
- Holiday
- Absent
- Other reason
- Agency used
- Variance

Variance is calculated as:

```text
Staff Used - Total Staff Required
```

## Fallback/dev seed rule

If the host budget service is unavailable, the screen can show UI fallback/dev seed rows so the workflow is still reviewable in browser/UI tests.

Fallback rows are not written to the database automatically and must not be described as production data.

## Actions

- Refresh: reloads budget from the host service when available.
- Edit: toggles editable input mode.
- Save & Close: validates rows and saves only when a persisted session/service is available.
- Print: routes to Preview/Reports.

## Security / rendering

New dynamic UI is rendered through DOM APIs (`document.createElement`, `textContent`, `dataset`, `replaceChildren`) instead of injecting user-controlled values through `innerHTML`.

## Not included

- Final print/export implementation.
- Final report/send workflow.
- New database schema.
- SQLite runtime provider switch.
- Screenshot commits.

## Next phase

Phase 10H — Attachments UI.
