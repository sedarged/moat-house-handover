# MOAT HOUSE HANDOVER v2 — DATA MODEL

## 1. Backend Strategy
- Shared Access backend file contains tables only.
- Local client reads/writes through DAO.
- Attachments and reports live in file storage, not as binary blobs in tables.

## 2. Core Tables

### tblHandoverHeader
| Column | Type | Notes |
|---|---|---|
| HandoverID | AutoNumber / Long | PK |
| ShiftDate | Date/Time | Indexed |
| ShiftCode | Short Text | AM / PM / NS |
| SessionStatus | Short Text | Open / Cleared / Finalized |
| CreatedAt | Date/Time | |
| CreatedBy | Short Text | |
| UpdatedAt | Date/Time | |
| UpdatedBy | Short Text | |

Indexes:
- Unique on `(ShiftDate, ShiftCode)`

### tblHandoverDept
| Column | Type | Notes |
|---|---|---|
| DeptRecordID | AutoNumber / Long | PK |
| HandoverID | Long | FK to tblHandoverHeader |
| DeptName | Short Text | Indexed |
| DeptStatus | Short Text | |
| DowntimeMin | Long | Metric departments only |
| EfficiencyPct | Double | Metric departments only |
| YieldPct | Double | Metric departments only |
| DeptNotes | Long Text | |
| CreatedAt | Date/Time | |
| CreatedBy | Short Text | |
| UpdatedAt | Date/Time | |
| UpdatedBy | Short Text | |
| VersionNo | Long | For optimistic concurrency |
| IsDeleted | Yes/No | Soft delete |

Indexes:
- Unique on `(HandoverID, DeptName)` where possible by design

### tblAttachments
| Column | Type | Notes |
|---|---|---|
| AttachmentID | AutoNumber / Long | PK |
| HandoverID | Long | FK |
| DeptRecordID | Long | FK |
| ShiftDate | Date/Time | |
| ShiftCode | Short Text | |
| DeptName | Short Text | |
| FilePath | Short Text / Long Text | Physical file path |
| DisplayName | Short Text | |
| CapturedOn | Date/Time | |
| SequenceNo | Long | Order within department |
| Notes | Long Text | Optional |
| IsDeleted | Yes/No | Soft delete |

Indexes:
- `(DeptRecordID, SequenceNo)`

### tblBudgetHeader
| Column | Type | Notes |
|---|---|---|
| BudgetHeaderID | AutoNumber / Long | PK |
| HandoverID | Long | FK |
| ShiftDate | Date/Time | |
| ShiftCode | Short Text | |
| CreatedAt | Date/Time | |
| CreatedBy | Short Text | |
| UpdatedAt | Date/Time | |
| UpdatedBy | Short Text | |

### tblBudgetRows
| Column | Type | Notes |
|---|---|---|
| BudgetRowID | AutoNumber / Long | PK |
| BudgetHeaderID | Long | FK |
| DeptName | Short Text | |
| PlannedQty | Double | |
| UsedQty | Double | |
| VarianceQty | Double | Derived/stored |
| ReasonText | Long Text | |
| UpdatedAt | Date/Time | |
| UpdatedBy | Short Text | |

## 3. Support Tables

### tblDepartments
| Column | Type | Notes |
|---|---|---|
| DeptName | Short Text | PK |
| DisplayOrder | Long | |
| IsMetricDept | Yes/No | True only for Injection, MetaPress, Berks, Wilts |
| IsClosureDept | Yes/No | Same four departments |
| IsActive | Yes/No | |

### tblShiftRules
| Column | Type | Notes |
|---|---|---|
| ShiftCode | Short Text | PK |
| ShiftName | Short Text | |
| EmailProfileKey | Short Text | |
| DisplayOrder | Long | |

### tblEmailProfiles
| Column | Type | Notes |
|---|---|---|
| EmailProfileKey | Short Text | PK |
| ToList | Long Text | |
| CcList | Long Text | |
| SubjectTemplate | Long Text | |
| BodyTemplate | Long Text | |
| IsActive | Yes/No | |

### tblConfig
| Column | Type | Notes |
|---|---|---|
| ConfigKey | Short Text | PK |
| ConfigValue | Long Text | |
| Notes | Long Text | |

### tblAuditLog
| Column | Type | Notes |
|---|---|---|
| AuditID | AutoNumber / Long | PK |
| EventAt | Date/Time | |
| UserName | Short Text | |
| EntityType | Short Text | HandoverHeader / HandoverDept / Attachment / Budget |
| EntityKey | Short Text | |
| ActionType | Short Text | Create / Update / Delete / Clear / Draft |
| Details | Long Text | |

## 4. Relationships
- `tblHandoverHeader` 1 → many `tblHandoverDept`
- `tblHandoverHeader` 1 → many `tblAttachments`
- `tblHandoverDept` 1 → many `tblAttachments`
- `tblHandoverHeader` 1 → 1 `tblBudgetHeader`
- `tblBudgetHeader` 1 → many `tblBudgetRows`

## 5. File Storage Rules
### Attachment root
`<SharedRoot>\Attachments\<ShiftCode>\<YYYY-MM-DD>\<DeptName>\`

### Report root
`<SharedRoot>\Reports\<ShiftCode>\<YYYY-MM-DD>\`

### Naming
- Attachment files: sequence-based safe names
- Reports: timestamped output names

## 6. Concurrency Strategy
- Use `UpdatedAt`, `UpdatedBy`, `VersionNo`
- Detect stale record before save
- If version mismatch: prompt refresh/reload path
