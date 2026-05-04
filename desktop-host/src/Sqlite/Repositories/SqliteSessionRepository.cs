using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteSessionRepository : SqliteRepositoryBase, ISessionRepository
{
    public SqliteSessionRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }

    public SessionPayload? OpenExistingSession(string shiftCode, DateTime shiftDate, string userName)
    {
        using var connection = OpenConnection();
        var header = FindHeader(connection, shiftCode, shiftDate);
        if (header is null) return null;
        EnsureDepartmentRows(connection, header.Value.HandoverId, userName);
        return LoadSessionPayload(connection, header.Value.HandoverId);
    }

    public SessionCreateResult CreateBlankSession(string shiftCode, DateTime shiftDate, string userName)
    {
        using var connection = OpenConnection();
        var existing = FindHeader(connection, shiftCode, shiftDate);
        if (existing is not null)
        {
            EnsureDepartmentRows(connection, existing.Value.HandoverId, userName);
            return new SessionCreateResult(false, LoadSessionPayload(connection, existing.Value.HandoverId)!);
        }

        using var insert = connection.CreateCommand();
        insert.CommandText = @"INSERT INTO tblHandoverHeader
(ShiftDate, ShiftCode, SessionStatus, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
VALUES ($date, $shift, 'Open', $now, $user, $now, $user)";
        insert.Parameters.AddWithValue("$date", shiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$shift", shiftCode);
        insert.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$user", userName);
        insert.ExecuteNonQuery();

        var header = FindHeader(connection, shiftCode, shiftDate) ?? throw new InvalidOperationException();
        EnsureDepartmentRows(connection, header.HandoverId, userName);
        return new SessionCreateResult(true, LoadSessionPayload(connection, header.HandoverId)!);
    }

    public SessionPayload ClearDay(long sessionId, string userName)
    {
        using var connection = OpenConnection();
        using var tx = connection.BeginTransaction();

        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        using (var update = connection.CreateCommand())
        {
            update.Transaction = tx;
            update.CommandText = "UPDATE tblHandoverHeader SET SessionStatus='Open', UpdatedAt=$now, UpdatedBy=$user WHERE HandoverID=$id";
            update.Parameters.AddWithValue("$now", now);
            update.Parameters.AddWithValue("$user", userName);
            update.Parameters.AddWithValue("$id", sessionId);
            update.ExecuteNonQuery();
        }

        foreach (var sql in new[]{
            "DELETE FROM tblAttachments WHERE HandoverID=$id",
            "DELETE FROM tblBudgetRows WHERE BudgetHeaderID IN (SELECT BudgetHeaderID FROM tblBudgetHeader WHERE HandoverID=$id)",
            "DELETE FROM tblBudgetHeader WHERE HandoverID=$id",
            "DELETE FROM tblHandoverDept WHERE HandoverID=$id"})
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$id", sessionId);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        EnsureDepartmentRows(connection, sessionId, userName);
        return LoadSessionPayload(connection, sessionId) ?? throw new InvalidOperationException();
    }


    public IReadOnlyList<SessionListItem> ListSessions(SessionListFilters filters)
    {
        using var c = OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT HandoverID,ShiftCode,ShiftDate,SessionStatus,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy FROM tblHandoverHeader";
        var items = new List<SessionListItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var shiftCode = r.IsDBNull(1) ? string.Empty : r.GetString(1);
            items.Add(new SessionListItem(
                r.GetInt64(0),
                shiftCode,
                shiftCode == "NS" ? "Night Shift" : string.IsNullOrWhiteSpace(shiftCode) ? string.Empty : $"{shiftCode} Shift",
                r.IsDBNull(2) ? string.Empty : r.GetString(2),
                r.IsDBNull(3) ? "Open" : r.GetString(3),
                r.IsDBNull(4) ? DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) : r.GetString(4),
                r.IsDBNull(5) ? string.Empty : r.GetString(5),
                r.IsDBNull(6) ? DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) : r.GetString(6),
                r.IsDBNull(7) ? string.Empty : r.GetString(7)));
        }

        IEnumerable<SessionListItem> filtered = items;
        if (!string.IsNullOrWhiteSpace(filters.ShiftCode)) filtered = filtered.Where(i => i.ShiftCode.Equals(filters.ShiftCode, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filters.ShiftDate)) filtered = filtered.Where(i => i.ShiftDate == filters.ShiftDate);
        if (!string.IsNullOrWhiteSpace(filters.SessionStatus)) filtered = filtered.Where(i => i.SessionStatus.Equals(filters.SessionStatus, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            filtered = filtered.Where(i => i.SessionId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                || i.CreatedBy.Contains(search, StringComparison.OrdinalIgnoreCase)
                || i.UpdatedBy.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        return filtered.OrderByDescending(i => i.ShiftDate).ThenByDescending(i => i.SessionId).ToList();
    }

    public SessionPayload? OpenSessionById(long sessionId, string userName)
    {
        using var c = OpenConnection();
        EnsureDepartmentRows(c, sessionId, userName);
        return LoadSessionPayload(c, sessionId);
    }
    private static void EnsureDepartmentRows(SqliteConnection connection, long sessionId, string userName)
    {
        var depts = new List<string>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT DeptName FROM tblDepartments WHERE IsActive=1 ORDER BY DisplayOrder";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) depts.Add(reader.GetString(0));
        }

        foreach (var deptName in depts)
        {
            using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT COUNT(*) FROM tblHandoverDept WHERE HandoverID=$sid AND DeptName=$dept";
            exists.Parameters.AddWithValue("$sid", sessionId);
            exists.Parameters.AddWithValue("$dept", deptName);
            if (Convert.ToInt32(exists.ExecuteScalar()) > 0) continue;

            using var insert = connection.CreateCommand();
            insert.CommandText = @"INSERT INTO tblHandoverDept
(HandoverID, DeptName, DeptStatus, DowntimeMin, EfficiencyPct, YieldPct, DeptNotes, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, VersionNo, IsDeleted)
VALUES ($sid, $dept, 'Not running', NULL, NULL, NULL, '', $now, $user, $now, $user, 1, 0)";
            insert.Parameters.AddWithValue("$sid", sessionId);
            insert.Parameters.AddWithValue("$dept", deptName);
            insert.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$user", userName);
            insert.ExecuteNonQuery();
        }
    }

    private static SessionPayload? LoadSessionPayload(SqliteConnection connection, long sessionId)
    {
        var header = FindHeader(connection, sessionId);
        if (header is null) return null;

        var departments = new List<DepartmentSummaryPayload>();
        using var dept = connection.CreateCommand();
        dept.CommandText = @"SELECT d.DeptName, d.DeptStatus, d.UpdatedAt, d.UpdatedBy,
SUM(CASE WHEN a.AttachmentID IS NOT NULL AND COALESCE(a.IsDeleted,0)=0 THEN 1 ELSE 0 END) AS AttachmentCount
FROM tblHandoverDept d
LEFT JOIN tblDepartments cfg ON cfg.DeptName=d.DeptName
LEFT JOIN tblAttachments a ON a.DeptRecordID=d.DeptRecordID
WHERE d.HandoverID=$sid AND COALESCE(d.IsDeleted,0)=0
GROUP BY d.DeptName,d.DeptStatus,d.UpdatedAt,d.UpdatedBy,cfg.DisplayOrder
ORDER BY cfg.DisplayOrder,d.DeptName";
        dept.Parameters.AddWithValue("$sid", sessionId);
        using var r = dept.ExecuteReader();
        while (r.Read())
        {
            departments.Add(new DepartmentSummaryPayload(r.IsDBNull(0)?"":r.GetString(0), r.IsDBNull(1)?"Not running":r.GetString(1), r.IsDBNull(2)?null:r.GetString(2), r.IsDBNull(3)?null:r.GetString(3), r.IsDBNull(4)?0:r.GetInt32(4)));
        }

        return new SessionPayload(header.Value.HandoverId, header.Value.ShiftCode, header.Value.ShiftDate, header.Value.SessionStatus, departments, header.Value.CreatedAt, header.Value.CreatedBy, header.Value.UpdatedAt, header.Value.UpdatedBy);
    }

    private static HeaderRow? FindHeader(SqliteConnection c, string shiftCode, DateTime shiftDate)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT HandoverID,ShiftCode,ShiftDate,SessionStatus,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy FROM tblHandoverHeader WHERE ShiftCode=$s AND ShiftDate=$d LIMIT 1";
        cmd.Parameters.AddWithValue("$s", shiftCode);
        cmd.Parameters.AddWithValue("$d", shiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        using var r = cmd.ExecuteReader();
        return ReadHeader(r);
    }

    private static HeaderRow? FindHeader(SqliteConnection c, long sessionId)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT HandoverID,ShiftCode,ShiftDate,SessionStatus,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy FROM tblHandoverHeader WHERE HandoverID=$id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", sessionId);
        using var r = cmd.ExecuteReader();
        return ReadHeader(r);
    }

    private static HeaderRow? ReadHeader(SqliteDataReader r)
    {
        if (!r.Read()) return null;
        return new HeaderRow(r.GetInt64(0), r.IsDBNull(1)?"":r.GetString(1), r.IsDBNull(2)?"":r.GetString(2), r.IsDBNull(3)?"Open":r.GetString(3), r.IsDBNull(4)?DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture):r.GetString(4), r.IsDBNull(5)?"":r.GetString(5), r.IsDBNull(6)?DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture):r.GetString(6), r.IsDBNull(7)?"":r.GetString(7));
    }

    private readonly record struct HeaderRow(long HandoverId, string ShiftCode, string ShiftDate, string SessionStatus, string CreatedAt, string CreatedBy, string UpdatedAt, string UpdatedBy);
}
