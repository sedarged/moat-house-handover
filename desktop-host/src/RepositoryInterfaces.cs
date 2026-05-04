using System;
using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public interface ISessionRepository
{
    SessionPayload? OpenExistingSession(string shiftCode, DateTime shiftDate, string userName);
    SessionCreateResult CreateBlankSession(string shiftCode, DateTime shiftDate, string userName);
    SessionPayload ClearDay(long sessionId, string userName);
    IReadOnlyList<SessionListItem> ListSessions(SessionListFilters filters);
    SessionPayload? OpenSessionById(long sessionId, string userName);
}
public interface IDepartmentRepository
{
    DepartmentPayload LoadDepartment(long sessionId, string deptName);
    DepartmentSaveResult SaveDepartment(DepartmentSaveRequest request, string userName);
}
public interface IAttachmentRepository
{
    AttachmentListResult ListAttachments(long sessionId, long deptRecordId, string deptName);
    AttachmentListResult AddAttachmentMetadata(AttachmentAddRequest request, string storedFilePath, string userName);
    AttachmentListResult RemoveAttachment(long attachmentId);
    AttachmentViewerPayload GetViewerPayload(long sessionId, long deptRecordId, long attachmentId);
}
public interface IBudgetRepository
{
    BudgetPayload LoadBudget(long sessionId, string userName);
    BudgetPayload SaveBudget(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta, string userName);
    BudgetPayload Recalculate(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta);
    BudgetSummaryPayload LoadBudgetSummary(long sessionId);
}
public interface IPreviewRepository { PreviewPayload LoadPreview(long sessionId); }
public interface IAuditLogRepository
{
    void Insert(AuditLogWriteRequest request);
    IReadOnlyList<AuditLogEntry> ListRecent(int limit);
    IReadOnlyList<AuditLogEntry> ListForSession(long sessionId, int limit);
}
public interface IEmailProfileRepository { EmailProfilePayload? LoadByShiftCode(string shiftCode); }
