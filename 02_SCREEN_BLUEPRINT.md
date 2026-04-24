# MOAT HOUSE HANDOVER v2 — SCREEN BLUEPRINT

## 1. Shift Screen
### Purpose
Entry point for choosing shift and date and opening or creating a handover session.

### Main Areas
- App title / branding
- Shift cards: AM / PM / NS
- Date picker
- Open button
- Recent sessions list
- Optional quick actions: Open latest, Reopen previous

### Fields
- ShiftCode
- ShiftDate

### Actions
- Open selected shift/date
- Load existing session if present
- Prompt for blank new day if no session exists

### On Submit
- Calls Session Service: `openSession(shiftCode, shiftDate, currentUser)`
- Returns dashboard payload

---

## 2. Dashboard Screen
### Purpose
Main operational overview for the selected shift/date.

### Main Areas
- Header bar:
  - Shift
  - Date
  - Last updated
  - User
- Department tile grid
- Work Order Closure panel
- Budget summary panel
- Attachments summary
- Bottom action bar

### Tile Content
- Department name
- Status
- Short note preview
- Attachment count
- Completion badge
- Last updated

### Main Actions
- Open department
- Open budget
- Open preview
- Generate reports
- Create/send draft
- Clear day
- Back to shift selection

### On Load
- Calls Dashboard Service: `loadDashboard(sessionId)`

### On Tile Click
- Calls Department Service: `loadDepartment(sessionId, deptName)`

---

## 3. Department Screen
### Purpose
Edit one department handover entry.

### Form Layout
- Header:
  - Department name
  - Shift/date
- Main form area
- Attachment side strip or lower panel
- Thumbnail preview area
- Footer action bar

### Fields
- DeptStatus
- DowntimeMin (metric departments only)
- EfficiencyPct (metric departments only)
- YieldPct (metric departments only)
- DeptNotes

### Attachment Area
- Attachment list
- Add attachment
- Remove attachment
- Previous/Next attachment
- View full
- Thumbnail preview
- Attachment meta text

### Actions
- Save department
- Cancel/close
- Add/remove attachment
- Prev/Next attachment
- View full attachment

### Rules
- Non-metric departments hide downtime/efficiency/yield fields.
- Default new department state is Not running.
- Attachment list is scoped to current department/session.

### On Save
- Calls Department Service: `saveDepartment(deptPayload)`

---

## 4. Budget Screen
### Purpose
Edit and review shift budget rows.

### Main Areas
- Header with shift/date
- Budget table
- Totals summary
- Save bar

### Columns
- Department
- Planned
- Used
- Variance
- Reason

### Actions
- Save budget
- Recalculate totals
- Return to dashboard

### On Save
- Calls Budget Service: `saveBudget(sessionId, rows)`

---

## 5. Preview Screen
### Purpose
Show the complete handover in a readable review format.

### Main Areas
- Session summary
- Department summaries
- Closure summary
- Budget summary
- Attachment references
- Report actions

### Actions
- Generate handover report
- Generate budget report
- Open output folder
- Go to send screen

---

## 6. Image Viewer
### Purpose
Full-size image viewing with navigation.

### Main Areas
- Large image display area
- Metadata line
- Prev / Next controls
- Close button

### Behavior
- Opens at current attachment
- Supports next/previous within current department/session
- Displays original/high-quality file path source

---

## 7. Send Screen
### Purpose
Final validation and email draft generation.

### Main Areas
- Validation checklist
- Output files list
- Email profile preview
- Draft/send buttons

### Actions
- Create email draft
- Open generated files
- Re-run validation

### On Draft Creation
- Calls Email Service: `createDraft(sessionId)`
