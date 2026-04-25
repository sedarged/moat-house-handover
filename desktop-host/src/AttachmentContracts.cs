using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed record FilePickRequest(string? Title, string? Filter);

public sealed record FilePickResult(bool Picked, string? SourcePath, string? DisplayName);

public sealed record AttachmentListRequest(long SessionId, long DeptRecordId, string DeptName);

public sealed record AttachmentAddRequest(
    long SessionId,
    long DeptRecordId,
    string DeptName,
    string SourceFilePath,
    string DisplayName,
    string UserName);

public sealed record AttachmentRemoveRequest(long AttachmentId, string UserName);

public sealed record AttachmentViewerRequest(long SessionId, long DeptRecordId, long AttachmentId);

public sealed record AttachmentPayload(
    long AttachmentId,
    long SessionId,
    long DeptRecordId,
    string DeptName,
    string DisplayName,
    string FilePath,
    string CapturedOn,
    long SequenceNo,
    bool IsDeleted);

public sealed record AttachmentListResult(
    long SessionId,
    long DeptRecordId,
    string DeptName,
    IReadOnlyList<AttachmentPayload> Attachments,
    int AttachmentCount,
    IReadOnlyList<DepartmentSummaryPayload> DashboardDepartments);

public sealed record AttachmentViewerPayload(
    AttachmentPayload Current,
    int CurrentIndex,
    int TotalCount,
    AttachmentPayload? Previous,
    AttachmentPayload? Next);
