using System;
using System.IO;
using System.Linq;

namespace MoatHouseHandover.Host.Migration;

public sealed class AccessToSqliteMigrationService
{
    private readonly AccessMigrationReader _reader = new();
    private readonly SqliteMigrationWriter _writer = new();
    private readonly MigrationValidator _validator = new();
    private readonly MigrationReportWriter _reports = new();

    public MigrationResult Run(MigrationOptions options)
    {
        var read = _reader.ReadAll(options.Paths.SourceAccessPath);
        var (tableResults, importIssues, configSummary) = _writer.Import(options, read);
        var validation = _validator.Validate(options.Paths.StagingSqlitePath, tableResults, read.SourceRowCounts, [..read.Issues, ..importIssues]);
        var finished = DateTimeOffset.UtcNow;

        var promoted = false;
        string? backupPath = null;
        if (options.Mode == MigrationMode.Execute && !validation.HasErrors)
        {
            if (File.Exists(options.Paths.TargetSqlitePath))
            {
                backupPath = options.Paths.TargetSqlitePath + ".pre-migration-" + finished.ToString("yyyyMMddHHmmss") + ".bak";
                File.Copy(options.Paths.TargetSqlitePath, backupPath, true);
            }

            File.Copy(options.Paths.StagingSqlitePath, options.Paths.TargetSqlitePath, true);
            promoted = true;
        }

        var status = options.Mode == MigrationMode.DryRun
            ? MigrationFinalStatus.DryRunOnly
            : validation.HasErrors
                ? MigrationFinalStatus.Failed
                : validation.Issues.Any(i => i.Severity == MigrationSeverity.Warning)
                    ? MigrationFinalStatus.SuccessWithWarnings
                    : MigrationFinalStatus.Success;

        var report = new MigrationReport(
            options,
            finished,
            status,
            tableResults,
            validation,
            validation.HasErrors ? "Fix validation errors and rerun dry-run." : "Proceed to Phase 6 backup/restore foundation.",
            configSummary,
            backupPath,
            promoted);

        var paths = _reports.Write(report);
        return new MigrationResult(report, paths.jsonPath, paths.txtPath, promoted, backupPath);
    }
}
