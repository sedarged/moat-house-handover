using System.Collections.Generic;

namespace MoatHouseHandover.Host.Sqlite;

public static class SqliteSchema
{
    public const string InitialMigrationId = "001_initial_sqlite_schema";

    public static IReadOnlyList<string> RequiredTables { get; } =
    [
        "tblHandoverHeader",
        "tblHandoverDept",
        "tblAttachments",
        "tblBudgetHeader",
        "tblBudgetRows",
        "tblDepartments",
        "tblShiftRules",
        "tblEmailProfiles",
        "tblConfig",
        "tblAuditLog",
        "tblSchemaMigrations"
    ];

    public static IReadOnlyList<string> CreateStatements { get; } =
    [
        """
        CREATE TABLE IF NOT EXISTS tblHandoverHeader (
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
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_tblHandoverHeader_ShiftDate_ShiftCode ON tblHandoverHeader (ShiftDate, ShiftCode);",
        """
        CREATE TABLE IF NOT EXISTS tblHandoverDept (
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
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_tblHandoverDept_HandoverID_DeptName ON tblHandoverDept (HandoverID, DeptName);",
        "CREATE INDEX IF NOT EXISTS IX_tblHandoverDept_HandoverID ON tblHandoverDept (HandoverID);",
        """
        CREATE TABLE IF NOT EXISTS tblAttachments (
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
        """,
        "CREATE INDEX IF NOT EXISTS IX_tblAttachments_DeptRecordID_SequenceNo ON tblAttachments (DeptRecordID, SequenceNo);",
        "CREATE INDEX IF NOT EXISTS IX_tblAttachments_HandoverID ON tblAttachments (HandoverID);",
        """
        CREATE TABLE IF NOT EXISTS tblBudgetHeader (
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
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_tblBudgetHeader_HandoverID ON tblBudgetHeader (HandoverID);",
        """
        CREATE TABLE IF NOT EXISTS tblBudgetRows (
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
        """,
        "CREATE INDEX IF NOT EXISTS IX_tblBudgetRows_BudgetHeaderID ON tblBudgetRows (BudgetHeaderID);",
        "CREATE INDEX IF NOT EXISTS IX_tblBudgetRows_DeptName ON tblBudgetRows (DeptName);",
        """
        CREATE TABLE IF NOT EXISTS tblDepartments (
          DeptName TEXT PRIMARY KEY,
          DisplayOrder INTEGER,
          IsMetricDept INTEGER NOT NULL DEFAULT 0 CHECK (IsMetricDept IN (0,1)),
          IsClosureDept INTEGER NOT NULL DEFAULT 0 CHECK (IsClosureDept IN (0,1)),
          IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1))
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS tblShiftRules (
          ShiftCode TEXT PRIMARY KEY,
          ShiftName TEXT,
          EmailProfileKey TEXT,
          DisplayOrder INTEGER
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS tblEmailProfiles (
          EmailProfileKey TEXT PRIMARY KEY,
          ToList TEXT,
          CcList TEXT,
          SubjectTemplate TEXT,
          BodyTemplate TEXT,
          IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1))
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS tblConfig (
          ConfigKey TEXT PRIMARY KEY,
          ConfigValue TEXT,
          Notes TEXT
        );
        """,
        """
        CREATE TABLE IF NOT EXISTS tblAuditLog (
          AuditID INTEGER PRIMARY KEY AUTOINCREMENT,
          EventAt TEXT,
          UserName TEXT,
          EntityType TEXT,
          EntityKey TEXT,
          ActionType TEXT,
          Details TEXT
        );
        """,
        "CREATE INDEX IF NOT EXISTS IX_tblAuditLog_EventAt ON tblAuditLog (EventAt);",
        "CREATE INDEX IF NOT EXISTS IX_tblAuditLog_Entity ON tblAuditLog (EntityType, EntityKey);",
        """
        CREATE TABLE IF NOT EXISTS tblSchemaMigrations (
          MigrationId TEXT PRIMARY KEY,
          AppliedAt TEXT NOT NULL,
          AppliedBy TEXT,
          Notes TEXT
        );
        """
    ];
}
