using System;
using System.Globalization;

namespace MoatHouseHandover.Host;

public sealed class SessionService
{
    private readonly ISessionRepository _repository;
    private readonly AuditLogService _auditLogService;

    public SessionService(ISessionRepository repository, AuditLogService auditLogService)
    {
        _repository = repository;
        _auditLogService = auditLogService;
    }

    public SessionOpenResult OpenSession(SessionOpenRequest request)
    {
        var shiftDate = ParseShiftDate(request.ShiftDate);
        var shiftCode = NormalizeShiftCode(request.ShiftCode);
        var userName = NormalizeUser(request.UserName);
        var session = _repository.OpenExistingSession(
            shiftCode,
            shiftDate,
            userName);

        if (session is not null)
        {
            _auditLogService.BestEffortLog(
                actionType: "session.open",
                entityType: "HandoverHeader",
                entityKey: AuditLogService.BuildSessionKey(session.SessionId),
                userName: userName,
                details: new { sessionId = session.SessionId, shiftCode = session.ShiftCode, shiftDate = session.ShiftDate });
        }

        return new SessionOpenResult(session is not null, session);
    }

    public SessionCreateResult CreateBlankSession(SessionCreateBlankRequest request)
    {
        var shiftDate = ParseShiftDate(request.ShiftDate);
        var shiftCode = NormalizeShiftCode(request.ShiftCode);
        var userName = NormalizeUser(request.UserName);
        var result = _repository.CreateBlankSession(shiftCode, shiftDate, userName);

        if (result.Created)
        {
            _auditLogService.BestEffortLog(
                actionType: "session.createBlank",
                entityType: "HandoverHeader",
                entityKey: AuditLogService.BuildSessionKey(result.Session.SessionId),
                userName: userName,
                details: new { sessionId = result.Session.SessionId, shiftCode, shiftDate = result.Session.ShiftDate });
        }

        return result;
    }

    public SessionPayload ClearDay(SessionClearDayRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required for clearDay.");
        }

        var userName = NormalizeUser(request.UserName);
        var payload = _repository.ClearDay(request.SessionId, userName);
        _auditLogService.BestEffortLog(
            actionType: "session.clearDay",
            entityType: "HandoverHeader",
            entityKey: AuditLogService.BuildSessionKey(payload.SessionId),
            userName: userName,
            details: new { sessionId = payload.SessionId, shiftCode = payload.ShiftCode, shiftDate = payload.ShiftDate });

        return payload;
    }


    public IReadOnlyList<SessionListItem> ListSessions(SessionListFilters filters)
    {
        var normalized = filters with
        {
            ShiftCode = string.IsNullOrWhiteSpace(filters.ShiftCode) ? null : NormalizeShiftCode(filters.ShiftCode),
            ShiftDate = string.IsNullOrWhiteSpace(filters.ShiftDate) ? null : ParseShiftDate(filters.ShiftDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SessionStatus = string.IsNullOrWhiteSpace(filters.SessionStatus) ? null : filters.SessionStatus.Trim(),
            Search = string.IsNullOrWhiteSpace(filters.Search) ? null : filters.Search.Trim()
        };

        return _repository.ListSessions(normalized);
    }

    public SessionPayload OpenSessionById(SessionOpenByIdRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        var userName = Environment.UserName;
        var session = _repository.OpenSessionById(request.SessionId, userName)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");

        _auditLogService.BestEffortLog(
            actionType: "session.open",
            entityType: "HandoverHeader",
            entityKey: AuditLogService.BuildSessionKey(session.SessionId),
            userName: userName,
            details: new { sessionId = session.SessionId, shiftCode = session.ShiftCode, shiftDate = session.ShiftDate, mode = "openById" });

        return session;
    }
    private static DateTime ParseShiftDate(string shiftDate)
    {
        if (!DateTime.TryParseExact(shiftDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new InvalidOperationException("shiftDate must be in yyyy-MM-dd format.");
        }

        return parsed.Date;
    }

    private static string NormalizeShiftCode(string shiftCode)
    {
        var normalized = (shiftCode ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized is not ("AM" or "PM" or "NS"))
        {
            throw new InvalidOperationException("shiftCode must be one of AM, PM, NS.");
        }

        return normalized;
    }

    private static string NormalizeUser(string userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Trim();
        }

        return Environment.UserName;
    }
}
