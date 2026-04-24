# MOAT HOUSE HANDOVER v2 — MASTER SPEC

## 1. Purpose
MOAT HOUSE HANDOVER v2 is a local Windows desktop application for shift handover operations.

This version replaces VBA UserForms with an HTML/CSS/JS app-like UI while preserving the original business workflow:
- Shift selection
- Handover dashboard
- Department handover entry
- Budget summary
- Preview
- Image viewer
- Report generation
- Email draft / send workflow

## 2. Core Principles
1. Local-first.
2. No terminal or server required for end users.
3. Shared data for multiple users through a common Access backend.
4. Attachments stored as files; metadata stored in database.
5. Original business rules from VBA version remain intact.
6. Build stages must be independently testable.
7. The app must remain usable in a normal workplace environment.

## 3. Target Runtime Architecture
- UI: HTML/CSS/JS
- Desktop host: WebView2-based Windows app
- Data backend: Microsoft Access split database (.accdb)
- Data access layer: DAO + Access SQL
- Attachment/report storage: shared network or approved local folder structure
- Optional export/report helper layer: Excel templates or generated Excel files where useful

## 4. In Scope
- Shift/date selection
- Session open/create
- Dashboard with department tiles
- Department editor with metric/non-metric logic
- Attachment add/remove/navigate/view
- Budget editing and summaries
- Preview screen
- Report generation
- Email draft workflow
- Multi-user safe shared backend pattern
- Audit logging basics

## 5. Out of Scope for Initial Build
- Cloud hosting
- Browser-only deployment
- Mobile app
- Enterprise identity integration
- SQL Server migration
- OCR/AI extraction from images
- Real-time push updates

## 6. Original Business Rules Preserved
- Only Injection, MetaPress, Berks, and Wilts use:
  - Downtime
  - Efficiency
  - Yield
- Only those four contribute to Work Order Closure totals.
- Non-metric departments must hide metric fields.
- Default department status is Not running.
- New day with no existing session must start blank after confirmation.
- Clear Day resets the current shift/date session.
- Attachments belong to departments and are viewable in sequence.

## 7. User Roles
### Supervisor / Shift User
- Open shift session
- Edit department handover data
- Add/remove attachments
- Edit budget
- Preview handover
- Generate/send draft

### Manager / Reviewer
- Open existing sessions
- Review summaries and preview
- Generate reports

### Admin / Maintainer
- Maintain config, email profiles, lookup data, and deployment assets

## 8. High-Level Modules
### UI Modules
- Shift Screen
- Dashboard Screen
- Department Screen
- Budget Screen
- Preview Screen
- Image Viewer
- Send Screen

### Service Modules
- Session Service
- Dashboard Service
- Department Service
- Attachment Service
- Budget Service
- Preview Service
- Report Service
- Email Service
- Config Service
- Audit Service

### Data Layer
- Access repositories using DAO QueryDef / Recordset
- Shared DTO/view-model payloads returned to UI

## 9. Core Technical Decisions
1. WebView2 shell is the UI host.
2. Access is the source of truth for structured data.
3. Files are stored outside the database.
4. Each user gets a local front-end app.
5. Shared Access backend is stored in a shared location.
6. SQL access is encapsulated in repositories/services.
7. Build with Claude Code in staged prompts.

## 10. Deployment Model
### Shared items
- Access backend file
- Attachments root folder
- Reports output folder

### Local per-user items
- Desktop app executable/package
- Static web assets
- Local config file
- Local logs/cache if needed

## 11. Success Criteria
The build is successful when a supervisor can:
1. Open AM/PM/NS for a selected date.
2. Start a blank session when none exists.
3. Edit department states with correct metric rules.
4. Add and browse department attachments.
5. Save and reopen data reliably.
6. Edit and save budget data.
7. See a correct preview.
8. Generate report outputs.
9. Create an email draft.
10. Use the system locally without terminal steps.
