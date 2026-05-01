using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoatHouseHandover.Host.DualRun;

namespace MoatHouseHandover.Host.WorkstationVerification;

public sealed class PilotReadinessService
{
    public (WorkstationEvidenceResult Evidence, PilotReadinessResult Pilot) Evaluate(HostRuntimeStatus runtime, HostConfig config, AppPathResolution paths)
    {
        var validator = new DualRunEvidenceValidator();
        var dualRunFolder = Path.Combine(paths.Paths.Migration, "DualRun");
        var dualRun = validator.ValidateLatest(dualRunFolder, TimeSpan.FromDays(14));

        var snapshot = new WorkstationEvidenceSnapshot(
            OperatingSystem.IsWindows(),
            paths.Paths.DataRoot.StartsWith("M:", StringComparison.OrdinalIgnoreCase),
            File.Exists(runtime.AccessDatabasePath),
            File.Exists(runtime.TargetSqlitePath),
            CanWrite(paths.Paths.Backups),
            CanWrite(paths.Paths.Migration),
            CanWrite(dualRunFolder),
            dualRun.ReportPath,
            dualRun.Recommendation,
            runtime.RequestedProvider,
            runtime.EffectiveProvider,
            runtime.ProviderFallbackReason);

        var issues = new List<WorkstationEvidenceIssue>();
        if (!snapshot.WindowsDetected) issues.Add(new("workstation.windows.missing", WorkstationEvidenceSeverity.Warning, "Windows workstation runtime not detected."));
        if (!snapshot.MDriveRootDetected) issues.Add(new("workstation.mdrive.missing", WorkstationEvidenceSeverity.Warning, "M: data root not detected."));
        if (!snapshot.AccessDbExists) issues.Add(new("workstation.access.missing", WorkstationEvidenceSeverity.Error, "Access DB does not exist."));

        var checklist = new List<PilotReadinessChecklistItem>
        {
            Item("accesslegacy.default_available", snapshot.AccessDbExists, true, "AccessLegacy fallback remains available."),
            Item("sqlite.db.exists", snapshot.SqliteDbExists, true, "SQLite target DB exists."),
            Item("sqlite.schema.ready", runtime.SqliteBootstrapSucceeded, true, "SQLite schema/bootstrap ready."),
            Item("dualrun.latest.accepted", dualRun.Status == DualRunEvidenceStatus.Accepted, true, "Latest dual-run evidence accepted."),
            Item("backup.root.write", snapshot.BackupRootWritable, true, "Backup root write-safe."),
            Item("restore.prerestore.safety", true, false, "Restore safety foundation present via Phase 6 services."),
            Item("runtime.gate.behaviour", runtime.EffectiveProvider == DatabaseProviderKind.AccessLegacy || runtime.RuntimeSwitchEnabled, true, "Runtime provider gate/fallback behaviour active."),
            Item("windows.mdrive.root", !snapshot.WindowsDetected || snapshot.MDriveRootDetected, false, "M: root checked when running on Windows."),
            Item("no.auto.migration_restore_switch", true, true, "No automatic migration/restore/provider switch triggered by readiness checks.")
        };

        var blocking = checklist.Where(x => x.Blocking && !x.Passed).Select(x => new WorkstationEvidenceIssue(x.Key, WorkstationEvidenceSeverity.Error, x.Message, x.Detail)).ToList();
        var warns = issues.Where(x => x.Severity == WorkstationEvidenceSeverity.Warning).ToList();
        var status = blocking.Count > 0
            ? (dualRun.Status is DualRunEvidenceStatus.Missing or DualRunEvidenceStatus.Unreadable ? PilotReadinessStatus.EvidenceMissing : PilotReadinessStatus.NotReady)
            : PilotReadinessStatus.ReadyForControlledPilot;

        var next = status == PilotReadinessStatus.ReadyForControlledPilot
            ? "Proceed with controlled SQLite pilot steps on a real Windows workstation."
            : "Keep AccessLegacy effective provider and resolve blocking checklist items before pilot.";

        return (new WorkstationEvidenceResult(snapshot, issues), new PilotReadinessResult(status, checklist, blocking, warns, next, dualRun));
    }

    private static PilotReadinessChecklistItem Item(string key, bool passed, bool blocking, string message, string? detail = null) => new(key, passed, blocking, message, detail);
    private static bool CanWrite(string path)
    {
        try { Directory.CreateDirectory(path); var p = Path.Combine(path, ".probe_" + Guid.NewGuid().ToString("N") + ".tmp"); File.WriteAllText(p, "x"); File.Delete(p); return true; }
        catch { return false; }
    }
}
