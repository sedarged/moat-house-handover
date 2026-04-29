# Access Backend Artifacts

This folder documents the Access backend for MOAT HOUSE HANDOVER v2.

## Schema bootstrap

The Access database schema and seed data are created automatically at application startup by the C# host (`desktop-host/src/AccessBootstrapper.cs`). No manual SQL scripts or separate tooling are required.

At startup, `AccessBootstrapper.EnsureDatabaseAndSchema` will:
1. Create the `.accdb` file if it does not exist (using ADOX.Catalog COM).
2. Create all required tables and indexes if they are missing (idempotent).
3. Insert seed data for departments, shift rules, email profiles, and config if missing (idempotent).

## Tables created

Core tables (in schema order):
- `tblHandoverHeader` — shift session records
- `tblHandoverDept` — per-department handover data
- `tblAttachments` — attachment metadata (file paths only, no binary blobs)
- `tblBudgetHeader` — budget header per session
- `tblBudgetRows` — budget row detail

Support/lookup tables:
- `tblDepartments` — active department list with metric/closure flags
- `tblShiftRules` — AM/PM/NS shift-to-email-profile mapping
- `tblEmailProfiles` — email template and recipient configuration
- `tblConfig` — runtime config values backed in database
- `tblAuditLog` — action audit trail

## Attachment and report file storage

Attachments and generated reports are **not stored as binary blobs** in the database. Only file path metadata is stored. Files are stored in managed folder structures under `attachmentsRoot` and `reportsOutputRoot` (configured in `runtime.config.json`).

## Database location

The database path is configured in `desktop-host/config/runtime.config.json` via the `accessDatabasePath` key.

## Connection string

The ACE OLEDB 12.0 provider is required:
`Provider=Microsoft.ACE.OLEDB.12.0;Data Source=<path>;Persist Security Info=False;`

The Access Database Engine must be installed on the workstation. Use Diagnostics screen to verify the connection.
