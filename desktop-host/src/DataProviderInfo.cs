namespace MoatHouseHandover.Host;

public enum DatabaseProviderKind
{
    AccessLegacy = 1,
    SQLite = 2
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

public sealed class RuntimeDataProvider : IDataProvider
{
    private readonly HostRuntimeStatus _runtimeStatus;
    private readonly AppPathResolution _pathResolution;

    public RuntimeDataProvider(HostRuntimeStatus runtimeStatus, AppPathResolution pathResolution)
    {
        _runtimeStatus = runtimeStatus;
        _pathResolution = pathResolution;
    }

    public DatabaseProviderInfo GetInfo()
    {
        var sqliteTargetPath = _runtimeStatus.TargetSqlitePath;
        var activePath = _runtimeStatus.EffectiveProvider == DatabaseProviderKind.SQLite
            ? _runtimeStatus.TargetSqlitePath
            : _runtimeStatus.AccessDatabasePath;

        return new DatabaseProviderInfo(
            ProviderKind: _runtimeStatus.EffectiveProvider,
            ActiveDatabasePath: activePath,
            TargetSqlitePath: sqliteTargetPath,
            ProviderStatus: _runtimeStatus.ProviderStatusMessage,
            MigrationStatus: "SQLite remains opt-in and gated; AccessLegacy is default safe provider.");
    }
}
