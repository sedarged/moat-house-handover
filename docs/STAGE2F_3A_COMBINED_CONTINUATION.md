# Stage 2F + Stage 3A Combined Continuation

## Scope completed in this slice

This continuation implements a controlled combined slice:
- **Stage 2F:** real Preview from persisted/saved state
- **Stage 3A foundation:** local HTML report generation from persisted state

No Outlook draft/send flow is included in this slice.

## Stage 2F implementation details

### Preview payload approach
- Added host-side preview contracts and persisted payload assembly:
  - `desktop-host/src/PreviewContracts.cs`
  - `desktop-host/src/PreviewRepository.cs`
  - `desktop-host/src/PreviewService.cs`
- `PreviewRepository.LoadPreview(sessionId)` reads only persisted Access data and returns:
  - session header metadata
  - department summary (status, metrics, notes, updated metadata, attachment count)
  - attachment summaries by department (+ display/captured/sequence metadata)
  - budget totals summary
  - budget rows summary

### Preview bridge action
- Added bridge action:
  - `preview.load` (input: `sessionId`, output: full preview payload)
- Kept preview path read-only and did not add preview-time save/mutate behavior.

### Preview screen behavior
- Replaced placeholder `webapp/js/screens/PreviewScreen.js` with a real read-only renderer.
- Added `webapp/js/services/previewService.js` host call wiring.
- Preview screen now:
  - loads persisted payload from `preview.load`
  - renders session header, departments, notes, metrics, attachment counts, budget totals, budget rows
  - displays explicit read-only notice
  - avoids unsafe persisted text injection by using DOM APIs and `textContent`

## Stage 3A implementation details

### Report output strategy
- Added host-side report contracts/service:
  - `desktop-host/src/ReportContracts.cs`
  - `desktop-host/src/ReportService.cs`
- Report generation uses persisted preview payload data (`PreviewService`) as source-of-truth.
- Output format is local HTML files only (no PDF libs, no Office automation, no web server).

### Generated report types
- Handover report HTML
- Budget report HTML
- Combined action that generates both files in one request

### Report output paths
- Uses configured `reportsOutputRoot` from runtime config.
- Session-scoped subfolder:
  - `<reportsOutputRoot>/<ShiftCode>/<YYYY-MM-DD>/`
- Safe report names:
  - `Handover_<ShiftCode>_<YYYY-MM-DD>_Session<sessionId>.html`
  - `Budget_<ShiftCode>_<YYYY-MM-DD>_Session<sessionId>.html`

### Report bridge actions
- Added bridge actions:
  - `reports.generateHandover`
  - `reports.generateBudget`
  - `reports.generateAll`
  - `shell.openReportsFolder` (session-specific when sessionId provided)
- Existing `shell.openOutputFolder` remains supported and routes to the same folder resolver.

### HTML safety
- Report builder escapes dynamic persisted/user-entered text before writing HTML.

## Dashboard/navigation wiring
- Dashboard keeps existing summary behavior.
- Added explicit `Open Preview` action to navigate into preview.


## Validation commands executed in cloud environment
- ✅ `cd /workspace/moat-house-handover && scripts/check-web.sh`
- ⚠️ `cd /workspace/moat-house-handover && scripts/build-host.sh` initially failed because local dotnet was missing
- ✅ `cd /workspace/moat-house-handover && scripts/bootstrap-dotnet.sh`
- ✅ `cd /workspace/moat-house-handover && scripts/build-host.sh` passed after bootstrap
- ✅ `cd /workspace/moat-house-handover && scripts/package-local.sh`
- ✅ `cd /workspace/moat-house-handover && scripts/verify-package-assets.sh`

## Deferred after this slice
- Outlook draft workflow
- Email send workflow
- Recipient profile-driven send UX
- PDF export path (dependency-heavy)
- Final runtime verification on real Windows (WebView2 + ACE/OLEDB)
- Report visual polish and formatting refinement

## Recommended next stage
Proceed with the approved send workflow stage (Outlook draft flow) only after Windows environment verification for preview/report generation is completed.
