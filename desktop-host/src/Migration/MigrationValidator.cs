using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Migration;

public sealed class MigrationValidator
{
    private static readonly string[] RequiredTables = ["tblHandoverHeader","tblHandoverDept","tblAttachments","tblBudgetHeader","tblBudgetRows","tblDepartments","tblShiftRules","tblEmailProfiles","tblConfig","tblAuditLog","tblSchemaMigrations"];

    public MigrationValidationResult Validate(string sqlitePath, IReadOnlyList<MigrationTableResult> tableResults, IReadOnlyList<MigrationIssue> importIssues)
    {
        var issues = new List<MigrationIssue>(importIssues);
        using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sqlitePath }.ToString());
        conn.Open();

        var journal = ScalarText(conn, "PRAGMA journal_mode;");
        if (string.Equals(journal, "wal", StringComparison.OrdinalIgnoreCase))
            issues.Add(new MigrationIssue("sqlite.journal.wal", MigrationSeverity.Error, "Journal mode must not be WAL."));

        foreach (var t in RequiredTables)
            if (ScalarInt(conn, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$n;", ("$n", t)) == 0)
                issues.Add(new MigrationIssue("sqlite.table.missing", MigrationSeverity.Error, $"Required target table '{t}' is missing.", Table: t));

        if (ScalarInt(conn, "SELECT COUNT(*) FROM tblSchemaMigrations WHERE MigrationId='001_initial_sqlite_schema';") == 0)
            issues.Add(new MigrationIssue("sqlite.migration.missing", MigrationSeverity.Error, "Migration marker 001_initial_sqlite_schema missing."));

        foreach (var tr in tableResults)
        {
            if (tr.FailedRows > 0 || tr.SkippedRows > 0)
                issues.Add(new MigrationIssue("import.table.partial", MigrationSeverity.Warning, $"Table {tr.SourceTable} has failed/skipped rows.", $"failed={tr.FailedRows}; skipped={tr.SkippedRows}", tr.SourceTable));
            if (tr.ImportedRows + tr.FailedRows + tr.SkippedRows != tr.SourceRows)
                issues.Add(new MigrationIssue("import.table.count_mismatch", MigrationSeverity.Error, $"Row accounting mismatch for {tr.SourceTable}.", Table: tr.SourceTable));
        }

        var orphanDept = ScalarInt(conn, "SELECT COUNT(*) FROM tblHandoverDept d LEFT JOIN tblHandoverHeader h ON h.HandoverID=d.HandoverID WHERE h.HandoverID IS NULL;");
        var orphanAttachments = ScalarInt(conn, "SELECT COUNT(*) FROM tblAttachments a LEFT JOIN tblHandoverDept d ON d.DeptRecordID=a.DeptRecordID WHERE d.DeptRecordID IS NULL;");
        var orphanBudgetHeader = ScalarInt(conn, "SELECT COUNT(*) FROM tblBudgetHeader b LEFT JOIN tblHandoverHeader h ON h.HandoverID=b.HandoverID WHERE h.HandoverID IS NULL;");
        var orphanBudgetRows = ScalarInt(conn, "SELECT COUNT(*) FROM tblBudgetRows r LEFT JOIN tblBudgetHeader h ON h.BudgetHeaderID=r.BudgetHeaderID WHERE h.BudgetHeaderID IS NULL;");
        if (orphanDept > 0) issues.Add(new MigrationIssue("fk.orphan.handover_dept", MigrationSeverity.Error, "Orphan tblHandoverDept rows found.", orphanDept.ToString(CultureInfo.InvariantCulture)));
        if (orphanAttachments > 0) issues.Add(new MigrationIssue("fk.orphan.attachments", MigrationSeverity.Warning, "Orphan tblAttachments rows found.", orphanAttachments.ToString(CultureInfo.InvariantCulture)));
        if (orphanBudgetHeader > 0) issues.Add(new MigrationIssue("fk.orphan.budget_header", MigrationSeverity.Error, "Orphan tblBudgetHeader rows found.", orphanBudgetHeader.ToString(CultureInfo.InvariantCulture)));
        if (orphanBudgetRows > 0) issues.Add(new MigrationIssue("fk.orphan.budget_rows", MigrationSeverity.Error, "Orphan tblBudgetRows rows found.", orphanBudgetRows.ToString(CultureInfo.InvariantCulture)));

        var varianceMismatch = ScalarInt(conn, "SELECT COUNT(*) FROM tblBudgetRows WHERE ABS(COALESCE(VarianceQty,0) - (COALESCE(UsedQty,0)-COALESCE(PlannedQty,0))) > 0.000001;");
        if (varianceMismatch > 0) issues.Add(new MigrationIssue("budget.variance.mismatch", MigrationSeverity.Warning, "Budget variance mismatches found.", varianceMismatch.ToString(CultureInfo.InvariantCulture)));

        if (ScalarInt(conn, "SELECT COUNT(*) FROM tblShiftRules WHERE ShiftCode IN ('AM','PM','NS');") < 3)
            issues.Add(new MigrationIssue("shift.rules.required", MigrationSeverity.Error, "Shift rules AM/PM/NS are incomplete."));
        if (ScalarInt(conn, "SELECT COUNT(*) FROM tblDepartments WHERE IsActive=1;") < 13)
            issues.Add(new MigrationIssue("departments.required", MigrationSeverity.Error, "Active departments are less than 13."));

        var badConfigPaths = ScalarInt(conn, "SELECT COUNT(*) FROM tblConfig WHERE ConfigValue LIKE 'C:/MOAT-Handover/shared%';");
        if (badConfigPaths > 0) issues.Add(new MigrationIssue("config.path.policy", MigrationSeverity.Error, "Legacy C:/ config paths remain after transform."));

        return new MigrationValidationResult(issues, varianceMismatch, orphanAttachments, orphanDept, orphanBudgetHeader, orphanBudgetRows, journal);
    }

    static int ScalarInt(SqliteConnection c, string sql, params (string, object)[] p) { using var cmd=c.CreateCommand(); cmd.CommandText=sql; foreach(var pair in p) cmd.Parameters.AddWithValue(pair.Item1,pair.Item2); return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture); }
    static string ScalarText(SqliteConnection c, string sql) { using var cmd=c.CreateCommand(); cmd.CommandText=sql; return Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty; }
}
