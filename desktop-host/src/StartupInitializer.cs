using System;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed class StartupInitializer
{
    public StartupResult Initialize()
    {
        var loader = new RuntimeConfigLoader();
        var configResult = loader.Load();

        var logRoot = string.IsNullOrWhiteSpace(configResult.Config.LogRoot)
            ? Path.Combine(Path.GetDirectoryName(configResult.SourcePath) ?? AppContext.BaseDirectory, "logs")
            : configResult.Config.LogRoot;

        var logger = new BootstrapLogger(Path.GetFullPath(logRoot!));
        logger.Log($"Loaded runtime config: {configResult.SourcePath}");

        EnsureDirectory(configResult.Config.AttachmentsRoot, logger, "attachmentsRoot");
        EnsureDirectory(configResult.Config.ReportsOutputRoot, logger, "reportsOutputRoot");

        var bootstrapper = new AccessBootstrapper(logger);
        bootstrapper.EnsureDatabaseAndSchema(configResult.Config.AccessDatabasePath);

        var assets = ResolveAssetRoot();
        logger.Log($"Resolved web asset root: {assets}");

        var status = new HostRuntimeStatus(
            ConfigPath: configResult.SourcePath,
            AccessDatabasePath: Path.GetFullPath(configResult.Config.AccessDatabasePath),
            AttachmentsRoot: Path.GetFullPath(configResult.Config.AttachmentsRoot),
            ReportsOutputRoot: Path.GetFullPath(configResult.Config.ReportsOutputRoot),
            LogRoot: Path.GetFullPath(logRoot!),
            AssetRoot: assets,
            IsWindows: OperatingSystem.IsWindows(),
            DatabaseReady: true,
            FoldersReady: true);

        return new StartupResult(status, logger, configResult.Config);
    }

    public static string ResolveAssetRoot()
    {
        var packagedPath = Path.Combine(AppContext.BaseDirectory, "webapp");
        if (File.Exists(Path.Combine(packagedPath, "index.html")))
        {
            return packagedPath;
        }

        var repoDevPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "webapp"));
        if (File.Exists(Path.Combine(repoDevPath, "index.html")))
        {
            return repoDevPath;
        }

        throw new InvalidOperationException("webapp/index.html not found in packaged or development paths.");
    }

    private static void EnsureDirectory(string path, BootstrapLogger logger, string keyName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Runtime config '{keyName}' is required.");
        }

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(fullPath);
        logger.Log($"Ensured directory: {keyName} => {fullPath}");
    }
}

public sealed record StartupResult(HostRuntimeStatus RuntimeStatus, BootstrapLogger Logger, HostConfig Config);
