using System;
using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record SessionPayload(
    long SessionId,
    string ShiftCode,
    string ShiftDate,
    string SessionStatus,
    IReadOnlyList<DepartmentSummaryPayload> Departments,
    string CreatedAt,
    string CreatedBy,
    string UpdatedAt,
    string UpdatedBy);

public sealed record DepartmentSummaryPayload(
    string DeptName,
    string DeptStatus,
    string? UpdatedAt,
    string? UpdatedBy,
    int AttachmentCount);

public sealed record SessionOpenResult(bool Found, SessionPayload? Session);

public sealed record SessionCreateResult(bool Created, SessionPayload Session);

public sealed record SessionOpenRequest(string ShiftCode, string ShiftDate, string UserName);

public sealed record SessionCreateBlankRequest(string ShiftCode, string ShiftDate, string UserName);

public sealed record SessionClearDayRequest(long SessionId, string UserName);
