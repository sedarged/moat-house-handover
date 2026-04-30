# Target SQLite Schema Design

Status: Phase 1 documentation only.

This document defines the first target SQLite schema design for MOAT HOUSE HANDOVER v2.

This PR does not implement SQLite, add runtime dependencies, switch providers, or migrate data. It only documents the target schema to guide later migration PRs.

## Target database path

Per ADR-001 and the local data storage policy:

```text
M:\Moat House\MoatHouse Handover\Data\moat-house.db
```

## Runtime assumptions

- SQLite is the approved local database target.
- Access remains the legacy/current implementation until migration completes.
- No SQL Server.
- No hosted backend server.
- No cloud database.
- No SignalR/server dependency.
- Attachments remain files on disk; the database stores metadata only.
- UI design must not change as part of database/storage work.

## Shared-drive safety rule

Because `M:` may be a network/shared drive, initial SQLite runtime must not assume WAL mode.

Initial conservative settings should be documented for implementation later:

```sql
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = DELETE;
PRAGMA busy_timeout = 5000;
```

WAL may only be enabled later if runtime checks prove the database is on local non-network storage and that behaviour is explicitly approved.

## Type and naming conventions

SQLite table names intentionally stay close to existing Access names for Phase 1 and migration traceability.

General mapping:

| Concept | SQLite convention |
|---|---|
| Primary key | `INTEGER PRIMARY KEY AUTOINCREMENT` |
| Foreign key | `INTEGER NOT NULL REFERENCES ...` |
| Boolean | `INTEGER NOT NULL DEFAULT 0 CHECK(column IN (0,1))` |
| Date/time | `TEXT` ISO-8601 string |
| Long text | `TEXT` |
| Name/key text | `TEXT` plus indexes/constraints where required |
| Numeric decimal-like values | `REAL` |

## Global timestamp rule

Phase 1 target is ISO-8601 `TEXT` values.

Implementation phase must decide and enforce one convention consistently:

```text
Recommended: UTC ISO-8601 for technical timestamps, local shift date as YYYY-MM-DD for business date.
```

Business shift dates should be stored as `YYYY-MM-DD` text where possible.

Runtime timestamps should be stored as ISO-8601 datetime strings.

## Tables

### tblHandoverHeader

Purpose: one shift/date handover session header.

```sql
CREATE TABLE tblHandoverHeader (
  HandoverID INTEGER PRIMARY KEY AUTOINCREMENT,
  ShiftDate TEXT NOT NULL,
  ShiftCode TEXT NOT NULL,
  SessionStatus TEXT NOT NULL,
  CreatedAt TEXT,
  CreatedBy TEXT,
  UpdatedAt TEXT,
  UpdatedBy TEXT,
  CHECK (length(trim(ShiftCode)) > 0),
  CHECK (length(trim(SessionStatus)) > 0)
);

CREATE UNIQUE INDEX UX_tblHandoverHeader_ShiftDate_ShiftCode
ON tblHandoverHeader (ShiftDate, ShiftCode);
```

Notes:

- `ShiftDate` should be business date text, e.g. `2026-04-30`.
- `ShiftCode` remains `AM`, `PM`, or `NS`.
- Later implementation may add a lookup FK to `tblShiftRules`, but migration parity comes first.

### tblHandoverDept

Purpose: one handover department row per session.

```sql
CREATE TABLE tblHandoverDept (
  DeptRecordID INTEGER PRIMARY KEY AUTOINCREMENT,
  HandoverID INTEGER NOT NULL,
  DeptName TEXT NOT NULL,
  DeptStatus TEXT,
  DowntimeMin INTEGER,
  EfficiencyPct REAL,
  YieldPct REAL,
  DeptNotes TEXT,
  CreatedAt TEXT,
  CreatedBy TEXT,
  UpdatedAt TEXT,
  UpdatedBy TEXT,
  VersionNo INTEGER DEFAULT 1,
  IsDeleted INTEGER NOT NULL DEFAULT 0 CHECK (IsDeleted IN (0,1)),
  FOREIGN KEY (HandoverID) REFERENCES tblHandoverHeader (HandoverID) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UX_tblHandoverDept_HandoverID_DeptName
ON tblHandoverDept (HandoverID, DeptName);

CREATE INDEX IX_tblHandoverDept_HandoverID
ON tblHandoverDept (HandoverID);
```

Notes:

- Metric values must only be used for Injection, MetaPress, Berks, and Wilts.
- Non-metric departments should keep metric fields null.
- `IsDeleted` is a soft-delete flag.

### tblAttachments

Purpose: metadata for managed files. Files are not stored in the database.

```sql
CREATE TABLE tblAttachments (
  AttachmentID INTEGER PRIMARY KEY AUTOINCREMENT,
  HandoverID INTEGER NOT NULL,
  DeptRecordID INTEGER NOT NULL,
  ShiftDate TEXT,
  ShiftCode TEXT,
  DeptName TEXT,
  FilePath TEXT,
  DisplayName TEXT,
  CapturedOn TEXT,
  SequenceNo INTEGER,
  Notes TEXT,
  IsDeleted INTEGER NOT NULL DEFAULT 0 CHECK (IsDeleted IN (0,1)),
  FOREIGN KEY (HandoverID) REFERENCES tblHandoverHeader (HandoverID) ON DELETE CASCADE,
  FOREIGN KEY (DeptRecordID) REFERENCES tblHandoverDept (DeptRecordID) ON DELETE CASCADE
);

CREATE INDEX IX_tblAttachments_DeptRecordID_SequenceNo
ON tblAttachments (DeptRecordID, SequenceNo);

CREATE INDEX IX_tblAttachments_HandoverID
ON tblAttachments (HandoverID);
```

Notes:

- `FilePath` must point inside the configured managed Attachments root.
- Remove is soft-delete metadata by default.

### tblBudgetHeader

Purpose: one budget header per session.

```sql
CREATE TABLE tblBudgetHeader (
  BudgetHeaderID INTEGER PRIMARY KEY AUTOINCREMENT,
  HandoverID INTEGER NOT NULL,
  ShiftDate TEXT,
  ShiftCode TEXT,
  LinesPlanned REAL,
  TotalStaffOnRegister REAL,
  Comments TEXT,
  CreatedAt TEXT,
  CreatedBy TEXT,
  UpdatedAt TEXT,
  UpdatedBy TEXT,
  FOREIGN KEY (HandoverID) REFERENCES tblHandoverHeader (HandoverID) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UX_tblBudgetHeader_HandoverID
ON tblBudgetHeader (HandoverID);
```

Notes:

- Header meta must feed Budget View, Dashboard Budget Summary, Preview, Budget Report, and Send package where applicable.

### tblBudgetRows

Purpose: granular budget/labour area rows.

```sql
CREATE TABLE tblBudgetRows (
  BudgetRowID INTEGER PRIMARY KEY AUTOINCREMENT,
  BudgetHeaderID INTEGER NOT NULL,
  DeptName TEXT,
  PlannedQty REAL,
  UsedQty REAL,
  VarianceQty REAL,
  ReasonText TEXT,
  UpdatedAt TEXT,
  UpdatedBy TEXT,
  FOREIGN KEY (BudgetHeaderID) REFERENCES tblBudgetHeader (BudgetHeaderID) ON DELETE CASCADE
);

CREATE INDEX IX_tblBudgetRows_BudgetHeaderID
ON tblBudgetRows (BudgetHeaderID);

CREATE INDEX IX_tblBudgetRows_DeptName
ON tblBudgetRows (DeptName);
```

Important budget rule:

```text
Variance = Staff Used - Total Staff Required
```

Implementation phase should keep `VarianceQty` for migration parity and report stability, but must also be able to recalculate from `UsedQty - PlannedQty`.

### tblDepartments

Purpose: active handover department configuration and display order.

```sql
CREATE TABLE tblDepartments (
  DeptName TEXT PRIMARY KEY,
  DisplayOrder INTEGER,
  IsMetricDept INTEGER NOT NULL DEFAULT 0 CHECK (IsMetricDept IN (0,1)),
  IsClosureDept INTEGER NOT NULL DEFAULT 0 CHECK (IsClosureDept IN (0,1)),
  IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1))
);
```

Target seed should align with source-of-truth 13 handover departments:

| DisplayOrder | DeptName | IsMetricDept | IsActive |
|---:|---|---:|---:|
| 1 | Injection | 1 | 1 |
| 2 | MetaPress | 1 | 1 |
| 3 | Berks | 1 | 1 |
| 4 | Wilts | 1 | 1 |
| 5 | Racking | 0 | 1 |
| 6 | Butchery | 0 | 1 |
| 7 | Further Processing | 0 | 1 |
| 8 | Tumblers | 0 | 1 |
| 9 | Smoke Tumbler | 0 | 1 |
| 10 | Minimums & Samples | 0 | 1 |
| 11 | Goods In & Despatch | 0 | 1 |
| 12 | Dry Goods | 0 | 1 |
| 13 | Additional | 0 | 1 |

Notes:

- `IsClosureDept` is retained for migration parity but should not drive UI wording. UI wording is `Departments Completed`.

### tblShiftRules

Purpose: shift metadata and email profile mapping.

```sql
CREATE TABLE tblShiftRules (
  ShiftCode TEXT PRIMARY KEY,
  ShiftName TEXT,
  EmailProfileKey TEXT,
  DisplayOrder INTEGER
);
```

Target seed:

| ShiftCode | ShiftName | EmailProfileKey | DisplayOrder |
|---|---|---|---:|
| AM | Morning | default_am | 1 |
| PM | Afternoon | default_pm | 2 |
| NS | Night | default_ns | 3 |

### tblEmailProfiles

Purpose: local Outlook draft email template/profile metadata.

```sql
CREATE TABLE tblEmailProfiles (
  EmailProfileKey TEXT PRIMARY KEY,
  ToList TEXT,
  CcList TEXT,
  SubjectTemplate TEXT,
  BodyTemplate TEXT,
  IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1))
);
```

Notes:

- Existing placeholder `example.com` seed values must not be treated as production config.
- Actual production recipients should be controlled through admin config later.

### tblConfig

Purpose: local app config key/value store.

```sql
CREATE TABLE tblConfig (
  ConfigKey TEXT PRIMARY KEY,
  ConfigValue TEXT,
  Notes TEXT
);
```

Target config seed must use ADR-001 storage policy:

| ConfigKey | Target ConfigValue |
|---|---|
| databaseProvider | SQLite |
| dataRoot | M:\Moat House\MoatHouse Handover |
| sqliteDatabasePath | M:\Moat House\MoatHouse Handover\Data\moat-house.db |
| attachmentsRoot | M:\Moat House\MoatHouse Handover\Attachments |
| reportsOutputRoot | M:\Moat House\MoatHouse Handover\Reports |
| backupsRoot | M:\Moat House\MoatHouse Handover\Backups |
| logsRoot | M:\Moat House\MoatHouse Handover\Logs |
| importsRoot | M:\Moat House\MoatHouse Handover\Imports |
| migrationRoot | M:\Moat House\MoatHouse Handover\Migration |

Important:

- Do not use `C:\ProgramData` as the default production data root.
- Do not silently fall back to `C:` if `M:` is unavailable.
- Missing `M:` should become a clear admin diagnostics error unless a future explicit admin override is implemented.

### tblAuditLog

Purpose: best-effort audit trail.

```sql
CREATE TABLE tblAuditLog (
  AuditID INTEGER PRIMARY KEY AUTOINCREMENT,
  EventAt TEXT,
  UserName TEXT,
  EntityType TEXT,
  EntityKey TEXT,
  ActionType TEXT,
  Details TEXT
);

CREATE INDEX IX_tblAuditLog_EventAt
ON tblAuditLog (EventAt);

CREATE INDEX IX_tblAuditLog_Entity
ON tblAuditLog (EntityType, EntityKey);
```

Notes:

- Audit logging is best-effort.
- Audit failure must not block normal workflow.
- Details text must be escaped when rendered.

## Recommended migration metadata table

Add a SQLite-only table to track schema and migration state:

```sql
CREATE TABLE tblSchemaMigrations (
  MigrationId TEXT PRIMARY KEY,
  AppliedAt TEXT NOT NULL,
  AppliedBy TEXT,
  Notes TEXT
);
```

Initial migration id:

```text
001_initial_sqlite_schema
```

This table does not exist in Access and should not be imported from Access.

## Target view/query replacements

Access query patterns must be translated in implementation PRs:

| Current Access pattern | SQLite implementation pattern |
|---|---|
| `SELECT TOP 1 ...` | `SELECT ... LIMIT 1` |
| `IIF(a, b, c)` | `CASE WHEN a THEN b ELSE c END` |
| `TRUE` / `FALSE` | `1` / `0` |
| `IsDeleted = FALSE OR IsDeleted IS NULL` | `COALESCE(IsDeleted, 0) = 0` |
| OleDb `?` parameter order | Named parameters such as `$sessionId` |

## Out of scope for this PR

- No SQLite implementation.
- No package/reference changes.
- No Access importer.
- No repository interface creation.
- No runtime provider switch.
- No UI changes.
- No installer changes.
