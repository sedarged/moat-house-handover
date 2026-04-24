# MOAT HOUSE HANDOVER v2 — BUILD ORDER

## Stage 1 — Foundation
- Create repository structure
- Create local desktop shell with WebView2
- Add static HTML/CSS/JS app skeleton
- Add local config loading

## Stage 2 — Access Backend
- Create split Access database structure
- Create all core and support tables
- Add indexes and integrity rules
- Seed department, shift, and config lookups

## Stage 3 — Data Access Layer
- Implement DAO connection layer
- Implement QueryDef / repository pattern
- Implement SessionRepository, DepartmentRepository, AttachmentRepository, BudgetRepository

## Stage 4 — Shift Screen
- Build shift/date UI
- Wire `openSession`
- Handle blank new-day confirmation

## Stage 5 — Dashboard Screen
- Build tile layout
- Load dashboard payload
- Show closure and budget summaries
- Wire department open action

## Stage 6 — Department Screen
- Build department editor UI
- Implement metric/non-metric visibility rules
- Save and reopen department data

## Stage 7 — Attachments
- Add file selection flow
- Copy/store files in managed folder structure
- Save attachment metadata
- Build thumb preview + full viewer + prev/next + remove

## Stage 8 — Budget
- Build budget table UI
- Implement save/reload and totals

## Stage 9 — Preview
- Build preview screen
- Render real session payload

## Stage 10 — Reports
- Generate handover and budget outputs
- Resolve output folder and file naming

## Stage 11 — Send Workflow
- Validate session completeness
- Resolve email profile
- Create draft payload / Outlook integration

## Stage 12 — Multi-user Hardening
- Add stale-data detection
- Add version checks
- Add refresh strategy
- Add audit visibility helpers

## Stage 13 — Packaging
- Local deployment packaging
- Config template
- User startup instructions
