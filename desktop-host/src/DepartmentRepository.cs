using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MoatHouseHandover.Host;

public sealed class DepartmentRepository
{
    private static readonly HashSet<string> MetricDepartments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Injection",
        "MetaPress",
        "Berks",
        "Wilts"
    };

    private readonly string _connectionString;

    public DepartmentRepository(string accessDatabasePath)
    {
        _connectionString = AccessBootstrapper.BuildConnectionString(accessDatabasePath);
    }

    public DepartmentPayload LoadDepartment(long sessionId, string deptName)
    {
        using var connection = OpenConnection();
        return LoadDepartmentCore(connection, sessionId, deptName)
            ?? throw new InvalidOperationException($"Department '{deptName}' for session '{sessionId}' was not found.");
    }

    public DepartmentSaveResult SaveDepartment(DepartmentSaveRequest request, string userName)
    {
        using var connection = OpenConnection();

        var now = DateTime.Now;
        var isMetricDept = IsMetricDepartment(request.DeptName);
        var deptStatus = string.IsNullOrWhiteSpace(request.DeptStatus) ? "Not running" : request.DeptStatus.Trim();
        var deptNotes = request.DeptNotes?.Trim() ?? string.Empty;
        int? downtimeMin = isMetricDept ? request.DowntimeMin : null;
        double? efficiencyPct = isMetricDept ? request.EfficiencyPct : null;
        double? yieldPct = isMetricDept ? request.YieldPct : null;

        using (var update = new OleDbCommand(@"UPDATE tblHandoverDept
SET DeptStatus = ?, DeptNotes = ?, DowntimeMin = ?, EfficiencyPct = ?, YieldPct = ?, UpdatedAt = ?, UpdatedBy = ?, VersionNo = VersionNo + 1
WHERE HandoverID = ? AND DeptName = ? AND (IsDeleted = FALSE OR IsDeleted IS NULL)", connection))
        {
            update.Parameters.AddWithValue("@p1", deptStatus);
            update.Parameters.AddWithValue("@p2", deptNotes);
            update.Parameters.AddWithValue("@p3", ToDbValue(downtimeMin));
            update.Parameters.AddWithValue("@p4", ToDbValue(efficiencyPct));
            update.Parameters.AddWithValue("@p5", ToDbValue(yieldPct));
            update.Parameters.AddWithValue("@p6", now);
            update.Parameters.AddWithValue("@p7", userName);
            update.Parameters.AddWithValue("@p8", request.SessionId);
            update.Parameters.AddWithValue("@p9", request.DeptName);

            var affected = update.ExecuteNonQuery();
            if (affected <= 0)
            {
                throw new InvalidOperationException($"Department '{request.DeptName}' for session '{request.SessionId}' could not be updated.");
            }
        }

        var department = LoadDepartmentCore(connection, request.SessionId, request.DeptName)
            ?? throw new InvalidOperationException("Department save completed but reload failed.");

        var dashboardDepartments = LoadDashboardDepartmentSummary(connection, request.SessionId);
        return new DepartmentSaveResult(department, dashboardDepartments);
    }

    private DepartmentPayload? LoadDepartmentCore(OleDbConnection connection, long sessionId, string deptName)
    {
        const string sql = @"SELECT DeptRecordID, HandoverID, DeptName, DeptStatus, DeptNotes, DowntimeMin, EfficiencyPct, YieldPct, UpdatedAt, UpdatedBy
FROM tblHandoverDept
WHERE HandoverID = ? AND DeptName = ? AND (IsDeleted = FALSE OR IsDeleted IS NULL)";

        using var cmd = new OleDbCommand(sql, connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);
        cmd.Parameters.AddWithValue("@p2", deptName);

        using var reader = cmd.ExecuteReader();
        if (!reader!.Read())
        {
            return null;
        }

        var loadedDeptName = Convert.ToString(reader["DeptName"]) ?? string.Empty;
        var isMetricDept = IsMetricDepartment(loadedDeptName);

        return new DepartmentPayload(
            DeptRecordId: Convert.ToInt64(reader["DeptRecordID"]),
            SessionId: Convert.ToInt64(reader["HandoverID"]),
            DeptName: loadedDeptName,
            DeptStatus: Convert.ToString(reader["DeptStatus"]) ?? "Not running",
            DeptNotes: Convert.ToString(reader["DeptNotes"]) ?? string.Empty,
            DowntimeMin: isMetricDept && reader["DowntimeMin"] != DBNull.Value ? Convert.ToInt32(reader["DowntimeMin"]) : null,
            EfficiencyPct: isMetricDept && reader["EfficiencyPct"] != DBNull.Value ? Convert.ToDouble(reader["EfficiencyPct"]) : null,
            YieldPct: isMetricDept && reader["YieldPct"] != DBNull.Value ? Convert.ToDouble(reader["YieldPct"]) : null,
            UpdatedAt: reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]),
            UpdatedBy: reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"]),
            IsMetricDept: isMetricDept);
    }

    private static List<DepartmentSummaryPayload> LoadDashboardDepartmentSummary(OleDbConnection connection, long sessionId)
    {
        var result = new List<DepartmentSummaryPayload>();
        const string sql = @"SELECT d.DeptName, d.DeptStatus, d.UpdatedAt, d.UpdatedBy,
SUM(IIF(a.AttachmentID IS NOT NULL AND (a.IsDeleted = FALSE OR a.IsDeleted IS NULL), 1, 0)) AS AttachmentCount
FROM tblHandoverDept AS d
LEFT JOIN tblDepartments AS cfg ON d.DeptName = cfg.DeptName
LEFT JOIN tblAttachments AS a ON a.DeptRecordID = d.DeptRecordID
WHERE d.HandoverID = ? AND (d.IsDeleted = FALSE OR d.IsDeleted IS NULL)
GROUP BY d.DeptName, d.DeptStatus, d.UpdatedAt, d.UpdatedBy, cfg.DisplayOrder
ORDER BY cfg.DisplayOrder, d.DeptName";

        using var cmd = new OleDbCommand(sql, connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);
        using var reader = cmd.ExecuteReader();
        while (reader!.Read())
        {
            result.Add(new DepartmentSummaryPayload(
                DeptName: Convert.ToString(reader["DeptName"]) ?? string.Empty,
                DeptStatus: Convert.ToString(reader["DeptStatus"]) ?? "Not running",
                UpdatedAt: reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]),
                UpdatedBy: reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"]),
                AttachmentCount: reader["AttachmentCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["AttachmentCount"])));
        }

        return result;
    }

    private OleDbConnection OpenConnection()
    {
        var connection = new OleDbConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static bool IsMetricDepartment(string deptName)
    {
        return MetricDepartments.Contains((deptName ?? string.Empty).Trim());
    }

    private static object ToDbValue<T>(T? value) where T : struct
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }

    private static string ToIso(object dateValue)
    {
        var date = Convert.ToDateTime(dateValue);
        return date.ToUniversalTime().ToString("o");
    }
}
