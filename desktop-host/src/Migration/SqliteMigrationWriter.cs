using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using MoatHouseHandover.Host.Sqlite;

namespace MoatHouseHandover.Host.Migration;

public sealed class SqliteMigrationWriter
{
    private static readonly (int Order, string Name, int IsMetric)[] RequiredDepartments =
    [
        (1, "Injection", 1), (2, "MetaPress", 1), (3, "Berks", 1), (4, "Wilts", 1),
        (5, "Racking", 0), (6, "Butchery", 0), (7, "Further Processing", 0), (8, "Tumblers", 0),
        (9, "Smoke Tumbler", 0), (10, "Minimums & Samples", 0), (11, "Goods In & Despatch", 0), (12, "Dry Goods", 0), (13, "Additional", 0)
    ];
    private readonly List<MigrationIssue> _issues = [];

    public (IReadOnlyList<MigrationTableResult> tableResults, IReadOnlyList<MigrationIssue> issues, Dictionary<string,string> configSummary) Import(MigrationOptions options, MigrationReadResult source)
    {
        _issues.Clear();
        if (File.Exists(options.Paths.StagingSqlitePath)) File.Delete(options.Paths.StagingSqlitePath);
        var dataRoot = Directory.GetParent(Path.GetDirectoryName(options.Paths.TargetSqlitePath) ?? options.Paths.TargetSqlitePath)?.FullName ?? @"M:\Moat House\MoatHouse Handover";
        new SqliteBootstrapper().EnsureBootstrapped(options.Paths.StagingSqlitePath, dataRoot, options.Actor);

        var factory = new SqliteConnectionFactory(dataRoot);
        using var conn = factory.Create(options.Paths.StagingSqlitePath);
        conn.Open();
        using var tx = conn.BeginTransaction();

        ClearSeededTables(conn, tx);

        var tableResults = new List<MigrationTableResult>
        {
            ImportTable(conn, tx, source, "tblHandoverHeader", InsertHandoverHeader, "HandoverID"),
            ImportTable(conn, tx, source, "tblHandoverDept", InsertHandoverDept, "DeptRecordID"),
            ImportTable(conn, tx, source, "tblAttachments", InsertAttachment, "AttachmentID"),
            ImportTable(conn, tx, source, "tblBudgetHeader", InsertBudgetHeader, "BudgetHeaderID"),
            ImportTable(conn, tx, source, "tblBudgetRows", InsertBudgetRow, "BudgetRowID"),
            ImportTable(conn, tx, source, "tblDepartments", InsertDepartment, "DeptName"),
            ImportTable(conn, tx, source, "tblShiftRules", InsertShiftRule, "ShiftCode"),
            ImportTable(conn, tx, source, "tblEmailProfiles", InsertEmailProfile, "EmailProfileKey"),
            ImportTable(conn, tx, source, "tblConfig", InsertConfig, "ConfigKey"),
            ImportTable(conn, tx, source, "tblAuditLog", InsertAuditLog, "AuditID")
        };
        EnsureRequiredDepartments(conn, tx);
        EnsureRequiredConfig(conn, tx);

        tx.Commit();
        return (tableResults, _issues, BuildConfigSummary(conn));
    }

    private static void ClearSeededTables(SqliteConnection c, SqliteTransaction tx)
    {
        foreach (var table in new[] { "tblDepartments", "tblShiftRules", "tblConfig" })
        {
            using var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = $"DELETE FROM {table};"; cmd.ExecuteNonQuery();
        }
    }

    private MigrationTableResult ImportTable(SqliteConnection c, SqliteTransaction tx, MigrationReadResult source, string table, Action<SqliteConnection, SqliteTransaction, DataRow> importAction, string key)
    {
        if (!source.Tables.TryGetValue(table, out var dt))
        {
            _issues.Add(new MigrationIssue("source.table.unavailable", MigrationSeverity.Error, $"Source table unavailable: {table}", Table: table));
            return new MigrationTableResult(table, table, 0, 0, 0, 0);
        }
        var imported=0; var failed=0; var skipped=0;
        foreach (DataRow row in dt.Rows)
        {
            var rowId = SafeToString(row, key);
            try { importAction(c, tx, row); imported++; }
            catch (Exception ex)
            {
                failed++;
                _issues.Add(new MigrationIssue("import.row.failed", MigrationSeverity.Error, ex.Message, ex.GetType().Name, table, rowId, false));
            }
        }
        return new MigrationTableResult(table, table, dt.Rows.Count, imported, skipped, failed);
    }

    private static void InsertHandoverHeader(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblHandoverHeader(HandoverID,ShiftDate,ShiftCode,SessionStatus,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy)
VALUES($id,$date,$code,$status,$ca,$cb,$ua,$ub);", p => { p.AddWithValue("$id", r["HandoverID"]); p.AddWithValue("$date", SafeBusinessDate(r, "ShiftDate")); p.AddWithValue("$code", SafeText(r, "ShiftCode")); p.AddWithValue("$status", SafeText(r, "SessionStatus") ?? "Open"); p.AddWithValue("$ca", SafeDateIso(r, "CreatedAt")); p.AddWithValue("$cb", SafeText(r, "CreatedBy")); p.AddWithValue("$ua", SafeDateIso(r, "UpdatedAt")); p.AddWithValue("$ub", SafeText(r, "UpdatedBy")); });
    private static void InsertHandoverDept(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblHandoverDept(DeptRecordID,HandoverID,DeptName,DeptStatus,DowntimeMin,EfficiencyPct,YieldPct,DeptNotes,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,VersionNo,IsDeleted)
VALUES($id,$hid,$name,$status,$down,$eff,$yield,$notes,$ca,$cb,$ua,$ub,$ver,$del);", p=>{ p.AddWithValue("$id", r["DeptRecordID"]); p.AddWithValue("$hid", r["HandoverID"]); p.AddWithValue("$name", SafeText(r,"DeptName")); p.AddWithValue("$status", SafeText(r,"DeptStatus")); p.AddWithValue("$down", SafeNumber(r,"DowntimeMin")); p.AddWithValue("$eff", SafeNumber(r,"EfficiencyPct")); p.AddWithValue("$yield", SafeNumber(r,"YieldPct")); p.AddWithValue("$notes", SafeText(r,"DeptNotes")); p.AddWithValue("$ca", SafeDateIso(r,"CreatedAt")); p.AddWithValue("$cb", SafeText(r,"CreatedBy")); p.AddWithValue("$ua", SafeDateIso(r,"UpdatedAt")); p.AddWithValue("$ub", SafeText(r,"UpdatedBy")); p.AddWithValue("$ver", SafeNumber(r,"VersionNo") ?? 1); p.AddWithValue("$del", SafeBool01(r,"IsDeleted"));});
    private static void InsertAttachment(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblAttachments(AttachmentID,HandoverID,DeptRecordID,ShiftDate,ShiftCode,DeptName,FilePath,DisplayName,CapturedOn,SequenceNo,Notes,IsDeleted)
VALUES($id,$hid,$did,$date,$shift,$name,$path,$disp,$cap,$seq,$notes,$del);", p=>{ p.AddWithValue("$id",r["AttachmentID"]); p.AddWithValue("$hid",r["HandoverID"]); p.AddWithValue("$did",r["DeptRecordID"]); p.AddWithValue("$date",SafeBusinessDate(r,"ShiftDate")); p.AddWithValue("$shift",SafeText(r,"ShiftCode")); p.AddWithValue("$name",SafeText(r,"DeptName")); p.AddWithValue("$path",SafeText(r,"FilePath")); p.AddWithValue("$disp",SafeText(r,"DisplayName")); p.AddWithValue("$cap",SafeDateIso(r,"CapturedOn")); p.AddWithValue("$seq",SafeNumber(r,"SequenceNo")); p.AddWithValue("$notes",SafeText(r,"Notes")); p.AddWithValue("$del",SafeBool01(r,"IsDeleted"));});
    private static void InsertBudgetHeader(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblBudgetHeader(BudgetHeaderID,HandoverID,ShiftDate,ShiftCode,LinesPlanned,TotalStaffOnRegister,Comments,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy)
VALUES($id,$hid,$date,$shift,$lp,$tot,$com,$ca,$cb,$ua,$ub);", p=>{ p.AddWithValue("$id",r["BudgetHeaderID"]); p.AddWithValue("$hid",r["HandoverID"]); p.AddWithValue("$date",SafeBusinessDate(r,"ShiftDate")); p.AddWithValue("$shift",SafeText(r,"ShiftCode")); p.AddWithValue("$lp",SafeNumber(r,"LinesPlanned")); p.AddWithValue("$tot",SafeNumber(r,"TotalStaffOnRegister")); p.AddWithValue("$com",SafeText(r,"Comments")); p.AddWithValue("$ca",SafeDateIso(r,"CreatedAt")); p.AddWithValue("$cb",SafeText(r,"CreatedBy")); p.AddWithValue("$ua",SafeDateIso(r,"UpdatedAt")); p.AddWithValue("$ub",SafeText(r,"UpdatedBy"));});
    private static void InsertBudgetRow(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblBudgetRows(BudgetRowID,BudgetHeaderID,DeptName,PlannedQty,UsedQty,VarianceQty,ReasonText,UpdatedAt,UpdatedBy)
VALUES($id,$hid,$name,$pl,$us,$var,$reason,$ua,$ub);", p=>{ p.AddWithValue("$id",r["BudgetRowID"]); p.AddWithValue("$hid",r["BudgetHeaderID"]); p.AddWithValue("$name",SafeText(r,"DeptName")); p.AddWithValue("$pl",SafeNumber(r,"PlannedQty")); p.AddWithValue("$us",SafeNumber(r,"UsedQty")); p.AddWithValue("$var",SafeNumber(r,"VarianceQty")); p.AddWithValue("$reason",SafeText(r,"ReasonText")); p.AddWithValue("$ua",SafeDateIso(r,"UpdatedAt")); p.AddWithValue("$ub",SafeText(r,"UpdatedBy"));});
    private static void InsertDepartment(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblDepartments(DeptName,DisplayOrder,IsMetricDept,IsClosureDept,IsActive)
VALUES($name,$ord,$metric,$closure,$active);", p=>{ p.AddWithValue("$name",SafeText(r,"DeptName")); p.AddWithValue("$ord",SafeNumber(r,"DisplayOrder") ?? 0); p.AddWithValue("$metric",SafeBool01(r,"IsMetricDept")); p.AddWithValue("$closure",SafeBool01(r,"IsClosureDept")); p.AddWithValue("$active",SafeBool01(r,"IsActive",1));});
    private static void InsertShiftRule(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblShiftRules(ShiftCode,ShiftName,EmailProfileKey,DisplayOrder)
VALUES($code,$name,$profile,$order);", p=>{ p.AddWithValue("$code",SafeText(r,"ShiftCode")); p.AddWithValue("$name",SafeText(r,"ShiftName")); p.AddWithValue("$profile",SafeText(r,"EmailProfileKey")); p.AddWithValue("$order",SafeNumber(r,"DisplayOrder") ?? 0);});
    private static void InsertEmailProfile(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblEmailProfiles(EmailProfileKey,ToList,CcList,SubjectTemplate,BodyTemplate,IsActive)
VALUES($key,$to,$cc,$sub,$body,$active);", p=>{ p.AddWithValue("$key",SafeText(r,"EmailProfileKey")); p.AddWithValue("$to",SafeText(r,"ToList")); p.AddWithValue("$cc",SafeText(r,"CcList")); p.AddWithValue("$sub",SafeText(r,"SubjectTemplate")); p.AddWithValue("$body",SafeText(r,"BodyTemplate")); p.AddWithValue("$active",SafeBool01(r,"IsActive",1));});
    private static void InsertConfig(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblConfig(ConfigKey,ConfigValue,Notes)
VALUES($key,$value,$notes);", p=>{ var key=SafeText(r,"ConfigKey") ?? ""; p.AddWithValue("$key", key); p.AddWithValue("$value", TransformConfig(key, SafeText(r,"ConfigValue") ?? "")); p.AddWithValue("$notes", SafeText(r,"Notes"));});
    private static void InsertAuditLog(SqliteConnection c, SqliteTransaction tx, DataRow r) => Exec(c, tx, @"INSERT INTO tblAuditLog(AuditID,EventAt,UserName,EntityType,EntityKey,ActionType,Details)
VALUES($id,$at,$user,$type,$key,$action,$details);", p=>{ p.AddWithValue("$id",r["AuditID"]); p.AddWithValue("$at",SafeDateIso(r,"EventAt")); p.AddWithValue("$user",SafeText(r,"UserName")); p.AddWithValue("$type",SafeText(r,"EntityType")); p.AddWithValue("$key",SafeText(r,"EntityKey")); p.AddWithValue("$action",SafeText(r,"ActionType")); p.AddWithValue("$details",SafeText(r,"Details"));});

    private static Dictionary<string, string> BuildConfigSummary(SqliteConnection c)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT ConfigKey, ConfigValue FROM tblConfig;";
        using var r = cmd.ExecuteReader();
        while (r.Read()) d[Convert.ToString(r[0]) ?? string.Empty] = Convert.ToString(r[1]) ?? string.Empty;
        return d;
    }

    private static void Exec(SqliteConnection c, SqliteTransaction tx, string sql, Action<SqliteParameterCollection> fill) { using var cmd=c.CreateCommand(); cmd.Transaction=tx; cmd.CommandText=sql; fill(cmd.Parameters); cmd.ExecuteNonQuery(); }
    private static string? Txt(object v) => v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
    private static string? SafeToString(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToString(r[c], CultureInfo.InvariantCulture) : null;
    private static object? N(DataRow r, string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? r[c] : null;
    private static string? Iso(object v) => v == DBNull.Value ? null : v is DateTime dt ? dt.ToString("O", CultureInfo.InvariantCulture) : Convert.ToString(v, CultureInfo.InvariantCulture);
    private static string? BusinessDate(object v) => v == DBNull.Value ? null : v is DateTime dt ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : Convert.ToString(v, CultureInfo.InvariantCulture);
    private static int Bool01(DataRow r, string c, int defaultValue=0) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? (Convert.ToBoolean(r[c], CultureInfo.InvariantCulture) ? 1 : 0) : defaultValue;
    private static string? SafeText(DataRow r, string c) => r.Table.Columns.Contains(c) ? Txt(r[c]) : null;
    private static object? SafeNumber(DataRow r, string c) => r.Table.Columns.Contains(c) ? N(r, c) : null;
    private static string? SafeDateIso(DataRow r, string c) => r.Table.Columns.Contains(c) ? Iso(r[c]) : null;
    private static string? SafeBusinessDate(DataRow r, string c) => r.Table.Columns.Contains(c) ? BusinessDate(r[c]) : null;
    private static int SafeBool01(DataRow r, string c, int defaultValue = 0) => r.Table.Columns.Contains(c) ? Bool01(r, c, defaultValue) : defaultValue;

    private static string TransformConfig(string key, string value)
    {
        const string root = @"M:\Moat House\MoatHouse Handover";
        return key switch
        {
            "databaseProvider" => "SQLite",
            "dataRoot" => root,
            "sqliteDatabasePath" => Path.Combine(root, "Data", "moat-house.db"),
            "attachmentsRoot" => Path.Combine(root, "Attachments"),
            "reportsOutputRoot" => Path.Combine(root, "Reports"),
            "backupsRoot" => Path.Combine(root, "Backups"),
            "logsRoot" => Path.Combine(root, "Logs"),
            "configRoot" => Path.Combine(root, "Config"),
            "importsRoot" => Path.Combine(root, "Imports"),
            "migrationRoot" => Path.Combine(root, "Migration"),
            _ => value.Replace("C:/MOAT-Handover/shared", root, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static void EnsureRequiredConfig(SqliteConnection c, SqliteTransaction tx)
    {
        const string root = @"M:\Moat House\MoatHouse Handover";
        var required = new Dictionary<string, string>
        {
            ["databaseProvider"] = "SQLite",
            ["dataRoot"] = root,
            ["sqliteDatabasePath"] = Path.Combine(root, "Data", "moat-house.db"),
            ["attachmentsRoot"] = Path.Combine(root, "Attachments"),
            ["reportsOutputRoot"] = Path.Combine(root, "Reports"),
            ["backupsRoot"] = Path.Combine(root, "Backups"),
            ["logsRoot"] = Path.Combine(root, "Logs"),
            ["configRoot"] = Path.Combine(root, "Config"),
            ["importsRoot"] = Path.Combine(root, "Imports"),
            ["migrationRoot"] = Path.Combine(root, "Migration")
        };
        foreach (var kv in required)
            Exec(c, tx, "INSERT OR REPLACE INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ($k,$v,'Phase 5 required key');", p => { p.AddWithValue("$k", kv.Key); p.AddWithValue("$v", kv.Value); });
    }

    private static void EnsureRequiredDepartments(SqliteConnection c, SqliteTransaction tx)
    {
        foreach (var dep in RequiredDepartments)
            Exec(c, tx, @"INSERT OR IGNORE INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive)
VALUES ($name, $order, $metric, 0, 1);", p => { p.AddWithValue("$name", dep.Name); p.AddWithValue("$order", dep.Order); p.AddWithValue("$metric", dep.IsMetric); });
    }
}
