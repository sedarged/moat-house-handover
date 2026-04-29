using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record BudgetLoadRequest(long SessionId, string UserName);

public sealed record BudgetSaveRequest(long SessionId, string UserName, IReadOnlyList<BudgetRowUpsertRequest> Rows, BudgetMetaUpsertRequest? Meta);

public sealed record BudgetRecalculateRequest(long SessionId, IReadOnlyList<BudgetRowUpsertRequest> Rows, BudgetMetaUpsertRequest? Meta);
public sealed record DashboardBudgetSummaryRequest(long SessionId);

public sealed record BudgetMetaUpsertRequest(double? LinesPlanned, double? TotalStaffOnRegister, string? Comments);

public sealed record BudgetRowUpsertRequest(
    long? BudgetRowId,
    string DeptName,
    double? PlannedQty,
    double? UsedQty,
    string ReasonText);

public sealed record BudgetPayload(
    long BudgetHeaderId,
    long SessionId,
    string ShiftCode,
    string ShiftDate,
    IReadOnlyList<BudgetRowPayload> Rows,
    BudgetTotalsPayload Totals,
    BudgetSummaryPayload Summary,
    string? UpdatedAt,
    string? UpdatedBy);

public sealed record BudgetRowPayload(
    long BudgetRowId,
    long BudgetHeaderId,
    long SessionId,
    string ShiftCode,
    string ShiftDate,
    string DeptName,
    double? PlannedQty,
    double? UsedQty,
    double Variance,
    string Status,
    string ReasonText,
    string? UpdatedAt,
    string? UpdatedBy);

public sealed record BudgetTotalsPayload(
    double PlannedTotal,
    double UsedTotal,
    double VarianceTotal,
    string Status);

public sealed record BudgetSummaryPayload(
    double PlannedTotal,
    double UsedTotal,
    double VarianceTotal,
    string Status,
    string? LastUpdatedAt,
    string? LastUpdatedBy,
    int RowCount,
    double? LinesPlanned,
    double? TotalStaffOnRegister,
    int HolidayCount,
    int AbsentCount,
    int OtherReasonCount,
    int AgencyUsedCount,
    string Comments);
