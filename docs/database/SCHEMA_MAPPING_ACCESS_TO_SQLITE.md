# Access to SQLite Schema Mapping

Status: Phase 1 documentation only.

This document maps the current Access/ACE schema and behaviour to the approved SQLite target schema.

No runtime code is changed by this PR.

## Source and target

Source/current implementation:

```text
Access `.accdb`
Microsoft.ACE.OLEDB.12.0
OleDb repositories
ADOX.Catalog bootstrap creation
```

Target/future implementation:

```text
SQLite database file
M:\Moat House\MoatHouse Handover\Data\moat-house.db
conservative shared-drive-safe SQLite defaults
repository/provider implementation added in later PRs
```

## High-level table mapping

| Access table | SQLite target table | Migration action |
|---|---|---|
| tblHandoverHeader | tblHandoverHeader | Copy all rows with type conversion. |
| tblHandoverDept | tblHandoverDept | Copy all rows with type conversion and boolean normalization. |
| tblAttachments | tblAttachments | Copy metadata only; do not copy file contents into DB. |
| tblBudgetHeader | tblBudgetHeader | Copy all rows including LinesPlanned, TotalStaffOnRegister, Comments. |
| tblBudgetRows | tblBudgetRows | Copy all rows; preserve VarianceQty but allow recalculation. |
| tblDepartments | tblDepartments | Copy rows, then align seed/default list to source-of-truth in a later implementation PR. |
| tblShiftRules | tblShiftRules | Copy rows. |
| tblEmailProfiles | tblEmailProfiles | Copy rows; validate placeholder values are not production config. |
| tblConfig | tblConfig | Do not blindly copy old C:/ values as final config; transform to ADR-001 M:\ policy. |
| tblAuditLog | tblAuditLog | Copy rows best-effort; audit import failures should be reported not hidden. |
| none | tblSchemaMigrations | Create SQLite-only metadata table. |

## Type mapping

| Access type / pattern | SQLite target | Notes |
|---|---|---|
| AUTOINCREMENT primary key | INTEGER PRIMARY KEY AUTOINCREMENT | Preserve integer IDs during migration where possible by explicit insert. |
| LONG | INTEGER | Used for IDs, counts, minutes, sequence/order. |
| DOUBLE | REAL | Used for efficiency, yield, planned/used staff, variance. |
| TEXT(n) | TEXT | Length validation can be app-level or later CHECK constraint. |
| LONGTEXT | TEXT | User-entered text must be escaped when rendered. |
| YESNO | INTEGER CHECK(value IN (0,1)) | Convert TRUE to 1, FALSE to 0, NULL to defined default where target NOT NULL. |
| DATETIME | TEXT | ISO-8601 target convention. |
| NULL | NULL | Preserve nulls unless target defines safe default. |

## Query syntax mapping

| Access / OleDb | SQLite |
|---|---|
| `SELECT TOP 1 ...` | `SELECT ... LIMIT 1` |
| `IIF(condition, a, b)` | `CASE WHEN condition THEN a ELSE b END` |
| `TRUE` | `1` |
| `FALSE` | `0` |
| `IsDeleted = FALSE OR IsDeleted IS NULL` | `COALESCE(IsDeleted, 0) = 0` |
| `?` positional parameters | named parameters such as `$sessionId` or `@sessionId` |
| `DateTime` OleDb values | ISO-8601 text values |

## Per-table mapping details

### tblHandoverHeader

| Access column | SQLite column | Transform |
|---|---|---|
| HandoverID | HandoverID | Preserve integer id. |
| ShiftDate | ShiftDate | Convert to `YYYY-MM-DD` business date text. |
| ShiftCode | ShiftCode | Trim; preserve AM/PM/NS. |
| SessionStatus | SessionStatus | Preserve, default to `Open` only if missing. |
| CreatedAt | CreatedAt | Convert to ISO-8601 text; preserve null if unknown. |
| CreatedBy | CreatedBy | Preserve text. |
| UpdatedAt | UpdatedAt | Convert to ISO-8601 text; preserve null if unknown. |
| UpdatedBy | UpdatedBy | Preserve text. |

Validation:

- `(ShiftDate, ShiftCode)` remains unique.
- Every session must have at least one department row after migration or bootstrap repair.

### tblHandoverDept

| Access column | SQLite column | Transform |
|---|---|---|
| DeptRecordID | DeptRecordID | Preserve integer id. |
| HandoverID | HandoverID | Preserve FK. |
| DeptName | DeptName | Preserve text. |
| DeptStatus | DeptStatus | Preserve, default to `Not running` only if missing. |
| DowntimeMin | DowntimeMin | Preserve integer/null. |
| EfficiencyPct | EfficiencyPct | Preserve real/null. |
| YieldPct | YieldPct | Preserve real/null. |
| DeptNotes | DeptNotes | Preserve text/null as empty string only if implementation requires. |
| CreatedAt | CreatedAt | Convert to ISO-8601 text. |
| CreatedBy | CreatedBy | Preserve text. |
| UpdatedAt | UpdatedAt | Convert to ISO-8601 text. |
| UpdatedBy | UpdatedBy | Preserve text. |
| VersionNo | VersionNo | Preserve integer; default 1 if null. |
| IsDeleted | IsDeleted | TRUE/FALSE/NULL -> 1/0/0. |

Validation:

- `(HandoverID, DeptName)` remains unique.
- Non-metric departments should not acquire metric values during migration.
- Metric department set remains Injection, MetaPress, Berks, Wilts.

### tblAttachments

| Access column | SQLite column | Transform |
|---|---|---|
| AttachmentID | AttachmentID | Preserve integer id. |
| HandoverID | HandoverID | Preserve FK. |
| DeptRecordID | DeptRecordID | Preserve FK. |
| ShiftDate | ShiftDate | Convert to `YYYY-MM-DD` business date if present. |
| ShiftCode | ShiftCode | Preserve text. |
| DeptName | DeptName | Preserve text. |
| FilePath | FilePath | Preserve path initially; later path policy may normalize under M:\ managed root. |
| DisplayName | DisplayName | Preserve text. |
| CapturedOn | CapturedOn | Convert to ISO-8601 text. |
| SequenceNo | SequenceNo | Preserve integer. |
| Notes | Notes | Preserve text. |
| IsDeleted | IsDeleted | TRUE/FALSE/NULL -> 1/0/0. |

Validation:

- Metadata only is copied.
- Do not store file bytes in SQLite.
- Later implementation must validate managed path containment before opening files.

### tblBudgetHeader

| Access column | SQLite column | Transform |
|---|---|---|
| BudgetHeaderID | BudgetHeaderID | Preserve integer id. |
| HandoverID | HandoverID | Preserve FK; unique per session. |
| ShiftDate | ShiftDate | Convert to `YYYY-MM-DD` business date if present. |
| ShiftCode | ShiftCode | Preserve text. |
| LinesPlanned | LinesPlanned | Preserve real/null. |
| TotalStaffOnRegister | TotalStaffOnRegister | Preserve real/null. |
| Comments | Comments | Preserve text. |
| CreatedAt | CreatedAt | Convert to ISO-8601 text. |
| CreatedBy | CreatedBy | Preserve text. |
| UpdatedAt | UpdatedAt | Convert to ISO-8601 text. |
| UpdatedBy | UpdatedBy | Preserve text. |

Validation:

- `HandoverID` uniqueness preserved.
- Comments are escaped when rendered later.

### tblBudgetRows

| Access column | SQLite column | Transform |
|---|---|---|
| BudgetRowID | BudgetRowID | Preserve integer id. |
| BudgetHeaderID | BudgetHeaderID | Preserve FK. |
| DeptName | DeptName | Preserve labour/budget area text. |
| PlannedQty | PlannedQty | Preserve real/null. |
| UsedQty | UsedQty | Preserve real/null. |
| VarianceQty | VarianceQty | Preserve real/null; compare with UsedQty - PlannedQty during validation. |
| ReasonText | ReasonText | Preserve text. |
| UpdatedAt | UpdatedAt | Convert to ISO-8601 text. |
| UpdatedBy | UpdatedBy | Preserve text. |

Validation:

- Budget variance business rule remains `UsedQty - PlannedQty`.
- Existing `VarianceQty` mismatches must be reported in migration report, not silently ignored.
- ReasonText must be escaped when rendered later.

### tblDepartments

| Access column | SQLite column | Transform |
|---|---|---|
| DeptName | DeptName | Preserve text primary key. |
| DisplayOrder | DisplayOrder | Preserve integer. |
| IsMetricDept | IsMetricDept | TRUE/FALSE/NULL -> 1/0/0. |
| IsClosureDept | IsClosureDept | TRUE/FALSE/NULL -> 1/0/0. |
| IsActive | IsActive | TRUE/FALSE/NULL -> 1/0/1. |

Validation:

- Source-of-truth 13 handover departments must exist in final target seed.
- Current Access seed only inserts 4 departments; this mismatch is documented and must be fixed in a later implementation PR.

### tblShiftRules

| Access column | SQLite column | Transform |
|---|---|---|
| ShiftCode | ShiftCode | Preserve text primary key. |
| ShiftName | ShiftName | Preserve text. |
| EmailProfileKey | EmailProfileKey | Preserve text. |
| DisplayOrder | DisplayOrder | Preserve integer. |

Validation:

- AM, PM, NS must exist.

### tblEmailProfiles

| Access column | SQLite column | Transform |
|---|---|---|
| EmailProfileKey | EmailProfileKey | Preserve text primary key. |
| ToList | ToList | Preserve text. |
| CcList | CcList | Preserve text. |
| SubjectTemplate | SubjectTemplate | Preserve text. |
| BodyTemplate | BodyTemplate | Preserve text. |
| IsActive | IsActive | TRUE/FALSE/NULL -> 1/0/1. |

Validation:

- Existing `example.com` recipient seed values must be marked placeholder/non-production in migration report or replaced by admin config later.
- No automatic email sending is introduced.

### tblConfig

| Access column | SQLite column | Transform |
|---|---|---|
| ConfigKey | ConfigKey | Preserve/transform key. |
| ConfigValue | ConfigValue | Transform old paths to ADR-001 values where keys are known. |
| Notes | Notes | Preserve/update notes. |

Required transforms:

| Access config key | SQLite target behaviour |
|---|---|
| accessDatabasePath | Do not carry as runtime target. Preserve only as legacy import source if needed. |
| attachmentsRoot | Set to `M:\Moat House\MoatHouse Handover\Attachments`. |
| reportsOutputRoot | Set to `M:\Moat House\MoatHouse Handover\Reports`. |
| dataRoot | Add if missing: `M:\Moat House\MoatHouse Handover`. |
| sqliteDatabasePath | Add if missing: `M:\Moat House\MoatHouse Handover\Data\moat-house.db`. |
| backupsRoot | Add if missing. |
| logsRoot | Add if missing. |
| importsRoot | Add if missing. |
| migrationRoot | Add if missing. |

Validation:

- No final runtime config points to `C:/MOAT-Handover/shared/...`.
- Do not silently fall back to C:\ProgramData.

### tblAuditLog

| Access column | SQLite column | Transform |
|---|---|---|
| AuditID | AuditID | Preserve integer id. |
| EventAt | EventAt | Convert to ISO-8601 text. |
| UserName | UserName | Preserve text. |
| EntityType | EntityType | Preserve text. |
| EntityKey | EntityKey | Preserve text. |
| ActionType | ActionType | Preserve text. |
| Details | Details | Preserve text. |

Validation:

- Audit import is best-effort.
- Failure to import an audit row should be recorded in the migration report.

## Migration validation checklist

Later importer implementation must produce a migration report with at least:

- source Access path
- target SQLite path
- started/finished timestamps
- table row counts before/after
- number of rows imported per table
- number of failed/skipped rows per table
- key foreign key validation result
- budget variance mismatch count
- orphan attachment metadata count
- config path transform summary
- source-of-truth department seed alignment status

## Expected later PR order

This Phase 1 mapping enables:

1. Phase 2 — `M:\` AppPathService/data root service.
2. Phase 3 — database provider boundary/repository interfaces.
3. Phase 4 — SQLite bootstrapper/schema creation.
4. Phase 5 — Access-to-SQLite importer.

## Out of scope for this PR

- No runtime code changes.
- No SQLite dependency added.
- No repository refactor.
- No data migration.
- No UI changes.
- No installer changes.
