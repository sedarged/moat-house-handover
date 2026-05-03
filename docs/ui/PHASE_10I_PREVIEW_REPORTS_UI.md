# Phase 10I — Preview / Reports UI

Phase 10I adds the final review screen before report generation and the future send/email review workflow.

## Purpose

Preview / Reports is a read-only consolidated review screen for the active shift handover package. It helps supervisors review saved session state before generating local report files.

It does not send emails and does not create Outlook drafts. Email review is Phase 10J.

## Routes

- `reports`
- legacy alias `preview` remains available.

Entry points:

- Handover Session → Preview / Reports
- Department Status Board → Preview
- Budget Summary → Print
- Attachments → Preview / Reports

## Screen structure

The screen includes:

- `PREVIEW / REPORTS` title
- Date / Shift / Session / Status context strip
- Report readiness cards
- Department Status preview
- Budget Summary preview
- Attachments preview
- Report actions
- Report output/status panel
- Bottom navigation actions

## Readiness cards

Readiness status labels are clamped to:

- Ready
- Needs review
- Missing
- Not available
- Future phase

The send readiness card is deliberately `Future phase` until Phase 10J adds the send/email review UI.

## Department Status preview

Department Status preview uses the handover department/status data from saved preview/session payloads.

Allowed department status labels are clamped to:

- Completed
- Incomplete
- Not updated
- Not running

This preview does not use the Shift Labour Budget department list for department status.

## Budget Summary preview

Budget preview shows compact labour budget information:

- Lines planned
- Total staff required
- Total staff used
- Variance
- Holiday
- Absent
- Agency used
- Sample budget rows when available

The Preview screen does not edit budget rows.

## Attachments preview

Attachments preview shows compact attachment evidence:

- Area
- File name
- Status
- Added at/by where available

It does not upload, remove, or open attachment files.

## Report actions

Available action buttons:

- Generate Handover Report
- Generate Budget Report
- Generate Attachment Pack / Evidence Pack
- Generate All Reports
- Open Reports Folder
- Continue to Send

Current behaviour:

- Handover/Budget/All report actions call the existing report service.
- Output shows only real file paths returned by the host/report service.
- Attachment Pack / Evidence Pack remains disabled/future.
- Continue to Send remains disabled/future until Phase 10J.

## Security / rendering

The Phase 10I screen renders dynamic values using DOM APIs such as:

- `document.createElement`
- `textContent`
- `replaceChildren`

It avoids dynamic `innerHTML` for persisted/user-controlled preview/report values.

## Not included

- Final email/send workflow.
- Outlook draft creation.
- Fake report output.
- New database schema.
- SQLite runtime provider switch.
- Screenshot commits.

## Next phase

Phase 10J — Send / Email Review UI.
