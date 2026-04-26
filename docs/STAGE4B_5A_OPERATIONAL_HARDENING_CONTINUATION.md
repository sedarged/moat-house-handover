# Stage 4B + 5A Combined Continuation — Audit Logging, Runtime Diagnostics, Operational Hardening

## Scope implemented in this slice

This continuation delivers a controlled operational-readiness slice:
- **Stage 4B:** host-side audit logging and audit visibility support
- **Stage 5A:** runtime diagnostics checks + diagnostics UI + send/report hardening messages

Architecture remains unchanged:
- local-first Windows desktop host (WPF + WebView2)
- HTML/CSS/JS UI
- Access-oriented backend
- file-based attachments/reports
- Outlook draft-only boundary (no automatic send)

## Audit logging approach

### New host audit components
Added:
- `desktop-host/src/AuditLogContracts.cs`
- `desktop-host/src/AuditLogRepository.cs`
- `desktop-host/src/AuditLogService.cs`

Behavior:
- Writes to existing `tblAuditLog` only (no new database)
- Best-effort/non-blocking writes (`BestEffortLog`) so workflow never crashes on audit failure
- Uses concise JSON detail payloads and truncates long detail text to avoid oversized/sensitive entries

### Actions logged in this slice
- `session.open`
- `session.createBlank`
- `session.clearDay`
- `department.save`
- `attachment.add`
- `attachment.remove`
- `budget.save`
- `reports.generateHandover`
- `reports.generateBudget`
- `reports.generateAll`
- `send.preparePackage`
- `send.createOutlookDraft result`

## Runtime diagnostics backend

### New diagnostics components
Added:
- `desktop-host/src/DiagnosticsContracts.cs`
- `desktop-host/src/DiagnosticsService.cs`

### Diagnostics checks included
- OS info + Windows/non-Windows status
- Runtime config values loaded
- Access DB path exists
- Access connection open
- `tblConfig` required key presence (`accessDatabasePath`, `attachmentsRoot`, `reportsOutputRoot`)
- Attachments root exists/can be created
- Reports root exists/can be created
- Attachments root write access
- Reports root write access
- Email profile mapping exists for AM/PM/NS
- Active email profile validity for AM/PM/NS (active + recipient sanity)
- Outlook COM availability (type/progID check only; no draft creation)

### Diagnostics payload
Diagnostics returns:
- `overallStatus` (`ok`, `warning`, `failed`)
- `checkedAt`
- check list entries with:
  - `checkName`
  - `status`
  - `message`
  - `details`

## Bridge actions added

Added in `HostWebBridge`:
- `diagnostics.run`
- `audit.listRecent`
- `audit.listForSession`

Bridge behavior remains structured:
- request-specific payload contracts
- structured errors when possible
- no raw stack trace passthrough to UI

## Diagnostics screen behavior

Added web UI:
- `webapp/js/screens/DiagnosticsScreen.js`
- `webapp/js/services/diagnosticsService.js`
- `webapp/js/services/auditService.js`

Navigation access:
- top nav Diagnostics route
- Shift screen quick Diagnostics button
- Dashboard Diagnostics button

Screen features:
- Run Diagnostics button
- overall status + checked timestamp
- per-check rows with status/message/details
- explicit status lines for DB path, attachments root, reports root, email profile checks, Outlook availability
- recent audit list display
- refresh audit button
- back to dashboard/shift actions

Rendering safety:
- DOM APIs only (`createElement`, `textContent`, `value`)
- no unsafe interpolation of persisted/user-entered text into `innerHTML`

## Send/report operational hardening changes

- Report file generation now throws clearer folder/file permission/path errors.
- Send package validation now shows clearer messages for:
  - missing/inactive profile
  - missing report files
  - no generated attachments
- Send screen draft status messaging now explicitly differentiates success vs failure and retains draft-only boundary.

## Outlook limitations and Windows verification

Outlook diagnostics check only validates COM type availability and does **not** create a draft.

Real verification still requires Windows runtime for:
- WebView2 behavior
- ACE/OLEDB behavior
- Outlook COM draft flow

## Deferred after this slice

- Automatic send action (still intentionally deferred)
- SMTP/cloud/external send paths
- admin-side audit editing/filtering UI beyond recent/session listing
- broader multi-user hardening stages beyond this slice

## Recommended next stage

Proceed with the approved next controlled slice for:
- multi-user stale-data/version conflict hardening
- expanded admin diagnostics visibility/filtering as approved
- full Windows workstation end-to-end validation and issue fix pass
