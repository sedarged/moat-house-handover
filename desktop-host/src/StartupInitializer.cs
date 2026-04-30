using System;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed class StartupInitializer
{
    public StartupResult Initialize()
    {
        var loader = new RuntimeConfigLoader();
        var configResult = loader.Load();

        var appPathService = new AppPathService();
        var pathResolution = appPathService.ResolveAndValidate(configResult.Config);

        if (!pathResolution.AllValid)
        {
            throw new InvalidOperationException("App path validation failed: " + string.Join(" | ", pathResolution.ValidationResults));
        }

        var resolvedConfig = new HostConfig
        {
            DataRoot = pathResolution.Paths.DataRoot,
            AccessDatabasePath = Path.Combine(pathResolution.Paths.Data, "moat_handover_be.accdb"),
            AttachmentsRoot = pathResolution.Paths.Attachments,
            ReportsOutputRoot = pathResolution.Paths.Reports,
            LogRoot = pathResolution.Paths.Logs
        };

        var logger = new BootstrapLogger(pathResolution.Paths.Logs);
        logger.Log($"Loaded runtime config: {configResult.SourcePath}");
        logger.Log($"Resolved data root: {pathResolution.Paths.DataRoot}");

        var bootstrapper = new AccessBootstrapper(logger);
        bootstrapper.EnsureDatabaseAndSchema(resolvedConfig.AccessDatabasePath!);

        var assets = ResolveAssetRoot();
        logger.Log($"Resolved web asset root: {assets}");

        var status = new HostRuntimeStatus(
            ConfigPath: configResult.SourcePath,
            AccessDatabasePath: Path.GetFullPath(resolvedConfig.AccessDatabasePath!),
            AttachmentsRoot: Path.GetFullPath(resolvedConfig.AttachmentsRoot!),
            ReportsOutputRoot: Path.GetFullPath(resolvedConfig.ReportsOutputRoot!),
            LogRoot: Path.GetFullPath(resolvedConfig.LogRoot!),
            AssetRoot: assets,
            IsWindows: OperatingSystem.IsWindows(),
            DatabaseReady: true,
            FoldersReady: pathResolution.AllValid);

        return new StartupResult(status, logger, resolvedConfig, pathResolution);
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

public sealed record StartupResult(HostRuntimeStatus RuntimeStatus, BootstrapLogger Logger, HostConfig Config, AppPathResolution PathResolution);
