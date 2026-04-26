namespace MoatHouseHandover.Host;

public sealed record HostRuntimeStatus(
    string ConfigPath,
    string AccessDatabasePath,
    string AttachmentsRoot,
    string ReportsOutputRoot,
    string LogRoot,
    string AssetRoot,
    bool IsWindows,
    bool DatabaseReady,
    bool FoldersReady);
