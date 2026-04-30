using System;
using System.Collections.Generic;
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
        var src = _reader.ReadAll(options.Paths.SourceAccessPath);
        var tableResults = _writer.Import(options, src);
        var validation = _validator.Validate(options.Paths.StagingSqlitePath, tableResults);
        var finished = DateTimeOffset.UtcNow;
        var status = options.Mode == MigrationMode.DryRun ? MigrationFinalStatus.DryRunOnly : validation.HasErrors ? MigrationFinalStatus.Failed : MigrationFinalStatus.Success;
        var report = new MigrationReport(options, finished, status, tableResults, validation, validation.HasErrors ? "Review errors and rerun dry-run." : "Proceed to runtime Phase 6 foundations.", BuildConfigSummary(src));
        var paths = _reports.Write(report);
        var promoted = false;
        if (options.Mode == MigrationMode.Execute && !validation.HasErrors)
        {
            var backup = options.Paths.TargetSqlitePath + ".pre-migration-" + finished.ToString("yyyyMMddHHmmss") + ".bak";
            if (File.Exists(options.Paths.TargetSqlitePath)) File.Copy(options.Paths.TargetSqlitePath, backup, true);
            File.Copy(options.Paths.StagingSqlitePath, options.Paths.TargetSqlitePath, true);
            promoted = true;
        }
        return new MigrationResult(report, paths.jsonPath, paths.txtPath, promoted);
    }

    static Dictionary<string,string> BuildConfigSummary(System.Collections.Generic.IReadOnlyDictionary<string, System.Data.DataTable> src)
    {
        var d = new Dictionary<string, string>();
        d["databaseProvider"] = "SQLite"; d["dataRoot"] = @"M:\Moat House\MoatHouse Handover"; d["sqliteDatabasePath"] = @"M:\Moat House\MoatHouse Handover\Data\moat-house.db";
        return d;
    }
}
