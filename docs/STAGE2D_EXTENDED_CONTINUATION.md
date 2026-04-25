# Stage 2D Extended Continuation — Attachments End-to-End

## What Stage 2D Extended implemented

### Host-side attachment repository/service
- Added a concrete `AttachmentRepository` for Access-backed metadata persistence and retrieval:
  - list active attachments per `(sessionId, deptRecordId)`
  - add one metadata row with managed `SequenceNo`
  - soft-delete one metadata row (`IsDeleted = TRUE`)
  - return refreshed list payload including attachment count
  - return refreshed dashboard department summaries including per-department attachment counts
- Added `AttachmentService` to enforce request validation and coordinate host file operations with repository metadata writes.

### Host file operations
- Added `FileDialogService` with `OpenFileDialog` for single-file local selection.
- Added managed file copy flow in `AttachmentService.addAttachment`:
  - validates source file exists
  - creates managed attachment folder under configured `attachmentsRoot`
  - writes file using safe generated file name
  - persists original display name separately from stored physical path
- Stage 2D Extended deletion approach:
  - attachment metadata is soft-deleted in Access
  - physical file is retained on disk for safety and auditability in this stage
  - physical cleanup policy is deferred

### Bridge actions implemented for this stage
- `file.pickFile`
- `attachment.list`
- `attachment.add`
- `attachment.remove`
- `attachment.openViewer`

No unrelated bridge actions were added.

### Department screen attachment panel
- Department route now loads and renders persisted attachment list for selected department.
- Supports add attachment (host picker + host copy + metadata save).
- Supports remove attachment (soft-delete metadata).
- Shows selected attachment metadata and attachment count.
- Refreshes list/count from real payloads after add/remove.

### Viewer flow
- Viewer route now loads selected attachment through `attachment.openViewer` payload.
- Displays selected attachment metadata and file-backed image source.
- Supports previous/next navigation through persisted ordered department list.

### Dashboard updates
- Dashboard department rows now display real `attachmentCount` values from session payload.
- Counts update after attachment add/remove via refreshed department summary payload.

## Deferred after Stage 2D Extended
- Budget persistence work remains deferred.
- Preview/report/send workflows remain deferred.
- Physical file purge/archive strategy for deleted attachment metadata remains deferred.
- Windows runtime verification for WebView2 image rendering and Access ACE/OLEDB behavior remains required.

## Next stage recommendation
- Continue with Stage 3/next approved slice focused on budget persistence and summary integration.
- Keep attachment/report/send scope unchanged until explicitly scheduled.
