using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MoatHouseHandover.Host.DualRun;

public sealed record DualRunEvidenceRunRequest(
    string ShiftCode,
    DateTime ShiftDate,
    string UserName,
    IReadOnlyList<string> SelectedDepartments,
    string? DataRoot = null);

public sealed record DualRunEvidenceRunResult(
    bool Success,
    string JsonReportPath,
    string TextReportPath,
    DualRunRecommendation Recommendation,
    DualRunEvidenceStatus EvidenceStatus,
    int MatchCount,
    int WarningCount,
    int MismatchCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<string> Issues,
    string NextAction);

public sealed class DualRunEvidenceRunner
{
    private static readonly string[] DefaultDepartments = ["Injection", "MetaPress", "Slicing", "Goods In & Despatch"];

    public DualRunEvidenceRunResult Run(DualRunEvidenceRunRequest request)
    {
        var shiftCode = NormalizeShiftCode(request.ShiftCode);
        var departments = NormalizeDepartments(request.SelectedDepartments);
        var root = AppData.AppDataRootInitializer.BuildRoot(AppData.AppDataRootInitializer.ResolveDataRoot(request.DataRoot));

        var options = new DualRunOptions(
            AccessDatabasePath: root.AccessLegacyDatabasePath,
            SqliteDatabasePath: root.SqliteDatabasePath,
            ApprovedDataRoot: root.DataRoot,
            ReportOutputFolder: root.DualRunFolder,
            SessionId: null,
            ShiftDate: request.ShiftDate,
            ShiftCode: shiftCode,
            UserName: string.IsNullOrWhiteSpace(request.UserName) ? "dualrun" : request.UserName.Trim(),
            SelectedDepartments: departments,
            VerificationMode: DualRunVerificationMode.ReadOnlyExistingData);

        var comparison = new DualRunComparisonService().Run(options);
        var reportPaths = new DualRunReportWriter().Write(comparison);

        var evidence = new DualRunEvidenceValidator().ValidateLatest(options.ReportOutputFolder, TimeSpan.FromDays(14));
        var counts = comparison.RepositoryResults.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count());
        var issues = evidence.Issues.Select(i => $"{i.Code}: {i.Message}{(string.IsNullOrWhiteSpace(i.Detail) ? string.Empty : $" ({i.Detail})")}").ToList();
        var nextAction = evidence.Status == DualRunEvidenceStatus.Accepted
            ? "Keep runtimeProvider as AccessLegacy; proceed to Phase 10A controlled SQLite pilot run checklist."
            : "Resolve mismatches/failed checks and rerun workstation dual-run evidence before any SQLite pilot/default switch work.";

        return new DualRunEvidenceRunResult(
            Success: evidence.Status == DualRunEvidenceStatus.Accepted,
            JsonReportPath: reportPaths.JsonPath,
            TextReportPath: reportPaths.TextPath,
            Recommendation: comparison.Recommendation,
            EvidenceStatus: evidence.Status,
            MatchCount: counts.GetValueOrDefault(DualRunComparisonStatus.Match),
            WarningCount: counts.GetValueOrDefault(DualRunComparisonStatus.MatchWithWarnings),
            MismatchCount: counts.GetValueOrDefault(DualRunComparisonStatus.Mismatch),
            FailedCount: counts.GetValueOrDefault(DualRunComparisonStatus.Failed),
            SkippedCount: counts.GetValueOrDefault(DualRunComparisonStatus.Skipped),
            Issues: issues,
            NextAction: nextAction);
    }

    private static string NormalizeShiftCode(string value)
    {
        var code = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("ShiftCode is required.") : value.Trim().ToUpperInvariant();
        return code is "AM" or "PM" or "NS" ? code : throw new ArgumentException("ShiftCode must be AM, PM, or NS.");
    }

    private static IReadOnlyList<string> NormalizeDepartments(IReadOnlyList<string> input)
    {
        var source = input is { Count: > 0 } ? input : DefaultDepartments;
        var values = source.Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return values.Length > 0 ? values : DefaultDepartments;
    }
}
