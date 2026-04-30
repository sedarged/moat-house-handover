using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Migration;

public sealed class MigrationValidator
{
    public MigrationValidationResult Validate(string sqlitePath, IReadOnlyList<MigrationTableResult> tables)
    {
        var issues = new List<MigrationIssue>();
        var journal = string.Empty;
        using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sqlitePath }.ToString());
        conn.Open();
        using var j = conn.CreateCommand(); j.CommandText = "PRAGMA journal_mode;"; journal = Convert.ToString(j.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
        if (string.Equals(journal, "wal", StringComparison.OrdinalIgnoreCase)) issues.Add(new MigrationIssue("journal.wal", MigrationSeverity.Error, "Journal mode must not be WAL."));
        CheckCount(conn, issues, "tblSchemaMigrations", "MigrationId='001_initial_sqlite_schema'", "schema.marker");
        CheckCount(conn, issues, "tblShiftRules", "ShiftCode in ('AM','PM','NS')", "shift.rules");
        CheckCount(conn, issues, "tblDepartments", null, "departments.count", 13);
        return new MigrationValidationResult(issues, 0, 0, journal);
    }

    static void CheckCount(SqliteConnection conn, List<MigrationIssue> issues, string table, string? where, string code, int min=1)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table}" + (string.IsNullOrWhiteSpace(where) ? "" : " WHERE " + where);
        var count = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (count < min) issues.Add(new MigrationIssue(code, MigrationSeverity.Error, $"Validation failed for {table}.", $"count={count}"));
    }
}
