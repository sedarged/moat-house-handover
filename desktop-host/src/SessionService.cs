using System;
using System.Globalization;

namespace MoatHouseHandover.Host;

public sealed class SessionService
{
    private readonly SessionRepository _repository;

    public SessionService(SessionRepository repository)
    {
        _repository = repository;
    }

    public SessionOpenResult OpenSession(SessionOpenRequest request)
    {
        var shiftDate = ParseShiftDate(request.ShiftDate);
        var session = _repository.OpenExistingSession(NormalizeShiftCode(request.ShiftCode), shiftDate);
        return new SessionOpenResult(session is not null, session);
    }

    public SessionCreateResult CreateBlankSession(SessionCreateBlankRequest request)
    {
        var shiftDate = ParseShiftDate(request.ShiftDate);
        return _repository.CreateBlankSession(NormalizeShiftCode(request.ShiftCode), shiftDate, NormalizeUser(request.UserName));
    }

    public SessionPayload ClearDay(SessionClearDayRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required for clearDay.");
        }

        return _repository.ClearDay(request.SessionId, NormalizeUser(request.UserName));
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
