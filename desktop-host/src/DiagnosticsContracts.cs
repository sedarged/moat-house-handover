using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record DiagnosticsRunRequest(string? UserName);

public sealed record DiagnosticsCheckResult(
    string CheckName,
    string Status,
    string Message,
    string Details);

public sealed record DiagnosticsPayload(
    string OverallStatus,
    string CheckedAt,
    IReadOnlyList<DiagnosticsCheckResult> Checks);
