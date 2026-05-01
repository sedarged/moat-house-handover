using System;
using System.Collections.Generic;

namespace MoatHouseHandover.Host.DualRun;

public enum DualRunVerificationMode
{
    ReadOnlyExistingData,
    PostMigrationReadOnly,
    FixtureOnlyFuture
}

public enum DualRunComparisonStatus
{
    Match,
    MatchWithWarnings,
    Mismatch,
    Skipped,
    Failed
}

public enum DualRunSeverity
{
    Info,
    Warning,
    Error
}

public enum DualRunRecommendation
{
    ReadyForRuntimeSwitchCandidate,
    NotReadyMismatchFound,
    NotReadyVerificationIncomplete,
    BlockedEnvironment
}

public sealed record DualRunOptions(
    string AccessDatabasePath,
    string SqliteDatabasePath,
    string ApprovedDataRoot,
    string ReportOutputFolder,
    long? SessionId,
    DateTime? ShiftDate,
    string ShiftCode,
    string UserName,
    IReadOnlyList<string> SelectedDepartments,
    DualRunVerificationMode VerificationMode,
    int AuditLimit = 50,
    bool NormalizePathSeparators = true);

public sealed record DualRunIssue(
    string Repository,
    string Method,
    string Path,
    DualRunSeverity Severity,
    string Message,
    string? AccessValue,
    string? SqliteValue);

public sealed record DualRunRepositoryResult(
    string Repository,
    string Method,
    DualRunComparisonStatus Status,
    string? AccessPayload,
    string? SqlitePayload,
    string DiffSummary,
    IReadOnlyList<DualRunIssue> Issues);

public sealed record DualRunResult(
    string ActiveRuntimeProvider,
    DualRunOptions Options,
    DateTime StartedAtUtc,
    DateTime FinishedAtUtc,
    IReadOnlyList<DualRunRepositoryResult> RepositoryResults,
    DualRunRecommendation Recommendation,
    string WindowsAndMDriveStatus);
