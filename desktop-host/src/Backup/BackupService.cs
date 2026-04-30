using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MoatHouseHandover.Host.Backup;

public sealed class BackupService
{
    private readonly HostRuntimeStatus _runtime;
    private readonly AppPathResolution _paths;
    public BackupService(HostRuntimeStatus runtime, AppPathResolution paths) { _runtime = runtime; _paths = paths; }

    public BackupResult CreateBackup(BackupOptions options)
    {
        var issues = new List<BackupIssue>();
        if (!File.Exists(_runtime.AccessDatabasePath)) return new(BackupOperationStatus.Failed, "", "", "", [new(BackupSeverity.Error, "access.missing", "Access DB missing", _runtime.AccessDatabasePath)], null);

        var stamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupId = $"{stamp}_{options.Kind}";
        var root = Path.Combine(_paths.Paths.Backups, backupId);
        var dataDir = Path.Combine(root, "Data"); Directory.CreateDirectory(dataDir);
        var migDir = Path.Combine(root, "Migration"); Directory.CreateDirectory(migDir);
        var files = new List<BackupFileEntry>();

        AddFile(_runtime.AccessDatabasePath, Path.Combine(dataDir, Path.GetFileName(_runtime.AccessDatabasePath)), true, files, issues);
        if (options.IncludeSqliteIfPresent && File.Exists(_runtime.TargetSqlitePath)) AddFile(_runtime.TargetSqlitePath, Path.Combine(dataDir, Path.GetFileName(_runtime.TargetSqlitePath)), false, files, issues);
        if (options.IncludeLatestMigrationReports && Directory.Exists(_paths.Paths.Migration))
        {
            foreach (var f in Directory.GetFiles(_paths.Paths.Migration, "migration_*.*").OrderByDescending(File.GetLastWriteTimeUtc).Take(2))
                AddFile(f, Path.Combine(migDir, Path.GetFileName(f)), false, files, issues);
        }

        var manifest = new BackupManifest(backupId, options.Kind, DateTimeOffset.UtcNow, options.Actor ?? Environment.UserName, _paths.Paths.Backups, files);
        var manifestPath = Path.Combine(root, "manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        var summaryPath = Path.Combine(root, "summary.txt");
        var summaryLines = new List<string> { $"Backup: {backupId}", $"Kind: {options.Kind}", $"CreatedUtc: {manifest.CreatedUtc:O}", $"Files: {files.Count}" };
        summaryLines.AddRange(files.Select(f => $" - {f.RelativePath} ({f.SizeBytes} bytes)"));
        File.WriteAllLines(summaryPath, summaryLines);

        var status = issues.Any(i => i.Severity == BackupSeverity.Error) ? BackupOperationStatus.Failed : issues.Count > 0 ? BackupOperationStatus.SuccessWithWarnings : BackupOperationStatus.Success;
        return new(status, root, manifestPath, summaryPath, issues, manifest);
    }

    private static void AddFile(string src, string dest, bool required, List<BackupFileEntry> files, List<BackupIssue> issues)
    {
        if (!File.Exists(src)) { if (required) issues.Add(new(BackupSeverity.Error, "file.missing", "Required source missing", src)); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(src, dest, true);
        var fi = new FileInfo(dest);
        files.Add(new(Path.GetRelativePath(Path.GetDirectoryName(Path.GetDirectoryName(dest)!)!, dest), src, fi.Length, Sha256(dest), required));
    }
    public static string Sha256(string file){ using var s=File.OpenRead(file); return Convert.ToHexString(SHA256.HashData(s)); }
}
