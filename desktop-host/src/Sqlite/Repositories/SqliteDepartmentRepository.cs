using System.Globalization;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteDepartmentRepository : SqliteRepositoryBase, IDepartmentRepository
{
    static readonly HashSet<string> Metric = new(StringComparer.OrdinalIgnoreCase){"Injection","MetaPress","Berks","Wilts"};
    public SqliteDepartmentRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }
    public DepartmentPayload LoadDepartment(long sessionId, string deptName)
    {
        using var c = OpenConnection(); using var cmd=c.CreateCommand();
        cmd.CommandText="SELECT DeptRecordID,HandoverID,DeptName,DeptStatus,DeptNotes,DowntimeMin,EfficiencyPct,YieldPct,UpdatedAt,UpdatedBy FROM tblHandoverDept WHERE HandoverID=$s AND DeptName=$d AND COALESCE(IsDeleted,0)=0 LIMIT 1"; cmd.Parameters.AddWithValue("$s",sessionId); cmd.Parameters.AddWithValue("$d",deptName);
        using var r=cmd.ExecuteReader(); if(!r.Read()) throw new InvalidOperationException(); var dn=r.GetString(2); var m=Metric.Contains(dn);
        return new DepartmentPayload(r.GetInt64(0),r.GetInt64(1),dn,r.IsDBNull(3)?"Not running":r.GetString(3),r.IsDBNull(4)?"":r.GetString(4),m&&!r.IsDBNull(5)?r.GetInt32(5):null,m&&!r.IsDBNull(6)?r.GetDouble(6):null,m&&!r.IsDBNull(7)?r.GetDouble(7):null,r.IsDBNull(8)?null:r.GetString(8),r.IsDBNull(9)?null:r.GetString(9),m);
    }
    public DepartmentSaveResult SaveDepartment(DepartmentSaveRequest request, string userName)
    {
        using var c=OpenConnection(); var m=Metric.Contains(request.DeptName); using var u=c.CreateCommand();
        u.CommandText="UPDATE tblHandoverDept SET DeptStatus=$st,DeptNotes=$n,DowntimeMin=$d,EfficiencyPct=$e,YieldPct=$y,UpdatedAt=$at,UpdatedBy=$u,VersionNo=COALESCE(VersionNo,0)+1 WHERE HandoverID=$s AND DeptName=$dn AND COALESCE(IsDeleted,0)=0";
        u.Parameters.AddWithValue("$st", string.IsNullOrWhiteSpace(request.DeptStatus)?"Not running":request.DeptStatus.Trim()); u.Parameters.AddWithValue("$n", request.DeptNotes?.Trim()??""); u.Parameters.AddWithValue("$d", m && request.DowntimeMin.HasValue ? request.DowntimeMin.Value : DBNull.Value); u.Parameters.AddWithValue("$e", m && request.EfficiencyPct.HasValue ? request.EfficiencyPct.Value : DBNull.Value); u.Parameters.AddWithValue("$y", m && request.YieldPct.HasValue ? request.YieldPct.Value : DBNull.Value); u.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)); u.Parameters.AddWithValue("$u", userName); u.Parameters.AddWithValue("$s", request.SessionId); u.Parameters.AddWithValue("$dn", request.DeptName); u.ExecuteNonQuery();
        return new DepartmentSaveResult(LoadDepartment(request.SessionId, request.DeptName), new List<DepartmentSummaryPayload>());
    }
}
