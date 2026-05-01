using System;
using System.Collections.Generic;

namespace MoatHouseHandover.Host.Backup;

public enum BackupKind { Manual, PreMigration, PreRestore, ScheduledFuture }
public enum BackupOperationStatus { Success, SuccessWithWarnings, Failed }
public enum BackupSeverity { Warning, Error }
public enum RestoreMode { ValidateOnly, RestoreToStaging, RestoreToLive }

public sealed record BackupOptions(BackupKind Kind, string? Actor, bool IncludeSqliteIfPresent = true, bool IncludeLatestMigrationReports = true);
public sealed record BackupFileEntry(string RelativePath, string SourcePath, long SizeBytes, string Sha256, bool Required);
public sealed record BackupIssue(BackupSeverity Severity, string Code, string Message, string? Path = null);
public sealed record BackupManifest(string BackupId, BackupKind Kind, DateTimeOffset CreatedUtc, string Actor, string BackupRoot, IReadOnlyList<BackupFileEntry> Files);
public sealed record BackupResult(BackupOperationStatus Status, string BackupFolder, string ManifestPath, string SummaryPath, IReadOnlyList<BackupIssue> Issues, BackupManifest? Manifest);
public sealed record BackupValidationResult(bool IsValid, IReadOnlyList<BackupIssue> Issues);
public sealed record RestoreOptions(string BackupFolder, RestoreMode Mode, string? Actor, string? StagingRoot = null);
public sealed record RestorePlan(bool CanProceed, string ManifestPath, IReadOnlyList<BackupFileEntry> Files, IReadOnlyList<string> OverwriteTargets, IReadOnlyList<BackupIssue> Issues);
public sealed record RestoreResult(BackupOperationStatus Status, RestoreMode Mode, string ReportJsonPath, string ReportTxtPath, IReadOnlyList<BackupIssue> Issues);
public sealed record BackupRetentionPolicy(int KeepDays, bool DeleteEnabled);
