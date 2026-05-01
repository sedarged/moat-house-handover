using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace MoatHouseHandover.Host.DualRun;

public sealed class DualRunReportWriter
{
    public (string JsonPath, string TextPath) Write(DualRunResult result)
    {
        Directory.CreateDirectory(result.Options.ReportOutputFolder);
        var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        var jsonPath = Path.Combine(result.Options.ReportOutputFolder, $"dualrun_{stamp}.json");
        var txtPath = Path.Combine(result.Options.ReportOutputFolder, $"dualrun_{stamp}.txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        var sb = new StringBuilder();
        sb.AppendLine("Dual-run Access vs SQLite verification");
        sb.AppendLine($"Runtime provider: {result.ActiveRuntimeProvider}");
        sb.AppendLine($"Access DB: {result.Options.AccessDatabasePath}");
        sb.AppendLine($"SQLite DB: {result.Options.SqliteDatabasePath}");
        sb.AppendLine($"Mode: {result.Options.VerificationMode}");
        sb.AppendLine($"Session: {result.Options.SessionId?.ToString() ?? "(none)"} | Shift: {result.Options.ShiftCode} | Date: {result.Options.ShiftDate:yyyy-MM-dd}");
        var byStatus = result.RepositoryResults.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count());
        sb.AppendLine($"Counts: match={byStatus.GetValueOrDefault(DualRunComparisonStatus.Match)} warning={byStatus.GetValueOrDefault(DualRunComparisonStatus.MatchWithWarnings)} mismatch={byStatus.GetValueOrDefault(DualRunComparisonStatus.Mismatch)} skipped={byStatus.GetValueOrDefault(DualRunComparisonStatus.Skipped)} failed={byStatus.GetValueOrDefault(DualRunComparisonStatus.Failed)}");
        sb.AppendLine($"Recommendation: {result.Recommendation}");
        sb.AppendLine($"Windows/M drive status: {result.WindowsAndMDriveStatus}");
        foreach (var rr in result.RepositoryResults)
        {
            sb.AppendLine($"- {rr.Repository}.{rr.Method}: {rr.Status} ({rr.DiffSummary})");
        }

        File.WriteAllText(txtPath, sb.ToString());
        return (jsonPath, txtPath);
    }
}
