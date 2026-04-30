namespace MoatHouseHandover.Host;

/// <summary>
/// Runtime configuration loaded from local JSON.
/// </summary>
public sealed class HostConfig
{
    public required string DataRoot { get; init; }

    public required string AccessDatabasePath { get; init; }

    public string? SQLiteDatabasePath { get; init; }

    public required string AttachmentsRoot { get; init; }

    public required string ReportsOutputRoot { get; init; }

    public required string BackupsRoot { get; init; }

    public required string LogRoot { get; init; }

    public required string ConfigRoot { get; init; }

    public required string ImportsRoot { get; init; }

    public required string MigrationRoot { get; init; }
}
