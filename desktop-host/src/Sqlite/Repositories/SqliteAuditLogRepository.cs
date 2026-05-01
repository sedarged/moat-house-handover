using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteAuditLogRepository : SqliteRepositoryBase, IAuditLogRepository
{
    public SqliteAuditLogRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }
    public void Insert(AuditLogWriteRequest request)
    {
        try
        {
            using var c = OpenConnection();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO tblAuditLog (EventAt, UserName, EntityType, EntityKey, ActionType, Details) VALUES ($at,$u,$et,$ek,$ac,$d)";
            cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$u", request.UserName);
            cmd.Parameters.AddWithValue("$et", request.EntityType);
            cmd.Parameters.AddWithValue("$ek", request.EntityKey);
            cmd.Parameters.AddWithValue("$ac", request.ActionType);
            cmd.Parameters.AddWithValue("$d", request.Details);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
    public IReadOnlyList<AuditLogEntry> ListRecent(int limit)
    {
        using var c = OpenConnection();
        return ListByQuery(c, "SELECT AuditID, EventAt, UserName, EntityType, EntityKey, ActionType, Details FROM tblAuditLog ORDER BY EventAt DESC, AuditID DESC LIMIT $l", cmd => cmd.Parameters.AddWithValue("$l", limit));
    }
    public IReadOnlyList<AuditLogEntry> ListForSession(long sessionId, int limit)
    {
        using var c = OpenConnection();
        return ListByQuery(c, "SELECT AuditID, EventAt, UserName, EntityType, EntityKey, ActionType, Details FROM tblAuditLog WHERE EntityKey LIKE $k ORDER BY EventAt DESC, AuditID DESC LIMIT $l", cmd => { cmd.Parameters.AddWithValue("$k", $"session:{sessionId}%"); cmd.Parameters.AddWithValue("$l", limit); });
    }
    static List<AuditLogEntry> ListByQuery(SqliteConnection c, string sql, Action<SqliteCommand> bind)
    {
        var r = new List<AuditLogEntry>(); using var cmd = c.CreateCommand(); cmd.CommandText = sql; bind(cmd); using var rd = cmd.ExecuteReader();
        while (rd.Read()) r.Add(new AuditLogEntry(rd.GetInt64(0), rd.IsDBNull(1)?string.Empty:rd.GetString(1), rd.IsDBNull(2)?"":rd.GetString(2), rd.IsDBNull(3)?"":rd.GetString(3), rd.IsDBNull(4)?"":rd.GetString(4), rd.IsDBNull(5)?"":rd.GetString(5), rd.IsDBNull(6)?"":rd.GetString(6)));
        return r;
    }
}
