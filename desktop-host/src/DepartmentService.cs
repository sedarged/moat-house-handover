using System;

namespace MoatHouseHandover.Host;

public sealed class DepartmentService
{
    private readonly IDepartmentRepository _repository;
    private readonly AuditLogService _auditLogService;

    public DepartmentService(IDepartmentRepository repository, AuditLogService auditLogService)
    {
        _repository = repository;
        _auditLogService = auditLogService;
    }

    public DepartmentPayload LoadDepartment(DepartmentLoadRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        var deptName = NormalizeDeptName(request.DeptName);
        return _repository.LoadDepartment(request.SessionId, deptName);
    }

    public DepartmentSaveResult SaveDepartment(DepartmentSaveRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        var normalized = request with
        {
            DeptName = NormalizeDeptName(request.DeptName),
            DeptStatus = string.IsNullOrWhiteSpace(request.DeptStatus) ? "Not running" : request.DeptStatus.Trim(),
            DeptNotes = request.DeptNotes?.Trim() ?? string.Empty,
            UserName = NormalizeUser(request.UserName)
        };

        ValidateMetrics(normalized);
        var result = _repository.SaveDepartment(normalized, normalized.UserName);
        _auditLogService.BestEffortLog(
            actionType: "department.save",
            entityType: "HandoverDept",
            entityKey: $"{AuditLogService.BuildSessionKey(normalized.SessionId)}|dept:{normalized.DeptName}",
            userName: normalized.UserName,
            details: new { normalized.SessionId, normalized.DeptName, normalized.DeptStatus });

        return result;
    }

    private static void ValidateMetrics(DepartmentSaveRequest request)
    {
        var isMetricDept = string.Equals(request.DeptName, "Injection", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.DeptName, "MetaPress", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.DeptName, "Berks", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.DeptName, "Wilts", StringComparison.OrdinalIgnoreCase);
        if (!isMetricDept)
        {
            return;
        }

        if (request.DowntimeMin.HasValue && request.DowntimeMin.Value < 0)
        {
            throw new InvalidOperationException("DowntimeMin must be zero or greater.");
        }

        if (request.EfficiencyPct.HasValue && (request.EfficiencyPct.Value < 0 || request.EfficiencyPct.Value > 100))
        {
            throw new InvalidOperationException("EfficiencyPct must be between 0 and 100.");
        }

        if (request.YieldPct.HasValue && (request.YieldPct.Value < 0 || request.YieldPct.Value > 100))
        {
            throw new InvalidOperationException("YieldPct must be between 0 and 100.");
        }
    }

    private static string NormalizeDeptName(string deptName)
    {
        var normalized = (deptName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("deptName is required.");
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
