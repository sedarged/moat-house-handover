using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record AuditLogWriteRequest(
    string ActionType,
    string EntityType,
    string EntityKey,
    string UserName,
    string Details);

public sealed record AuditLogEntry(
    long AuditId,
    string EventAt,
    string UserName,
    string EntityType,
    string EntityKey,
    string ActionType,
    string Details);

public sealed record AuditListRecentRequest(int? Limit);

public sealed record AuditListForSessionRequest(long SessionId, int? Limit);

public sealed record AuditListResult(
    bool Success,
    IReadOnlyList<AuditLogEntry> Entries,
    string? Error);
