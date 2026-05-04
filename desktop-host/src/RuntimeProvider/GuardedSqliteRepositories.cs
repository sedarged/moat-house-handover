using System;
using System.Collections.Generic;
using MoatHouseHandover.Host.AppData;
using MoatHouseHandover.Host.AppLock;

namespace MoatHouseHandover.Host;

internal static class SqliteWriteGuardHelpers
{
    public static void EnsureWriteAllowed(AppWriteGuard guard)
    {
        var result = guard.EnsureWriteAllowed(DatabaseProviderKind.SQLite);
        if (result.Allowed) return;

        var owner = result.LockState.Owner is null
            ? "owner=none"
            : $"owner={result.LockState.Owner.UserName}@{result.LockState.Owner.MachineName} pid={result.LockState.Owner.ProcessId}";

        throw new InvalidOperationException($"SQLite write lock is not held. {result.Message} status={result.LockState.Status}; {owner}");
    }
}

internal sealed class GuardedSqliteSessionRepository : ISessionRepository
{
    private readonly ISessionRepository _inner;
    private readonly AppWriteGuard _guard;
    public GuardedSqliteSessionRepository(ISessionRepository inner, AppWriteGuard guard){_inner=inner;_guard=guard;}
    public SessionPayload? OpenExistingSession(string shiftCode, DateTime shiftDate, string userName) => _inner.OpenExistingSession(shiftCode, shiftDate, userName);
    public SessionCreateResult CreateBlankSession(string shiftCode, DateTime shiftDate, string userName){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); return _inner.CreateBlankSession(shiftCode, shiftDate, userName);}    
    public SessionPayload ClearDay(long sessionId, string userName){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); return _inner.ClearDay(sessionId, userName);}    
    public IReadOnlyList<SessionListItem> ListSessions(SessionListFilters filters)=>_inner.ListSessions(filters);
    public SessionPayload? OpenSessionById(long sessionId, string userName)=>_inner.OpenSessionById(sessionId, userName);
}

internal sealed class GuardedSqliteDepartmentRepository : IDepartmentRepository
{
    private readonly IDepartmentRepository _inner; private readonly AppWriteGuard _guard;
    public GuardedSqliteDepartmentRepository(IDepartmentRepository inner, AppWriteGuard guard){_inner=inner;_guard=guard;}
    public DepartmentPayload LoadDepartment(long sessionId, string deptName)=>_inner.LoadDepartment(sessionId,deptName);
    public DepartmentSaveResult SaveDepartment(DepartmentSaveRequest request, string userName){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); return _inner.SaveDepartment(request,userName);}    
}

internal sealed class GuardedSqliteAttachmentRepository : IAttachmentRepository
{
    private readonly IAttachmentRepository _inner; private readonly AppWriteGuard _guard;
    public GuardedSqliteAttachmentRepository(IAttachmentRepository inner, AppWriteGuard guard){_inner=inner;_guard=guard;}
    public AttachmentListResult ListAttachments(long sessionId, long deptRecordId, string deptName)=>_inner.ListAttachments(sessionId,deptRecordId,deptName);
    public AttachmentListResult AddAttachmentMetadata(AttachmentAddRequest request, string storedFilePath, string userName){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); return _inner.AddAttachmentMetadata(request,storedFilePath,userName);}    
    public AttachmentListResult RemoveAttachment(long attachmentId){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); return _inner.RemoveAttachment(attachmentId);}    
    public AttachmentViewerPayload GetViewerPayload(long sessionId, long deptRecordId, long attachmentId)=>_inner.GetViewerPayload(sessionId,deptRecordId,attachmentId);
}

internal sealed class GuardedSqliteBudgetRepository : IBudgetRepository
{
    private readonly IBudgetRepository _inner; private readonly AppWriteGuard _guard;
    public GuardedSqliteBudgetRepository(IBudgetRepository inner, AppWriteGuard guard){_inner=inner;_guard=guard;}
    public BudgetPayload LoadBudget(long sessionId, string userName){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); return _inner.LoadBudget(sessionId,userName);}    
    public BudgetPayload SaveBudget(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta, string userName){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); return _inner.SaveBudget(sessionId,rows,meta,userName);}    
    public BudgetPayload Recalculate(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta)=>_inner.Recalculate(sessionId,rows,meta);
    public BudgetSummaryPayload LoadBudgetSummary(long sessionId)=>_inner.LoadBudgetSummary(sessionId);
}

internal sealed class GuardedSqliteAuditLogRepository : IAuditLogRepository
{
    private readonly IAuditLogRepository _inner; private readonly AppWriteGuard _guard;
    public GuardedSqliteAuditLogRepository(IAuditLogRepository inner, AppWriteGuard guard){_inner=inner;_guard=guard;}
    public void Insert(AuditLogWriteRequest request){ SqliteWriteGuardHelpers.EnsureWriteAllowed(_guard); _inner.Insert(request);}    
    public IReadOnlyList<AuditLogEntry> ListRecent(int limit)=>_inner.ListRecent(limit);
    public IReadOnlyList<AuditLogEntry> ListForSession(long sessionId, int limit)=>_inner.ListForSession(sessionId,limit);
}
