using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record ReportGenerateRequest(long SessionId, string UserName);

public sealed record ReportGenerateResult(
    bool Success,
    string ReportType,
    string GeneratedAt,
    string GeneratedBy,
    long SessionId,
    string ShiftCode,
    string ShiftDate,
    IReadOnlyList<string> FilePaths);

public sealed record ReportsFolderRequest(long? SessionId);

public sealed record ReportsFolderResult(string OpenedPath);
