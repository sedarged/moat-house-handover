using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record PreviewLoadRequest(long SessionId);

public sealed record PreviewPayload(
    PreviewSessionHeader Session,
    IReadOnlyList<PreviewDepartmentSummary> Departments,
    IReadOnlyList<PreviewAttachmentDepartmentSummary> AttachmentSummary,
    PreviewBudgetSummary BudgetSummary,
    IReadOnlyList<PreviewBudgetRowSummary> BudgetRows);

public sealed record PreviewSessionHeader(
    long SessionId,
    string ShiftCode,
    string ShiftDate,
    string SessionStatus,
    string? CreatedAt,
    string CreatedBy,
    string? UpdatedAt,
    string UpdatedBy);

public sealed record PreviewDepartmentSummary(
    long DeptRecordId,
    string DeptName,
    string DeptStatus,
    int? DowntimeMin,
    double? EfficiencyPct,
    double? YieldPct,
    string Notes,
    string? UpdatedAt,
    string? UpdatedBy,
    int AttachmentCount);

public sealed record PreviewAttachmentDepartmentSummary(
    string DeptName,
    int AttachmentCount,
    IReadOnlyList<PreviewAttachmentMeta> Attachments);

public sealed record PreviewAttachmentMeta(
    long AttachmentId,
    string DisplayName,
    string? CapturedOn,
    long SequenceNo);

public sealed record PreviewBudgetSummary(
    double PlannedTotal,
    double UsedTotal,
    double VarianceTotal,
    string Status,
    string? LastUpdatedAt,
    string? LastUpdatedBy,
    int RowCount);

public sealed record PreviewBudgetRowSummary(
    string DeptName,
    double? PlannedQty,
    double? UsedQty,
    double Variance,
    string Status,
    string ReasonText,
    string? UpdatedAt,
    string? UpdatedBy);
