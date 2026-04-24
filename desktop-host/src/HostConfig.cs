namespace MoatHouseHandover.Host;

/// <summary>
/// Runtime configuration loaded from local JSON.
/// </summary>
public sealed class HostConfig
{
    public required string AccessDatabasePath { get; init; }

    public required string AttachmentsRoot { get; init; }

    public required string ReportsOutputRoot { get; init; }

    public string? LogRoot { get; init; }
}
