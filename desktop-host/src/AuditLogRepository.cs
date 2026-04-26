using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;

namespace MoatHouseHandover.Host;

public sealed class AuditLogRepository
{
    private readonly string _connectionString;

    public AuditLogRepository(string accessDatabasePath)
    {
        _connectionString = AccessBootstrapper.BuildConnectionString(accessDatabasePath);
    }

    public void Insert(AuditLogWriteRequest request)
    {
        using var connection = OpenConnection();
        using var command = new OleDbCommand(@"INSERT INTO tblAuditLog
(EventAt, UserName, EntityType, EntityKey, ActionType, Details)
VALUES (?, ?, ?, ?, ?, ?)", connection);
        command.Parameters.AddWithValue("@p1", DateTime.Now);
        command.Parameters.AddWithValue("@p2", request.UserName);
        command.Parameters.AddWithValue("@p3", request.EntityType);
        command.Parameters.AddWithValue("@p4", request.EntityKey);
        command.Parameters.AddWithValue("@p5", request.ActionType);
        command.Parameters.AddWithValue("@p6", request.Details);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<AuditLogEntry> ListRecent(int limit)
    {
        using var connection = OpenConnection();
        return ListByQuery(connection, "SELECT TOP " + limit.ToString(CultureInfo.InvariantCulture) + " AuditID, EventAt, UserName, EntityType, EntityKey, ActionType, Details FROM tblAuditLog ORDER BY EventAt DESC, AuditID DESC", null);
    }

    public IReadOnlyList<AuditLogEntry> ListForSession(long sessionId, int limit)
    {
        using var connection = OpenConnection();
        const string sql = @"SELECT TOP 200 AuditID, EventAt, UserName, EntityType, EntityKey, ActionType, Details
FROM tblAuditLog
WHERE EntityKey LIKE ?
ORDER BY EventAt DESC, AuditID DESC";

        var values = ListByQuery(connection, sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@p1", "session:" + sessionId.ToString(CultureInfo.InvariantCulture) + "%");
        });

        if (values.Count <= limit)
        {
            return values;
        }

        return values[..limit];
    }

    private static List<AuditLogEntry> ListByQuery(OleDbConnection connection, string sql, Action<OleDbCommand>? bind)
    {
        var results = new List<AuditLogEntry>();
        using var command = new OleDbCommand(sql, connection);
        bind?.Invoke(command);
        using var reader = command.ExecuteReader();
        while (reader!.Read())
        {
            results.Add(new AuditLogEntry(
                AuditId: Convert.ToInt64(reader["AuditID"]),
                EventAt: reader["EventAt"] == DBNull.Value ? string.Empty : Convert.ToDateTime(reader["EventAt"]).ToString("O", CultureInfo.InvariantCulture),
                UserName: Convert.ToString(reader["UserName"]) ?? string.Empty,
                EntityType: Convert.ToString(reader["EntityType"]) ?? string.Empty,
                EntityKey: Convert.ToString(reader["EntityKey"]) ?? string.Empty,
                ActionType: Convert.ToString(reader["ActionType"]) ?? string.Empty,
                Details: Convert.ToString(reader["Details"]) ?? string.Empty));
        }

        return results;
    }

    private OleDbConnection OpenConnection()
    {
        var connection = new OleDbConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
