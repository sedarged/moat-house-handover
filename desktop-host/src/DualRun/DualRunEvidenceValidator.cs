using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MoatHouseHandover.Host.DualRun;

public enum DualRunEvidenceStatus { Accepted, Missing, Unreadable, NotAccepted, Stale, FailedChecks }
public sealed record DualRunEvidenceIssue(string Code, string Message, string? Detail = null);
public sealed record DualRunEvidenceValidationResult(DualRunEvidenceStatus Status, string? ReportPath, string? Recommendation, DateTime? ReportTimestampUtc, bool FailedCountZero, bool MismatchCountZero, bool AccessPathRecorded, bool SqlitePathRecorded, bool ActiveProviderAccessLegacy, IReadOnlyList<DualRunEvidenceIssue> Issues);

public sealed class DualRunEvidenceValidator
{
    public DualRunEvidenceValidationResult ValidateLatest(string dualRunRoot, TimeSpan maxAge)
    {
        if (!Directory.Exists(dualRunRoot)) return new(DualRunEvidenceStatus.Missing, null, null, null, false, false, false, false, false, new[] { new DualRunEvidenceIssue("dualrun.folder.missing", "Dual-run report folder not found.", dualRunRoot) });
        var latest = Directory.GetFiles(dualRunRoot, "dualrun_*.json").Select(x => new FileInfo(x)).OrderByDescending(x => x.LastWriteTimeUtc).ThenByDescending(x => x.Name).FirstOrDefault();
        if (latest is null) return new(DualRunEvidenceStatus.Missing, null, null, null, false, false, false, false, false, new[] { new DualRunEvidenceIssue("dualrun.report.missing", "No dual-run JSON reports were found.") });

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(latest.FullName));
            var root = doc.RootElement;
            var issues = new List<DualRunEvidenceIssue>();

            var recommendation = ReadToken(root, "Recommendation");
            var accepted = IsRecommendationAccepted(recommendation);
            if (!accepted) issues.Add(new("dualrun.recommendation.not_accepted", "Recommendation is not accepted.", recommendation));

            var failedCount = CountByStatus(root, DualRunComparisonStatus.Failed);
            var mismatchCount = CountByStatus(root, DualRunComparisonStatus.Mismatch);
            var failedZero = failedCount == 0;
            var mismatchZero = mismatchCount == 0;
            if (!failedZero) issues.Add(new("dualrun.failed.present", "Failed comparison entries exist.", failedCount.ToString()));
            if (!mismatchZero) issues.Add(new("dualrun.mismatch.present", "Mismatch comparison entries exist.", mismatchCount.ToString()));

            var accessRecorded = !string.IsNullOrWhiteSpace(ReadToken(root, "Options", "AccessDatabasePath"));
            var sqliteRecorded = !string.IsNullOrWhiteSpace(ReadToken(root, "Options", "SqliteDatabasePath"));
            if (!accessRecorded) issues.Add(new("dualrun.access.path.missing", "Access DB path is not recorded."));
            if (!sqliteRecorded) issues.Add(new("dualrun.sqlite.path.missing", "SQLite DB path is not recorded."));

            var accessLegacy = string.Equals(ReadToken(root, "ActiveRuntimeProvider"), "AccessLegacy", StringComparison.OrdinalIgnoreCase);
            if (!accessLegacy) issues.Add(new("dualrun.active_provider.invalid", "ActiveRuntimeProvider must be AccessLegacy."));

            var stale = (DateTime.UtcNow - latest.LastWriteTimeUtc) > maxAge;
            if (stale) issues.Add(new("dualrun.stale", "Dual-run report is older than allowed age.", latest.LastWriteTimeUtc.ToString("O")));

            var status = issues.Count == 0 ? DualRunEvidenceStatus.Accepted : stale ? DualRunEvidenceStatus.Stale : !accepted ? DualRunEvidenceStatus.NotAccepted : DualRunEvidenceStatus.FailedChecks;
            return new(status, latest.FullName, recommendation, latest.LastWriteTimeUtc, failedZero, mismatchZero, accessRecorded, sqliteRecorded, accessLegacy, issues);
        }
        catch (Exception ex)
        {
            return new(DualRunEvidenceStatus.Unreadable, latest.FullName, null, latest.LastWriteTimeUtc, false, false, false, false, false, new[] { new DualRunEvidenceIssue("dualrun.report.unreadable", "Dual-run report is unreadable.", ex.Message) });
        }
    }

    private static int CountByStatus(JsonElement root, DualRunComparisonStatus status)
        => !root.TryGetProperty("RepositoryResults", out var rr) || rr.ValueKind != JsonValueKind.Array ? 0 : rr.EnumerateArray().Count(x => IsStatusMatch(x, status));

    private static bool IsStatusMatch(JsonElement item, DualRunComparisonStatus status)
    {
        if (!item.TryGetProperty("Status", out var s)) return false;
        if (s.ValueKind == JsonValueKind.Number && s.TryGetInt32(out var n)) return n == (int)status;
        var token = s.ValueKind == JsonValueKind.String ? s.GetString() : s.ToString();
        return string.Equals(token, status.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadToken(JsonElement element, params string[] path)
    {
        var cur = element;
        foreach (var segment in path) if (!cur.TryGetProperty(segment, out cur)) return null;
        return cur.ValueKind == JsonValueKind.String ? cur.GetString() : cur.ToString();
    }

    private static bool IsRecommendationAccepted(string? token)
        => string.Equals(token, DualRunRecommendation.ReadyForRuntimeSwitchCandidate.ToString(), StringComparison.OrdinalIgnoreCase)
           || (int.TryParse(token, out var n) && n == (int)DualRunRecommendation.ReadyForRuntimeSwitchCandidate);
}
