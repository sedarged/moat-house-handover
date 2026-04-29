# MOAT HOUSE HANDOVER v2 — Source of Truth

This file is the single business/product source of truth for MOAT HOUSE HANDOVER v2.

It replaces older fragmented plan/spec/reference files. If another document conflicts with this file, this file wins.

## Section index

1. [Product purpose](#1-product-purpose)
2. [Locked architecture](#2-locked-architecture)
3. [Global data rule](#3-global-data-rule)
4. [Roles and access](#4-roles-and-access)
5. [UI direction](#5-ui-direction)
6. [Handover department list](#6-handover-department-list)
7. [Metric vs non-metric handover departments](#7-metric-vs-non-metric-handover-departments)
8. [Departments Completed summary](#8-departments-completed-summary)
9. [Shift / Session screen](#9-shift--session-screen)
10. [Dashboard screen](#10-dashboard-screen)
11. [Department Handover View](#11-department-handover-view)
12. [Image Viewer](#12-image-viewer)
13. [Budget View — business requirement](#13-budget-view--business-requirement)
14. [Preview View](#14-preview-view)
15. [Reports](#15-reports)
16. [Send / Outlook Draft View](#16-send--outlook-draft-view)
17. [Diagnostics — admin only](#17-diagnostics--admin-only)
18. [Audit Log](#18-audit-log)
19. [Status rules](#19-status-rules)
20. [Never-do rules](#20-never-do-rules)
21. [Security / escaping](#21-security--escaping)
22. [Verification expectations](#22-verification-expectations)
23. [Screenshot evidence expectations](#23-screenshot-evidence-expectations)

## 1. Product purpose

MOAT HOUSE HANDOVER v2 is a local-first Windows desktop handover app for shift operations.

It is used to:

- open a shift/date session
- record department handover status and notes
- record production metrics for selected metric departments
- add department attachments/evidence
- record labour budget planned vs used
- preview saved handover data
- generate local handover and budget reports
- prepare an Outlook draft package
- run admin-only diagnostics/audit checks

## 2. Locked architecture

The app must remain:

- WPF desktop host
- WebView2 HTML/CSS/JS frontend
- SQLite local database target
- Access legacy/current implementation until migration completes
- local-first Windows deployment
- attachments stored as files/folders
- reports stored as files/folders
- Outlook draft-only workflow

Do not replace this with:

- SQL Server
- hosted backend server
- cloud database
- SignalR dependency
- local web server requirement
- SMTP/cloud email sending
- browser-only app
- Electron rewrite
- mobile app
- automatic email sending

Access is the current legacy runtime implementation until migration completes. Do not replace Access by hidden refactor. Access to SQLite migration must follow ADR-001 and phased PR sequence.

Primary live data root is `M:\Moat House\MoatHouse Handover\`.

Do not redesign the UI. Preserve the current modern Moat House dark/orange WebView2 design. Database/storage/deployment work must not change screen layout, colours, spacing, cards, buttons, badges, hover states, or workflow visuals unless explicitly requested.

## 3. Global data rule

Everything belongs to an active shift/date/session.

Session identity:

- SessionId
- ShiftCode: AM / PM / NS
- ShiftDate
- UserName from Windows/current runtime user

UserName is read-only. It must not be editable by operators or supervisors.

Preview, reports, dashboard summaries, and send packages must read from saved/persisted state, not unsaved UI state.

## 4. Roles and access

Normal workflow is for shift users/operators and supervisors.

Admin-only areas:

- Settings
- Diagnostics

Settings is admin-only. It is not for operators. It is not for supervisors.

Diagnostics is admin-only. It is not for operators. It is not for supervisors. It may work before a session exists, but it must not appear as a normal daily workflow action.

If admin authentication is not implemented yet, keep admin routes hidden from normal workflow and label them as admin-only/future admin-only.

## 5. UI direction

The target is a modern polished app-like interface:

- clean dark professional theme
- orange Moat House brand header
- modern panels/cards
- balanced spacing
- strong visual hierarchy
- full use of available screen height
- no huge empty bottom gaps
- hover states
- active/selected states
- subtle transitions
- practical operational density
- realistic populated screenshots where possible

Do not copy any old visual style. Legacy screens may only be used to understand business information, not as visual targets.

## 6. Handover department list

Use this exact handover department list for handover/status workflow:

1. Injection
2. MetaPress
3. Berks
4. Wilts
5. Racking
6. Butchery
7. Further Processing
8. Tumblers
9. Smoke Tumbler
10. Minimums & Samples
11. Goods In & Despatch
12. Dry Goods
13. Additional

This handover list is used in:

- Dashboard handover status
- Department View
- Preview handover sections
- Handover Report
- Departments Completed summary
- attachment counts by handover department

Do not show misleading counts such as 10 departments if the active handover list is 13.

Important distinction: Budget View may have its own more detailed labour/budget area list because staffing is more granular than handover departments.

## 7. Metric vs non-metric handover departments

Metric departments:

- Injection
- MetaPress
- Berks
- Wilts

Only these four use production metric fields:

- Downtime minutes
- Efficiency %
- Yield %

Metric departments must show/save:

- Status
- Downtime
- Efficiency %
- Yield %
- Notes
- Attachments

Non-metric departments:

- Racking
- Butchery
- Further Processing
- Tumblers
- Smoke Tumbler
- Minimums & Samples
- Goods In & Despatch
- Dry Goods
- Additional

Non-metric departments must show/save only:

- Status
- Notes
- Attachments

Non-metric departments must not show or save downtime, efficiency, or yield.

## 8. Departments Completed summary

Use the wording `Departments Completed`.

Do not use:

- Work Order Closure
- Work Order Closure totals
- Work Order Closure summary
- Work Order Closure performance summary

Departments Completed is a status summary for all 13 handover departments.

It shows:

- Complete count
- Incomplete count
- Not running count
- Total departments: 13
- optional completion ratio, for example `8 / 13 departments complete`

Departments Completed is separate from Metric Summary.

Departments Completed uses all 13 handover departments and is based only on status.

Metric Summary uses only Injection, MetaPress, Berks, and Wilts, and is based on downtime, efficiency, and yield.

Keep these labels exactly:

- Total Efficiency
- Total Yield
- Total Downtime

Do not rename these to Average Efficiency or Average Yield.

## 9. Shift / Session screen

Purpose: entry point into the app.

Must show:

- app title: MOAT HOUSE HANDOVER
- instruction text
- AM / PM / NS shift cards or selector
- date selector with today as default and past/future dates allowed
- read-only user identity from Windows/current runtime user
- Open Session action
- message/status area

Open Session behaviour:

- read ShiftCode
- read ShiftDate
- read runtime UserName
- call session.open
- if session exists, load it and navigate to Dashboard
- if no session exists, ask: `No session exists for this shift/date. Create a blank session?`
- if confirmed, call session.createBlank and navigate to Dashboard
- if cancelled, stay on Shift screen

Must not:

- allow user name editing
- create a session without confirmation when none exists
- clear/delete session from this screen
- open Dashboard without valid active session
- expose Settings/Diagnostics as normal workflow

## 10. Dashboard screen

Purpose: main operational overview for the active shift/date/session.

Must show:

- session information: ShiftCode, ShiftDate, SessionId, SessionStatus, last updated, updated by
- handover department status overview
- Departments Completed summary
- Metric Summary for Injection, MetaPress, Berks, Wilts only
- attachment summary
- budget summary
- navigation to main workflows

Department cards/rows should show:

- Department name
- Status: Not running / Complete / Incomplete
- Attachment count
- Last updated / updated by if available
- compact metrics for metric departments where useful

Normal Dashboard actions:

- Open Department
- Open Budget
- Open Preview
- Open Send
- Clear Day if required and protected with strong confirmation
- Back to Shift

Admin-only actions:

- Settings
- Diagnostics

Admin-only actions must not appear as normal operator/supervisor actions.

Clear Day:

- must show strong confirmation
- resets current shift/date session data only
- refreshes Dashboard after completion
- writes audit entry
- must not be easy to trigger accidentally

## 11. Department Handover View

Purpose: enter handover information for one selected handover department.

Header should show:

- Department name
- ShiftCode
- ShiftDate
- SessionId
- metric/non-metric indicator

Common fields:

- Status: Not running / Complete / Incomplete
- Notes
- Attachments

Metric fields only for Injection, MetaPress, Berks, Wilts:

- Downtime
- Efficiency %
- Yield %

Non-metric departments must hide these metric fields and must not save metric values.

Attachment panel must show:

- attachment list
- selected attachment metadata
- attachment count
- thumbnail/preview if supported
- clear message if preview cannot load
- Add Attachment
- Remove Attachment
- View Attachment
- Previous/Next Attachment

Attachments:

- files are copied to managed AttachmentsRoot
- Access stores metadata only
- original display name is preserved
- managed path is stored
- remove is soft-delete metadata by default
- physical delete is deferred unless approved later

## 12. Image Viewer

Purpose: show selected department attachment larger than Department View thumbnail.

Must show:

- selected attachment display name
- department name
- captured/uploaded date
- current index, for example 1 of 3
- image/file preview
- Previous
- Next
- Close/Back

Rules:

- maintain aspect ratio
- do not stretch badly
- update image and metadata on Previous/Next
- show clear message for missing/unsupported files
- only open managed files under AttachmentsRoot
- do not open arbitrary local file paths

## 13. Budget View — business requirement

Budget View is a labour budget summary screen.

Budget information references are business/data requirements, not visual design targets. Adapt the required information into the new modern app-like design.

Budget View tracks staffing:

- budget/planned staff
- staff used
- reason
- totals
- variance
- holidays/absence/agency/other reason counts
- comments

Budget is separate from production metrics:

- downtime
- efficiency
- yield
- metric summary

Budget applies to budget/labour areas, not only the 13 handover departments.

Budget View can have its own budget/labour area list because staffing is more detailed than handover sections.

Budget must load by:

- SessionId
- ShiftCode
- ShiftDate

Budget View must include:

### Header / session info

- Budget Summary title
- Shift
- Date
- Lines planned / lines count

### Main budget table

Columns:

- Department / labour area
- Budget Staff / Planned Staff
- Staff Used
- Reason / note

Budget rows must support detailed labour/budget areas such as:

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

Do not force Budget View into only the 13 handover departments if the business workflow requires these granular labour rows.

### Editable row fields

Each row must allow:

- planned / budget staff count
- staff used count
- reason or note

Reason/note must support:

- Holiday
- Absent
- Other reason
- Agency Cover
- free text such as `operative mixing brines`

### Totals row

Budget table must show:

- Total number of staff required / budget staff total
- Total number of staff used

### Summary panel

Budget View must show:

- Date
- Shift
- Lines planned
- Total staff required
- Total number of staff used
- Total staff on register
- Holiday count
- Absent count
- Other reason count
- Agency used count
- Variance

Variance = Staff Used - Total Staff Required

Examples:

- Required 41, Used 38 = -3
- Required 41, Used 41 = 0
- Required 41, Used 44 = +3

### Comments

Budget View must include a comments box / comments area for additional budget notes.

### Actions

Budget View must support:

- Refresh / Recalculate
- Edit selected row or inline editing
- Save & Close / Save Budget
- Back / Close

### Integration

Budget data must feed:

- Dashboard Budget Summary
- Preview Budget Summary
- Budget Report
- Send package where applicable

### Screenshot proof

Budget screenshot evidence must show:

- Budget Summary title
- Shift
- Date
- Lines planned / lines count
- populated budget rows
- Budget Staff / Planned Staff values
- Staff Used values
- at least one reason/note
- totals row
- summary panel
- total staff required
- total staff used
- total staff on register
- holiday count
- absent count
- other reason count
- agency used count
- variance
- comments area
- action buttons

Do not submit an empty Budget screen as proof. Do not only show Planned / Used / Variance / Status if the business workflow requires Reason, Lines, Register, Holiday, Absent, Other reason, Agency used, and Comments.

## 14. Preview View

Purpose: read-only consolidated view of saved session data before report generation/send.

Preview must load from persisted saved state, not unsaved UI state.

Must show:

- session header
- Departments Completed
- department summaries for each handover department
- metric values for metric departments only
- Total Downtime / Total Efficiency / Total Yield
- Budget Summary including required staffing totals and reason categories
- Budget Rows
- attachment summary
- generated report status where applicable

Preview is read-only. It must not edit department data, edit budget data, add/remove attachments, send email, or create Outlook draft automatically.

## 15. Reports

Reports are local files created from saved session data.

Report types:

- Handover report
- Budget report
- optional combined report if implemented

Handover report includes:

- shift/date/session header
- Departments Completed summary
- department statuses
- department notes
- metric values for Injection, MetaPress, Berks, Wilts
- Metric Summary
- attachment counts
- generated at/by

Budget report includes:

- shift/date/session header
- lines planned
- budget/labour area rows
- budget/planned staff
- staff used
- reason/notes
- total staff required
- total staff used
- total staff on register
- holiday count
- absent count
- other reason count
- agency used count
- variance
- comments
- generated at/by

Reports must use persisted saved state and escape user-entered text.

## 16. Send / Outlook Draft View

Purpose: prepare an email package and create an Outlook draft only.

It must never automatically send an email.

Must show:

- session info
- email profile key
- To recipients
- CC recipients
- subject preview
- body preview
- report attachment paths
- validation messages
- readiness status

Buttons:

- Prepare Package
- Create Outlook Draft
- Back to Preview
- Back to Dashboard

Create Outlook Draft:

- requires prepared package
- must be disabled until package is prepared and ready
- if package is not ready, do not call host draft creation
- attaches reports
- does not send
- returns success/failure
- audits result

Must not:

- send automatically
- include Send Now button
- use SMTP
- use cloud email
- hide validation errors
- claim draft success unless host confirms it

## 17. Diagnostics — admin only

Diagnostics checks whether the app can run correctly on the current machine.

Diagnostics is admin-only.

It must work before a shift session is opened, but it must not be part of normal operator/supervisor workflow.

Must show:

- Runtime Diagnostics title
- admin-only indication
- Run Diagnostics guidance
- Windows/WebView2/ACE/Outlook boundary message
- overall status: OK / Warning / Failed
- runtime paths
- diagnostic check rows
- recent audit entries where available

Must not:

- require active session
- create Outlook draft
- send email
- modify business data
- crash because one diagnostic check failed
- appear in normal operator/supervisor dashboard actions

## 18. Audit Log

Audit logging is best-effort. If audit fails, the workflow continues.

Audit fields:

- AuditId
- EventAt
- UserName
- EntityType
- EntityKey
- ActionType
- Details

Actions to audit:

- session.open
- session.createBlank
- session.clearDay
- department.save
- attachment.add
- attachment.remove
- budget.save
- reports.generateHandover
- reports.generateBudget
- reports.generateAll
- send.preparePackage
- send.createOutlookDraft result

## 19. Status rules

Department statuses:

- Not running
- Complete
- Incomplete

Default department status:

- Not running

Budget statuses:

- On target
- Over
- Under
- Not set

Diagnostics statuses:

- ok
- warning
- failed

Send package statuses:

- ready
- not ready
- warning where useful

## 20. Never-do rules

Never:

- do not use Work Order Closure wording
- do not send email automatically
- do not add Send Now
- do not use SMTP/cloud email
- do not replace Access
- do not add local web server
- do not store attachments inside database
- do not show downtime/efficiency/yield for non-metric handover departments
- do not save metrics for non-metric handover departments
- do not calculate Departments Completed from metric departments only
- do not calculate Metric Summary from non-metric departments
- do not generate reports from unsaved UI state
- do not create Outlook draft without user action
- do not create Outlook draft before package is prepared and ready
- do not claim Windows runtime is verified unless tested on real Windows
- do not hide validation failures
- do not mark failed bridge calls as success
- do not expose admin-only Settings/Diagnostics to normal operator/supervisor workflow
- do not force Budget View to only use the 13 handover departments if granular labour rows are required
- do not omit Budget fields such as lines, register, holiday, absent, other reason, agency used, variance, and comments

## 21. Security / escaping

Any user-entered or persisted text rendered through innerHTML must be escaped.

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

Prefer textContent for dynamic user data where possible.

## 22. Verification expectations

Use the strongest available checks.

Common commands:

```bash
bash scripts/check-web.sh
bash scripts/build-host.sh
bash scripts/package-local.sh
bash scripts/verify-package-assets.sh
```

If a command cannot run, mark it blocked with the exact reason.

Do not claim real Windows runtime verification unless tested in real WPF/WebView2 on Windows.

## 23. Screenshot evidence expectations

Screenshots must show realistic populated data where possible.

Required screenshots for UI redesign work:

- test-evidence/screenshots/01-shift-screen.png
- test-evidence/screenshots/02-dashboard.png
- test-evidence/screenshots/03-department-injection.png
- test-evidence/screenshots/04-department-non-metric.png
- test-evidence/screenshots/05-budget.png
- test-evidence/screenshots/06-preview.png
- test-evidence/screenshots/07-send.png
- test-evidence/screenshots/08-image-viewer.png
- test-evidence/screenshots/09-diagnostics-admin.png
- test-evidence/screenshots/10-validation-error-state.png

Screenshot rules:

- browser/dev screenshots may use safe seeded mock data
- do not use empty host-bridge-unavailable screens as main evidence
- host bridge unavailable may only be used as a specific error-state screenshot
- clearly state whether screenshots are browser/mock or real WPF/WebView2
- admin-only Diagnostics screenshot must be labelled/understood as admin-only evidence
- normal workflow screenshots must not show Settings/Diagnostics as normal operator/supervisor actions
