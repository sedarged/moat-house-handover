namespace MoatHouseHandover.Host;

public sealed record HostRuntimeStatus(
    string ConfigPath,
    string AccessDatabasePath,
    string TargetSqlitePath,
    string AttachmentsRoot,
    string ReportsOutputRoot,
    string LogRoot,
    string AssetRoot,
    bool IsWindows,
    bool DatabaseReady,
    bool FoldersReady,
    bool SqliteBootstrapSucceeded,
    string? SqliteBootstrapMessage,
    DatabaseProviderKind RequestedProvider,
    DatabaseProviderKind EffectiveProvider,
    RuntimeProviderSource ProviderSelectionSource,
    RuntimeProviderGateStatus ProviderGateStatus,
    string? ProviderFallbackReason,
    string ApprovedDataRoot,
    string? LatestDualRunReportPath,
    bool RuntimeSwitchEnabled,
    string ProviderStatusMessage);
