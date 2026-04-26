# Stage 3B + Stage 4A Combined Continuation

## Scope implemented in this slice

This continuation delivers the next controlled send slice:
- **Stage 3B:** email profile loading for shift-based recipient/template data from Access
- **Stage 4A:** send package preparation and Outlook draft foundation (draft only, never auto-send)

Architecture remains unchanged:
- local-first desktop host
- WPF + WebView2
- Access-oriented backend
- file-based report attachments
- no local web server, no SMTP/cloud send path

## Stage 3B implementation

### Email profile repository/service
Added:
- `desktop-host/src/EmailProfileContracts.cs`
- `desktop-host/src/EmailProfileRepository.cs`
- `desktop-host/src/EmailProfileService.cs`

Behavior:
- resolves shift rule via `tblShiftRules.ShiftCode`
- uses `tblShiftRules.EmailProfileKey` to load `tblEmailProfiles`
- returns profile payload with:
  - `toList`
  - `ccList`
  - `subjectTemplate`
  - `bodyTemplate`
  - `isActive`
  - `emailProfileKey`
  - `shiftCode`
- enforces active-only profile use for send preparation
- returns clear errors for missing/inactive mappings

Token placeholders now supported in send-package template build:
- `{ShiftCode}`
- `{ShiftDate}`
- `{SessionId}`
- `{ReportPaths}`

## Stage 4A implementation

### Send package service
Added:
- `desktop-host/src/SendContracts.cs`
- `desktop-host/src/SendPackageService.cs`

Behavior:
- prepares package for a specific session
- loads persisted preview payload as source-of-truth
- ensures handover + budget reports exist by generating/reusing session report outputs
- verifies report file paths exist on disk
- resolves active email profile for session shift
- builds final draft subject/body by applying tokens to profile templates
- validates recipients (requires at least one To/CC recipient)
- returns explicit readiness status + validation messages
- does **not** send email

Payload returned to UI includes:
- `sessionId`, `shiftCode`, `shiftDate`
- `emailProfileKey`
- `toList`, `ccList`
- `subject`, `body`
- `attachmentPaths`
- `generatedAt`, `generatedBy`
- `isReady`, `readinessStatus`, `validationMessages`

### Outlook draft boundary
Added:
- `desktop-host/src/OutlookDraftContracts.cs`
- `desktop-host/src/OutlookDraftService.cs`

Behavior:
- isolated Outlook COM logic in dedicated host service
- creates **draft only** (`MailItem.Save()`), never `Send()`
- sets To/CC/Subject/Body and attaches generated report paths
- handles non-Windows / missing Outlook COM gracefully with clear result messages
- avoids extra NuGet dependencies by using COM late binding

## Bridge actions added

`desktop-host/src/HostWebBridge.cs` now supports:
- `emailProfile.loadForShift` (optional helper)
- `send.preparePackage`
- `send.createOutlookDraft`

No send-now, SMTP, cloud, or external API actions were added.

## Send screen behavior

Updated:
- `webapp/js/screens/SendScreen.js`
- `webapp/js/services/sendService.js`
- `webapp/js/state/appState.js`

UI behavior:
- Prepare Package action loads package payload for active session
- shows recipients/profile/subject/body/attachments
- shows readiness + all validation messages
- Create Outlook Draft action requests host draft creation only
- Back to Preview and Back to Dashboard navigation
- safe rendering via DOM APIs (`createElement`, `textContent`, `value`), no unsafe interpolation of persisted text into `innerHTML`

## Navigation updates

Updated:
- `webapp/js/screens/PreviewScreen.js`
- `webapp/js/screens/DashboardScreen.js`

Flow updates:
- Dashboard includes `Open Send`
- Preview includes `Go to Send`
- Send includes return buttons to Preview/Dashboard

## Deferred after this slice

- Actual final email send action (intentionally deferred)
- Production recipient sign-off/finalization
- PDF report export
- Final Windows workstation end-to-end verification
- Final UI polish and installer/distribution polish

## Runtime verification notes

Cloud/Linux checks can validate JS and host compilation/package flow, but real runtime still requires Windows workstation verification for:
- Outlook COM draft creation behavior
- WebView2 runtime integration
- ACE/OLEDB Access runtime specifics

## Recommended next stage

Proceed with a controlled next slice focused on:
- explicit manual send confirmation workflow (if approved)
- audit logging enhancements for send/draft events
- final Windows workstation end-to-end validation and UX polish
