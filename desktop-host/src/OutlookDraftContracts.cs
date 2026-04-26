namespace MoatHouseHandover.Host;

public sealed record OutlookDraftRequest(
    string ToList,
    string CcList,
    string Subject,
    string Body,
    System.Collections.Generic.IReadOnlyList<string> AttachmentPaths);

public sealed record OutlookDraftResult(
    bool DraftCreated,
    string Message,
    string? DraftEntryId,
    string CreatedAt,
    int AttachmentCount);
