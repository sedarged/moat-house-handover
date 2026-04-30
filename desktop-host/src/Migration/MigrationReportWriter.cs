using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MoatHouseHandover.Host.Migration;

public sealed class MigrationReportWriter
{
    public (string jsonPath, string txtPath) Write(MigrationReport report)
    {
        Directory.CreateDirectory(report.Options.Paths.ReportDirectory);
        var stamp = report.Options.StartedAtUtc.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        var jsonPath = Path.Combine(report.Options.Paths.ReportDirectory, $"migration_{stamp}.json");
        var txtPath = Path.Combine(report.Options.Paths.ReportDirectory, $"migration_{stamp}.txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        var sb = new StringBuilder();
        sb.AppendLine("Access-to-SQLite Migration Report");
        sb.AppendLine($"Mode: {report.Options.Mode}");
        sb.AppendLine($"Actor: {report.Options.Actor}");
        sb.AppendLine($"Started: {report.Options.StartedAtUtc:O}");
        sb.AppendLine($"Finished: {report.FinishedAtUtc:O}");
        sb.AppendLine($"Source Access: {report.Options.Paths.SourceAccessPath}");
        sb.AppendLine($"Target SQLite: {report.Options.Paths.TargetSqlitePath}");
        sb.AppendLine($"Staging SQLite: {report.Options.Paths.StagingSqlitePath}");
        sb.AppendLine($"Report Directory: {report.Options.Paths.ReportDirectory}");
        sb.AppendLine($"Status: {report.Status}");
        sb.AppendLine($"Promoted: {report.TargetPromoted}");
        sb.AppendLine($"Backup Path: {report.BackupPath ?? "(none)"}");
        sb.AppendLine($"Journal Mode: {report.Validation.JournalMode}");
        sb.AppendLine($"Budget variance mismatches: {report.Validation.BudgetVarianceMismatchCount}");
        sb.AppendLine($"Orphans -> Attachments:{report.Validation.OrphanAttachmentCount}, HandoverDept:{report.Validation.OrphanHandoverDeptCount}, BudgetHeader:{report.Validation.OrphanBudgetHeaderCount}, BudgetRows:{report.Validation.OrphanBudgetRowsCount}");
        sb.AppendLine();
        sb.AppendLine("Table Results");
        foreach (var t in report.Tables)
            sb.AppendLine($"- {t.SourceTable}: source={t.SourceRows}, imported={t.ImportedRows}, skipped={t.SkippedRows}, failed={t.FailedRows}");
        sb.AppendLine();
        sb.AppendLine("Validation Issues");
        foreach (var i in report.Validation.Issues)
            sb.AppendLine($"- [{i.Severity}] {i.Code} | table={i.Table} | row={i.RowIdentifier} | {i.Message} | {i.Detail}");
        sb.AppendLine();
        sb.AppendLine("Config Summary");
        foreach (var kv in report.ConfigTransformSummary)
            sb.AppendLine($"- {kv.Key} = {kv.Value}");
        sb.AppendLine();
        sb.AppendLine($"Next action: {report.NextRecommendedAction}");
        File.WriteAllText(txtPath, sb.ToString());
        return (jsonPath, txtPath);
    }
}
