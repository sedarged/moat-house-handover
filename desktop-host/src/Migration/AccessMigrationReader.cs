using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace MoatHouseHandover.Host.Migration;

public sealed class AccessMigrationReader
{
    public static readonly string[] SupportedTables = ["tblHandoverHeader","tblHandoverDept","tblAttachments","tblBudgetHeader","tblBudgetRows","tblDepartments","tblShiftRules","tblEmailProfiles","tblConfig","tblAuditLog"];

    public Dictionary<string, DataTable> ReadAll(string accessPath)
    {
        var result = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
        using var conn = new OleDbConnection(AccessBootstrapper.BuildConnectionString(accessPath));
        conn.Open();
        foreach (var table in SupportedTables)
        {
            using var cmd = new OleDbCommand($"SELECT * FROM [{table}]", conn);
            using var adapter = new OleDbDataAdapter(cmd);
            var dt = new DataTable(table);
            adapter.Fill(dt);
            result[table] = dt;
        }
        return result;
    }
}
