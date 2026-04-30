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
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions{WriteIndented=true}));
        var sb = new StringBuilder();
        sb.AppendLine($"Mode: {report.Options.Mode}");
        sb.AppendLine($"Source: {report.Options.Paths.SourceAccessPath}");
        sb.AppendLine($"Target: {report.Options.Paths.TargetSqlitePath}");
        sb.AppendLine($"Staging: {report.Options.Paths.StagingSqlitePath}");
        sb.AppendLine($"Status: {report.Status}");
        foreach (var t in report.Tables) sb.AppendLine($"{t.SourceTable}: src={t.SourceRows} imported={t.ImportedRows} failed={t.FailedRows}");
        foreach (var i in report.Validation.Issues) sb.AppendLine($"[{i.Severity}] {i.Code}: {i.Message} {i.Detail}");
        File.WriteAllText(txtPath, sb.ToString());
        return (jsonPath, txtPath);
    }
}
