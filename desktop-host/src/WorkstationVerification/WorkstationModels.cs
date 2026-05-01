using System;
using System.Collections.Generic;
using MoatHouseHandover.Host.DualRun;

namespace MoatHouseHandover.Host.WorkstationVerification;

public enum WorkstationEvidenceSeverity { Info, Warning, Error }
public sealed record WorkstationEvidenceIssue(string Code, WorkstationEvidenceSeverity Severity, string Message, string? Detail = null);
public sealed record WorkstationEvidenceSnapshot(
    bool WindowsDetected,
    bool MDriveRootDetected,
    bool AccessDbExists,
    bool SqliteDbExists,
    bool BackupRootWritable,
    bool MigrationRootWritable,
    bool DualRunFolderWritable,
    string? LatestDualRunReportPath,
    string? LatestDualRunRecommendation,
    DatabaseProviderKind RuntimeRequestedProvider,
    DatabaseProviderKind RuntimeEffectiveProvider,
    string? FallbackStatus);
public sealed record WorkstationEvidenceResult(WorkstationEvidenceSnapshot Snapshot, IReadOnlyList<WorkstationEvidenceIssue> Issues);

public enum PilotReadinessStatus { ReadyForControlledPilot, NotReady, Blocked, EvidenceMissing }
public sealed record PilotReadinessChecklistItem(string Key, bool Passed, bool Blocking, string Message, string? Detail = null);
public sealed record PilotReadinessResult(PilotReadinessStatus Status, IReadOnlyList<PilotReadinessChecklistItem> ChecklistItems, IReadOnlyList<WorkstationEvidenceIssue> BlockingIssues, IReadOnlyList<WorkstationEvidenceIssue> Warnings, string RecommendedNextAction, DualRunEvidenceValidationResult DualRunEvidence);
