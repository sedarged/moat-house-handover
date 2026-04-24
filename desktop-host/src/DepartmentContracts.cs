using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record DepartmentPayload(
    long DeptRecordId,
    long SessionId,
    string DeptName,
    string DeptStatus,
    string DeptNotes,
    int? DowntimeMin,
    double? EfficiencyPct,
    double? YieldPct,
    string? UpdatedAt,
    string? UpdatedBy,
    bool IsMetricDept);

public sealed record DepartmentLoadRequest(long SessionId, string DeptName);

public sealed record DepartmentSaveRequest(
    long DeptRecordId,
    long SessionId,
    string DeptName,
    string DeptStatus,
    string DeptNotes,
    int? DowntimeMin,
    double? EfficiencyPct,
    double? YieldPct,
    string UserName);

public sealed record DepartmentSaveResult(
    DepartmentPayload Department,
    IReadOnlyList<DepartmentSummaryPayload> DashboardDepartments);
