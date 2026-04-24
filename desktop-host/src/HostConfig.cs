namespace MoatHouseHandover.Host;

/// <summary>
/// Host runtime configuration contract. Maps to `webapp/js/config/app.config.template.json` keys.
/// Stage 1: design-only, concrete loading and validation lands in later stages.
/// </summary>
public sealed class HostConfig
{
    public required string AccessDatabasePath { get; init; }

    public required string AttachmentsRoot { get; init; }

    public required string ReportsOutputRoot { get; init; }
}
