using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MoatHouseHandover.Host.Migration;

public sealed class AccessMigrationReader
{
    public static readonly string[] SupportedTables = ["tblHandoverHeader", "tblHandoverDept", "tblAttachments", "tblBudgetHeader", "tblBudgetRows", "tblDepartments", "tblShiftRules", "tblEmailProfiles", "tblConfig", "tblAuditLog"];
    private static readonly Dictionary<string, string[]> RequiredColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tblHandoverHeader"] = ["HandoverID", "ShiftDate", "ShiftCode", "SessionStatus"],
        ["tblHandoverDept"] = ["DeptRecordID", "HandoverID", "DeptName"],
        ["tblAttachments"] = ["AttachmentID", "HandoverID", "DeptRecordID"],
        ["tblBudgetHeader"] = ["BudgetHeaderID", "HandoverID"],
        ["tblBudgetRows"] = ["BudgetRowID", "BudgetHeaderID"],
        ["tblDepartments"] = ["DeptName"],
        ["tblShiftRules"] = ["ShiftCode"],
        ["tblEmailProfiles"] = ["EmailProfileKey"],
        ["tblConfig"] = ["ConfigKey", "ConfigValue"],
        ["tblAuditLog"] = ["AuditID", "EventAt"]
    };

    private static readonly Dictionary<string, string[]> OptionalColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tblBudgetHeader"] = ["Comments", "CreatedAt", "UpdatedAt", "TotalStaffOnRegister", "LinesPlanned"],
        ["tblAttachments"] = ["CapturedOn", "Notes"],
        ["tblAuditLog"] = ["Details"],
        ["tblHandoverHeader"] = ["CreatedAt", "UpdatedAt"],
        ["tblHandoverDept"] = ["CreatedAt", "UpdatedAt"]
    };

    public MigrationReadResult ReadAll(string accessPath)
    {
        var issues = new List<MigrationIssue>();
        var tables = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        using var conn = new OleDbConnection(AccessBootstrapper.BuildConnectionString(accessPath));
        conn.Open();

        foreach (var table in SupportedTables)
        {
            if (!TableExists(conn, table))
            {
                issues.Add(new MigrationIssue("source.table.missing", MigrationSeverity.Error, $"Required Access table '{table}' is missing.", Table: table));
                continue;
            }

            using var cmd = new OleDbCommand($"SELECT * FROM [{table}]", conn);
            using var adapter = new OleDbDataAdapter(cmd);
            var dt = new DataTable(table);
            adapter.Fill(dt);
            tables[table] = dt;
            counts[table] = dt.Rows.Count;
            ValidateColumns(table, dt.Columns, issues);
        }

        return new MigrationReadResult(tables, counts, issues);
    }

    private static bool TableExists(OleDbConnection conn, string table)
    {
        var schema = conn.GetSchema("Tables", new[] { null, null, table, "TABLE" });
        return schema.Rows.Count > 0;
    }

    private static void ValidateColumns(string table, DataColumnCollection columns, List<MigrationIssue> issues)
    {
        if (RequiredColumns.TryGetValue(table, out var required))
        {
            foreach (var col in required)
            {
                if (!columns.Contains(col))
                    issues.Add(new MigrationIssue("source.column.required.missing", MigrationSeverity.Error, $"Required column '{col}' missing in {table}.", Table: table));
            }
        }

        if (OptionalColumns.TryGetValue(table, out var optional))
        {
            foreach (var col in optional)
            {
                if (!columns.Contains(col))
                    issues.Add(new MigrationIssue("source.column.optional.missing", MigrationSeverity.Warning, $"Optional column '{col}' missing in {table}.", Table: table));
            }
        }
    }
}
