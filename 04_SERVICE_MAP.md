# MOAT HOUSE HANDOVER v2 — SERVICE MAP

## 1. Session Service
### Responsibilities
- Open or create session by shift/date
- Return session header + dashboard seed state
- Clear day workflow

### Main Methods
- `openSession(shiftCode, shiftDate, userName)`
- `sessionExists(shiftCode, shiftDate)`
- `createBlankSession(shiftCode, shiftDate, userName)`
- `clearDay(sessionId, userName)`

## 2. Dashboard Service
### Responsibilities
- Build dashboard payload from header, department rows, budget summary, attachment counts

### Main Methods
- `loadDashboard(sessionId)`
- `getClosureSummary(sessionId)`
- `getBudgetSummary(sessionId)`

## 3. Department Service
### Responsibilities
- Load one department
- Save one department
- Apply metric/non-metric rules

### Main Methods
- `loadDepartment(sessionId, deptName)`
- `saveDepartment(deptPayload)`
- `validateDepartment(deptPayload)`

## 4. Attachment Service
### Responsibilities
- Add attachment
- Remove attachment (soft delete)
- Return ordered attachment list
- Resolve viewer navigation state

### Main Methods
- `listAttachments(deptRecordId)`
- `addAttachment(deptRecordId, sourceFilePath, userName)`
- `removeAttachment(attachmentId, userName)`
- `getAttachmentViewerPayload(deptRecordId, attachmentId)`

## 5. Budget Service
### Responsibilities
- Load budget header and rows
- Save rows
- Recalculate totals

### Main Methods
- `loadBudget(sessionId)`
- `saveBudget(sessionId, budgetRows, userName)`
- `calculateVariance(planned, used)`

## 6. Preview Service
### Responsibilities
- Assemble complete preview payload for the selected session

### Main Methods
- `buildPreviewPayload(sessionId)`

## 7. Report Service
### Responsibilities
- Generate report outputs from preview/session data
- Resolve output paths

### Main Methods
- `generateHandoverReport(sessionId)`
- `generateBudgetReport(sessionId)`
- `openOutputFolder(sessionId)`

## 8. Email Service
### Responsibilities
- Resolve email profile by shift
- Create draft payload
- Trigger local Outlook draft workflow if supported

### Main Methods
- `getEmailProfile(shiftCode)`
- `createDraft(sessionId)`

## 9. Config Service
### Responsibilities
- Read app settings
- Resolve shared roots and output paths

### Main Methods
- `getConfig(key)`
- `getAttachmentRoot()`
- `getReportRoot()`

## 10. Audit Service
### Responsibilities
- Write audit trail entries for create/update/delete/clear/draft actions

### Main Methods
- `log(actionType, entityType, entityKey, details, userName)`
