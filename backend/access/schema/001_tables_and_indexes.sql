-- MOAT HOUSE HANDOVER v2 - Access schema draft (Stage 1)
-- Notes:
-- 1) Access DDL execution can be performed via DAO ADOX or manual query execution.
-- 2) Some constraints (e.g., partial unique filters) are enforced at service layer by design.

CREATE TABLE tblHandoverHeader (
  HandoverID AUTOINCREMENT CONSTRAINT PK_tblHandoverHeader PRIMARY KEY,
  ShiftDate DATETIME NOT NULL,
  ShiftCode TEXT(10) NOT NULL,
  SessionStatus TEXT(20) NOT NULL,
  CreatedAt DATETIME,
  CreatedBy TEXT(255),
  UpdatedAt DATETIME,
  UpdatedBy TEXT(255)
);
CREATE UNIQUE INDEX UX_tblHandoverHeader_ShiftDate_ShiftCode
  ON tblHandoverHeader (ShiftDate, ShiftCode);

CREATE TABLE tblHandoverDept (
  DeptRecordID AUTOINCREMENT CONSTRAINT PK_tblHandoverDept PRIMARY KEY,
  HandoverID LONG NOT NULL,
  DeptName TEXT(100) NOT NULL,
  DeptStatus TEXT(50),
  DowntimeMin LONG,
  EfficiencyPct DOUBLE,
  YieldPct DOUBLE,
  DeptNotes LONGTEXT,
  CreatedAt DATETIME,
  CreatedBy TEXT(255),
  UpdatedAt DATETIME,
  UpdatedBy TEXT(255),
  VersionNo LONG,
  IsDeleted YESNO,
  CONSTRAINT FK_tblHandoverDept_tblHandoverHeader FOREIGN KEY (HandoverID)
    REFERENCES tblHandoverHeader (HandoverID)
);
CREATE UNIQUE INDEX UX_tblHandoverDept_HandoverID_DeptName
  ON tblHandoverDept (HandoverID, DeptName);

CREATE TABLE tblAttachments (
  AttachmentID AUTOINCREMENT CONSTRAINT PK_tblAttachments PRIMARY KEY,
  HandoverID LONG NOT NULL,
  DeptRecordID LONG NOT NULL,
  ShiftDate DATETIME,
  ShiftCode TEXT(10),
  DeptName TEXT(100),
  FilePath LONGTEXT,
  DisplayName TEXT(255),
  CapturedOn DATETIME,
  SequenceNo LONG,
  Notes LONGTEXT,
  IsDeleted YESNO,
  CONSTRAINT FK_tblAttachments_tblHandoverHeader FOREIGN KEY (HandoverID)
    REFERENCES tblHandoverHeader (HandoverID),
  CONSTRAINT FK_tblAttachments_tblHandoverDept FOREIGN KEY (DeptRecordID)
    REFERENCES tblHandoverDept (DeptRecordID)
);
CREATE INDEX IX_tblAttachments_DeptRecordID_SequenceNo
  ON tblAttachments (DeptRecordID, SequenceNo);

CREATE TABLE tblBudgetHeader (
  BudgetHeaderID AUTOINCREMENT CONSTRAINT PK_tblBudgetHeader PRIMARY KEY,
  HandoverID LONG NOT NULL,
  ShiftDate DATETIME,
  ShiftCode TEXT(10),
  CreatedAt DATETIME,
  CreatedBy TEXT(255),
  UpdatedAt DATETIME,
  UpdatedBy TEXT(255),
  CONSTRAINT FK_tblBudgetHeader_tblHandoverHeader FOREIGN KEY (HandoverID)
    REFERENCES tblHandoverHeader (HandoverID)
);
CREATE UNIQUE INDEX UX_tblBudgetHeader_HandoverID
  ON tblBudgetHeader (HandoverID);

CREATE TABLE tblBudgetRows (
  BudgetRowID AUTOINCREMENT CONSTRAINT PK_tblBudgetRows PRIMARY KEY,
  BudgetHeaderID LONG NOT NULL,
  DeptName TEXT(100),
  PlannedQty DOUBLE,
  UsedQty DOUBLE,
  VarianceQty DOUBLE,
  ReasonText LONGTEXT,
  UpdatedAt DATETIME,
  UpdatedBy TEXT(255),
  CONSTRAINT FK_tblBudgetRows_tblBudgetHeader FOREIGN KEY (BudgetHeaderID)
    REFERENCES tblBudgetHeader (BudgetHeaderID)
);

CREATE TABLE tblDepartments (
  DeptName TEXT(100) CONSTRAINT PK_tblDepartments PRIMARY KEY,
  DisplayOrder LONG,
  IsMetricDept YESNO,
  IsClosureDept YESNO,
  IsActive YESNO
);

CREATE TABLE tblShiftRules (
  ShiftCode TEXT(10) CONSTRAINT PK_tblShiftRules PRIMARY KEY,
  ShiftName TEXT(50),
  EmailProfileKey TEXT(50),
  DisplayOrder LONG
);

CREATE TABLE tblEmailProfiles (
  EmailProfileKey TEXT(50) CONSTRAINT PK_tblEmailProfiles PRIMARY KEY,
  ToList LONGTEXT,
  CcList LONGTEXT,
  SubjectTemplate LONGTEXT,
  BodyTemplate LONGTEXT,
  IsActive YESNO
);

CREATE TABLE tblConfig (
  ConfigKey TEXT(100) CONSTRAINT PK_tblConfig PRIMARY KEY,
  ConfigValue LONGTEXT,
  Notes LONGTEXT
);

CREATE TABLE tblAuditLog (
  AuditID AUTOINCREMENT CONSTRAINT PK_tblAuditLog PRIMARY KEY,
  EventAt DATETIME,
  UserName TEXT(255),
  EntityType TEXT(100),
  EntityKey TEXT(255),
  ActionType TEXT(50),
  Details LONGTEXT
);
CREATE INDEX IX_tblAuditLog_EventAt ON tblAuditLog (EventAt);
