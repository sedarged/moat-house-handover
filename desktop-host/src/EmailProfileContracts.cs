namespace MoatHouseHandover.Host;

public sealed record EmailProfileLoadRequest(string ShiftCode);

public sealed record EmailProfilePayload(
    string EmailProfileKey,
    string ShiftCode,
    string ToList,
    string CcList,
    string SubjectTemplate,
    string BodyTemplate,
    bool IsActive);
