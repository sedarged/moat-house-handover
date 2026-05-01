using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MoatHouseHandover.Host.Backup;

public sealed class RestoreService
{
    private readonly BackupService _backup;
    private readonly RestorePlanner _planner;
    private readonly HostRuntimeStatus _runtime;
    private readonly AppPathResolution _paths;
    public RestoreService(BackupService backup, RestorePlanner planner, HostRuntimeStatus runtime, AppPathResolution paths){_backup=backup;_planner=planner;_runtime=runtime;_paths=paths;}

    public RestoreResult Execute(RestoreOptions options)
    {
        var plan = _planner.Plan(options.BackupFolder);
        var issues = plan.Issues.ToList();
        if (!plan.CanProceed) return WriteReport(options, BackupOperationStatus.Failed, issues);
        if (options.Mode == RestoreMode.ValidateOnly) return WriteReport(options, BackupOperationStatus.Success, issues);
        if (options.Mode == RestoreMode.RestoreToLive)
        {
            foreach (var file in plan.Files)
            {
                if (!IsApprovedLiveTarget(file.SourcePath))
                {
                    issues.Add(new BackupIssue(BackupSeverity.Error, "restore.target.outside_approved_root", "Restore target is outside approved runtime paths.", file.SourcePath));
                }
            }

            if (issues.Any(i => i.Severity == BackupSeverity.Error)) return WriteReport(options, BackupOperationStatus.Failed, issues);

            var pre = _backup.CreateBackup(new BackupOptions(BackupKind.PreRestore, options.Actor));
            if (pre.Status == BackupOperationStatus.Failed) return WriteReport(options, BackupOperationStatus.Failed, [..issues, ..pre.Issues]);
        }

        var stageRoot = options.Mode == RestoreMode.RestoreToStaging ? (options.StagingRoot ?? Path.Combine(_paths.Paths.Migration, "restore-staging", DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"))) : null;
        if (stageRoot is not null)
        {
            var fullStage = Path.GetFullPath(stageRoot);
            var approvedStageBase = Path.GetFullPath(_paths.Paths.Migration);
            if (!fullStage.StartsWith(approvedStageBase, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new BackupIssue(BackupSeverity.Error, "restore.staging.outside_migration", "Staging root must be within migration path.", fullStage));
                return WriteReport(options, BackupOperationStatus.Failed, issues);
            }
            Directory.CreateDirectory(stageRoot);
        }

        foreach (var file in plan.Files)
        {
            var src = Path.Combine(options.BackupFolder, file.RelativePath);
            var dest = options.Mode == RestoreMode.RestoreToStaging
                ? Path.Combine(stageRoot!, Path.GetFileName(file.SourcePath))
                : file.SourcePath;
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(src, dest, true);
        }
        return WriteReport(options, BackupOperationStatus.Success, issues);
    }

    private bool IsApprovedLiveTarget(string sourcePath)
    {
        var target = Path.GetFullPath(sourcePath);
        var approvedRoot = Path.GetFullPath(AppPathService.ApprovedDataRoot);
        var accessPath = Path.GetFullPath(_runtime.AccessDatabasePath);
        var sqlitePath = Path.GetFullPath(_runtime.TargetSqlitePath);
        var migrationPath = Path.GetFullPath(_paths.Paths.Migration);

        if (target.StartsWith(approvedRoot, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(target, accessPath, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(target, sqlitePath, StringComparison.OrdinalIgnoreCase)) return true;
        if (target.StartsWith(migrationPath, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private RestoreResult WriteReport(RestoreOptions options, BackupOperationStatus status, IReadOnlyList<BackupIssue> issues)
    {
        var ts = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        var json = Path.Combine(_paths.Paths.Migration, $"restore_{ts}.json");
        var txt = Path.Combine(_paths.Paths.Migration, $"restore_{ts}.txt");
        File.WriteAllText(json, JsonSerializer.Serialize(new { options.Mode, options.BackupFolder, status, issues }, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllLines(txt, new[] { $"Mode: {options.Mode}", $"Backup: {options.BackupFolder}", $"Status: {status}", $"Issues: {issues.Count}" });
        return new(status, options.Mode, json, txt, issues);
    }
}
