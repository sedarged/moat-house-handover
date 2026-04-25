using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MoatHouseHandover.Host;

public sealed class AttachmentRepository
{
    private readonly string _connectionString;

    public AttachmentRepository(string accessDatabasePath)
    {
        _connectionString = AccessBootstrapper.BuildConnectionString(accessDatabasePath);
    }

    public AttachmentListResult ListAttachments(long sessionId, long deptRecordId, string deptName)
    {
        using var connection = OpenConnection();
        var attachments = LoadAttachments(connection, deptRecordId);
        var dashboardDepartments = LoadDashboardDepartmentSummary(connection, sessionId);
        return new AttachmentListResult(sessionId, deptRecordId, deptName, attachments, attachments.Count, dashboardDepartments);
    }

    public AttachmentListResult AddAttachmentMetadata(AttachmentAddRequest request, string storedFilePath, string userName)
    {
        using var connection = OpenConnection();

        var now = DateTime.Now;
        var nextSequence = GetNextSequence(connection, request.DeptRecordId);

        using (var insert = new OleDbCommand(@"INSERT INTO tblAttachments
(HandoverID, DeptRecordID, ShiftDate, ShiftCode, DeptName, FilePath, DisplayName, CapturedOn, SequenceNo, IsDeleted)
SELECT d.HandoverID, d.DeptRecordID, h.ShiftDate, h.ShiftCode, d.DeptName, ?, ?, ?, ?, FALSE
FROM tblHandoverDept AS d
INNER JOIN tblHandoverHeader AS h ON h.HandoverID = d.HandoverID
WHERE d.DeptRecordID = ? AND d.HandoverID = ?", connection))
        {
            insert.Parameters.AddWithValue("@p1", storedFilePath);
            insert.Parameters.AddWithValue("@p2", request.DisplayName);
            insert.Parameters.AddWithValue("@p3", now);
            insert.Parameters.AddWithValue("@p4", nextSequence);
            insert.Parameters.AddWithValue("@p5", request.DeptRecordId);
            insert.Parameters.AddWithValue("@p6", request.SessionId);

            var affected = insert.ExecuteNonQuery();
            if (affected <= 0)
            {
                throw new InvalidOperationException("Attachment insert failed; department/session lookup did not match.");
            }
        }

        return ListAttachments(request.SessionId, request.DeptRecordId, request.DeptName);
    }

    public AttachmentListResult RemoveAttachment(long attachmentId, string userName)
    {
        using var connection = OpenConnection();
        var target = GetAttachmentContext(connection, attachmentId)
            ?? throw new InvalidOperationException($"Attachment '{attachmentId}' was not found.");

        using (var update = new OleDbCommand(@"UPDATE tblAttachments
SET IsDeleted = TRUE
WHERE AttachmentID = ?", connection))
        {
            update.Parameters.AddWithValue("@p1", attachmentId);
            var affected = update.ExecuteNonQuery();
            if (affected <= 0)
            {
                throw new InvalidOperationException($"Attachment '{attachmentId}' could not be removed.");
            }
        }

        // Stage 2D Extended: metadata uses soft delete; physical file retention is handled by host policy.
        return ListAttachments(target.SessionId, target.DeptRecordId, target.DeptName);
    }

    public AttachmentViewerPayload GetViewerPayload(long sessionId, long deptRecordId, long attachmentId)
    {
        using var connection = OpenConnection();
        var attachments = LoadAttachments(connection, deptRecordId);
        if (attachments.Count == 0)
        {
            throw new InvalidOperationException("No active attachments exist for this department.");
        }

        var currentIndex = attachments.FindIndex(item => item.AttachmentId == attachmentId);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var current = attachments[currentIndex];
        var previous = currentIndex > 0 ? attachments[currentIndex - 1] : null;
        var next = currentIndex < attachments.Count - 1 ? attachments[currentIndex + 1] : null;

        if (current.SessionId != sessionId)
        {
            throw new InvalidOperationException("Attachment does not belong to the requested session.");
        }

        return new AttachmentViewerPayload(current, currentIndex, attachments.Count, previous, next);
    }

    private static List<AttachmentPayload> LoadAttachments(OleDbConnection connection, long deptRecordId)
    {
        var attachments = new List<AttachmentPayload>();
        using var list = new OleDbCommand(@"SELECT AttachmentID, HandoverID, DeptRecordID, DeptName, DisplayName, FilePath, CapturedOn, SequenceNo, IsDeleted
FROM tblAttachments
WHERE DeptRecordID = ? AND (IsDeleted = FALSE OR IsDeleted IS NULL)
ORDER BY SequenceNo, AttachmentID", connection);
        list.Parameters.AddWithValue("@p1", deptRecordId);

        using var reader = list.ExecuteReader();
        while (reader!.Read())
        {
            attachments.Add(new AttachmentPayload(
                AttachmentId: Convert.ToInt64(reader["AttachmentID"]),
                SessionId: Convert.ToInt64(reader["HandoverID"]),
                DeptRecordId: Convert.ToInt64(reader["DeptRecordID"]),
                DeptName: Convert.ToString(reader["DeptName"]) ?? string.Empty,
                DisplayName: Convert.ToString(reader["DisplayName"]) ?? string.Empty,
                FilePath: Convert.ToString(reader["FilePath"]) ?? string.Empty,
                CapturedOn: reader["CapturedOn"] == DBNull.Value ? string.Empty : ToIso(reader["CapturedOn"]),
                SequenceNo: reader["SequenceNo"] == DBNull.Value ? 0 : Convert.ToInt64(reader["SequenceNo"]),
                IsDeleted: reader["IsDeleted"] != DBNull.Value && Convert.ToBoolean(reader["IsDeleted"])));
        }

        return attachments;
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

    private static long GetNextSequence(OleDbConnection connection, long deptRecordId)
    {
        using var cmd = new OleDbCommand("SELECT MAX(SequenceNo) FROM tblAttachments WHERE DeptRecordID = ?", connection);
        cmd.Parameters.AddWithValue("@p1", deptRecordId);
        var currentMax = cmd.ExecuteScalar();
        if (currentMax == null || currentMax == DBNull.Value)
        {
            return 1;
        }

        return Convert.ToInt64(currentMax) + 1;
    }

    private static (long SessionId, long DeptRecordId, string DeptName)? GetAttachmentContext(OleDbConnection connection, long attachmentId)
    {
        using var cmd = new OleDbCommand("SELECT HandoverID, DeptRecordID, DeptName FROM tblAttachments WHERE AttachmentID = ?", connection);
        cmd.Parameters.AddWithValue("@p1", attachmentId);
        using var reader = cmd.ExecuteReader();
        if (!reader!.Read())
        {
            return null;
        }

        return (
            SessionId: Convert.ToInt64(reader["HandoverID"]),
            DeptRecordId: Convert.ToInt64(reader["DeptRecordID"]),
            DeptName: Convert.ToString(reader["DeptName"]) ?? string.Empty);
    }

    private OleDbConnection OpenConnection()
    {
        var connection = new OleDbConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string ToIso(object dateValue)
    {
        var date = Convert.ToDateTime(dateValue);
        return date.ToUniversalTime().ToString("o");
    }
}
