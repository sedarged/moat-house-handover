using System.Globalization;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteSessionRepository : SqliteRepositoryBase, ISessionRepository
{
    public SqliteSessionRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }
    public SessionPayload? OpenExistingSession(string shiftCode, DateTime shiftDate, string userName)
    {
        using var c = OpenConnection();
        var id = FindId(c, shiftCode, shiftDate);
        if (id is null) return null;
        EnsureDepartmentRows(c, id.Value, userName);
        return Load(c, id.Value);
    }
    public SessionCreateResult CreateBlankSession(string shiftCode, DateTime shiftDate, string userName)
    {
        using var c = OpenConnection();
        var existing = FindId(c, shiftCode, shiftDate);
        if (existing.HasValue) return new SessionCreateResult(false, Load(c, existing.Value)!);
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO tblHandoverHeader (ShiftDate,ShiftCode,SessionStatus,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy) VALUES ($d,$s,'Open',$n,$u,$n,$u)";
        cmd.Parameters.AddWithValue("$d", shiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); cmd.Parameters.AddWithValue("$s", shiftCode); cmd.Parameters.AddWithValue("$n", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)); cmd.Parameters.AddWithValue("$u", userName); cmd.ExecuteNonQuery();
        var id = FindId(c, shiftCode, shiftDate) ?? throw new InvalidOperationException();
        EnsureDepartmentRows(c, id, userName);
        return new SessionCreateResult(true, Load(c, id)!);
    }
    public SessionPayload ClearDay(long sessionId, string userName) => Load(OpenConnection(), sessionId)!;

    static long? FindId(Microsoft.Data.Sqlite.SqliteConnection c, string shiftCode, DateTime shiftDate){ using var cmd=c.CreateCommand(); cmd.CommandText="SELECT HandoverID FROM tblHandoverHeader WHERE ShiftCode=$s AND ShiftDate=$d LIMIT 1"; cmd.Parameters.AddWithValue("$s", shiftCode); cmd.Parameters.AddWithValue("$d", shiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); var v=cmd.ExecuteScalar(); return v is null?null:Convert.ToInt64(v); }
    static void EnsureDepartmentRows(Microsoft.Data.Sqlite.SqliteConnection c,long sid,string user){ using var list=c.CreateCommand(); list.CommandText="SELECT DeptName FROM tblDepartments WHERE IsActive=1 ORDER BY DisplayOrder"; using var r=list.ExecuteReader(); var depts=new List<string>(); while(r.Read()) depts.Add(r.GetString(0)); foreach(var d in depts){ using var e=c.CreateCommand(); e.CommandText="SELECT COUNT(*) FROM tblHandoverDept WHERE HandoverID=$h AND DeptName=$d"; e.Parameters.AddWithValue("$h",sid); e.Parameters.AddWithValue("$d",d); if(Convert.ToInt32(e.ExecuteScalar())>0) continue; using var i=c.CreateCommand(); i.CommandText="INSERT INTO tblHandoverDept (HandoverID,DeptName,DeptStatus,DeptNotes,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,VersionNo,IsDeleted) VALUES ($h,$d,'Not running','',$n,$u,$n,$u,1,0)"; i.Parameters.AddWithValue("$h",sid); i.Parameters.AddWithValue("$d",d); i.Parameters.AddWithValue("$n",DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)); i.Parameters.AddWithValue("$u",user); i.ExecuteNonQuery(); }}
    static SessionPayload? Load(Microsoft.Data.Sqlite.SqliteConnection c,long sid){ using var cmd=c.CreateCommand(); cmd.CommandText="SELECT HandoverID,ShiftCode,ShiftDate,SessionStatus,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy FROM tblHandoverHeader WHERE HandoverID=$h LIMIT 1"; cmd.Parameters.AddWithValue("$h",sid); using var r=cmd.ExecuteReader(); if(!r.Read()) return null; return new SessionPayload(r.GetInt64(0), r.IsDBNull(1)?"":r.GetString(1), r.IsDBNull(2)?"":r.GetString(2), r.IsDBNull(3)?"Open":r.GetString(3), new List<DepartmentSummaryPayload>(), r.IsDBNull(4)?DateTime.UtcNow.ToString("O"):r.GetString(4), r.IsDBNull(5)?"":r.GetString(5), r.IsDBNull(6)?DateTime.UtcNow.ToString("O"):r.GetString(6), r.IsDBNull(7)?"":r.GetString(7)); }
}
