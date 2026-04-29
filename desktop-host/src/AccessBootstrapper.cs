using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed class AccessBootstrapper
{
    private readonly BootstrapLogger _logger;

    public AccessBootstrapper(BootstrapLogger logger)
    {
        _logger = logger;
    }

    public void EnsureDatabaseAndSchema(string accessDatabasePath)
    {
        var fullPath = Path.GetFullPath(accessDatabasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException($"Invalid accessDatabasePath: {accessDatabasePath}");
        }

        Directory.CreateDirectory(directory);

        if (!File.Exists(fullPath))
        {
            _logger.Log($"Access database missing. Creating: {fullPath}");
            CreateAccessDatabase(fullPath);
        }

        _logger.Log("Ensuring Access schema objects.");
        using var connection = new OleDbConnection(BuildConnectionString(fullPath));
        connection.Open();

        EnsureSchema(connection);
        EnsureSeedData(connection);
    }

    private static void CreateAccessDatabase(string fullPath)
    {
        var catalogType = Type.GetTypeFromProgID("ADOX.Catalog")
            ?? throw new InvalidOperationException("ADOX.Catalog COM type was not found. Install Access Database Engine.");

        dynamic catalog = Activator.CreateInstance(catalogType)
            ?? throw new InvalidOperationException("Failed to create ADOX.Catalog instance.");

        try
        {
            catalog.Create(BuildConnectionString(fullPath));
        }
        finally
        {
            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(catalog);
            }
            catch
            {
                // no-op best effort cleanup
            }
        }
    }

    private void EnsureSchema(OleDbConnection connection)
    {
        ExecuteIfMissingTable(connection, "tblHandoverHeader", @"CREATE TABLE tblHandoverHeader (
  HandoverID AUTOINCREMENT CONSTRAINT PK_tblHandoverHeader PRIMARY KEY,
  ShiftDate DATETIME NOT NULL,
  ShiftCode TEXT(10) NOT NULL,
  SessionStatus TEXT(20) NOT NULL,
  CreatedAt DATETIME,
  CreatedBy TEXT(255),
  UpdatedAt DATETIME,
  UpdatedBy TEXT(255)
)");
        ExecuteIfMissingIndex(connection, "UX_tblHandoverHeader_ShiftDate_ShiftCode", "CREATE UNIQUE INDEX UX_tblHandoverHeader_ShiftDate_ShiftCode ON tblHandoverHeader (ShiftDate, ShiftCode)");

        ExecuteIfMissingTable(connection, "tblHandoverDept", @"CREATE TABLE tblHandoverDept (
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
  CONSTRAINT FK_tblHandoverDept_tblHandoverHeader FOREIGN KEY (HandoverID) REFERENCES tblHandoverHeader (HandoverID)
)");
        ExecuteIfMissingIndex(connection, "UX_tblHandoverDept_HandoverID_DeptName", "CREATE UNIQUE INDEX UX_tblHandoverDept_HandoverID_DeptName ON tblHandoverDept (HandoverID, DeptName)");

        ExecuteIfMissingTable(connection, "tblAttachments", @"CREATE TABLE tblAttachments (
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
  CONSTRAINT FK_tblAttachments_tblHandoverHeader FOREIGN KEY (HandoverID) REFERENCES tblHandoverHeader (HandoverID),
  CONSTRAINT FK_tblAttachments_tblHandoverDept FOREIGN KEY (DeptRecordID) REFERENCES tblHandoverDept (DeptRecordID)
)");
        ExecuteIfMissingIndex(connection, "IX_tblAttachments_DeptRecordID_SequenceNo", "CREATE INDEX IX_tblAttachments_DeptRecordID_SequenceNo ON tblAttachments (DeptRecordID, SequenceNo)");

        ExecuteIfMissingTable(connection, "tblBudgetHeader", @"CREATE TABLE tblBudgetHeader (
  BudgetHeaderID AUTOINCREMENT CONSTRAINT PK_tblBudgetHeader PRIMARY KEY,
  HandoverID LONG NOT NULL,
  ShiftDate DATETIME,
  ShiftCode TEXT(10),
  CreatedAt DATETIME,
  CreatedBy TEXT(255),
  UpdatedAt DATETIME,
  UpdatedBy TEXT(255),
  CONSTRAINT FK_tblBudgetHeader_tblHandoverHeader FOREIGN KEY (HandoverID) REFERENCES tblHandoverHeader (HandoverID)
)");
        ExecuteIfMissingIndex(connection, "UX_tblBudgetHeader_HandoverID", "CREATE UNIQUE INDEX UX_tblBudgetHeader_HandoverID ON tblBudgetHeader (HandoverID)");
        ExecuteIfMissingColumn(connection, "tblBudgetHeader", "LinesPlanned", "ALTER TABLE tblBudgetHeader ADD COLUMN LinesPlanned DOUBLE");
        ExecuteIfMissingColumn(connection, "tblBudgetHeader", "TotalStaffOnRegister", "ALTER TABLE tblBudgetHeader ADD COLUMN TotalStaffOnRegister DOUBLE");
        ExecuteIfMissingColumn(connection, "tblBudgetHeader", "Comments", "ALTER TABLE tblBudgetHeader ADD COLUMN Comments LONGTEXT");


        ExecuteIfMissingTable(connection, "tblBudgetRows", @"CREATE TABLE tblBudgetRows (
  BudgetRowID AUTOINCREMENT CONSTRAINT PK_tblBudgetRows PRIMARY KEY,
  BudgetHeaderID LONG NOT NULL,
  DeptName TEXT(100),
  PlannedQty DOUBLE,
  UsedQty DOUBLE,
  VarianceQty DOUBLE,
  ReasonText LONGTEXT,
  UpdatedAt DATETIME,
  UpdatedBy TEXT(255),
  CONSTRAINT FK_tblBudgetRows_tblBudgetHeader FOREIGN KEY (BudgetHeaderID) REFERENCES tblBudgetHeader (BudgetHeaderID)
)");

        ExecuteIfMissingTable(connection, "tblDepartments", @"CREATE TABLE tblDepartments (
  DeptName TEXT(100) CONSTRAINT PK_tblDepartments PRIMARY KEY,
  DisplayOrder LONG,
  IsMetricDept YESNO,
  IsClosureDept YESNO,
  IsActive YESNO
)");

        ExecuteIfMissingTable(connection, "tblShiftRules", @"CREATE TABLE tblShiftRules (
  ShiftCode TEXT(10) CONSTRAINT PK_tblShiftRules PRIMARY KEY,
  ShiftName TEXT(50),
  EmailProfileKey TEXT(50),
  DisplayOrder LONG
)");

        ExecuteIfMissingTable(connection, "tblEmailProfiles", @"CREATE TABLE tblEmailProfiles (
  EmailProfileKey TEXT(50) CONSTRAINT PK_tblEmailProfiles PRIMARY KEY,
  ToList LONGTEXT,
  CcList LONGTEXT,
  SubjectTemplate LONGTEXT,
  BodyTemplate LONGTEXT,
  IsActive YESNO
)");

        ExecuteIfMissingTable(connection, "tblConfig", @"CREATE TABLE tblConfig (
  ConfigKey TEXT(100) CONSTRAINT PK_tblConfig PRIMARY KEY,
  ConfigValue LONGTEXT,
  Notes LONGTEXT
)");

        ExecuteIfMissingTable(connection, "tblAuditLog", @"CREATE TABLE tblAuditLog (
  AuditID AUTOINCREMENT CONSTRAINT PK_tblAuditLog PRIMARY KEY,
  EventAt DATETIME,
  UserName TEXT(255),
  EntityType TEXT(100),
  EntityKey TEXT(255),
  ActionType TEXT(50),
  Details LONGTEXT
)");
        ExecuteIfMissingIndex(connection, "IX_tblAuditLog_EventAt", "CREATE INDEX IX_tblAuditLog_EventAt ON tblAuditLog (EventAt)");
    }

    private void EnsureSeedData(OleDbConnection connection)
    {
        EnsureRecord(connection, "tblDepartments", "DeptName", "Injection", "INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('Injection', 1, TRUE, TRUE, TRUE)");
        EnsureRecord(connection, "tblDepartments", "DeptName", "MetaPress", "INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('MetaPress', 2, TRUE, TRUE, TRUE)");
        EnsureRecord(connection, "tblDepartments", "DeptName", "Berks", "INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('Berks', 3, TRUE, TRUE, TRUE)");
        EnsureRecord(connection, "tblDepartments", "DeptName", "Wilts", "INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('Wilts', 4, TRUE, TRUE, TRUE)");

        EnsureRecord(connection, "tblShiftRules", "ShiftCode", "AM", "INSERT INTO tblShiftRules (ShiftCode, ShiftName, EmailProfileKey, DisplayOrder) VALUES ('AM', 'Morning', 'default_am', 1)");
        EnsureRecord(connection, "tblShiftRules", "ShiftCode", "PM", "INSERT INTO tblShiftRules (ShiftCode, ShiftName, EmailProfileKey, DisplayOrder) VALUES ('PM', 'Afternoon', 'default_pm', 2)");
        EnsureRecord(connection, "tblShiftRules", "ShiftCode", "NS", "INSERT INTO tblShiftRules (ShiftCode, ShiftName, EmailProfileKey, DisplayOrder) VALUES ('NS', 'Night', 'default_ns', 3)");

        EnsureRecord(connection, "tblEmailProfiles", "EmailProfileKey", "default_am", "INSERT INTO tblEmailProfiles (EmailProfileKey, ToList, CcList, SubjectTemplate, BodyTemplate, IsActive) VALUES ('default_am', 'handover-am@example.com', '', 'AM Handover - {ShiftDate}', 'Please find AM handover attached.', TRUE)");
        EnsureRecord(connection, "tblEmailProfiles", "EmailProfileKey", "default_pm", "INSERT INTO tblEmailProfiles (EmailProfileKey, ToList, CcList, SubjectTemplate, BodyTemplate, IsActive) VALUES ('default_pm', 'handover-pm@example.com', '', 'PM Handover - {ShiftDate}', 'Please find PM handover attached.', TRUE)");
        EnsureRecord(connection, "tblEmailProfiles", "EmailProfileKey", "default_ns", "INSERT INTO tblEmailProfiles (EmailProfileKey, ToList, CcList, SubjectTemplate, BodyTemplate, IsActive) VALUES ('default_ns', 'handover-ns@example.com', '', 'NS Handover - {ShiftDate}', 'Please find NS handover attached.', TRUE)");

        EnsureRecord(connection, "tblConfig", "ConfigKey", "accessDatabasePath", "INSERT INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ('accessDatabasePath', 'C:/MOAT-Handover/shared/moat_handover_be.accdb', 'Shared Access backend path')");
        EnsureRecord(connection, "tblConfig", "ConfigKey", "attachmentsRoot", "INSERT INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ('attachmentsRoot', 'C:/MOAT-Handover/shared/Attachments', 'Attachment root path')");
        EnsureRecord(connection, "tblConfig", "ConfigKey", "reportsOutputRoot", "INSERT INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ('reportsOutputRoot', 'C:/MOAT-Handover/shared/Reports', 'Report output root path')");
    }

    private void ExecuteIfMissingTable(OleDbConnection connection, string tableName, string createStatement)
    {
        if (TableExists(connection, tableName))
        {
            _logger.Log($"Table exists: {tableName}");
            return;
        }

        using var cmd = new OleDbCommand(createStatement, connection);
        cmd.ExecuteNonQuery();
        _logger.Log($"Created table: {tableName}");
    }

    private void ExecuteIfMissingIndex(OleDbConnection connection, string indexName, string createStatement)
    {
        if (IndexExists(connection, indexName))
        {
            _logger.Log($"Index exists: {indexName}");
            return;
        }

        using var cmd = new OleDbCommand(createStatement, connection);
        cmd.ExecuteNonQuery();
        _logger.Log($"Created index: {indexName}");
    }

    private void ExecuteIfMissingColumn(OleDbConnection connection, string tableName, string columnName, string alterStatement)
    {
        if (ColumnExists(connection, tableName, columnName))
        {
            _logger.Log($"Column exists: {tableName}.{columnName}");
            return;
        }

        using var cmd = new OleDbCommand(alterStatement, connection);
        cmd.ExecuteNonQuery();
        _logger.Log($"Added column: {tableName}.{columnName}");
    }

    private static bool ColumnExists(OleDbConnection connection, string tableName, string columnName)
    {
        var schema = connection.GetSchema("Columns", new[] { null, null, tableName, null });
        foreach (System.Data.DataRow row in schema.Rows)
        {
            if (string.Equals(Convert.ToString(row["COLUMN_NAME"]), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureRecord(OleDbConnection connection, string table, string keyColumn, string keyValue, string insertSql)
    {
        var existsSql = $"SELECT COUNT(*) FROM {table} WHERE {keyColumn} = ?";
        using var existsCmd = new OleDbCommand(existsSql, connection);
        existsCmd.Parameters.AddWithValue("@p1", keyValue);
        var count = Convert.ToInt32(existsCmd.ExecuteScalar());

        if (count > 0)
        {
            _logger.Log($"Seed exists: {table}.{keyColumn}='{keyValue}'");
            return;
        }

        using var insertCmd = new OleDbCommand(insertSql, connection);
        insertCmd.ExecuteNonQuery();
        _logger.Log($"Inserted seed: {table}.{keyColumn}='{keyValue}'");
    }

    private static bool TableExists(OleDbConnection connection, string tableName)
    {
        var schema = connection.GetSchema("Tables", new[] { null, null, tableName, "TABLE" });
        return schema.Rows.Count > 0;
    }

    private static bool IndexExists(OleDbConnection connection, string indexName)
    {
        var indexes = connection.GetSchema("Indexes", new[] { null, null, null, null, indexName });
        return indexes.Rows.Count > 0;
    }

    public static string BuildConnectionString(string fullPath) => $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={fullPath};Persist Security Info=False;";
}
