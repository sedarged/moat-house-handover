using System;
using System.IO;
using MoatHouseHandover.Host.Sqlite;
using MoatHouseHandover.Host.AppData;
using MoatHouseHandover.Host.AppLock;

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


        var appLockService = new AppLockService(root, typeof(StartupInitializer).Assembly.GetName().Version?.ToString());
        var appLockRequired = false;
        var appLockState = appLockService.CheckStatus(lockRequired: appLockRequired);

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
            AppDataFirstRunInitialized: appDataStatus.IsFirstRun,
            AppLockPath: appLockState.LockFilePath,
            AppLockStatus: appLockState.Status.ToString(),
            AppLockOwnerMachine: appLockState.Owner?.MachineName,
            AppLockOwnerUser: appLockState.Owner?.UserName,
            AppLockOwnerProcessId: appLockState.Owner?.ProcessId,
            AppLockCreatedAt: appLockState.Owner?.CreatedAtUtc.ToString("O"),
            AppLockHeartbeatAt: appLockState.Owner?.HeartbeatAtUtc.ToString("O"),
            AppCanRead: appLockState.CanRead,
            AppCanWrite: appLockState.CanWrite,
            AppLockMessage: appLockState.Message);

        var selection = new RuntimeProviderSelector().Select(provisionalStatus, resolvedConfig, pathResolution);
        var effectiveLockState = appLockService.CheckStatus(lockRequired: selection.EffectiveProvider == DatabaseProviderKind.SQLite);
        var status = provisionalStatus with
        {
            RequestedProvider = selection.RequestedProvider,
            EffectiveProvider = selection.EffectiveProvider,
            ProviderSelectionSource = selection.Source,
            ProviderGateStatus = selection.GateResult.Status,
            ProviderFallbackReason = selection.GateResult.FallbackReason,
            LatestDualRunReportPath = selection.GateResult.LatestDualRunReportPath,
            RuntimeSwitchEnabled = selection.RuntimeSwitchEnabled,
            ProviderStatusMessage = selection.RuntimeStatusMessage,
            AppLockStatus = effectiveLockState.Status.ToString(),
            AppLockOwnerMachine = effectiveLockState.Owner?.MachineName,
            AppLockOwnerUser = effectiveLockState.Owner?.UserName,
            AppLockOwnerProcessId = effectiveLockState.Owner?.ProcessId,
            AppLockCreatedAt = effectiveLockState.Owner?.CreatedAtUtc.ToString("O"),
            AppLockHeartbeatAt = effectiveLockState.Owner?.HeartbeatAtUtc.ToString("O"),
            AppCanRead = effectiveLockState.CanRead,
            AppCanWrite = effectiveLockState.CanWrite,
            AppLockMessage = effectiveLockState.Message
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
