using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteEmailProfileRepository : SqliteRepositoryBase, IEmailProfileRepository
{
    public SqliteEmailProfileRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }

    public EmailProfilePayload? LoadByShiftCode(string shiftCode)
    {
        using var c = OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT s.ShiftCode, s.EmailProfileKey, p.ToList, p.CcList, p.SubjectTemplate, p.BodyTemplate, p.IsActive
FROM tblShiftRules s LEFT JOIN tblEmailProfiles p ON p.EmailProfileKey=s.EmailProfileKey
WHERE s.ShiftCode=$s LIMIT 1";
        cmd.Parameters.AddWithValue("$s", shiftCode);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var key = r.IsDBNull(1) ? string.Empty : r.GetString(1);
        if (string.IsNullOrWhiteSpace(key)) return null;
        return new EmailProfilePayload(key, r.IsDBNull(0)?string.Empty:r.GetString(0), r.IsDBNull(2)?string.Empty:r.GetString(2), r.IsDBNull(3)?string.Empty:r.GetString(3), r.IsDBNull(4)?string.Empty:r.GetString(4), r.IsDBNull(5)?string.Empty:r.GetString(5), !r.IsDBNull(6) && r.GetInt64(6)==1);
    }
}
