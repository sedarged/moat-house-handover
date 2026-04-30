using System;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed class StartupInitializer
{
    public StartupResult Initialize()
    {
        var loader = new RuntimeConfigLoader();
        var configResult = loader.Load();

        var pathService = new AppPathService();
        var paths = pathService.Resolve(configResult.Config);

        var logger = new BootstrapLogger(paths.LogsRoot);
        logger.Log($"Loaded runtime config: {configResult.SourcePath}");
        logger.Log($"Resolved primary data root: {paths.DataRoot}");

        pathService.EnsureRequiredDirectories(paths, logger);

        var bootstrapper = new AccessBootstrapper(logger);
        bootstrapper.EnsureDatabaseAndSchema(paths.AccessDatabasePath);

        var assets = ResolveAssetRoot();
        logger.Log($"Resolved web asset root: {assets}");

        var status = new HostRuntimeStatus(
            ConfigPath: configResult.SourcePath,
            DataRoot: paths.DataRoot,
            DataDirectory: paths.DataDirectory,
            AccessDatabasePath: Path.GetFullPath(paths.AccessDatabasePath),
            SQLiteDatabasePath: Path.GetFullPath(paths.SQLiteDatabasePath),
            AttachmentsRoot: Path.GetFullPath(paths.AttachmentsRoot),
            ReportsOutputRoot: Path.GetFullPath(paths.ReportsOutputRoot),
            BackupsRoot: Path.GetFullPath(paths.BackupsRoot),
            LogRoot: Path.GetFullPath(paths.LogsRoot),
            ConfigRoot: Path.GetFullPath(paths.ConfigRoot),
            ImportsRoot: Path.GetFullPath(paths.ImportsRoot),
            MigrationRoot: Path.GetFullPath(paths.MigrationRoot),
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
}

public sealed record StartupResult(HostRuntimeStatus RuntimeStatus, BootstrapLogger Logger, HostConfig Config);
