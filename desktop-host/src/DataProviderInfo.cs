namespace MoatHouseHandover.Host;

public enum DatabaseProviderKind
{
    AccessLegacy = 1
}

public sealed record DatabaseProviderInfo(
    DatabaseProviderKind ProviderKind,
    string ActiveDatabasePath,
    string? TargetSqlitePath,
    string ProviderStatus,
    string MigrationStatus);

public interface IDataProvider
{
    DatabaseProviderInfo GetInfo();
}

public sealed class AccessLegacyDataProvider : IDataProvider
{
    private readonly HostRuntimeStatus _runtimeStatus;
    private readonly AppPathResolution _pathResolution;

    public AccessLegacyDataProvider(HostRuntimeStatus runtimeStatus, AppPathResolution pathResolution)
    {
        _runtimeStatus = runtimeStatus;
        _pathResolution = pathResolution;
    }

    public DatabaseProviderInfo GetInfo()
    {
        var sqliteTargetPath = _runtimeStatus.TargetSqlitePath;
        return new DatabaseProviderInfo(
            ProviderKind: DatabaseProviderKind.AccessLegacy,
            ActiveDatabasePath: _runtimeStatus.AccessDatabasePath,
            TargetSqlitePath: sqliteTargetPath,
            ProviderStatus: "Access legacy/current runtime provider is active.",
            MigrationStatus: "SQLite is approved as the future target provider and is not active in runtime yet.");
    }
}
