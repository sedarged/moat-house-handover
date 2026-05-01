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
        var sqliteSession = new SqliteSessionRepository(options.SqliteDatabasePath, options.ReportOutputFolder);

        var sessionA = accessSession.OpenExistingSession(options.ShiftCode, options.ShiftDate ?? DateTime.Today, options.UserName);
        var sessionS = sqliteSession.OpenExistingSession(options.ShiftCode, options.ShiftDate ?? DateTime.Today, options.UserName);
        results.Add(Compare("SessionRepository", "OpenExistingSession", sessionA, sessionS, options.NormalizePathSeparators));

        if (sessionA is not null && sessionS is not null)
        {
            var accessDept = new DepartmentRepository(options.AccessDatabasePath);
            var sqliteDept = new SqliteDepartmentRepository(options.SqliteDatabasePath, options.ReportOutputFolder);
            foreach (var dept in options.SelectedDepartments.DefaultIfEmpty("Injection"))
            {
                results.Add(Compare("DepartmentRepository", "LoadDepartment", accessDept.LoadDepartment(sessionA.SessionId, dept), sqliteDept.LoadDepartment(sessionS.SessionId, dept), options.NormalizePathSeparators));
            }

            var accessBudget = new BudgetRepository(options.AccessDatabasePath);
            var sqliteBudget = new SqliteBudgetRepository(options.SqliteDatabasePath, options.ReportOutputFolder);
            results.Add(Compare("BudgetRepository", "LoadBudgetSummary", accessBudget.LoadBudgetSummary(sessionA.SessionId), sqliteBudget.LoadBudgetSummary(sessionS.SessionId), options.NormalizePathSeparators));
            results.Add(Skipped("BudgetRepository", "LoadBudget", "Skipped — mutating method not allowed in Phase 8 read-only dual-run."));

            var accessPreview = new PreviewRepository(options.AccessDatabasePath);
            var sqlitePreview = new SqlitePreviewRepository(options.SqliteDatabasePath, options.ReportOutputFolder);
            results.Add(Compare("PreviewRepository", "LoadPreview", accessPreview.LoadPreview(sessionA.SessionId), sqlitePreview.LoadPreview(sessionS.SessionId), options.NormalizePathSeparators));

            var accessAttachment = new AttachmentRepository(options.AccessDatabasePath);
            var sqliteAttachment = new SqliteAttachmentRepository(options.SqliteDatabasePath, options.ReportOutputFolder);
            foreach (var dept in sessionA.Departments.Take(3))
            {
                var accessDeptPayload = accessDept.LoadDepartment(sessionA.SessionId, dept.DeptName);
                var sqliteDeptPayload = sqliteDept.LoadDepartment(sessionS.SessionId, dept.DeptName);
                var la = accessAttachment.ListAttachments(sessionA.SessionId, accessDeptPayload.DeptRecordId, dept.DeptName);
                var ls = sqliteAttachment.ListAttachments(sessionS.SessionId, sqliteDeptPayload.DeptRecordId, dept.DeptName);
                results.Add(Compare("AttachmentRepository", "ListAttachments", la, ls, options.NormalizePathSeparators));
            }
        }

        var accessEmail = new EmailProfileRepository(options.AccessDatabasePath);
        var sqliteEmail = new SqliteEmailProfileRepository(options.SqliteDatabasePath, options.ReportOutputFolder);
        foreach (var shift in new[] { "AM", "PM", "NS" })
        {
            results.Add(Compare("EmailProfileRepository", $"LoadByShiftCode({shift})", accessEmail.LoadByShiftCode(shift), sqliteEmail.LoadByShiftCode(shift), options.NormalizePathSeparators));
        }

        var accessAudit = new AuditLogRepository(options.AccessDatabasePath);
        var sqliteAudit = new SqliteAuditLogRepository(options.SqliteDatabasePath, options.ReportOutputFolder);
        results.Add(Compare("AuditLogRepository", "ListRecent", accessAudit.ListRecent(options.AuditLimit), sqliteAudit.ListRecent(options.AuditLimit), options.NormalizePathSeparators, warningOnly: true));

        var recommendation = results.Any(r => r.Status is DualRunComparisonStatus.Mismatch or DualRunComparisonStatus.Failed)
            ? DualRunRecommendation.NotReadyMismatchFound
            : results.Any(r => r.Status == DualRunComparisonStatus.Skipped)
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

    private static DualRunRepositoryResult Skipped(string repo, string method, string reason)
        => new(repo, method, DualRunComparisonStatus.Skipped, null, null, reason, new[] { new DualRunIssue(repo, method, "method", DualRunSeverity.Info, reason, null, null) });

    private static string Trim(string value) => value.Length > 1200 ? value[..1200] + "...<trimmed>" : value;
}
