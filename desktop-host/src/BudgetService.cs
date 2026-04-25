using System;
using System.Collections.Generic;

namespace MoatHouseHandover.Host;

public sealed class BudgetService
{
    private readonly BudgetRepository _repository;

    public BudgetService(BudgetRepository repository)
    {
        _repository = repository;
    }

    public BudgetPayload LoadBudget(BudgetLoadRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        return _repository.LoadBudget(request.SessionId, NormalizeUser(request.UserName));
    }

    public BudgetPayload SaveBudget(BudgetSaveRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        var rows = request.Rows ?? Array.Empty<BudgetRowUpsertRequest>();
        ValidateRows(rows);
        return _repository.SaveBudget(request.SessionId, rows, NormalizeUser(request.UserName));
    }

    public BudgetPayload Recalculate(BudgetRecalculateRequest request)
    {
        if (request.SessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        var rows = request.Rows ?? Array.Empty<BudgetRowUpsertRequest>();
        ValidateRows(rows);
        return _repository.Recalculate(request.SessionId, rows);
    }

    public BudgetSummaryPayload LoadBudgetSummary(long sessionId)
    {
        if (sessionId <= 0)
        {
            throw new InvalidOperationException("SessionId is required.");
        }

        return _repository.LoadBudgetSummary(sessionId);
    }

    private static void ValidateRows(IReadOnlyList<BudgetRowUpsertRequest> rows)
    {
        for (var i = 0; i < rows.Count; i += 1)
        {
            var row = rows[i];
            if (string.IsNullOrWhiteSpace(row.DeptName))
            {
                throw new InvalidOperationException($"Budget row {i + 1}: deptName is required.");
            }

            if (row.PlannedQty.HasValue && row.PlannedQty.Value < 0)
            {
                throw new InvalidOperationException($"Budget row '{row.DeptName}': planned value cannot be negative.");
            }

            if (row.UsedQty.HasValue && row.UsedQty.Value < 0)
            {
                throw new InvalidOperationException($"Budget row '{row.DeptName}': used value cannot be negative.");
            }
        }
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
