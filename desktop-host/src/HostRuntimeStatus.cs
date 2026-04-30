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
    string? SqliteBootstrapMessage);
