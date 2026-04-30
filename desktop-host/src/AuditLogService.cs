using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace MoatHouseHandover.Host;

public sealed class AuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly BootstrapLogger _logger;

    public AuditLogService(IAuditLogRepository repository, BootstrapLogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public void BestEffortLog(string actionType, string entityType, string entityKey, string userName, object? details = null)
    {
        try
        {
            var request = new AuditLogWriteRequest(
                ActionType: Normalize(actionType, "unknown"),
                EntityType: Normalize(entityType, "unknown"),
                EntityKey: Normalize(entityKey, "unknown"),
                UserName: Normalize(userName, Environment.UserName),
                Details: SerializeDetails(details));

            _repository.Insert(request);
        }
        catch (Exception ex)
        {
            _logger.Log($"Audit log write failed (best-effort): {ex.Message}");
        }
    }

    public AuditListResult ListRecent(AuditListRecentRequest request)
    {
        try
        {
            var limit = ClampLimit(request.Limit);
            var entries = _repository.ListRecent(limit);
            return new AuditListResult(true, entries, null);
        }
        catch (Exception ex)
        {
            return new AuditListResult(false, Array.Empty<AuditLogEntry>(), ex.Message);
        }
    }

    public AuditListResult ListForSession(AuditListForSessionRequest request)
    {
        if (request.SessionId <= 0)
        {
            return new AuditListResult(false, Array.Empty<AuditLogEntry>(), "SessionId is required.");
        }

        try
        {
            var limit = ClampLimit(request.Limit);
            var entries = _repository.ListForSession(request.SessionId, limit);
            return new AuditListResult(true, entries, null);
        }
        catch (Exception ex)
        {
            return new AuditListResult(false, Array.Empty<AuditLogEntry>(), ex.Message);
        }
    }

    private static int ClampLimit(int? limit)
    {
        if (!limit.HasValue)
        {
            return 25;
        }

        return Math.Max(1, Math.Min(100, limit.Value));
    }

    private static string SerializeDetails(object? details)
    {
        if (details is null)
        {
            return string.Empty;
        }

        string text;
        if (details is string raw)
        {
            text = raw;
        }
        else
        {
            text = JsonSerializer.Serialize(details);
        }

        if (text.Length > 2000)
        {
            return text[..2000] + "...(truncated)";
        }

        return text;
    }

    private static string Normalize(string value, string fallback)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    public static string BuildSessionKey(long sessionId)
    {
        return "session:" + sessionId.ToString(CultureInfo.InvariantCulture);
    }
}
