using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MoatHouseHandover.Host.DualRun;

public enum DualRunEvidenceStatus
{
    Accepted,
    Missing,
    Unreadable,
    NotAccepted,
    Stale,
    FailedChecks
}

public sealed record DualRunEvidenceIssue(string Code, string Message, string? Detail = null);

public sealed record DualRunEvidenceValidationResult(
    DualRunEvidenceStatus Status,
    string? ReportPath,
    string? Recommendation,
    DateTime? ReportTimestampUtc,
    bool FailedCountZero,
    bool MismatchCountZero,
    bool AccessPathRecorded,
    bool SqlitePathRecorded,
    bool ActiveProviderAccessLegacy,
    IReadOnlyList<DualRunEvidenceIssue> Issues);

public sealed class DualRunEvidenceValidator
{
    public DualRunEvidenceValidationResult ValidateLatest(string dualRunRoot, TimeSpan maxAge)
    {
        if (!Directory.Exists(dualRunRoot))
        {
            return new(DualRunEvidenceStatus.Missing, null, null, null, false, false, false, false, false, new[] { new DualRunEvidenceIssue("dualrun.folder.missing", "Dual-run report folder not found.", dualRunRoot) });
        }

        var latest = Directory.GetFiles(dualRunRoot, "dualrun_*.json").Select(x => new FileInfo(x)).OrderByDescending(x => x.LastWriteTimeUtc).ThenByDescending(x => x.Name).FirstOrDefault();
        if (latest is null)
        {
            return new(DualRunEvidenceStatus.Missing, null, null, null, false, false, false, false, false, new[] { new DualRunEvidenceIssue("dualrun.report.missing", "No dual-run JSON reports were found.") });
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(latest.FullName));
            var root = doc.RootElement;
            var issues = new List<DualRunEvidenceIssue>();

            var recommendation = ReadString(root, "Recommendation");
            var accepted = string.Equals(recommendation, DualRunRecommendation.ReadyForRuntimeSwitchCandidate.ToString(), StringComparison.Ordinal);
            if (!accepted) issues.Add(new("dualrun.recommendation.not_accepted", "Recommendation is not accepted.", recommendation));

            var failedCount = CountByStatus(root, "Failed");
            var mismatchCount = CountByStatus(root, "Mismatch");
            var failedZero = failedCount == 0;
            var mismatchZero = mismatchCount == 0;
            if (!failedZero) issues.Add(new("dualrun.failed.present", "Failed comparison entries exist.", failedCount.ToString()));
            if (!mismatchZero) issues.Add(new("dualrun.mismatch.present", "Mismatch comparison entries exist.", mismatchCount.ToString()));

            var accessPath = ReadString(root, "Options", "AccessDatabasePath");
            var sqlitePath = ReadString(root, "Options", "SqliteDatabasePath");
            var accessRecorded = !string.IsNullOrWhiteSpace(accessPath);
            var sqliteRecorded = !string.IsNullOrWhiteSpace(sqlitePath);
            if (!accessRecorded) issues.Add(new("dualrun.access.path.missing", "Access DB path is not recorded."));
            if (!sqliteRecorded) issues.Add(new("dualrun.sqlite.path.missing", "SQLite DB path is not recorded."));

            var activeProvider = ReadString(root, "ActiveRuntimeProvider");
            var accessLegacy = string.Equals(activeProvider, "AccessLegacy", StringComparison.OrdinalIgnoreCase);
            if (!accessLegacy) issues.Add(new("dualrun.active_provider.invalid", "ActiveRuntimeProvider must be AccessLegacy.", activeProvider));

            var reportAt = latest.LastWriteTimeUtc;
            var stale = (DateTime.UtcNow - reportAt) > maxAge;
            if (stale) issues.Add(new("dualrun.stale", "Dual-run report is older than allowed age.", reportAt.ToString("O")));

            var status = DetermineStatus(issues, stale, accepted);
            return new(status, latest.FullName, recommendation, reportAt, failedZero, mismatchZero, accessRecorded, sqliteRecorded, accessLegacy, issues);
        }
        catch (Exception ex)
        {
            return new(DualRunEvidenceStatus.Unreadable, latest.FullName, null, latest.LastWriteTimeUtc, false, false, false, false, false, new[] { new DualRunEvidenceIssue("dualrun.report.unreadable", "Dual-run report is unreadable.", ex.Message) });
        }
    }

    private static DualRunEvidenceStatus DetermineStatus(List<DualRunEvidenceIssue> issues, bool stale, bool accepted)
    {
        if (issues.Count == 0) return DualRunEvidenceStatus.Accepted;
        if (stale) return DualRunEvidenceStatus.Stale;
        if (!accepted) return DualRunEvidenceStatus.NotAccepted;
        return DualRunEvidenceStatus.FailedChecks;
    }

    private static int CountByStatus(JsonElement root, string status)
    {
        if (!root.TryGetProperty("RepositoryResults", out var rr) || rr.ValueKind != JsonValueKind.Array) return 0;
        return rr.EnumerateArray().Count(x => string.Equals(ReadString(x, "Status"), status, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadString(JsonElement e, params string[] path)
    {
        var cur = e;
        foreach (var p in path)
        {
            if (!cur.TryGetProperty(p, out cur)) return null;
        }

        return cur.ValueKind == JsonValueKind.String ? cur.GetString() : cur.ToString();
    }
}
