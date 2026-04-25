# Stage 2C Continuation Notes

## Implemented in Stage 2C

### Real Department repository/service persistence
- Added `DepartmentRepository` with Access SQL encapsulation for:
  - loading one department by `(sessionId, deptName)`
  - saving one department row
  - returning the saved department payload
  - reloading dashboard department summaries after save
- Added `DepartmentService` for input normalization and business-rule-safe validation before repository calls.

### Department payload contract (minimal for this stage)
- `deptRecordId`
- `sessionId`
- `deptName`
- `deptStatus`
- `deptNotes`
- `downtimeMin`
- `efficiencyPct`
- `yieldPct`
- `updatedAt`
- `updatedBy`
- `isMetricDept`

### Business-rule enforcement in this stage
- Metric departments are fixed to: `Injection`, `MetaPress`, `Berks`, `Wilts`.
- Only those four keep persisted values for `downtimeMin`, `efficiencyPct`, and `yieldPct`.
- Non-metric save logic forces those three fields to `NULL`.
- Department status remains compatible with existing direction by defaulting to `Not running` when empty.

### Host bridge support (department flow only)
- Added bridge message handlers for:
  - `department.load`
  - `department.save`
- Kept repository/service boundaries clean:
  - bridge delegates to `DepartmentService`
  - data access stays in `DepartmentRepository`

### Web app department flow wiring
- Replaced department service stubs with real host bridge calls for load/save.
- Added form-level validation and user-visible save/load error messages.
- Added real Department screen behavior:
  - choose department
  - load persisted values
  - edit status/notes
  - edit downtime/efficiency/yield only for metric departments
  - save and optionally return to dashboard

### Dashboard minimal improvement for persistence proof
- Dashboard department summary now shows:
  - department name
  - department status
  - updatedAt / updatedBy
- Department save returns refreshed summary list, and UI updates dashboard state from that persisted payload.

## Deferred (still out of scope after Stage 2C)
- Attachment persistence and viewer state wiring
- Budget persistence and summary calculations
- Preview/report/send flows
- File picker bridge action and attachment file operations
- Cross-user stale-write conflict UX (`VersionNo` concurrency path)

## Stage 2D should do next
1. Implement attachment repository/service with file copy/remove + metadata persistence.
2. Add bridge actions for attachment list/add/remove + viewer payloads.
3. Wire Department screen attachments panel to real host-backed attachment flows.
4. Expand integration checks for Stage 2C + Stage 2D combined behavior.
