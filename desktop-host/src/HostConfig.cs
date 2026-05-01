namespace MoatHouseHandover.Host;

/// <summary>
/// Runtime configuration loaded from local JSON.
/// </summary>
public sealed class HostConfig
{
    public string? DataRoot { get; init; }

    public string? AccessDatabasePath { get; init; }

    public string? AttachmentsRoot { get; init; }

    public string? ReportsOutputRoot { get; init; }

    public string? LogRoot { get; init; }

    public string? RuntimeProvider { get; init; }
}
