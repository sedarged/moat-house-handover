using System;
using System.Collections.Generic;
using System.IO;

namespace MoatHouseHandover.Host;

public static class AppPathDefaults
{
    public const string PrimaryDataRoot = @"M:\Moat House\MoatHouse Handover";
    public const string AccessDatabaseFileName = "moat_handover_be.accdb";
    public const string SQLiteDatabaseFileName = "moat-house.db";
}

public sealed record AppPaths(
    string DataRoot,
    string DataDirectory,
    string AccessDatabasePath,
    string SQLiteDatabasePath,
    string AttachmentsRoot,
    string ReportsOutputRoot,
    string BackupsRoot,
    string LogsRoot,
    string ConfigRoot,
    string ImportsRoot,
    string MigrationRoot)
{
    public IReadOnlyDictionary<string, string> RequiredDirectories => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["data"] = DataDirectory,
        ["attachments"] = AttachmentsRoot,
        ["reports"] = ReportsOutputRoot,
        ["backups"] = BackupsRoot,
        ["logs"] = LogsRoot,
        ["config"] = ConfigRoot,
        ["imports"] = ImportsRoot,
        ["migration"] = MigrationRoot
    };
}

public sealed class AppPathService
{
    public AppPaths Resolve(HostConfig config)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var dataRoot = NormalizeRequired(config.DataRoot, "dataRoot");
        EnsurePrimaryDataRoot(dataRoot);

        var dataDirectory = ResolveChildRoot(dataRoot, "Data");
        return new AppPaths(
            DataRoot: dataRoot,
            DataDirectory: dataDirectory,
            AccessDatabasePath: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.AccessDatabasePath)
                    ? Path.Combine(dataDirectory, AppPathDefaults.AccessDatabaseFileName)
                    : config.AccessDatabasePath,
                dataRoot,
                "accessDatabasePath"),
            SQLiteDatabasePath: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.SQLiteDatabasePath)
                    ? Path.Combine(dataDirectory, AppPathDefaults.SQLiteDatabaseFileName)
                    : config.SQLiteDatabasePath,
                dataRoot,
                "sqliteDatabasePath"),
            AttachmentsRoot: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.AttachmentsRoot)
                    ? ResolveChildRoot(dataRoot, "Attachments")
                    : config.AttachmentsRoot,
                dataRoot,
                "attachmentsRoot"),
            ReportsOutputRoot: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.ReportsOutputRoot)
                    ? ResolveChildRoot(dataRoot, "Reports")
                    : config.ReportsOutputRoot,
                dataRoot,
                "reportsOutputRoot"),
            BackupsRoot: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.BackupsRoot)
                    ? ResolveChildRoot(dataRoot, "Backups")
                    : config.BackupsRoot,
                dataRoot,
                "backupsRoot"),
            LogsRoot: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.LogRoot)
                    ? ResolveChildRoot(dataRoot, "Logs")
                    : config.LogRoot,
                dataRoot,
                "logRoot"),
            ConfigRoot: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.ConfigRoot)
                    ? ResolveChildRoot(dataRoot, "Config")
                    : config.ConfigRoot,
                dataRoot,
                "configRoot"),
            ImportsRoot: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.ImportsRoot)
                    ? ResolveChildRoot(dataRoot, "Imports")
                    : config.ImportsRoot,
                dataRoot,
                "importsRoot"),
            MigrationRoot: NormalizeInsideRoot(
                string.IsNullOrWhiteSpace(config.MigrationRoot)
                    ? ResolveChildRoot(dataRoot, "Migration")
                    : config.MigrationRoot,
                dataRoot,
                "migrationRoot"));
    }

    public void EnsureRequiredDirectories(AppPaths paths, BootstrapLogger logger)
    {
        foreach (var pair in paths.RequiredDirectories)
        {
            Directory.CreateDirectory(pair.Value);
            logger.Log($"Ensured app path: {pair.Key} => {pair.Value}");
        }
    }

    public static bool IsConfiguredPrimaryDataRoot(string dataRoot)
    {
        var actual = TrimDirectorySeparator(Path.GetFullPath(dataRoot));
        var expected = TrimDirectorySeparator(Path.GetFullPath(AppPathDefaults.PrimaryDataRoot));
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRequired(string? path, string keyName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"Runtime config '{keyName}' is required. Primary live data root must be '{AppPathDefaults.PrimaryDataRoot}'.");
        }

        return TrimDirectorySeparator(Path.GetFullPath(path));
    }

    private static string ResolveChildRoot(string dataRoot, string childName)
    {
        return Path.Combine(dataRoot, childName);
    }

    private static void EnsurePrimaryDataRoot(string dataRoot)
    {
        if (!IsConfiguredPrimaryDataRoot(dataRoot))
        {
            throw new InvalidOperationException(
                "Invalid live data root. This app must not silently fall back to C:\\ProgramData or another local path. "
                + $"Configured dataRoot='{dataRoot}'. Expected primary dataRoot='{AppPathDefaults.PrimaryDataRoot}'.");
        }
    }

    private static string NormalizeInsideRoot(string? path, string dataRoot, string keyName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Runtime config '{keyName}' resolved to an empty path.");
        }

        var fullPath = Path.GetFullPath(path);
        var normalizedRoot = TrimDirectorySeparator(Path.GetFullPath(dataRoot));
        var normalizedFullPath = TrimDirectorySeparator(fullPath);

        var isInsideRoot = normalizedFullPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            || normalizedFullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || normalizedFullPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        if (!isInsideRoot)
        {
            throw new InvalidOperationException(
                $"Runtime config '{keyName}' must resolve inside '{normalizedRoot}'. Actual='{fullPath}'. No silent fallback paths are allowed.");
        }

        return fullPath;
    }

    private static string TrimDirectorySeparator(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
