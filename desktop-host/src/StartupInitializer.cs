using System;
using System.IO;
using MoatHouseHandover.Host.Sqlite;

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
            LogRoot = pathResolution.Paths.Logs,
            RuntimeProvider = configResult.Config.RuntimeProvider
        };

        var logger = new BootstrapLogger(pathResolution.Paths.Logs);
        logger.Log($"Loaded runtime config: {configResult.SourcePath}");
        logger.Log($"Resolved data root: {pathResolution.Paths.DataRoot}");

        var bootstrapper = new AccessBootstrapper(logger);
        bootstrapper.EnsureDatabaseAndSchema(resolvedConfig.AccessDatabasePath!);

        var sqlitePath = Path.Combine(pathResolution.Paths.Data, "moat-house.db");
        var sqliteBootstrapper = new SqliteBootstrapper();
        var sqliteSucceeded = false;
        string? sqliteMessage = null;

        try
        {
            var sqliteResult = sqliteBootstrapper.EnsureBootstrapped(sqlitePath, pathResolution.Paths.DataRoot, Environment.UserName);
            sqliteSucceeded = sqliteResult.Success;
            sqliteMessage = sqliteResult.Message;
            logger.Log($"SQLite bootstrap readiness: {sqliteResult.Message}");
        }
        catch (Exception ex)
        {
            sqliteMessage = ex.Message;
            logger.Log($"SQLite bootstrap readiness failed: {ex.Message}");
        }

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
            FoldersReady: pathResolution.AllValid,
            SqliteBootstrapSucceeded: sqliteSucceeded,
            SqliteBootstrapMessage: sqliteMessage,
            RequestedProvider: DatabaseProviderKind.AccessLegacy,
            EffectiveProvider: DatabaseProviderKind.AccessLegacy,
            ProviderSelectionSource: RuntimeProviderSource.Default,
            ProviderGateStatus: RuntimeProviderGateStatus.Allowed,
            ProviderFallbackReason: null,
            ApprovedDataRoot: AppPathService.ApprovedDataRoot,
            LatestDualRunReportPath: null,
            RuntimeSwitchEnabled: false,
            ProviderStatusMessage: "AccessLegacy is active default provider.");

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
