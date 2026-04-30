using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MoatHouseHandover.Host.Migration;

public sealed class AccessMigrationReader
{
    public static readonly string[] SupportedTables = ["tblHandoverHeader", "tblHandoverDept", "tblAttachments", "tblBudgetHeader", "tblBudgetRows", "tblDepartments", "tblShiftRules", "tblEmailProfiles", "tblConfig", "tblAuditLog"];

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
        }

        return new MigrationReadResult(tables, counts, issues);
    }

    private static bool TableExists(OleDbConnection conn, string table)
    {
        var schema = conn.GetSchema("Tables", new[] { null, null, table, "TABLE" });
        return schema.Rows.Count > 0;
    }
}
