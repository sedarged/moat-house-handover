using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteAttachmentRepository : SqliteRepositoryBase, IAttachmentRepository
{
    public SqliteAttachmentRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }

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
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var nextSequence = GetNextSequence(connection, request.DeptRecordId);

        using var insert = connection.CreateCommand();
        insert.CommandText = @"INSERT INTO tblAttachments
(HandoverID, DeptRecordID, ShiftDate, ShiftCode, DeptName, FilePath, DisplayName, CapturedOn, SequenceNo, Notes, IsDeleted)
SELECT d.HandoverID, d.DeptRecordID, h.ShiftDate, h.ShiftCode, d.DeptName, $path, $name, $capturedOn, $sequence, '', 0
FROM tblHandoverDept d
INNER JOIN tblHandoverHeader h ON h.HandoverID = d.HandoverID
WHERE d.DeptRecordID = $deptRecordId AND d.HandoverID = $sessionId";
        insert.Parameters.AddWithValue("$path", storedFilePath);
        insert.Parameters.AddWithValue("$name", request.DisplayName);
        insert.Parameters.AddWithValue("$capturedOn", now);
        insert.Parameters.AddWithValue("$sequence", nextSequence);
        insert.Parameters.AddWithValue("$deptRecordId", request.DeptRecordId);
        insert.Parameters.AddWithValue("$sessionId", request.SessionId);
        var affected = insert.ExecuteNonQuery();
        if (affected <= 0) throw new InvalidOperationException("Attachment insert failed; department/session lookup did not match.");

        return ListAttachments(request.SessionId, request.DeptRecordId, request.DeptName);
    }

    public AttachmentListResult RemoveAttachment(long attachmentId)
    {
        using var connection = OpenConnection();
        var target = GetAttachmentContext(connection, attachmentId)
            ?? throw new InvalidOperationException($"Attachment '{attachmentId}' was not found.");

        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE tblAttachments SET IsDeleted = 1 WHERE AttachmentID = $id";
        update.Parameters.AddWithValue("$id", attachmentId);
        if (update.ExecuteNonQuery() <= 0) throw new InvalidOperationException($"Attachment '{attachmentId}' could not be removed.");

        return ListAttachments(target.SessionId, target.DeptRecordId, target.DeptName);
    }

    public AttachmentViewerPayload GetViewerPayload(long sessionId, long deptRecordId, long attachmentId)
    {
        using var connection = OpenConnection();
        var attachments = LoadAttachments(connection, deptRecordId);
        if (attachments.Count == 0) throw new InvalidOperationException("No active attachments exist for this department.");

        var currentIndex = attachments.FindIndex(item => item.AttachmentId == attachmentId);
        if (currentIndex < 0) currentIndex = 0;

        var current = attachments[currentIndex];
        var previous = currentIndex > 0 ? attachments[currentIndex - 1] : null;
        var next = currentIndex < attachments.Count - 1 ? attachments[currentIndex + 1] : null;

        if (current.SessionId != sessionId) throw new InvalidOperationException("Attachment does not belong to the requested session.");
        return new AttachmentViewerPayload(current, currentIndex, attachments.Count, previous, next);
    }

    private static List<AttachmentPayload> LoadAttachments(SqliteConnection connection, long deptRecordId)
    {
        var attachments = new List<AttachmentPayload>();
        using var list = connection.CreateCommand();
        list.CommandText = @"SELECT AttachmentID, HandoverID, DeptRecordID, DeptName, DisplayName, FilePath, CapturedOn, SequenceNo, COALESCE(IsDeleted, 0)
FROM tblAttachments
WHERE DeptRecordID = $deptRecordId AND COALESCE(IsDeleted, 0) = 0
ORDER BY SequenceNo, AttachmentID";
        list.Parameters.AddWithValue("$deptRecordId", deptRecordId);
        using var reader = list.ExecuteReader();
        while (reader.Read())
        {
            attachments.Add(new AttachmentPayload(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.IsDBNull(3) ? string.Empty : reader.GetString(3), reader.IsDBNull(4) ? string.Empty : reader.GetString(4), reader.IsDBNull(5) ? string.Empty : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.IsDBNull(7) ? 0 : reader.GetInt64(7), !reader.IsDBNull(8) && reader.GetInt64(8) != 0));
        }

        return attachments;
    }

    private static List<DepartmentSummaryPayload> LoadDashboardDepartmentSummary(SqliteConnection connection, long sessionId)
    {
        var result = new List<DepartmentSummaryPayload>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT d.DeptName, d.DeptStatus, d.UpdatedAt, d.UpdatedBy,
SUM(CASE WHEN a.AttachmentID IS NOT NULL AND COALESCE(a.IsDeleted, 0) = 0 THEN 1 ELSE 0 END) AS AttachmentCount
FROM tblHandoverDept d
LEFT JOIN tblDepartments cfg ON d.DeptName = cfg.DeptName
LEFT JOIN tblAttachments a ON a.DeptRecordID = d.DeptRecordID
WHERE d.HandoverID = $sessionId AND COALESCE(d.IsDeleted, 0) = 0
GROUP BY d.DeptName, d.DeptStatus, d.UpdatedAt, d.UpdatedBy, cfg.DisplayOrder
ORDER BY cfg.DisplayOrder, d.DeptName";
        cmd.Parameters.AddWithValue("$sessionId", sessionId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new DepartmentSummaryPayload(reader.IsDBNull(0) ? string.Empty : reader.GetString(0), reader.IsDBNull(1) ? "Not running" : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? 0 : reader.GetInt32(4)));
        }

        return result;
    }

    private static long GetNextSequence(SqliteConnection connection, long deptRecordId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(SequenceNo), 0) FROM tblAttachments WHERE DeptRecordID = $deptRecordId";
        cmd.Parameters.AddWithValue("$deptRecordId", deptRecordId);
        return Convert.ToInt64(cmd.ExecuteScalar()) + 1;
    }

    private static (long SessionId, long DeptRecordId, string DeptName)? GetAttachmentContext(SqliteConnection connection, long attachmentId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT HandoverID, DeptRecordID, DeptName FROM tblAttachments WHERE AttachmentID = $id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", attachmentId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt64(0), reader.GetInt64(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
    }
}
