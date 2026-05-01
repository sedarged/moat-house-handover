using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoatHouseHandover.Host.Sqlite;

namespace MoatHouseHandover.Host.AppData;

public sealed class AppDataRootInitializer
{
    public const string DefaultDataRoot = @"M:\Moat House\MoatHouse Handover\";

    public AppDataRootStatus Initialize(HostConfig config, string userName)
    {
        var rootPath = ResolveDataRoot(config.DataRoot);
        var root = BuildRoot(rootPath);
        var created = new List<string>();
        var existing = new List<string>();
        var blocking = new List<AppDataRootIssue>();
        var warnings = new List<AppDataRootIssue>();

        EnsureFolder(root.DataRoot, created, existing, blocking, "app_data.root");
        EnsureFolder(root.DataFolder, created, existing, blocking, "app_data.data_folder");
        EnsureFolder(root.AttachmentsFolder, created, existing, blocking, "app_data.attachments_folder");
        EnsureFolder(root.ReportsFolder, created, existing, blocking, "app_data.reports_folder");
        EnsureFolder(root.BackupsFolder, created, existing, blocking, "app_data.backups_folder");
        EnsureFolder(root.MigrationFolder, created, existing, blocking, "app_data.migration_folder");
        EnsureFolder(root.DualRunFolder, created, existing, blocking, "app_data.dualrun_folder");
        EnsureFolder(root.LogsFolder, created, existing, blocking, "app_data.logs_folder");
        EnsureFolder(root.ConfigFolder, created, existing, blocking, "app_data.config_folder");

        var sqliteCreated = false;
        var schemaReady = false;
        if (!blocking.Any())
        {
            sqliteCreated = !File.Exists(root.SqliteDatabasePath);
            var sqliteResult = new SqliteBootstrapper().EnsureBootstrapped(root.SqliteDatabasePath, root.DataRoot, userName);
            schemaReady = sqliteResult.Success;
            if (!sqliteResult.Success)
            {
                blocking.Add(new AppDataRootIssue("app_data.sqlite_schema", sqliteResult.Message, true));
            }
        }

        if (!File.Exists(root.AccessLegacyDatabasePath))
        {
            warnings.Add(new AppDataRootIssue("app_data.accesslegacy.exists", "AccessLegacy database is missing; fallback/import source unavailable.", false));
        }

        var firstRun = created.Count > 0 || sqliteCreated;
        var status = blocking.Any()
            ? AppDataOwnershipStatus.Blocked
            : firstRun
                ? AppDataOwnershipStatus.FirstRunInitialized
                : warnings.Any() || !schemaReady ? AppDataOwnershipStatus.ReadyWithWarnings : AppDataOwnershipStatus.Ready;

        return new AppDataRootStatus(root, firstRun, created, existing, blocking, warnings, status);
    }

    public static string ResolveDataRoot(string? configuredRoot)
    {
        var envRoot = Environment.GetEnvironmentVariable("MOAT_HOUSE_DATA_ROOT");
        var raw = string.IsNullOrWhiteSpace(envRoot)
            ? string.IsNullOrWhiteSpace(configuredRoot) ? DefaultDataRoot : configuredRoot
            : envRoot;

        var fullPath = Path.GetFullPath(raw);
        return fullPath.EndsWith(Path.DirectorySeparatorChar) ? fullPath : fullPath + Path.DirectorySeparatorChar;
    }

    public static AppDataRoot BuildRoot(string dataRoot)
    {
        var normalized = dataRoot.EndsWith(Path.DirectorySeparatorChar) ? dataRoot : dataRoot + Path.DirectorySeparatorChar;
        var dataFolder = Path.Combine(normalized, "Data");
        return new AppDataRoot(
            DataRoot: normalized,
            DataFolder: dataFolder,
            AttachmentsFolder: Path.Combine(normalized, "Attachments"),
            ReportsFolder: Path.Combine(normalized, "Reports"),
            BackupsFolder: Path.Combine(normalized, "Backups"),
            MigrationFolder: Path.Combine(normalized, "Migration"),
            DualRunFolder: Path.Combine(normalized, "Migration", "DualRun"),
            LogsFolder: Path.Combine(normalized, "Logs"),
            ConfigFolder: Path.Combine(normalized, "Config"),
            SqliteDatabasePath: Path.Combine(dataFolder, "moat-house.db"),
            AccessLegacyDatabasePath: Path.Combine(dataFolder, "moat_handover_be.accdb"));
    }

    private static void EnsureFolder(string path, List<string> created, List<string> existing, List<AppDataRootIssue> issues, string code)
    {
        try
        {
            var exists = Directory.Exists(path);
            Directory.CreateDirectory(path);
            if (exists) existing.Add(path); else created.Add(path);
        }
        catch (Exception ex)
        {
            issues.Add(new AppDataRootIssue(code, ex.Message, true));
        }
    }
}
