using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;

namespace MoatHouseHandover.Host;

public sealed class PreviewRepository : IPreviewRepository
{
    private readonly string _connectionString;

    public PreviewRepository(string accessDatabasePath)
    {
        _connectionString = AccessBootstrapper.BuildConnectionString(accessDatabasePath);
    }

    public PreviewPayload LoadPreview(long sessionId)
    {
        using var connection = OpenConnection();

        var header = LoadHeader(connection, sessionId)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        var departments = LoadDepartments(connection, sessionId);
        var attachmentSummary = LoadAttachmentSummary(connection, sessionId);
        var budgetRows = LoadBudgetRows(connection, sessionId);
        var budgetSummary = BuildBudgetSummary(budgetRows);

        return new PreviewPayload(header, departments, attachmentSummary, budgetSummary, budgetRows);
    }

    private static PreviewSessionHeader? LoadHeader(OleDbConnection connection, long sessionId)
    {
        using var cmd = new OleDbCommand(@"SELECT TOP 1 HandoverID, ShiftCode, ShiftDate, SessionStatus, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
FROM tblHandoverHeader
WHERE HandoverID = ?", connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);
        using var reader = cmd.ExecuteReader();
        if (!reader!.Read())
        {
            return null;
        }

        var createdAt = reader["CreatedAt"] == DBNull.Value ? null : ToIso(reader["CreatedAt"]);
        var updatedAt = reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]);

        return new PreviewSessionHeader(
            SessionId: Convert.ToInt64(reader["HandoverID"]),
            ShiftCode: Convert.ToString(reader["ShiftCode"]) ?? string.Empty,
            ShiftDate: Convert.ToDateTime(reader["ShiftDate"]).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SessionStatus: Convert.ToString(reader["SessionStatus"]) ?? "Open",
            CreatedAt: createdAt,
            CreatedBy: Convert.ToString(reader["CreatedBy"]) ?? string.Empty,
            UpdatedAt: updatedAt,
            UpdatedBy: Convert.ToString(reader["UpdatedBy"]) ?? string.Empty);
    }

    private static List<PreviewDepartmentSummary> LoadDepartments(OleDbConnection connection, long sessionId)
    {
        var departments = new List<PreviewDepartmentSummary>();

        using var cmd = new OleDbCommand(@"SELECT d.DeptRecordID, d.DeptName, d.DeptStatus, d.DowntimeMin, d.EfficiencyPct, d.YieldPct,
d.DeptNotes, d.UpdatedAt, d.UpdatedBy,
SUM(IIF(a.AttachmentID IS NOT NULL AND (a.IsDeleted = FALSE OR a.IsDeleted IS NULL), 1, 0)) AS AttachmentCount,
cfg.DisplayOrder
FROM tblHandoverDept AS d
LEFT JOIN tblDepartments AS cfg ON cfg.DeptName = d.DeptName
LEFT JOIN tblAttachments AS a ON a.DeptRecordID = d.DeptRecordID
WHERE d.HandoverID = ? AND (d.IsDeleted = FALSE OR d.IsDeleted IS NULL)
GROUP BY d.DeptRecordID, d.DeptName, d.DeptStatus, d.DowntimeMin, d.EfficiencyPct, d.YieldPct, d.DeptNotes, d.UpdatedAt, d.UpdatedBy, cfg.DisplayOrder
ORDER BY cfg.DisplayOrder, d.DeptName", connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);

        using var reader = cmd.ExecuteReader();
        while (reader!.Read())
        {
            departments.Add(new PreviewDepartmentSummary(
                DeptRecordId: Convert.ToInt64(reader["DeptRecordID"]),
                DeptName: Convert.ToString(reader["DeptName"]) ?? string.Empty,
                DeptStatus: Convert.ToString(reader["DeptStatus"]) ?? "Not running",
                DowntimeMin: reader["DowntimeMin"] == DBNull.Value ? null : Convert.ToInt32(reader["DowntimeMin"]),
                EfficiencyPct: reader["EfficiencyPct"] == DBNull.Value ? null : Convert.ToDouble(reader["EfficiencyPct"]),
                YieldPct: reader["YieldPct"] == DBNull.Value ? null : Convert.ToDouble(reader["YieldPct"]),
                Notes: Convert.ToString(reader["DeptNotes"]) ?? string.Empty,
                UpdatedAt: reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]),
                UpdatedBy: reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"]),
                AttachmentCount: reader["AttachmentCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["AttachmentCount"])));
        }

        return departments;
    }

    private static List<PreviewAttachmentDepartmentSummary> LoadAttachmentSummary(OleDbConnection connection, long sessionId)
    {
        var grouped = new Dictionary<string, List<PreviewAttachmentMeta>>(StringComparer.OrdinalIgnoreCase);

        using var cmd = new OleDbCommand(@"SELECT DeptName, AttachmentID, DisplayName, CapturedOn, SequenceNo
FROM tblAttachments
WHERE HandoverID = ? AND (IsDeleted = FALSE OR IsDeleted IS NULL)
ORDER BY DeptName, SequenceNo, AttachmentID", connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);

        using var reader = cmd.ExecuteReader();
        while (reader!.Read())
        {
            var deptName = Convert.ToString(reader["DeptName"]) ?? string.Empty;
            if (!grouped.TryGetValue(deptName, out var list))
            {
                list = new List<PreviewAttachmentMeta>();
                grouped[deptName] = list;
            }

            list.Add(new PreviewAttachmentMeta(
                AttachmentId: Convert.ToInt64(reader["AttachmentID"]),
                DisplayName: Convert.ToString(reader["DisplayName"]) ?? string.Empty,
                CapturedOn: reader["CapturedOn"] == DBNull.Value ? null : ToIso(reader["CapturedOn"]),
                SequenceNo: reader["SequenceNo"] == DBNull.Value ? 0 : Convert.ToInt64(reader["SequenceNo"])));
        }

        return grouped
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new PreviewAttachmentDepartmentSummary(pair.Key, pair.Value.Count, pair.Value))
            .ToList();
    }

    private static List<PreviewBudgetRowSummary> LoadBudgetRows(OleDbConnection connection, long sessionId)
    {
        var rows = new List<PreviewBudgetRowSummary>();

        using var cmd = new OleDbCommand(@"SELECT r.DeptName, r.PlannedQty, r.UsedQty, r.VarianceQty, r.ReasonText, r.UpdatedAt, r.UpdatedBy, d.DisplayOrder
FROM tblBudgetRows AS r
INNER JOIN tblBudgetHeader AS h ON h.BudgetHeaderID = r.BudgetHeaderID
LEFT JOIN tblDepartments AS d ON d.DeptName = r.DeptName
WHERE h.HandoverID = ?
ORDER BY d.DisplayOrder, r.DeptName, r.BudgetRowID", connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);

        using var reader = cmd.ExecuteReader();
        while (reader!.Read())
        {
            double? planned = reader["PlannedQty"] == DBNull.Value ? null : Convert.ToDouble(reader["PlannedQty"]);
            double? used = reader["UsedQty"] == DBNull.Value ? null : Convert.ToDouble(reader["UsedQty"]);
            var variance = reader["VarianceQty"] == DBNull.Value
                ? (planned ?? 0) - (used ?? 0)
                : Convert.ToDouble(reader["VarianceQty"]);

            rows.Add(new PreviewBudgetRowSummary(
                DeptName: Convert.ToString(reader["DeptName"]) ?? string.Empty,
                PlannedQty: planned,
                UsedQty: used,
                Variance: variance,
                Status: ResolveBudgetStatus(planned, used, variance),
                ReasonText: reader["ReasonText"] == DBNull.Value ? string.Empty : (Convert.ToString(reader["ReasonText"]) ?? string.Empty),
                UpdatedAt: reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]),
                UpdatedBy: reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"])));
        }

        return rows;
    }

    private static PreviewBudgetSummary BuildBudgetSummary(IReadOnlyList<PreviewBudgetRowSummary> rows)
    {
        var planned = rows.Sum(row => row.PlannedQty ?? 0);
        var used = rows.Sum(row => row.UsedQty ?? 0);
        var variance = planned - used;
        var status = ResolveBudgetStatus(planned, used, variance);

        var lastUpdated = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.UpdatedAt))
            .OrderByDescending(row => row.UpdatedAt, StringComparer.Ordinal)
            .FirstOrDefault();

        return new PreviewBudgetSummary(
            PlannedTotal: planned,
            UsedTotal: used,
            VarianceTotal: variance,
            Status: status,
            LastUpdatedAt: lastUpdated?.UpdatedAt,
            LastUpdatedBy: lastUpdated?.UpdatedBy,
            RowCount: rows.Count);
    }

    private static string ResolveBudgetStatus(double? planned, double? used, double variance)
    {
        var plannedValue = planned ?? 0;
        var usedValue = used ?? 0;

        if (Math.Abs(plannedValue) < 0.0001 && Math.Abs(usedValue) < 0.0001)
        {
            return "not set";
        }

        if (Math.Abs(variance) < 0.0001)
        {
            return "on target";
        }

        return variance < 0 ? "over" : "under";
    }

    private OleDbConnection OpenConnection()
    {
        var connection = new OleDbConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string ToIso(object dateValue)
    {
        var value = Convert.ToDateTime(dateValue);
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }
}
