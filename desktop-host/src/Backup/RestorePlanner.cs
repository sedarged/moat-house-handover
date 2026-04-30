using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MoatHouseHandover.Host.Backup;

public sealed class RestorePlanner
{
    private readonly HostRuntimeStatus _runtime;
    public RestorePlanner(HostRuntimeStatus runtime) { _runtime = runtime; }

    public RestorePlan Plan(string backupFolder)
    {
        var issues = new List<BackupIssue>();
        var manifestPath = Path.Combine(backupFolder, "manifest.json");
        if (!File.Exists(manifestPath)) return new(false, manifestPath, [], [], [new(BackupSeverity.Error, "manifest.missing", "manifest.json missing", manifestPath)]);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(manifestPath));
        if (manifest is null) return new(false, manifestPath, [], [], [new(BackupSeverity.Error, "manifest.invalid", "manifest unreadable", manifestPath)]);
        var overwrites = new List<string>();
        foreach (var file in manifest.Files)
        {
            var full = Path.Combine(backupFolder, file.RelativePath);
            if (!File.Exists(full)) issues.Add(new(BackupSeverity.Error, "backup.file_missing", "File missing from backup", full));
            else
            {
                var fi = new FileInfo(full);
                if (fi.Length != file.SizeBytes) issues.Add(new(BackupSeverity.Error, "backup.size_mismatch", "Backup size mismatch", full));
                if (!string.Equals(BackupService.Sha256(full), file.Sha256, StringComparison.OrdinalIgnoreCase)) issues.Add(new(BackupSeverity.Error, "backup.hash_mismatch", "Backup checksum mismatch", full));
            }
            if (file.SourcePath == _runtime.AccessDatabasePath || file.SourcePath == _runtime.TargetSqlitePath) overwrites.Add(file.SourcePath);
        }
        return new(!issues.Any(i => i.Severity == BackupSeverity.Error), manifestPath, manifest.Files, overwrites, issues);
    }
}
