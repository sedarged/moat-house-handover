using System;
using System.IO;
using MoatHouseHandover.Host.Sqlite;
using MoatHouseHandover.Host.AppData;

namespace MoatHouseHandover.Host;

public sealed class StartupInitializer
{
    public StartupResult Initialize()
    {
        var loader = new RuntimeConfigLoader();
        var configResult = loader.Load();

        var appDataStatus = new AppDataRootInitializer().Initialize(configResult.Config, Environment.UserName);
        var root = appDataStatus.Root;

        var pathResolution = new AppPathService().ResolveAndValidateRoot(root.DataRoot);

        var resolvedConfig = new HostConfig
        {
            DataRoot = root.DataRoot,
            AccessDatabasePath = root.AccessLegacyDatabasePath,
            AttachmentsRoot = root.AttachmentsFolder,
            ReportsOutputRoot = root.ReportsFolder,
            LogRoot = root.LogsFolder,
            RuntimeProvider = configResult.Config.RuntimeProvider
        };

        var logger = new BootstrapLogger(root.LogsFolder);
        logger.Log($"Loaded runtime config: {configResult.SourcePath}");
        logger.Log($"Resolved data root: {root.DataRoot}");

        var bootstrapper = new AccessBootstrapper(logger);
        if (File.Exists(resolvedConfig.AccessDatabasePath!))
        {
            bootstrapper.EnsureDatabaseAndSchema(resolvedConfig.AccessDatabasePath!);
        }
        else
        {
            logger.Log("AccessLegacy database missing; startup continues with warning for fallback/import role.");
        }

        var sqlitePath = root.SqliteDatabasePath;
        var sqliteSucceeded = appDataStatus.SqliteBootstrapSucceeded;
        var sqliteMessage = appDataStatus.SqliteBootstrapMessage;
        logger.Log($"SQLite bootstrap readiness: {sqliteMessage ?? "(no message)"}");

        var assets = ResolveAssetRoot();
        logger.Log($"Resolved web asset root: {assets}");

        var provisionalStatus = new HostRuntimeStatus(
            ConfigPath: configResult.SourcePath,
            AccessDatabasePath: Path.GetFullPath(resolvedConfig.AccessDatabasePath!),
            TargetSqlitePath: Path.GetFullPath(sqlitePath),
            AttachmentsRoot: Path.GetFullPath(resolvedConfig.AttachmentsRoot!),
            ReportsOutputRoot: Path.GetFullPath(resolvedConfig.ReportsOutputRoot!),
            LogRoot: Path.GetFullPath(resolvedConfig.LogRoot!),
            AssetRoot: assets,
            IsWindows: OperatingSystem.IsWindows(),
            DatabaseReady: true,
            FoldersReady: appDataStatus.OwnershipStatus != AppDataOwnershipStatus.Blocked,
            SqliteBootstrapSucceeded: sqliteSucceeded,
            SqliteBootstrapMessage: sqliteMessage,
            RequestedProvider: DatabaseProviderKind.AccessLegacy,
            EffectiveProvider: DatabaseProviderKind.AccessLegacy,
            ProviderSelectionSource: RuntimeProviderSource.Default,
            ProviderGateStatus: RuntimeProviderGateStatus.Allowed,
            ProviderFallbackReason: null,
            ApprovedDataRoot: root.DataRoot,
            LatestDualRunReportPath: null,
            RuntimeSwitchEnabled: false,
            ProviderStatusMessage: "AccessLegacy is active default provider.",
            AppDataOwnershipStatus: appDataStatus.OwnershipStatus.ToString(),
            AppDataFirstRunInitialized: appDataStatus.IsFirstRun);

        var selection = new RuntimeProviderSelector().Select(provisionalStatus, resolvedConfig, pathResolution);
        var status = provisionalStatus with
        {
            RequestedProvider = selection.RequestedProvider,
            EffectiveProvider = selection.EffectiveProvider,
            ProviderSelectionSource = selection.Source,
            ProviderGateStatus = selection.GateResult.Status,
            ProviderFallbackReason = selection.GateResult.FallbackReason,
            LatestDualRunReportPath = selection.GateResult.LatestDualRunReportPath,
            RuntimeSwitchEnabled = selection.RuntimeSwitchEnabled,
            ProviderStatusMessage = selection.RuntimeStatusMessage
        };

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
