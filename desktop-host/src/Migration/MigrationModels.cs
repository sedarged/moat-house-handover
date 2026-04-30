using System;
using System.Collections.Generic;
using System.Linq;

namespace MoatHouseHandover.Host.Migration;

public enum MigrationMode { DryRun = 1, Execute = 2 }
public enum MigrationSeverity { Info = 1, Warning = 2, Error = 3 }
public enum MigrationFinalStatus { Success = 1, SuccessWithWarnings = 2, Failed = 3, DryRunOnly = 4 }

public sealed record MigrationPaths(string SourceAccessPath, string TargetSqlitePath, string StagingSqlitePath, string ReportDirectory);
public sealed record MigrationOptions(MigrationMode Mode, MigrationPaths Paths, string Actor, DateTimeOffset StartedAtUtc);
public sealed record MigrationIssue(string Code, MigrationSeverity Severity, string Message, string? Detail = null, string? Table = null);
public sealed record MigrationTableResult(string SourceTable, string TargetTable, int SourceRows, int ImportedRows, int SkippedRows, int FailedRows);
public sealed record MigrationValidationResult(IReadOnlyList<MigrationIssue> Issues, int BudgetVarianceMismatchCount, int OrphanAttachmentCount, string JournalMode)
{
    public bool HasErrors => Issues.Any(i => i.Severity == MigrationSeverity.Error);
}

public sealed record MigrationReport(MigrationOptions Options, DateTimeOffset FinishedAtUtc, MigrationFinalStatus Status, IReadOnlyList<MigrationTableResult> Tables, MigrationValidationResult Validation, string NextRecommendedAction, Dictionary<string,string> ConfigTransformSummary);
public sealed record MigrationResult(MigrationReport Report, string JsonReportPath, string TextReportPath, bool TargetPromoted);
