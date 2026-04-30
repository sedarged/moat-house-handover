# Current Access Schema Inventory

Status: Phase 1 documentation only.

This inventory documents the current Access/ACE schema created by `desktop-host/src/AccessBootstrapper.cs` and used by the current OleDb repositories.

This file does not approve runtime changes. Access remains the legacy/current implementation until the phased migration is complete.

## Runtime dependency summary

Current implementation uses:

- `System.Data.OleDb`
- `Microsoft.ACE.OLEDB.12.0`
- `ADOX.Catalog` to create a missing `.accdb`
- Access SQL syntax such as `AUTOINCREMENT`, `YESNO`, `LONGTEXT`, `TEXT(n)`, `SELECT TOP 1`, `IIF(...)`, and `TRUE/FALSE`

Current primary schema owner:

- `desktop-host/src/AccessBootstrapper.cs`

Current repository consumers include:

- `desktop-host/src/SessionRepository.cs`
- `desktop-host/src/DepartmentRepository.cs`
- `desktop-host/src/AttachmentRepository.cs`
- `desktop-host/src/BudgetRepository.cs`
- `desktop-host/src/PreviewRepository.cs`

## Current tables

### tblHandoverHeader

Purpose: one shift/date handover session header.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| HandoverID | AUTOINCREMENT | Primary key | SessionId in web/bridge payloads. |
| ShiftDate | DATETIME | NOT NULL | Shift work date. |
| ShiftCode | TEXT(10) | NOT NULL | AM / PM / NS. |
| SessionStatus | TEXT(20) | NOT NULL | Usually `Open`. |
| CreatedAt | DATETIME | Nullable | Local runtime timestamp currently. |
| CreatedBy | TEXT(255) | Nullable | Windows/current runtime user. |
| UpdatedAt | DATETIME | Nullable | Local runtime timestamp currently. |
| UpdatedBy | TEXT(255) | Nullable | Windows/current runtime user. |

Indexes:

| Index | Columns | Rule |
|---|---|---|
| UX_tblHandoverHeader_ShiftDate_ShiftCode | ShiftDate, ShiftCode | UNIQUE |

Repository usage:

- `SessionRepository.OpenExistingSession`
- `SessionRepository.CreateBlankSession`
- `SessionRepository.ClearDay`
- `BudgetRepository.GetSessionContext`
- attachment insert via joined session/department lookup

### tblHandoverDept

Purpose: one department record per session and handover department.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| DeptRecordID | AUTOINCREMENT | Primary key | Department record id. |
| HandoverID | LONG | NOT NULL | FK to `tblHandoverHeader.HandoverID`. |
| DeptName | TEXT(100) | NOT NULL | Handover department name. |
| DeptStatus | TEXT(50) | Nullable | Not running / Complete / Incomplete. |
| DowntimeMin | LONG | Nullable | Metric departments only. |
| EfficiencyPct | DOUBLE | Nullable | Metric departments only. |
| YieldPct | DOUBLE | Nullable | Metric departments only. |
| DeptNotes | LONGTEXT | Nullable | User-entered text; must be escaped when rendered. |
| CreatedAt | DATETIME | Nullable | Local runtime timestamp currently. |
| CreatedBy | TEXT(255) | Nullable | Windows/current runtime user. |
| UpdatedAt | DATETIME | Nullable | Local runtime timestamp currently. |
| UpdatedBy | TEXT(255) | Nullable | Windows/current runtime user. |
| VersionNo | LONG | Nullable | Incremented on department save. |
| IsDeleted | YESNO | Nullable | Soft delete flag. |

Indexes:

| Index | Columns | Rule |
|---|---|---|
| UX_tblHandoverDept_HandoverID_DeptName | HandoverID, DeptName | UNIQUE |

Foreign keys:

| Column | References |
|---|---|
| HandoverID | tblHandoverHeader.HandoverID |

Repository usage:

- seeded automatically for active departments when a session is opened/created
- `DepartmentRepository.LoadDepartment`
- `DepartmentRepository.SaveDepartment`
- `AttachmentRepository.AddAttachmentMetadata`
- dashboard summary joins to attachments

### tblAttachments

Purpose: attachment metadata only. Physical files stay in managed folders.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| AttachmentID | AUTOINCREMENT | Primary key | Attachment metadata id. |
| HandoverID | LONG | NOT NULL | FK to session header. |
| DeptRecordID | LONG | NOT NULL | FK to department record. |
| ShiftDate | DATETIME | Nullable | Denormalized session date snapshot. |
| ShiftCode | TEXT(10) | Nullable | Denormalized shift snapshot. |
| DeptName | TEXT(100) | Nullable | Denormalized department snapshot. |
| FilePath | LONGTEXT | Nullable | Managed file path, not arbitrary path. |
| DisplayName | TEXT(255) | Nullable | Original/display file name. |
| CapturedOn | DATETIME | Nullable | Upload/capture timestamp. |
| SequenceNo | LONG | Nullable | Display order. |
| Notes | LONGTEXT | Nullable | Currently schema-owned but not heavily used. |
| IsDeleted | YESNO | Nullable | Soft delete flag. |

Indexes:

| Index | Columns | Rule |
|---|---|---|
| IX_tblAttachments_DeptRecordID_SequenceNo | DeptRecordID, SequenceNo | Non-unique |

Foreign keys:

| Column | References |
|---|---|
| HandoverID | tblHandoverHeader.HandoverID |
| DeptRecordID | tblHandoverDept.DeptRecordID |

Repository usage:

- `AttachmentRepository.ListAttachments`
- `AttachmentRepository.AddAttachmentMetadata`
- `AttachmentRepository.RemoveAttachment`
- `AttachmentRepository.GetViewerPayload`
- dashboard/department summary attachment counts

### tblBudgetHeader

Purpose: one budget header per session.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| BudgetHeaderID | AUTOINCREMENT | Primary key | Budget header id. |
| HandoverID | LONG | NOT NULL | FK to session header. |
| ShiftDate | DATETIME | Nullable | Shift work date snapshot. |
| ShiftCode | TEXT(10) | Nullable | Shift code snapshot. |
| CreatedAt | DATETIME | Nullable | Local runtime timestamp currently. |
| CreatedBy | TEXT(255) | Nullable | Windows/current runtime user. |
| UpdatedAt | DATETIME | Nullable | Local runtime timestamp currently. |
| UpdatedBy | TEXT(255) | Nullable | Windows/current runtime user. |
| LinesPlanned | DOUBLE | Added if missing | Budget header meta. |
| TotalStaffOnRegister | DOUBLE | Added if missing | Budget header meta. |
| Comments | LONGTEXT | Added if missing | User-entered budget comments. |

Indexes:

| Index | Columns | Rule |
|---|---|---|
| UX_tblBudgetHeader_HandoverID | HandoverID | UNIQUE |

Foreign keys:

| Column | References |
|---|---|
| HandoverID | tblHandoverHeader.HandoverID |

Repository usage:

- `BudgetRepository.LoadBudget`
- `BudgetRepository.SaveBudget`
- `BudgetRepository.LoadBudgetSummary`
- `PreviewRepository` budget sections
- budget report generation inputs

### tblBudgetRows

Purpose: one granular budget/labour row per budget header.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| BudgetRowID | AUTOINCREMENT | Primary key | Budget row id. |
| BudgetHeaderID | LONG | NOT NULL | FK to budget header. |
| DeptName | TEXT(100) | Nullable | Labour/budget area name. |
| PlannedQty | DOUBLE | Nullable | Budget/planned staff. |
| UsedQty | DOUBLE | Nullable | Staff used. |
| VarianceQty | DOUBLE | Nullable | Used - Planned. |
| ReasonText | LONGTEXT | Nullable | Reason/note text; must be escaped. |
| UpdatedAt | DATETIME | Nullable | Local runtime timestamp currently. |
| UpdatedBy | TEXT(255) | Nullable | Windows/current runtime user. |

Foreign keys:

| Column | References |
|---|---|
| BudgetHeaderID | tblBudgetHeader.BudgetHeaderID |

Repository usage:

- seeded from `BudgetRepository.BudgetLabourAreas`
- `BudgetRepository.LoadBudgetRows`
- `BudgetRepository.SaveBudget`
- `BudgetRepository.Recalculate`
- preview/report budget sections

Important budget rule:

```text
Variance = Staff Used - Total Staff Required
```

### tblDepartments

Purpose: active handover department configuration and display order.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| DeptName | TEXT(100) | Primary key | Department key/name. |
| DisplayOrder | LONG | Nullable | Dashboard/display ordering. |
| IsMetricDept | YESNO | Nullable | Metric departments only. |
| IsClosureDept | YESNO | Nullable | Legacy naming; current source-of-truth uses Departments Completed. |
| IsActive | YESNO | Nullable | Active handover department row source. |

Seed data currently present in `AccessBootstrapper`:

| DeptName | DisplayOrder | IsMetricDept | IsClosureDept | IsActive |
|---|---:|---:|---:|---:|
| Injection | 1 | TRUE | TRUE | TRUE |
| MetaPress | 2 | TRUE | TRUE | TRUE |
| Berks | 3 | TRUE | TRUE | TRUE |
| Wilts | 4 | TRUE | TRUE | TRUE |

Known alignment issue:

The source-of-truth handover list contains 13 departments, but the current bootstrap seed only inserts 4 department rows. Phase 1 documents this mismatch. A later implementation PR must align seed data with the source-of-truth department list without redesigning the UI.

### tblShiftRules

Purpose: shift metadata and email profile mapping.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| ShiftCode | TEXT(10) | Primary key | AM / PM / NS. |
| ShiftName | TEXT(50) | Nullable | Display name. |
| EmailProfileKey | TEXT(50) | Nullable | FK-like reference to email profile key. |
| DisplayOrder | LONG | Nullable | Shift display order. |

Seed data:

| ShiftCode | ShiftName | EmailProfileKey | DisplayOrder |
|---|---|---|---:|
| AM | Morning | default_am | 1 |
| PM | Afternoon | default_pm | 2 |
| NS | Night | default_ns | 3 |

### tblEmailProfiles

Purpose: local Outlook draft email template/profile metadata.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| EmailProfileKey | TEXT(50) | Primary key | Profile key. |
| ToList | LONGTEXT | Nullable | Semicolon/comma recipient list. |
| CcList | LONGTEXT | Nullable | CC recipient list. |
| SubjectTemplate | LONGTEXT | Nullable | Template text. |
| BodyTemplate | LONGTEXT | Nullable | Template text. |
| IsActive | YESNO | Nullable | Active profile flag. |

Seed data currently uses placeholder `example.com` recipients and must not be treated as production email configuration.

### tblConfig

Purpose: key/value runtime configuration.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| ConfigKey | TEXT(100) | Primary key | Config key. |
| ConfigValue | LONGTEXT | Nullable | Config value. |
| Notes | LONGTEXT | Nullable | Human-readable note. |

Current seed values use old `C:/MOAT-Handover/shared/...` paths:

| ConfigKey | Current ConfigValue | Notes |
|---|---|---|
| accessDatabasePath | C:/MOAT-Handover/shared/moat_handover_be.accdb | Must be superseded by ADR-001. |
| attachmentsRoot | C:/MOAT-Handover/shared/Attachments | Must move to `M:\Moat House\MoatHouse Handover\Attachments\`. |
| reportsOutputRoot | C:/MOAT-Handover/shared/Reports | Must move to `M:\Moat House\MoatHouse Handover\Reports\`. |

Known alignment issue:

The current config seed conflicts with ADR-001 live data root. Phase 2 (`M:\` AppPathService/data root service) must replace this with one authoritative path policy.

### tblAuditLog

Purpose: best-effort audit trail.

| Column | Access type | Null rule / default | Notes |
|---|---:|---|---|
| AuditID | AUTOINCREMENT | Primary key | Audit row id. |
| EventAt | DATETIME | Nullable | Event timestamp. |
| UserName | TEXT(255) | Nullable | Runtime user. |
| EntityType | TEXT(100) | Nullable | Entity kind. |
| EntityKey | TEXT(255) | Nullable | Entity identifier. |
| ActionType | TEXT(50) | Nullable | Action. |
| Details | LONGTEXT | Nullable | Details text; must be escaped when rendered. |

Indexes:

| Index | Columns | Rule |
|---|---|---|
| IX_tblAuditLog_EventAt | EventAt | Non-unique |

## Access-specific SQL behaviours to remove during migration

SQLite implementation must replace Access/OleDb-specific syntax:

| Access/OleDb behaviour | SQLite target approach |
|---|---|
| `AUTOINCREMENT` type | `INTEGER PRIMARY KEY AUTOINCREMENT` |
| `YESNO` / `TRUE` / `FALSE` | `INTEGER NOT NULL DEFAULT 0 CHECK(column IN (0,1))` |
| `LONGTEXT` | `TEXT` |
| `TEXT(n)` | `TEXT` plus validation where needed |
| `DATETIME` | `TEXT` ISO-8601 string unless a later ADR chooses another representation |
| `SELECT TOP 1` | `LIMIT 1` |
| `IIF(condition, a, b)` | `CASE WHEN condition THEN a ELSE b END` |
| OleDb positional `?` parameters | named parameters such as `$sessionId` / `@sessionId` |

## Current risks captured by this inventory

1. Access bootstrap currently depends on ACE/ADOX being installed.
2. Current config seed still points to old `C:/MOAT-Handover/shared/...` paths.
3. Current department seed only inserts 4 departments while source-of-truth requires 13 handover departments.
4. Date/time handling is inconsistent between repositories: some payload conversion uses local `DateTime.Now`; some output calls `ToUniversalTime()`; migration must define one consistent SQLite storage convention.
5. Access queries depend on Access-specific `IIF`, `TOP`, `TRUE/FALSE`, and OleDb positional parameters.

## Out of scope for this PR

- No SQLite package/reference addition.
- No runtime provider switch.
- No repository refactor.
- No data migration/importer.
- No UI or design changes.
- No installer/deployment changes.
