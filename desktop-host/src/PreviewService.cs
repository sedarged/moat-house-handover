using System;

namespace MoatHouseHandover.Host;

public sealed class PreviewService
{
    private readonly PreviewRepository _repository;

    public PreviewService(PreviewRepository repository)
    {
        _repository = repository;
    }

    public PreviewPayload LoadPreview(PreviewLoadRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        return _repository.LoadPreview(request.SessionId);
    }
}
