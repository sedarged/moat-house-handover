using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;

namespace MoatHouseHandover.Host;

public sealed class SessionRepository
{
    private readonly string _connectionString;

    public SessionRepository(string accessDatabasePath)
    {
        _connectionString = AccessBootstrapper.BuildConnectionString(accessDatabasePath);
    }

    public SessionPayload? OpenExistingSession(string shiftCode, DateTime shiftDate)
    {
        using var connection = OpenConnection();
        var header = FindHeader(connection, shiftCode, shiftDate);
        if (header is null)
        {
            return null;
        }

        EnsureDepartmentRows(connection, header.Value.HandoverId, "system");
        return LoadSessionPayload(connection, header.Value.HandoverId);
    }

    public SessionCreateResult CreateBlankSession(string shiftCode, DateTime shiftDate, string userName)
    {
        using var connection = OpenConnection();

        var existing = FindHeader(connection, shiftCode, shiftDate);
        if (existing is not null)
        {
            EnsureDepartmentRows(connection, existing.Value.HandoverId, userName);
            var existingPayload = LoadSessionPayload(connection, existing.Value.HandoverId)
                ?? throw new InvalidOperationException("Unable to load existing session after lookup.");
            return new SessionCreateResult(false, existingPayload);
        }

        var now = DateTime.Now;
        const string insertSql = @"INSERT INTO tblHandoverHeader
(ShiftDate, ShiftCode, SessionStatus, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
VALUES (?, ?, 'Open', ?, ?, ?, ?)";

        using (var insert = new OleDbCommand(insertSql, connection))
        {
            insert.Parameters.AddWithValue("@p1", shiftDate.Date);
            insert.Parameters.AddWithValue("@p2", shiftCode);
            insert.Parameters.AddWithValue("@p3", now);
            insert.Parameters.AddWithValue("@p4", userName);
            insert.Parameters.AddWithValue("@p5", now);
            insert.Parameters.AddWithValue("@p6", userName);
            insert.ExecuteNonQuery();
        }

        var header = FindHeader(connection, shiftCode, shiftDate)
            ?? throw new InvalidOperationException("Unable to find newly created session header.");

        EnsureDepartmentRows(connection, header.Value.HandoverId, userName);

        var payload = LoadSessionPayload(connection, header.Value.HandoverId)
            ?? throw new InvalidOperationException("Unable to load newly created session payload.");

        return new SessionCreateResult(true, payload);
    }

    public SessionPayload ClearDay(long sessionId, string userName)
    {
        using var connection = OpenConnection();

        using var tx = connection.BeginTransaction();
        var now = DateTime.Now;

        using (var updateHeader = new OleDbCommand("UPDATE tblHandoverHeader SET SessionStatus = 'Open', UpdatedAt = ?, UpdatedBy = ? WHERE HandoverID = ?", connection, tx))
        {
            updateHeader.Parameters.AddWithValue("@p1", now);
            updateHeader.Parameters.AddWithValue("@p2", userName);
            updateHeader.Parameters.AddWithValue("@p3", sessionId);
            updateHeader.ExecuteNonQuery();
        }

        using (var deleteAttachments = new OleDbCommand("DELETE FROM tblAttachments WHERE HandoverID = ?", connection, tx))
        {
            deleteAttachments.Parameters.AddWithValue("@p1", sessionId);
            deleteAttachments.ExecuteNonQuery();
        }

        using (var deleteBudgetRows = new OleDbCommand("DELETE FROM tblBudgetRows WHERE BudgetHeaderID IN (SELECT BudgetHeaderID FROM tblBudgetHeader WHERE HandoverID = ?)", connection, tx))
        {
            deleteBudgetRows.Parameters.AddWithValue("@p1", sessionId);
            deleteBudgetRows.ExecuteNonQuery();
        }

        using (var deleteBudgetHeader = new OleDbCommand("DELETE FROM tblBudgetHeader WHERE HandoverID = ?", connection, tx))
        {
            deleteBudgetHeader.Parameters.AddWithValue("@p1", sessionId);
            deleteBudgetHeader.ExecuteNonQuery();
        }

        using (var deleteDepartments = new OleDbCommand("DELETE FROM tblHandoverDept WHERE HandoverID = ?", connection, tx))
        {
            deleteDepartments.Parameters.AddWithValue("@p1", sessionId);
            deleteDepartments.ExecuteNonQuery();
        }

        tx.Commit();

        EnsureDepartmentRows(connection, sessionId, userName);

        var payload = LoadSessionPayload(connection, sessionId)
            ?? throw new InvalidOperationException($"Session '{sessionId}' not found during clear-day reload.");

        return payload;
    }

    private void EnsureDepartmentRows(OleDbConnection connection, long sessionId, string userName)
    {
        var activeDepartments = new List<string>();
        using (var cmd = new OleDbCommand("SELECT DeptName FROM tblDepartments WHERE IsActive = TRUE ORDER BY DisplayOrder", connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader!.Read())
            {
                activeDepartments.Add(reader.GetString(0));
            }
        }

        foreach (var deptName in activeDepartments)
        {
            using var exists = new OleDbCommand("SELECT COUNT(*) FROM tblHandoverDept WHERE HandoverID = ? AND DeptName = ?", connection);
            exists.Parameters.AddWithValue("@p1", sessionId);
            exists.Parameters.AddWithValue("@p2", deptName);
            var count = Convert.ToInt32(exists.ExecuteScalar());
            if (count > 0)
            {
                continue;
            }

            var now = DateTime.Now;
            using var insert = new OleDbCommand(@"INSERT INTO tblHandoverDept
(HandoverID, DeptName, DeptStatus, DowntimeMin, EfficiencyPct, YieldPct, DeptNotes, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, VersionNo, IsDeleted)
VALUES (?, ?, 'Not running', NULL, NULL, NULL, '', ?, ?, ?, ?, 1, FALSE)", connection);
            insert.Parameters.AddWithValue("@p1", sessionId);
            insert.Parameters.AddWithValue("@p2", deptName);
            insert.Parameters.AddWithValue("@p3", now);
            insert.Parameters.AddWithValue("@p4", userName);
            insert.Parameters.AddWithValue("@p5", now);
            insert.Parameters.AddWithValue("@p6", userName);
            insert.ExecuteNonQuery();
        }
    }

    private SessionPayload? LoadSessionPayload(OleDbConnection connection, long sessionId)
    {
        const string headerSql = @"SELECT HandoverID, ShiftCode, ShiftDate, SessionStatus, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
FROM tblHandoverHeader WHERE HandoverID = ?";

        HeaderRow? header = null;
        using (var cmd = new OleDbCommand(headerSql, connection))
        {
            cmd.Parameters.AddWithValue("@p1", sessionId);
            using var reader = cmd.ExecuteReader();
            if (reader!.Read())
            {
                header = new HeaderRow(
                    HandoverId: Convert.ToInt64(reader["HandoverID"]),
                    ShiftCode: Convert.ToString(reader["ShiftCode"]) ?? string.Empty,
                    ShiftDate: Convert.ToDateTime(reader["ShiftDate"]),
                    SessionStatus: Convert.ToString(reader["SessionStatus"]) ?? "Open",
                    CreatedAt: Convert.ToDateTime(reader["CreatedAt"]),
                    CreatedBy: Convert.ToString(reader["CreatedBy"]) ?? string.Empty,
                    UpdatedAt: Convert.ToDateTime(reader["UpdatedAt"]),
                    UpdatedBy: Convert.ToString(reader["UpdatedBy"]) ?? string.Empty);
            }
        }

        if (header is null)
        {
            return null;
        }

        var departments = new List<DepartmentSummaryPayload>();
        const string deptSql = @"SELECT DeptName, DeptStatus, UpdatedAt, UpdatedBy
FROM tblHandoverDept
WHERE HandoverID = ? AND (IsDeleted = FALSE OR IsDeleted IS NULL)
ORDER BY DeptName";

        using (var deptCmd = new OleDbCommand(deptSql, connection))
        {
            deptCmd.Parameters.AddWithValue("@p1", sessionId);
            using var reader = deptCmd.ExecuteReader();
            while (reader!.Read())
            {
                var updatedAt = reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]);
                var updatedBy = reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"]);
                departments.Add(new DepartmentSummaryPayload(
                    DeptName: Convert.ToString(reader["DeptName"]) ?? string.Empty,
                    DeptStatus: Convert.ToString(reader["DeptStatus"]) ?? "Not running",
                    UpdatedAt: updatedAt,
                    UpdatedBy: updatedBy));
            }
        }

        return new SessionPayload(
            SessionId: header.Value.HandoverId,
            ShiftCode: header.Value.ShiftCode,
            ShiftDate: header.Value.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SessionStatus: header.Value.SessionStatus,
            Departments: departments,
            CreatedAt: ToIso(header.Value.CreatedAt),
            CreatedBy: header.Value.CreatedBy,
            UpdatedAt: ToIso(header.Value.UpdatedAt),
            UpdatedBy: header.Value.UpdatedBy);
    }

    private HeaderRow? FindHeader(OleDbConnection connection, string shiftCode, DateTime shiftDate)
    {
        const string sql = @"SELECT TOP 1 HandoverID, ShiftCode, ShiftDate, SessionStatus, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
FROM tblHandoverHeader
WHERE ShiftCode = ? AND ShiftDate = ?";

        using var cmd = new OleDbCommand(sql, connection);
        cmd.Parameters.AddWithValue("@p1", shiftCode);
        cmd.Parameters.AddWithValue("@p2", shiftDate.Date);
        using var reader = cmd.ExecuteReader();
        if (!reader!.Read())
        {
            return null;
        }

        return new HeaderRow(
            HandoverId: Convert.ToInt64(reader["HandoverID"]),
            ShiftCode: Convert.ToString(reader["ShiftCode"]) ?? string.Empty,
            ShiftDate: Convert.ToDateTime(reader["ShiftDate"]),
            SessionStatus: Convert.ToString(reader["SessionStatus"]) ?? "Open",
            CreatedAt: Convert.ToDateTime(reader["CreatedAt"]),
            CreatedBy: Convert.ToString(reader["CreatedBy"]) ?? string.Empty,
            UpdatedAt: Convert.ToDateTime(reader["UpdatedAt"]),
            UpdatedBy: Convert.ToString(reader["UpdatedBy"]) ?? string.Empty);
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
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private readonly record struct HeaderRow(
        long HandoverId,
        string ShiftCode,
        DateTime ShiftDate,
        string SessionStatus,
        DateTime CreatedAt,
        string CreatedBy,
        DateTime UpdatedAt,
        string UpdatedBy);
}
