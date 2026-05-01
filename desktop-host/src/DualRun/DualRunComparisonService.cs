using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MoatHouseHandover.Host.Sqlite.Repositories;

namespace MoatHouseHandover.Host.DualRun;

public sealed class DualRunComparisonService
{
    private readonly DualRunPayloadNormalizer _normalizer = new();

    public DualRunResult Run(DualRunOptions options)
    {
        var started = DateTime.UtcNow;
        var results = new List<DualRunRepositoryResult>();
        var accessSession = new SessionRepository(options.AccessDatabasePath);
        var sqliteSession = new SqliteSessionRepository(options.SqliteDatabasePath, options.ApprovedDataRoot);
        var accessDept = new DepartmentRepository(options.AccessDatabasePath);
        var sqliteDept = new SqliteDepartmentRepository(options.SqliteDatabasePath, options.ApprovedDataRoot);
        var accessBudget = new BudgetRepository(options.AccessDatabasePath);
        var sqliteBudget = new SqliteBudgetRepository(options.SqliteDatabasePath, options.ApprovedDataRoot);
        var accessPreview = new PreviewRepository(options.AccessDatabasePath);
        var sqlitePreview = new SqlitePreviewRepository(options.SqliteDatabasePath, options.ApprovedDataRoot);
        var accessAttachment = new AttachmentRepository(options.AccessDatabasePath);
        var sqliteAttachment = new SqliteAttachmentRepository(options.SqliteDatabasePath, options.ApprovedDataRoot);
        var accessEmail = new EmailProfileRepository(options.AccessDatabasePath);
        var sqliteEmail = new SqliteEmailProfileRepository(options.SqliteDatabasePath, options.ApprovedDataRoot);
        var accessAudit = new AuditLogRepository(options.AccessDatabasePath);
        var sqliteAudit = new SqliteAuditLogRepository(options.SqliteDatabasePath, options.ApprovedDataRoot);

        var sessionComparison = SafeCompare(
            "SessionRepository",
            "OpenExistingSession",
            () => accessSession.OpenExistingSession(options.ShiftCode, options.ShiftDate ?? DateTime.Today, options.UserName),
            () => sqliteSession.OpenExistingSession(options.ShiftCode, options.ShiftDate ?? DateTime.Today, options.UserName),
            options.NormalizePathSeparators);
        results.Add(sessionComparison);

        if (sessionComparison.Status is DualRunComparisonStatus.Match or DualRunComparisonStatus.MatchWithWarnings && sessionComparison.AccessPayload is not null && sessionComparison.SqlitePayload is not null)
        {
            var sessionA = accessSession.OpenExistingSession(options.ShiftCode, options.ShiftDate ?? DateTime.Today, options.UserName);
            var sessionS = sqliteSession.OpenExistingSession(options.ShiftCode, options.ShiftDate ?? DateTime.Today, options.UserName);
            if (sessionA is null || sessionS is null)
            {
                results.Add(Failed("SessionRepository", "OpenExistingSession", "Session payload was not available for follow-on repository comparisons."));
            }
            else
            {
            foreach (var dept in options.SelectedDepartments.DefaultIfEmpty("Injection"))
            {
                results.Add(SafeCompare("DepartmentRepository", $"LoadDepartment({dept})",
                    () => accessDept.LoadDepartment(sessionA.SessionId, dept),
                    () => sqliteDept.LoadDepartment(sessionS.SessionId, dept),
                    options.NormalizePathSeparators));
            }

            results.Add(SafeCompare("BudgetRepository", "LoadBudgetSummary",
                () => accessBudget.LoadBudgetSummary(sessionA.SessionId),
                () => sqliteBudget.LoadBudgetSummary(sessionS.SessionId),
                options.NormalizePathSeparators));
            results.Add(SkippedExpected("BudgetRepository", "LoadBudget", "Skipped — mutating method not allowed in Phase 8 read-only dual-run."));

            results.Add(SafeCompare("PreviewRepository", "LoadPreview",
                () => accessPreview.LoadPreview(sessionA.SessionId),
                () => sqlitePreview.LoadPreview(sessionS.SessionId),
                options.NormalizePathSeparators));
            foreach (var dept in sessionA.Departments.Take(3))
            {
                results.Add(SafeCompare("AttachmentRepository", $"ListAttachments({dept.DeptName})", () =>
                    {
                        var accessDeptPayload = accessDept.LoadDepartment(sessionA.SessionId, dept.DeptName);
                        return accessAttachment.ListAttachments(sessionA.SessionId, accessDeptPayload.DeptRecordId, dept.DeptName);
                    },
                    () =>
                    {
                        var sqliteDeptPayload = sqliteDept.LoadDepartment(sessionS.SessionId, dept.DeptName);
                        return sqliteAttachment.ListAttachments(sessionS.SessionId, sqliteDeptPayload.DeptRecordId, dept.DeptName);
                    },
                    options.NormalizePathSeparators));
            }
            }
        }

        foreach (var shift in new[] { "AM", "PM", "NS" })
        {
            results.Add(SafeCompare("EmailProfileRepository", $"LoadByShiftCode({shift})",
                () => accessEmail.LoadByShiftCode(shift),
                () => sqliteEmail.LoadByShiftCode(shift),
                options.NormalizePathSeparators));
        }

        results.Add(SafeCompare("AuditLogRepository", "ListRecent",
            () => accessAudit.ListRecent(options.AuditLimit),
            () => sqliteAudit.ListRecent(options.AuditLimit),
            options.NormalizePathSeparators,
            warningOnly: true));

        var recommendation = results.Any(r => r.Status == DualRunComparisonStatus.Failed)
            ? DualRunRecommendation.BlockedEnvironment
            : results.Any(r => r.Status is DualRunComparisonStatus.Mismatch)
            ? DualRunRecommendation.NotReadyMismatchFound
            : HasBlockingSkip(results)
                ? DualRunRecommendation.NotReadyVerificationIncomplete
                : DualRunRecommendation.ReadyForRuntimeSwitchCandidate;

        var winStatus = OperatingSystem.IsWindows() && options.ReportOutputFolder.StartsWith("M:", StringComparison.OrdinalIgnoreCase)
            ? "Windows+M drive path configured."
            : "Non-Windows and/or non-M drive validation path used.";

        return new DualRunResult("AccessLegacy", options, started, DateTime.UtcNow, results, recommendation, winStatus);
    }

    private DualRunRepositoryResult Compare(string repo, string method, object? accessPayload, object? sqlitePayload, bool normalizePaths, bool warningOnly = false)
    {
        var normalizedA = _normalizer.Normalize(accessPayload, normalizePaths);
        var normalizedS = _normalizer.Normalize(sqlitePayload, normalizePaths);
        var aJson = JsonSerializer.Serialize(normalizedA);
        var sJson = JsonSerializer.Serialize(normalizedS);
        if (aJson == sJson)
        {
            return new DualRunRepositoryResult(repo, method, DualRunComparisonStatus.Match, aJson, sJson, "Normalized payloads match.", Array.Empty<DualRunIssue>());
        }

        var issue = new DualRunIssue(repo, method, "payload", warningOnly ? DualRunSeverity.Warning : DualRunSeverity.Error, "Normalized payload mismatch.", Trim(aJson), Trim(sJson));
        return new DualRunRepositoryResult(repo, method, warningOnly ? DualRunComparisonStatus.MatchWithWarnings : DualRunComparisonStatus.Mismatch, aJson, sJson, "Payload mismatch detected.", new[] { issue });
    }


    private static DualRunRepositoryResult SkippedExpected(string repo, string method, string reason)
        => new(repo, method, DualRunComparisonStatus.Skipped, null, null, reason, new[] { new DualRunIssue(repo, method, "expected_skip", DualRunSeverity.Info, reason, null, null) });

    private static bool HasBlockingSkip(IReadOnlyList<DualRunRepositoryResult> results)
        => results.Any(r => r.Status == DualRunComparisonStatus.Skipped
            && !r.Issues.Any(i => string.Equals(i.Path, "expected_skip", StringComparison.OrdinalIgnoreCase)));

    private DualRunRepositoryResult SafeCompare(string repo, string method, Func<object?> accessCall, Func<object?> sqliteCall, bool normalizePaths, bool warningOnly = false)
    {
        try
        {
            var accessPayload = accessCall();
            var sqlitePayload = sqliteCall();
            return Compare(repo, method, accessPayload, sqlitePayload, normalizePaths, warningOnly);
        }
        catch (Exception ex)
        {
            return Failed(repo, method, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static DualRunRepositoryResult Failed(string repo, string method, string message)
        => new(
            repo,
            method,
            DualRunComparisonStatus.Failed,
            null,
            null,
            "Comparison failed due to repository exception.",
            new[] { new DualRunIssue(repo, method, "exception", DualRunSeverity.Error, message, null, null) });

    private static string Trim(string value) => value.Length > 1200 ? value[..1200] + "...<trimmed>" : value;
}
