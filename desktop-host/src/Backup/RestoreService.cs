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
            var pre = _backup.CreateBackup(new BackupOptions(BackupKind.PreRestore, options.Actor));
            if (pre.Status == BackupOperationStatus.Failed) return WriteReport(options, BackupOperationStatus.Failed, [..issues, ..pre.Issues]);
        }

        var stageRoot = options.Mode == RestoreMode.RestoreToStaging ? (options.StagingRoot ?? Path.Combine(_paths.Paths.Migration, "restore-staging", DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"))) : null;
        if (stageRoot is not null) Directory.CreateDirectory(stageRoot);

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
