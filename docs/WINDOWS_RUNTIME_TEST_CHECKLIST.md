# Windows Runtime Test Checklist

Use this checklist on a real Windows workstation with WebView2 runtime, Outlook desktop, and Access Database Engine behavior available.

## Runtime launch and bootstrap
- [ ] Launch desktop host executable on Windows.
- [ ] Confirm WebView2 loads UI (Shift screen visible).
- [ ] Confirm Access database is created/opened at configured path.

## Core shift/session flow
- [ ] Open AM session for a test date.
- [ ] Open PM session for a test date.
- [ ] Open NS session for a test date.
- [ ] Confirm blank-day create path works when session does not exist.

## Department flow
- [ ] Save department updates and reopen to confirm persistence.
- [ ] Verify metric dept fields show for Injection/MetaPress/Berks/Wilts.
- [ ] Verify non-metric dept hides metric fields.

## Attachment flow
- [ ] Add attachments in department screen.
- [ ] Remove an attachment and confirm list refresh/persistence.
- [ ] Open viewer and verify prev/next navigation and metadata.

## Budget flow
- [ ] Save budget rows and reopen.
- [ ] Verify budget totals/variance recalc and dashboard summary alignment.

## Preview + report flow
- [ ] Open preview and confirm saved session state is rendered.
- [ ] Generate handover report to reports folder.
- [ ] Generate budget report to reports folder.
- [ ] Generate both reports action and confirm files exist.

## Send workflow (draft-only boundary)
- [ ] Prepare send package.
- [ ] Confirm profile/recipients/validation messages are accurate.
- [ ] Create Outlook draft and confirm attachments are included.
- [ ] Confirm no automatic email send occurs.

## Audit + diagnostics flow
- [ ] Run Diagnostics screen checks.
- [ ] Confirm diagnostics status is all green or warnings are explained.
- [ ] Refresh recent audit list and confirm key actions appear.
- [ ] Verify audit entries exist for clear day, department save, attachment add/remove, budget save, report generation, send prepare, and draft result.

## Regression confidence checks
- [ ] Dashboard still loads department + budget + attachment summary correctly.
- [ ] Department save/load still functions.
- [ ] Attachment list/add/remove/viewer still functions.
- [ ] Budget load/save/recalculate still functions.
- [ ] Preview loads.
- [ ] Report generation works.
- [ ] Send prepare + draft-only behavior works.
