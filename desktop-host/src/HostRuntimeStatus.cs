namespace MoatHouseHandover.Host;

public sealed record HostRuntimeStatus(
    string ConfigPath,
    string DataRoot,
    string DataDirectory,
    string AccessDatabasePath,
    string SQLiteDatabasePath,
    string AttachmentsRoot,
    string ReportsOutputRoot,
    string BackupsRoot,
    string LogRoot,
    string ConfigRoot,
    string ImportsRoot,
    string MigrationRoot,
    string AssetRoot,
    bool IsWindows,
    bool DatabaseReady,
    bool FoldersReady);
