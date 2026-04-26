using System;

namespace MoatHouseHandover.Host;

public sealed class EmailProfileService
{
    private readonly EmailProfileRepository _repository;

    public EmailProfileService(EmailProfileRepository repository)
    {
        _repository = repository;
    }

    public EmailProfilePayload LoadActiveForShift(EmailProfileLoadRequest request)
    {
        var shiftCode = NormalizeShiftCode(request.ShiftCode);
        var profile = _repository.LoadByShiftCode(shiftCode);
        if (profile is null)
        {
            throw new InvalidOperationException($"No email profile mapping exists for shift '{shiftCode}'.");
        }

        if (!profile.IsActive)
        {
            throw new InvalidOperationException($"Email profile '{profile.EmailProfileKey}' for shift '{shiftCode}' is inactive.");
        }

        return profile with { ShiftCode = shiftCode };
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
}
