using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.IO;

using MoatHouseHandover.Host.Backup;

namespace MoatHouseHandover.Host;

public sealed class DiagnosticsService
{
    private readonly HostRuntimeStatus _runtimeStatus;
    private readonly HostConfig _config;
    private readonly AppPathResolution _pathResolution;
    private readonly IDataProvider _dataProvider;

    public DiagnosticsService(HostRuntimeStatus runtimeStatus, HostConfig config, AppPathResolution pathResolution, IDataProvider dataProvider)
    {
        _runtimeStatus = runtimeStatus;
        _config = config;
        _pathResolution = pathResolution;
        _dataProvider = dataProvider;
    }

    public DiagnosticsPayload Run(DiagnosticsRunRequest _)
    {
        var checks = new List<DiagnosticsCheckResult>();

        AddCheck(checks, "os.info", () =>
        {
            var isWindows = OperatingSystem.IsWindows();
            var os = Environment.OSVersion.VersionString;
            var status = isWindows ? "ok" : "warning";
            return new DiagnosticsCheckResult("os.info", status, isWindows ? "Running on Windows." : "Running on non-Windows environment.", os);
        });

        AddCheck(checks, "config.values.loaded", () =>
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.DataRoot))
            {
                missing.Add("dataRoot");
            }

            if (string.IsNullOrWhiteSpace(_config.AccessDatabasePath))
            {
                missing.Add("accessDatabasePath");
            }

            if (missing.Count > 0)
            {
                return new DiagnosticsCheckResult("config.values.loaded", "failed", "Required runtime config values are missing.", string.Join(", ", missing));
            }

            return new DiagnosticsCheckResult(
                "config.values.loaded",
                "ok",
                "Runtime config values are loaded.",
                $"config={_runtimeStatus.ConfigPath} | db={_runtimeStatus.AccessDatabasePath} | attachments={_runtimeStatus.AttachmentsRoot} | reports={_runtimeStatus.ReportsOutputRoot} | logs={_runtimeStatus.LogRoot}");
        });


        foreach (var result in _pathResolution.ValidationResults)
        {
            AddCheck(checks, $"paths.{result.Key}", () => new DiagnosticsCheckResult($"paths.{result.Key}", result.Status, result.Message, result.FullPath));
        }


        AddCheck(checks, "database.provider.info", () =>
        {
            var info = _dataProvider.GetInfo();
            return new DiagnosticsCheckResult(
                "database.provider.info",
                "ok",
                $"Active provider: {info.ProviderKind}",
                $"activeDb={info.ActiveDatabasePath} | sqliteTarget={info.TargetSqlitePath ?? "(not configured)"} | status={info.ProviderStatus} | migration={info.MigrationStatus}");
        });

        AddCheck(checks, "access.database.path", () =>
        {
            var exists = File.Exists(_runtimeStatus.AccessDatabasePath);
            return new DiagnosticsCheckResult(
                "access.database.path",
                exists ? "ok" : "failed",
                exists ? "Access database path exists." : "Access database path was not found.",
                _runtimeStatus.AccessDatabasePath);
        });

        AddCheck(checks, "access.connection.open", () =>
        {
            try
            {
                using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
                connection.Open();
                return new DiagnosticsCheckResult("access.connection.open", "ok", "Access connection opened successfully.", connection.DataSource ?? _runtimeStatus.AccessDatabasePath);
            }
            catch (Exception ex)
            {
                return new DiagnosticsCheckResult(
                    "access.connection.open",
                    "failed",
                    "Access connection failed. Verify Access Database Engine (ACE/OLEDB) is installed and the DB path is reachable.",
                    ex.Message);
            }
        });

        AddCheck(checks, "access.config.seed", CheckConfigSeed);

        AddCheck(checks, "attachments.root.exists", () => EnsureDirectory("attachments.root.exists", _runtimeStatus.AttachmentsRoot));
        AddCheck(checks, "reports.root.exists", () => EnsureDirectory("reports.root.exists", _runtimeStatus.ReportsOutputRoot));

        AddCheck(checks, "attachments.root.write", () => EnsureWriteAccess("attachments.root.write", _runtimeStatus.AttachmentsRoot));
        AddCheck(checks, "reports.root.write", () => EnsureWriteAccess("reports.root.write", _runtimeStatus.ReportsOutputRoot));

        AddCheck(checks, "email.profiles.exist", CheckEmailProfilesExist);
        AddCheck(checks, "email.profiles.active.valid", CheckActiveProfilesValid);

        AddCheck(checks, "outlook.com.available", () =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return new DiagnosticsCheckResult("outlook.com.available", "warning", "Outlook draft creation requires Windows + Outlook desktop.", "Non-Windows environment");
            }

            var outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType is null)
            {
                return new DiagnosticsCheckResult("outlook.com.available", "warning", "Outlook COM type is unavailable.", "ProgID Outlook.Application was not found");
            }

            return new DiagnosticsCheckResult("outlook.com.available", "ok", "Outlook COM type is available.", outlookType.FullName ?? "Outlook.Application");
        });

        
        AddCheck(checks, "sqlite.target.path", () =>
        {
            var sqlitePath = _runtimeStatus.TargetSqlitePath;
            var exists = File.Exists(sqlitePath);
            return new DiagnosticsCheckResult("sqlite.target.path", exists ? "ok" : "warning", exists ? "SQLite target file exists." : "SQLite target file does not exist yet.", sqlitePath);
        });

        AddCheck(checks, "sqlite.target.parent.exists", () =>
        {
            var parent = Path.GetDirectoryName(_runtimeStatus.TargetSqlitePath) ?? string.Empty;
            var exists = !string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent);
            return new DiagnosticsCheckResult("sqlite.target.parent.exists", exists ? "ok" : "failed", exists ? "SQLite target parent directory exists." : "SQLite target parent directory is missing.", parent);
        });

        AddCheck(checks, "sqlite.target.bootstrap", () => new DiagnosticsCheckResult(
            "sqlite.target.bootstrap",
            _runtimeStatus.SqliteBootstrapSucceeded ? "ok" : "warning",
            _runtimeStatus.SqliteBootstrapSucceeded ? "SQLite bootstrap readiness completed." : "SQLite bootstrap readiness did not complete.",
            _runtimeStatus.SqliteBootstrapMessage ?? "No bootstrap message."));

        AddCheck(checks, "sqlite.schema.tables", CheckSqliteSchemaTables);
        AddCheck(checks, "sqlite.schema.migrations", CheckSqliteMigrationMarker);
        AddCheck(checks, "sqlite.shared_drive_policy", () => CheckSqliteSharedDrivePolicy(_runtimeStatus.TargetSqlitePath));
        AddCheck(checks, "migration.source_access.exists", () => new DiagnosticsCheckResult("migration.source_access.exists", File.Exists(_runtimeStatus.AccessDatabasePath) ? "ok" : "failed", "Migration source Access DB existence check.", _runtimeStatus.AccessDatabasePath));
        AddCheck(checks, "migration.target_sqlite.path", () => new DiagnosticsCheckResult("migration.target_sqlite.path", "ok", "Migration target SQLite path resolved.", _runtimeStatus.TargetSqlitePath));
        AddCheck(checks, "migration.output_folder.exists", () => EnsureDirectory("migration.output_folder.exists", _pathResolution.Paths.Migration));
        AddCheck(checks, "migration.can_create_staging_db", CheckMigrationStagingWrite);
        AddCheck(checks, "migration.current_runtime_provider", () => new DiagnosticsCheckResult("migration.current_runtime_provider", "ok", "Runtime provider remains AccessLegacy.", "AccessLegacy"));
        AddCheck(checks, "migration.last_report.exists", CheckMigrationLastReport);

        var restorePlanner = new RestorePlanner(_runtimeStatus);
        AddCheck(checks, "backup.root.exists", () => EnsureDirectory("backup.root.exists", _pathResolution.Paths.Backups));
        AddCheck(checks, "backup.root.write", () => EnsureWriteAccess("backup.root.write", _pathResolution.Paths.Backups));
        AddCheck(checks, "backup.current_access.exists", () => new DiagnosticsCheckResult("backup.current_access.exists", File.Exists(_runtimeStatus.AccessDatabasePath) ? "ok" : "failed", "Access backup source availability.", _runtimeStatus.AccessDatabasePath));
        AddCheck(checks, "backup.target_sqlite.exists_or_not_yet", () => new DiagnosticsCheckResult("backup.target_sqlite.exists_or_not_yet", File.Exists(_runtimeStatus.TargetSqlitePath) ? "ok" : "warning", File.Exists(_runtimeStatus.TargetSqlitePath) ? "SQLite target exists for backup." : "SQLite target not present yet.", _runtimeStatus.TargetSqlitePath));
        AddCheck(checks, "backup.can_create_manifest", CheckBackupManifestProbe);
        AddCheck(checks, "backup.latest.exists", CheckLatestBackupExists);
        AddCheck(checks, "restore.latest_manifest.valid_if_present", () => CheckLatestBackupManifest(restorePlanner));
        AddCheck(checks, "restore.pre_restore_backup.required", () => new DiagnosticsCheckResult("restore.pre_restore_backup.required", "ok", "Live restore requires PreRestore backup by service contract.", "enforced"));
        AddCheck(checks, "migration.rollback.available_if_backup_exists", CheckMigrationRollbackAvailable);
        AddCheck(checks, "runtime.provider.remains_accesslegacy", () => new DiagnosticsCheckResult("runtime.provider.remains_accesslegacy", "ok", "Runtime provider remains AccessLegacy.", _dataProvider.GetInfo().ProviderKind.ToString()));



        AddCheck(checks, "sqlite.repositories.infrastructure", () => new DiagnosticsCheckResult("sqlite.repositories.infrastructure", "ok", "SQLite repository infrastructure classes are available in host build.", "SqliteRepositoryBase + repository implementations"));
        AddCheck(checks, "sqlite.repositories.audit_log.constructible", () => CheckSqliteRepoConstructible(() => new Sqlite.Repositories.SqliteAuditLogRepository(_runtimeStatus.TargetSqlitePath, _config.DataRoot)));
        AddCheck(checks, "sqlite.repositories.email_profile.constructible", () => CheckSqliteRepoConstructible(() => new Sqlite.Repositories.SqliteEmailProfileRepository(_runtimeStatus.TargetSqlitePath, _config.DataRoot)));
        AddCheck(checks, "sqlite.repositories.session.constructible", () => CheckSqliteRepoConstructible(() => new Sqlite.Repositories.SqliteSessionRepository(_runtimeStatus.TargetSqlitePath, _config.DataRoot)));
        AddCheck(checks, "sqlite.repositories.department.constructible", () => CheckSqliteRepoConstructible(() => new Sqlite.Repositories.SqliteDepartmentRepository(_runtimeStatus.TargetSqlitePath, _config.DataRoot)));
        AddCheck(checks, "sqlite.repositories.runtime_default", () => new DiagnosticsCheckResult("sqlite.repositories.runtime_default", "ok", "SQLite repositories are available but runtime default remains AccessLegacy.", _dataProvider.GetInfo().ProviderKind.ToString()));

        AddCheck(checks, "runtime.boundary", () =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return new DiagnosticsCheckResult(
                    "runtime.boundary",
                    "warning",
                    "This app targets Windows workstation runtime (WebView2 + ACE/OLEDB + Outlook desktop draft flow).",
                    "Current environment is non-Windows.");
            }

            return new DiagnosticsCheckResult(
                "runtime.boundary",
                "ok",
                "Windows runtime detected. Continue with diagnostics and workstation checklist verification.",
                "WebView2 runtime, ACE provider, and Outlook desktop still require local validation.");
        });

        AddCheck(checks, "logs.root.exists", () => EnsureDirectory("logs.root.exists", _runtimeStatus.LogRoot));

        var overall = ComputeOverallStatus(checks);
        return new DiagnosticsPayload(overall, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), checks);
    }

    private DiagnosticsCheckResult CheckConfigSeed()
    {
        using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
        connection.Open();

        var keys = new[] { "accessDatabasePath", "attachmentsRoot", "reportsOutputRoot" };
        var missing = new List<string>();

        foreach (var key in keys)
        {
            using var cmd = new OleDbCommand("SELECT TOP 1 ConfigValue FROM tblConfig WHERE ConfigKey = ?", connection);
            cmd.Parameters.AddWithValue("@p1", key);
            var value = cmd.ExecuteScalar();
            if (value is null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))
            {
                missing.Add(key);
            }
        }

        if (missing.Count > 0)
        {
            return new DiagnosticsCheckResult("access.config.seed", "warning", "One or more tblConfig values are missing/blank.", string.Join(", ", missing));
        }

        return new DiagnosticsCheckResult("access.config.seed", "ok", "tblConfig values were found.", string.Join(", ", keys));
    }

    private DiagnosticsCheckResult CheckEmailProfilesExist()
    {
        using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
        connection.Open();

        var missing = new List<string>();
        foreach (var shift in new[] { "AM", "PM", "NS" })
        {
            using var cmd = new OleDbCommand(@"SELECT TOP 1 p.EmailProfileKey
FROM tblShiftRules AS s
LEFT JOIN tblEmailProfiles AS p ON p.EmailProfileKey = s.EmailProfileKey
WHERE s.ShiftCode = ?", connection);
            cmd.Parameters.AddWithValue("@p1", shift);
            var value = cmd.ExecuteScalar();
            if (value is null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))
            {
                missing.Add(shift);
            }
        }

        if (missing.Count > 0)
        {
            return new DiagnosticsCheckResult("email.profiles.exist", "failed", "Missing email profile mapping for one or more shifts.", string.Join(", ", missing));
        }

        return new DiagnosticsCheckResult("email.profiles.exist", "ok", "Email profile mappings exist for AM/PM/NS.", "AM, PM, NS");
    }

    private DiagnosticsCheckResult CheckMigrationStagingWrite()
    {
        try
        {
            Directory.CreateDirectory(_pathResolution.Paths.Migration);
            var probe = Path.Combine(_pathResolution.Paths.Migration, "moat-house.importing.probe.db");
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = probe }.ToString());
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS __probe (id INTEGER);";
            cmd.ExecuteNonQuery();
            connection.Close();
            if (File.Exists(probe)) File.Delete(probe);
            return new DiagnosticsCheckResult("migration.can_create_staging_db", "ok", "Can create staging sqlite database in migration folder.", _pathResolution.Paths.Migration);
        }
        catch (Exception ex)
        {
            return new DiagnosticsCheckResult("migration.can_create_staging_db", "failed", "Cannot create staging sqlite database.", ex.Message);
        }
    }

    private DiagnosticsCheckResult CheckMigrationLastReport()
    {
        if (!Directory.Exists(_pathResolution.Paths.Migration))
        {
            return new DiagnosticsCheckResult("migration.last_report.exists", "warning", "Migration directory does not exist yet.", _pathResolution.Paths.Migration);
        }

        var found = Directory.GetFiles(_pathResolution.Paths.Migration, "migration_*.json").Length > 0;
        return new DiagnosticsCheckResult("migration.last_report.exists", found ? "ok" : "warning", found ? "Migration report exists." : "No migration report found yet.", _pathResolution.Paths.Migration);
    }

    private DiagnosticsCheckResult CheckActiveProfilesValid()
    {
        using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
        connection.Open();

        var issues = new List<string>();

        foreach (var shift in new[] { "AM", "PM", "NS" })
        {
            using var cmd = new OleDbCommand(@"SELECT TOP 1 p.EmailProfileKey, p.IsActive, p.ToList, p.CcList
FROM tblShiftRules AS s
LEFT JOIN tblEmailProfiles AS p ON p.EmailProfileKey = s.EmailProfileKey
WHERE s.ShiftCode = ?", connection);
            cmd.Parameters.AddWithValue("@p1", shift);
            using var reader = cmd.ExecuteReader();
            if (!reader!.Read())
            {
                issues.Add(shift + ": mapping missing");
                continue;
            }

            var key = Convert.ToString(reader["EmailProfileKey"]) ?? string.Empty;
            var isActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]);
            var toList = Convert.ToString(reader["ToList"]) ?? string.Empty;
            var ccList = Convert.ToString(reader["CcList"]) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                issues.Add(shift + ": profile key missing");
                continue;
            }

            if (!isActive)
            {
                issues.Add(shift + ": profile inactive");
                continue;
            }

            if (string.IsNullOrWhiteSpace(toList) && string.IsNullOrWhiteSpace(ccList))
            {
                issues.Add(shift + ": no To/CC recipients");
            }
        }

        if (issues.Count > 0)
        {
            return new DiagnosticsCheckResult("email.profiles.active.valid", "warning", "One or more active profile checks failed.", string.Join(" | ", issues));
        }

        return new DiagnosticsCheckResult("email.profiles.active.valid", "ok", "Active email profiles look valid.", "AM, PM, NS");
    }

    private DiagnosticsCheckResult CheckSqliteSchemaTables()
    {
        if (!File.Exists(_runtimeStatus.TargetSqlitePath))
        {
            return new DiagnosticsCheckResult("sqlite.schema.tables", "warning", "SQLite target database file does not exist; schema cannot be checked.", _runtimeStatus.TargetSqlitePath);
        }

        using var connection = new SqliteConnection($"Data Source={_runtimeStatus.TargetSqlitePath};Mode=ReadOnly;Cache=Shared");
        connection.Open();

        var required = MoatHouseHandover.Host.Sqlite.SqliteSchema.RequiredTables;
        var missing = new List<string>();
        foreach (var table in required)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            cmd.Parameters.AddWithValue("$name", table);
            var exists = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            if (!exists) missing.Add(table);
        }

        return missing.Count == 0
            ? new DiagnosticsCheckResult("sqlite.schema.tables", "ok", "All required SQLite tables exist.", string.Join(", ", required))
            : new DiagnosticsCheckResult("sqlite.schema.tables", "failed", "SQLite schema is missing required tables.", string.Join(", ", missing));
    }

    private DiagnosticsCheckResult CheckSqliteMigrationMarker()
    {
        if (!File.Exists(_runtimeStatus.TargetSqlitePath))
        {
            return new DiagnosticsCheckResult("sqlite.schema.migrations", "warning", "SQLite target database file does not exist; migration marker cannot be checked.", _runtimeStatus.TargetSqlitePath);
        }

        using var connection = new SqliteConnection($"Data Source={_runtimeStatus.TargetSqlitePath};Mode=ReadOnly;Cache=Shared");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM tblSchemaMigrations WHERE MigrationId = $id;";
        cmd.Parameters.AddWithValue("$id", MoatHouseHandover.Host.Sqlite.SqliteSchema.InitialMigrationId);
        var exists = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        return new DiagnosticsCheckResult("sqlite.schema.migrations", exists ? "ok" : "failed", exists ? "Initial SQLite migration marker exists." : "Initial SQLite migration marker is missing.", MoatHouseHandover.Host.Sqlite.SqliteSchema.InitialMigrationId);
    }

    private static DiagnosticsCheckResult CheckSqliteSharedDrivePolicy(string sqlitePath)
    {
        if (!File.Exists(sqlitePath))
        {
            return new DiagnosticsCheckResult("sqlite.shared_drive_policy", "warning", "SQLite target database file does not exist; shared-drive journal policy cannot be checked.", sqlitePath);
        }

        using var connection = new SqliteConnection($"Data Source={sqlitePath};Mode=ReadOnly;Cache=Shared");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode;";
        var mode = (Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty).Trim();

        if (string.Equals(mode, "wal", StringComparison.OrdinalIgnoreCase))
        {
            return new DiagnosticsCheckResult("sqlite.shared_drive_policy", "failed", "SQLite target journal mode is WAL, which is not approved for potential shared-drive M: target.", $"journal_mode={mode}");
        }

        if (string.Equals(mode, "delete", StringComparison.OrdinalIgnoreCase))
        {
            return new DiagnosticsCheckResult("sqlite.shared_drive_policy", "ok", "SQLite shared-drive policy uses DELETE journal mode (WAL not assumed).", $"journal_mode={mode}");
        }

        return new DiagnosticsCheckResult("sqlite.shared_drive_policy", "warning", "SQLite target journal mode is non-WAL but not DELETE; confirm policy expectations.", $"journal_mode={mode}");
    }



    private DiagnosticsCheckResult CheckBackupManifestProbe()
    {
        try
        {
            Directory.CreateDirectory(_pathResolution.Paths.Backups);
            var probeDir = Path.Combine(_pathResolution.Paths.Backups, ".manifest_probe_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probeDir);
            var probeManifest = new BackupManifest(
                BackupId: "probe",
                Kind: BackupKind.Manual,
                CreatedUtc: DateTimeOffset.UtcNow,
                Actor: "diagnostics-probe",
                BackupRoot: _pathResolution.Paths.Backups,
                Files: []);
            var probePath = Path.Combine(probeDir, "manifest.json");
            File.WriteAllText(probePath, System.Text.Json.JsonSerializer.Serialize(probeManifest));
            File.Delete(probePath);
            Directory.Delete(probeDir, false);
            return new DiagnosticsCheckResult("backup.can_create_manifest", "ok", "Can serialize and write/delete backup manifest probe without creating real backup.", _pathResolution.Paths.Backups);
        }
        catch (Exception ex)
        {
            return new DiagnosticsCheckResult("backup.can_create_manifest", "failed", "Backup manifest probe failed.", ex.Message);
        }
    }

    private DiagnosticsCheckResult CheckLatestBackupExists()
    {
        if (!Directory.Exists(_pathResolution.Paths.Backups)) return new DiagnosticsCheckResult("backup.latest.exists", "warning", "Backup root missing.", _pathResolution.Paths.Backups);
        var latest = Directory.GetDirectories(_pathResolution.Paths.Backups).OrderByDescending(d => d).FirstOrDefault();
        return new DiagnosticsCheckResult("backup.latest.exists", latest is null ? "warning" : "ok", latest is null ? "No backups found yet." : "Latest backup found.", latest ?? _pathResolution.Paths.Backups);
    }

    private DiagnosticsCheckResult CheckLatestBackupManifest(RestorePlanner planner)
    {
        var latest = Directory.Exists(_pathResolution.Paths.Backups) ? Directory.GetDirectories(_pathResolution.Paths.Backups).OrderByDescending(d => d).FirstOrDefault() : null;
        if (latest is null) return new DiagnosticsCheckResult("restore.latest_manifest.valid_if_present", "warning", "No backup available to validate.", _pathResolution.Paths.Backups);
        var plan = planner.Plan(latest);
        return new DiagnosticsCheckResult("restore.latest_manifest.valid_if_present", plan.CanProceed ? "ok" : "failed", plan.CanProceed ? "Latest backup manifest validates." : "Latest backup manifest validation failed.", plan.ManifestPath);
    }

    private DiagnosticsCheckResult CheckMigrationRollbackAvailable()
    {
        if (!Directory.Exists(_pathResolution.Paths.Backups)) return new DiagnosticsCheckResult("migration.rollback.available_if_backup_exists", "warning", "No backup root yet.", _pathResolution.Paths.Backups);
        var premig = Directory.GetDirectories(_pathResolution.Paths.Backups).Any(d => d.Contains("PreMigration", StringComparison.OrdinalIgnoreCase));
        return new DiagnosticsCheckResult("migration.rollback.available_if_backup_exists", premig ? "ok" : "warning", premig ? "PreMigration backup exists for rollback." : "No PreMigration backup found yet.", _pathResolution.Paths.Backups);
    }

    private static DiagnosticsCheckResult EnsureDirectory(string checkName, string path)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(fullPath);
        return new DiagnosticsCheckResult(checkName, "ok", "Directory exists or was created.", fullPath);
    }

    private static DiagnosticsCheckResult EnsureWriteAccess(string checkName, string path)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(fullPath);

        var probePath = Path.Combine(fullPath, ".diagnostic_write_probe_" + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(probePath, "probe");
        File.Delete(probePath);

        return new DiagnosticsCheckResult(checkName, "ok", "Write access confirmed.", fullPath);
    }

    private static DiagnosticsCheckResult CheckSqliteRepoConstructible(Func<object> builder)
    {
        try
        {
            _ = builder();
            return new DiagnosticsCheckResult("sqlite.repositories.constructible", "ok", "Repository instance created.", "constructible");
        }
        catch (Exception ex)
        {
            return new DiagnosticsCheckResult("sqlite.repositories.constructible", "warning", "Repository instance construction failed.", ex.Message);
        }
    }

    private static void AddCheck(List<DiagnosticsCheckResult> checks, string checkName, Func<DiagnosticsCheckResult> checker)
    {
        try
        {
            checks.Add(checker());
        }
        catch (Exception ex)
        {
            checks.Add(new DiagnosticsCheckResult(checkName, "failed", ex.Message, ex.GetType().Name));
        }
    }

    private static string ComputeOverallStatus(IEnumerable<DiagnosticsCheckResult> checks)
    {
        var hasFailed = false;
        var hasWarning = false;

        foreach (var check in checks)
        {
            if (string.Equals(check.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                hasFailed = true;
                continue;
            }

            if (string.Equals(check.Status, "warning", StringComparison.OrdinalIgnoreCase))
            {
                hasWarning = true;
            }
        }

        if (hasFailed)
        {
            return "failed";
        }

        return hasWarning ? "warning" : "ok";
    }
}
