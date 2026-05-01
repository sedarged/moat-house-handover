using System.Collections.Generic;
using System;

namespace MoatHouseHandover.Host;

public enum RuntimeProviderSource
{
    Default = 1,
    Config = 2,
    EnvironmentVariable = 3,
    AdminSetting = 4
}

public enum RuntimeProviderGateStatus
{
    Allowed = 1,
    Blocked = 2,
    WarningFallback = 3
}

public enum RuntimeProviderSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3
}

public sealed record RuntimeProviderIssue(string Code, RuntimeProviderSeverity Severity, string Message, string? Detail = null);

public sealed record RuntimeProviderOptions(string? RequestedProviderRaw, RuntimeProviderSource Source, bool DeveloperOverrideEnabled);

public sealed record RuntimeProviderGateResult(
    RuntimeProviderGateStatus Status,
    bool SqliteDbExists,
    bool SqliteSchemaReady,
    bool SqliteRepositoriesReady,
    bool AccessLegacyFallbackAvailable,
    string? LatestDualRunReportPath,
    string? FallbackReason,
    IReadOnlyList<RuntimeProviderIssue> Issues);

public sealed record RuntimeProviderSelection(
    DatabaseProviderKind RequestedProvider,
    DatabaseProviderKind EffectiveProvider,
    RuntimeProviderSource Source,
    RuntimeProviderGateResult GateResult,
    string RuntimeStatusMessage,
    bool RuntimeSwitchEnabled);
