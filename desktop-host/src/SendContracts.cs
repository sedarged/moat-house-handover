using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record SendPreparePackageRequest(long SessionId, string UserName);

public sealed record SendCreateOutlookDraftRequest(long SessionId, string UserName);

public sealed record SendPackagePayload(
    long SessionId,
    string ShiftCode,
    string ShiftDate,
    string EmailProfileKey,
    string ToList,
    string CcList,
    string Subject,
    string Body,
    IReadOnlyList<string> AttachmentPaths,
    string GeneratedAt,
    string GeneratedBy,
    bool IsReady,
    string ReadinessStatus,
    IReadOnlyList<string> ValidationMessages);

public sealed record SendPreparePackageResult(
    bool Success,
    SendPackagePayload Package);

public sealed record SendCreateOutlookDraftResult(
    bool Success,
    SendPackagePayload Package,
    OutlookDraftResult Draft);
