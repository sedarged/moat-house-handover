# MASTER TASK — Deep Review, UI/UX Correction, Plan Compliance, Screenshot Review, Fixes, and Verification

You are Claude Code working on this repository:

https://github.com/sedarged/moat-house-handover

Your mission is to deeply review the current work against `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`, inspect the current implementation and screenshots, identify exactly what is correct, what is wrong, what is missing, and then fix the app until it is behaviour-compliant, visually polished, app-like, and ready for proper Windows runtime testing.

This is not a small patch.
This is not a cosmetic-only task.
This is a full correction pass.

Read these files first:

1. `AGENTS.md`
2. `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`

Those two files are the active source of truth. Do not use old deleted plans or older assumptions.

## Target

The app must become a modern polished local Windows desktop/web app:

- clean dark professional theme
- orange Moat House brand header
- modern app-like desktop feel
- strong operational dashboard feel
- balanced spacing
- full use of available screen height
- no huge empty bottom gaps
- clear data hierarchy
- smooth hover states
- subtle transitions
- useful operational density
- realistic populated screens
- admin-only tools kept out of normal operator/supervisor workflow
- draft-only email workflow
- no fake success states

Do not clone any old UI.
Do not create a retro interface.
The new app must look modern and professional.

## Critical owner corrections

These rules override any older or conflicting assumptions:

- User name is read-only and comes from Windows/current runtime user.
- User name must not be editable.
- Settings is admin-only, not supervisor/operator.
- Diagnostics is admin-only, not supervisor/operator.
- Settings/Diagnostics must not appear in the normal operator workflow.
- Keep the labels `Total Efficiency`, `Total Yield`, and `Total Downtime`.
- Do not rename them to Average.
- Budget View is a labour budget summary screen and can have its own granular budget/labour area list.
- Budget View must not be forced into only the 13 handover departments.
- Budget information references are business/data requirements, not visual design targets.
- Adapt required information into the new modern app-like UI.

## Review requirements

Before editing, review:

1. What currently matches `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`
2. What currently conflicts with it
3. What is missing from the UI/workflow
4. Which screenshots are insufficient
5. What must be corrected before the work can be considered ready

Do not just check syntax. Check behaviour, workflow, information completeness, UI density, screenshot evidence, and Windows-runtime honesty.

## Main areas to verify and correct

### 1. Shift / Session

Verify and fix:

- AM / PM / NS shift selection
- date selector with today default and past/future support
- read-only runtime user identity
- open existing session
- confirm before creating blank new session
- no editable user field
- no Settings/Diagnostics in normal operator workflow

### 2. Dashboard

Verify and fix:

- 13 handover departments are represented correctly
- no misleading counts such as 10 departments when the active handover list is 13
- Departments Completed uses all 13 departments and status only
- Metric Summary uses only Injection, MetaPress, Berks, Wilts
- labels remain Total Efficiency / Total Yield / Total Downtime
- attachments and budget summaries are visible
- admin-only actions are not normal operator actions
- no huge empty layout gaps
- modern card hover/transition polish

### 3. Department Handover

Verify and fix:

- metric fields only for Injection, MetaPress, Berks, Wilts
- non-metric departments hide and do not save metrics
- status, notes, attachments save correctly
- attachment list, selected preview, metadata, add/remove/view/prev/next work correctly
- stale preview is not left behind
- attachment files are managed under AttachmentsRoot
- user/persisted text is safely escaped/rendered

### 4. Image Viewer

Verify and fix:

- large selected attachment preview
- metadata
- index/counter
- Previous/Next updates image and metadata
- missing/unsupported file states are clear
- only managed attachment files are opened
- no arbitrary file path trust

### 5. Budget View

This is a major priority.

Budget View must include the full operational labour budget information from `HANDOVER_APP_V2_SOURCE_OF_TRUTH.md`.

It must include:

- Budget Summary title
- Shift
- Date
- Lines planned / lines count
- budget/labour area rows
- Budget Staff / Planned Staff
- Staff Used
- Reason / note
- totals row
- Total staff required
- Total staff used
- Total staff on register
- Holiday count
- Absent count
- Other reason count
- Agency used count
- Variance
- Comments box/area
- Refresh/Recalculate
- Edit selected row or inline editing
- Save & Close / Save Budget
- Back / Close

Budget rows must support detailed labour/budget areas, for example:

- Injection
- MP / MetaPress
- Berks
- Wilts
- FP / Further Processing
- Brine operative
- Rack cleaner / domestic
- Goods in
- Dry Goods
- Supervisors
- Admin
- Cleaners
- Slam
- OH/MH Yard cleaner
- Stock controller
- Training
- Trolley Porter T1/T2
- Oak House
- Butchery

Do not force Budget View into only the 13 handover departments if the business workflow needs granular labour rows.

Reason/note must support:

- Holiday
- Absent
- Other reason
- Agency Cover
- free text such as `operative mixing brines`

Variance rule:

`Variance = Staff Used - Total Staff Required`

Examples:

- Required 41, Used 38 = -3
- Required 41, Used 41 = 0
- Required 41, Used 44 = +3

Budget screenshot evidence must show populated realistic data, including rows, reasons, totals, summary panel, comments, and action buttons. Do not submit an empty Budget screen as proof.

### 6. Preview

Verify and fix:

- Preview is read-only
- Preview loads from persisted saved state
- Preview does not use unsaved UI state
- includes Departments Completed
- includes Metric Summary
- includes department summaries
- includes Budget Summary with staffing totals and reason categories
- includes Budget Rows
- includes attachment summary
- has no huge blank areas when data exists

### 7. Reports

Verify and fix:

- Handover report uses saved session data
- Budget report includes the full budget information required in the source of truth
- generated paths are returned to UI
- user text is escaped
- no cloud/SMTP/local web server dependency
- no automatic sending

### 8. Send / Outlook Draft

Verify and fix:

- draft-only workflow
- no Send Now button
- Prepare Package button
- Create Outlook Draft button
- Create Outlook Draft disabled until package is prepared and ready
- if package is not ready, do not call host draft creation
- validation messages are clear
- draft success is not claimed unless host confirms it

### 9. Diagnostics — admin-only

Verify and fix:

- Diagnostics works before a session exists
- Diagnostics is admin-only
- not exposed in normal operator/supervisor workflow
- no normal Dashboard footer action
- compact warning states
- useful runtime path/check/audit panels
- no huge empty bottom gap

### 10. Security / escaping

Fix unsafe rendering.

Any user-entered or persisted text rendered through `innerHTML` must be escaped.

This includes:

- department notes
- budget reasons
- budget comments
- attachment names
- report paths
- audit details
- validation messages
- email preview data
- generated report display data

Prefer `textContent` for dynamic user data where possible.

## UI / Layout correction

Fix layout properly:

- screen uses full viewport height
- header fixed height
- infobar fixed height
- content flexes to fill remaining height
- footer fixed at bottom
- panels stretch where appropriate
- tables fill available panel height
- internal scroll inside panels/tables where needed
- no large empty bottom gaps
- no content stuck only at top
- no decorative filler to hide layout problems

Add modern app polish:

- hover states on buttons/cards/list rows
- active/selected states
- subtle transitions
- consistent border radius
- consistent spacing scale
- clear section hierarchy
- readable typography
- balanced cards/panels
- no unfinished empty panels

## Screenshot review and regeneration

Review current screenshots and regenerate after fixes.

Required screenshot files:

- `test-evidence/screenshots/01-shift-screen.png`
- `test-evidence/screenshots/02-dashboard.png`
- `test-evidence/screenshots/03-department-injection.png`
- `test-evidence/screenshots/04-department-non-metric.png`
- `test-evidence/screenshots/05-budget.png`
- `test-evidence/screenshots/06-preview.png`
- `test-evidence/screenshots/07-send.png`
- `test-evidence/screenshots/08-image-viewer.png`
- `test-evidence/screenshots/09-diagnostics-admin.png`
- `test-evidence/screenshots/10-validation-error-state.png`

Screenshot rules:

- screenshots must show realistic populated data where possible
- browser/dev screenshots may use safe seeded mock data
- do not use empty host-bridge-unavailable screens as main evidence
- host bridge unavailable may only be used as a specific error-state screenshot
- clearly state whether screenshots are browser/mock or real WPF/WebView2
- do not claim full Windows runtime verification unless actually tested in WPF/WebView2
- admin-only Diagnostics screenshot must be labelled/understood as admin-only evidence
- normal workflow screenshots must not show Settings/Diagnostics as normal operator/supervisor actions

Validation/error screenshot examples:

- invalid efficiency > 100
- negative budget/staff value
- send package not prepared
- compact host bridge unavailable warning

## Verification

Run the strongest available verification:

```bash
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

Also check GitHub Actions after push.

If a command cannot run, mark it blocked with exact reason.

Do not claim Windows runtime verification unless tested in real WPF/WebView2 on Windows.

## Required final report

Final report must include:

1. Summary
   - what was reviewed
   - what was corrected
   - current readiness state

2. Source-of-truth compliance
   - compliant areas
   - non-compliant areas fixed
   - any remaining non-compliance

3. Budget View compliance
   - required operational budget fields present
   - granular budget/labour areas supported
   - summary panel includes lines/register/holiday/absent/other/agency/variance/comments
   - screenshot is populated and not empty

4. UI/UX corrections
   - layout gaps fixed
   - modern app polish added
   - hover/transition improvements
   - screens changed

5. Admin-only compliance
   - Settings status
   - Diagnostics status
   - confirmation normal operator/supervisor workflow does not expose Settings/Diagnostics

6. User identity
   - user is read-only
   - user comes from Windows/current runtime where available

7. Screenshot evidence
   - list all screenshot paths
   - browser/mock or real WPF/WebView2
   - what each screenshot proves
   - screenshot limitations

8. Verification evidence
   - command
   - PASS / FAIL / BLOCKED
   - output summary

9. Remaining Windows-only checks
   - WebView2 runtime
   - Access/ACE
   - Outlook COM draft
   - filesystem/shared folder permissions
   - full operator workflow on Windows

10. Final state

Choose one:

- READY FOR WINDOWS RUNTIME TESTING
- READY TO MERGE
- NOT READY — BLOCKERS REMAIN

Do not choose READY TO MERGE unless:

- all non-Windows-blocked checks pass
- screenshots are regenerated
- Budget View contains all required business information
- admin-only flow is correct
- no critical/high issue remains
- no normal workflow exposes admin-only Settings/Diagnostics
- Send remains draft-only
- no big empty layout gaps remain
