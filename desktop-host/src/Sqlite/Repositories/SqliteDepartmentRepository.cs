using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteDepartmentRepository : SqliteRepositoryBase, IDepartmentRepository
{
    private static readonly HashSet<string> MetricDepartments = new(StringComparer.OrdinalIgnoreCase) { "Injection", "MetaPress", "Berks", "Wilts" };

    public SqliteDepartmentRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }

    public DepartmentPayload LoadDepartment(long sessionId, string deptName)
    {
        using var c = OpenConnection();
        return LoadDepartmentCore(c, sessionId, deptName) ?? throw new InvalidOperationException();
    }

    public DepartmentSaveResult SaveDepartment(DepartmentSaveRequest request, string userName)
    {
        using var c = OpenConnection();
        var isMetric = MetricDepartments.Contains(request.DeptName);
        using var u = c.CreateCommand();
        u.CommandText = @"UPDATE tblHandoverDept SET DeptStatus=$st, DeptNotes=$notes, DowntimeMin=$down, EfficiencyPct=$eff, YieldPct=$yield,
UpdatedAt=$at, UpdatedBy=$user, VersionNo=COALESCE(VersionNo,0)+1
WHERE HandoverID=$sid AND DeptName=$dept AND COALESCE(IsDeleted,0)=0";
        u.Parameters.AddWithValue("$st", string.IsNullOrWhiteSpace(request.DeptStatus) ? "Not running" : request.DeptStatus.Trim());
        u.Parameters.AddWithValue("$notes", request.DeptNotes?.Trim() ?? string.Empty);
        u.Parameters.AddWithValue("$down", isMetric && request.DowntimeMin.HasValue ? request.DowntimeMin.Value : DBNull.Value);
        u.Parameters.AddWithValue("$eff", isMetric && request.EfficiencyPct.HasValue ? request.EfficiencyPct.Value : DBNull.Value);
        u.Parameters.AddWithValue("$yield", isMetric && request.YieldPct.HasValue ? request.YieldPct.Value : DBNull.Value);
        u.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        u.Parameters.AddWithValue("$user", userName);
        u.Parameters.AddWithValue("$sid", request.SessionId);
        u.Parameters.AddWithValue("$dept", request.DeptName);
        var affected = u.ExecuteNonQuery();
        if (affected <= 0)
        {
            throw new InvalidOperationException($"Department '{request.DeptName}' for session '{request.SessionId}' could not be updated.");
        }

        return new DepartmentSaveResult(LoadDepartmentCore(c, request.SessionId, request.DeptName)!, LoadDashboardDepartmentSummary(c, request.SessionId));
    }

    private static DepartmentPayload? LoadDepartmentCore(SqliteConnection c, long sessionId, string deptName)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT DeptRecordID, HandoverID, DeptName, DeptStatus, DeptNotes, DowntimeMin, EfficiencyPct, YieldPct, UpdatedAt, UpdatedBy
FROM tblHandoverDept
WHERE HandoverID=$sid AND DeptName=$dept AND COALESCE(IsDeleted,0)=0 LIMIT 1";
        cmd.Parameters.AddWithValue("$sid", sessionId);
        cmd.Parameters.AddWithValue("$dept", deptName);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var dn = r.IsDBNull(2) ? string.Empty : r.GetString(2);
        var isMetric = MetricDepartments.Contains(dn);
        return new DepartmentPayload(r.GetInt64(0), r.GetInt64(1), dn, r.IsDBNull(3) ? "Not running" : r.GetString(3), r.IsDBNull(4) ? string.Empty : r.GetString(4), isMetric && !r.IsDBNull(5) ? r.GetInt32(5) : null, isMetric && !r.IsDBNull(6) ? r.GetDouble(6) : null, isMetric && !r.IsDBNull(7) ? r.GetDouble(7) : null, r.IsDBNull(8) ? null : r.GetString(8), r.IsDBNull(9) ? null : r.GetString(9), isMetric);
    }

    private static List<DepartmentSummaryPayload> LoadDashboardDepartmentSummary(SqliteConnection c, long sessionId)
    {
        var result = new List<DepartmentSummaryPayload>();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT d.DeptName, d.DeptStatus, d.UpdatedAt, d.UpdatedBy,
SUM(CASE WHEN a.AttachmentID IS NOT NULL AND COALESCE(a.IsDeleted,0)=0 THEN 1 ELSE 0 END) AS AttachmentCount
FROM tblHandoverDept d
LEFT JOIN tblDepartments cfg ON d.DeptName = cfg.DeptName
LEFT JOIN tblAttachments a ON a.DeptRecordID = d.DeptRecordID
WHERE d.HandoverID=$sid AND COALESCE(d.IsDeleted,0)=0
GROUP BY d.DeptName,d.DeptStatus,d.UpdatedAt,d.UpdatedBy,cfg.DisplayOrder
ORDER BY cfg.DisplayOrder,d.DeptName";
        cmd.Parameters.AddWithValue("$sid", sessionId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            result.Add(new DepartmentSummaryPayload(r.IsDBNull(0)?string.Empty:r.GetString(0), r.IsDBNull(1)?"Not running":r.GetString(1), r.IsDBNull(2)?null:r.GetString(2), r.IsDBNull(3)?null:r.GetString(3), r.IsDBNull(4)?0:r.GetInt32(4)));
        }
        return result;
    }
}
