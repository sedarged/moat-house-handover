using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public sealed class SqliteBudgetRepository : SqliteRepositoryBase, IBudgetRepository
{
    public SqliteBudgetRepository(string sqlitePath, string dataRoot) : base(sqlitePath, dataRoot) { }

    public BudgetPayload LoadBudget(long sessionId, string userName)
    {
        using var c = OpenConnection();
        var header = EnsureBudgetHeaderAndRows(c, sessionId, userName);
        var rows = LoadBudgetRows(c, header.BudgetHeaderId, sessionId, header.ShiftCode, header.ShiftDateIso);
        var totals = CalculateTotals(rows);
        var rowUpdated = GetBudgetRowLastUpdated(c, header.BudgetHeaderId);
        var meta = LoadBudgetHeaderMeta(c, header.BudgetHeaderId);
        var counts = CountReasonBuckets(rows);
        var summary = new BudgetSummaryPayload(totals.PlannedTotal, totals.UsedTotal, totals.VarianceTotal, totals.Status, rowUpdated.UpdatedAt, rowUpdated.UpdatedBy, rows.Count, meta.LinesPlanned, meta.TotalStaffOnRegister, counts.HolidayCount, counts.AbsentCount, counts.OtherReasonCount, counts.AgencyUsedCount, meta.Comments);
        return new BudgetPayload(header.BudgetHeaderId, sessionId, header.ShiftCode, header.ShiftDateIso, rows, totals, summary, header.UpdatedAt, header.UpdatedBy);
    }

    public BudgetPayload SaveBudget(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta, string userName)
    {
        using var c = OpenConnection();
        var header = EnsureBudgetHeaderAndRows(c, sessionId, userName);
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        using var tx = c.BeginTransaction();

        foreach (var row in rows)
        {
            var variance = (row.UsedQty ?? 0) - (row.PlannedQty ?? 0);
            if (row.BudgetRowId.HasValue && row.BudgetRowId.Value > 0)
            {
                using var update = c.CreateCommand();
                update.Transaction = tx;
                update.CommandText = @"UPDATE tblBudgetRows SET DeptName=$dept, PlannedQty=$planned, UsedQty=$used, VarianceQty=$variance, ReasonText=$reason, UpdatedAt=$at, UpdatedBy=$user
WHERE BudgetRowID=$id AND BudgetHeaderID=$headerId";
                update.Parameters.AddWithValue("$dept", row.DeptName?.Trim() ?? string.Empty);
                update.Parameters.AddWithValue("$planned", ToDbNullable(row.PlannedQty));
                update.Parameters.AddWithValue("$used", ToDbNullable(row.UsedQty));
                update.Parameters.AddWithValue("$variance", variance);
                update.Parameters.AddWithValue("$reason", row.ReasonText?.Trim() ?? string.Empty);
                update.Parameters.AddWithValue("$at", now);
                update.Parameters.AddWithValue("$user", userName);
                update.Parameters.AddWithValue("$id", row.BudgetRowId.Value);
                update.Parameters.AddWithValue("$headerId", header.BudgetHeaderId);
                if (update.ExecuteNonQuery() > 0) continue;
            }

            using var insert = c.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = @"INSERT INTO tblBudgetRows
(BudgetHeaderID, DeptName, PlannedQty, UsedQty, VarianceQty, ReasonText, UpdatedAt, UpdatedBy)
VALUES ($headerId, $dept, $planned, $used, $variance, $reason, $at, $user)";
            insert.Parameters.AddWithValue("$headerId", header.BudgetHeaderId);
            insert.Parameters.AddWithValue("$dept", row.DeptName?.Trim() ?? string.Empty);
            insert.Parameters.AddWithValue("$planned", ToDbNullable(row.PlannedQty));
            insert.Parameters.AddWithValue("$used", ToDbNullable(row.UsedQty));
            insert.Parameters.AddWithValue("$variance", variance);
            insert.Parameters.AddWithValue("$reason", row.ReasonText?.Trim() ?? string.Empty);
            insert.Parameters.AddWithValue("$at", now);
            insert.Parameters.AddWithValue("$user", userName);
            insert.ExecuteNonQuery();
        }

        using (var updateHeader = c.CreateCommand())
        {
            updateHeader.Transaction = tx;
            updateHeader.CommandText = "UPDATE tblBudgetHeader SET UpdatedAt=$at, UpdatedBy=$user WHERE BudgetHeaderID=$id";
            updateHeader.Parameters.AddWithValue("$at", now);
            updateHeader.Parameters.AddWithValue("$user", userName);
            updateHeader.Parameters.AddWithValue("$id", header.BudgetHeaderId);
            updateHeader.ExecuteNonQuery();
        }

        if (meta is not null)
        {
            using var updateMeta = c.CreateCommand();
            updateMeta.Transaction = tx;
            updateMeta.CommandText = "UPDATE tblBudgetHeader SET LinesPlanned=$lines, TotalStaffOnRegister=$staff, Comments=$comments WHERE BudgetHeaderID=$id";
            updateMeta.Parameters.AddWithValue("$lines", ToDbNullable(meta.LinesPlanned));
            updateMeta.Parameters.AddWithValue("$staff", ToDbNullable(meta.TotalStaffOnRegister));
            updateMeta.Parameters.AddWithValue("$comments", meta.Comments?.Trim() ?? string.Empty);
            updateMeta.Parameters.AddWithValue("$id", header.BudgetHeaderId);
            updateMeta.ExecuteNonQuery();
        }

        tx.Commit();
        return LoadBudget(sessionId, userName);
    }

    public BudgetPayload Recalculate(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta)
    { using var c = OpenConnection(); var context = GetSessionContext(c, sessionId) ?? throw new InvalidOperationException(); var headerId = FindBudgetHeaderId(c, sessionId) ?? 0; var projectedRows = rows.Select(r => new BudgetRowPayload(r.BudgetRowId ?? 0, headerId, sessionId, context.ShiftCode, context.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), r.DeptName?.Trim() ?? string.Empty, r.PlannedQty, r.UsedQty, (r.UsedQty ?? 0) - (r.PlannedQty ?? 0), ResolveRowStatus(r.PlannedQty, r.UsedQty, (r.UsedQty ?? 0) - (r.PlannedQty ?? 0)), r.ReasonText?.Trim() ?? string.Empty, null, null)).ToList(); var totals = CalculateTotals(projectedRows); var counts = CountReasonBuckets(projectedRows); var summary = new BudgetSummaryPayload(totals.PlannedTotal, totals.UsedTotal, totals.VarianceTotal, totals.Status, null, null, projectedRows.Count, meta?.LinesPlanned, meta?.TotalStaffOnRegister, counts.HolidayCount, counts.AbsentCount, counts.OtherReasonCount, counts.AgencyUsedCount, meta?.Comments?.Trim() ?? string.Empty); return new BudgetPayload(headerId, sessionId, context.ShiftCode, context.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), projectedRows, totals, summary, null, null); }
    public BudgetSummaryPayload LoadBudgetSummary(long sessionId)
    {
        using var c = OpenConnection();
        _ = GetSessionContext(c, sessionId)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        var headerId = FindBudgetHeaderId(c, sessionId);
        if (!headerId.HasValue)
        {
            return new BudgetSummaryPayload(0, 0, 0, "not set", null, null, 0, null, null, 0, 0, 0, 0, string.Empty);
        }

        var rows = LoadBudgetRows(c, headerId.Value, sessionId, string.Empty, string.Empty);
        var totals = CalculateTotals(rows);
        var updated = GetBudgetRowLastUpdated(c, headerId.Value);
        var meta = LoadBudgetHeaderMeta(c, headerId.Value);
        var counts = CountReasonBuckets(rows);
        return new BudgetSummaryPayload(totals.PlannedTotal, totals.UsedTotal, totals.VarianceTotal, totals.Status, updated.UpdatedAt, updated.UpdatedBy, rows.Count, meta.LinesPlanned, meta.TotalStaffOnRegister, counts.HolidayCount, counts.AbsentCount, counts.OtherReasonCount, counts.AgencyUsedCount, meta.Comments);
    }

    // helper methods copied from Access behavior semantics
    private static string ResolveBudgetStatus(double planned, double used, double variance) => planned == 0 && used == 0 ? "not set" : Math.Abs(variance) < 0.0001 ? "on target" : variance > 0 ? "over" : "under";
    private static string ResolveRowStatus(double? planned, double? used, double variance) => !planned.HasValue && !used.HasValue ? "not set" : Math.Abs(variance) < 0.0001 ? "on target" : variance > 0 ? "over" : "under";
    private static object ToDbNullable(double? v) => v.HasValue ? v.Value : DBNull.Value;

    private (long BudgetHeaderId, string ShiftCode, string ShiftDateIso, string? UpdatedAt, string? UpdatedBy) EnsureBudgetHeaderAndRows(SqliteConnection c, long sid, string user){ var ctx=GetSessionContext(c,sid)??throw new InvalidOperationException($"Session '{sid}' was not found."); var id=FindBudgetHeaderId(c,sid); if(!id.HasValue){ using var i=c.CreateCommand(); i.CommandText="INSERT INTO tblBudgetHeader (HandoverID,ShiftCode,ShiftDate,LinesPlanned,TotalStaffOnRegister,Comments,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy) VALUES ($sid,$shift,$date,NULL,NULL,'',$now,$user,$now,$user)"; i.Parameters.AddWithValue("$sid",sid); i.Parameters.AddWithValue("$shift",ctx.ShiftCode); i.Parameters.AddWithValue("$date",ctx.ShiftDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)); i.Parameters.AddWithValue("$now",DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture)); i.Parameters.AddWithValue("$user",user); i.ExecuteNonQuery(); id=FindBudgetHeaderId(c,sid);} EnsureBudgetRows(c,id!.Value,user); using var h=c.CreateCommand(); h.CommandText="SELECT UpdatedAt, UpdatedBy FROM tblBudgetHeader WHERE BudgetHeaderID=$id LIMIT 1"; h.Parameters.AddWithValue("$id",id.Value); using var r=h.ExecuteReader(); return r.Read()?(id.Value,ctx.ShiftCode,ctx.ShiftDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),r.IsDBNull(0)?null:r.GetString(0),r.IsDBNull(1)?null:r.GetString(1)):(id.Value,ctx.ShiftCode,ctx.ShiftDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),null,null); }
    private static void EnsureBudgetRows(SqliteConnection c,long headerId,string user){ using var count=c.CreateCommand(); count.CommandText="SELECT COUNT(*) FROM tblBudgetRows WHERE BudgetHeaderID=$id"; count.Parameters.AddWithValue("$id",headerId); if(Convert.ToInt32(count.ExecuteScalar())>0)return; foreach(var d in BudgetLabourAreas){ using var ins=c.CreateCommand(); ins.CommandText="INSERT INTO tblBudgetRows (BudgetHeaderID,DeptName,PlannedQty,UsedQty,VarianceQty,ReasonText,UpdatedAt,UpdatedBy) VALUES ($id,$dept,NULL,NULL,0,'',$at,$user)"; ins.Parameters.AddWithValue("$id",headerId); ins.Parameters.AddWithValue("$dept",d); ins.Parameters.AddWithValue("$at",DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture)); ins.Parameters.AddWithValue("$user",user); ins.ExecuteNonQuery(); } }
    private static List<BudgetRowPayload> LoadBudgetRows(SqliteConnection c,long headerId,long sid,string shift,string date){ var rows=new List<BudgetRowPayload>(); using var cmd=c.CreateCommand(); cmd.CommandText=@"SELECT r.BudgetRowID,r.BudgetHeaderID,r.DeptName,r.PlannedQty,r.UsedQty,r.VarianceQty,r.ReasonText,r.UpdatedAt,r.UpdatedBy,d.DisplayOrder FROM tblBudgetRows r LEFT JOIN tblDepartments d ON d.DeptName=r.DeptName WHERE r.BudgetHeaderID=$id ORDER BY COALESCE(d.DisplayOrder,999), r.BudgetRowID, r.DeptName"; cmd.Parameters.AddWithValue("$id",headerId); using var rd=cmd.ExecuteReader(); while(rd.Read()){ double? p=rd.IsDBNull(3)?null:rd.GetDouble(3); double? u=rd.IsDBNull(4)?null:rd.GetDouble(4); var v=rd.IsDBNull(5)?(u??0)-(p??0):rd.GetDouble(5); rows.Add(new BudgetRowPayload(rd.GetInt64(0),rd.GetInt64(1),sid,shift,date,rd.IsDBNull(2)?"":rd.GetString(2),p,u,v,ResolveRowStatus(p,u,v),rd.IsDBNull(6)?"":rd.GetString(6),rd.IsDBNull(7)?null:rd.GetString(7),rd.IsDBNull(8)?null:rd.GetString(8))); } return rows; }
    private static BudgetTotalsPayload CalculateTotals(IReadOnlyList<BudgetRowPayload> rows){ var p=rows.Sum(r=>r.PlannedQty??0); var u=rows.Sum(r=>r.UsedQty??0); var v=u-p; return new BudgetTotalsPayload(p,u,v,ResolveBudgetStatus(p,u,v)); }
    private static (string ShiftCode, DateTime ShiftDate)? GetSessionContext(SqliteConnection c,long sid){ using var cmd=c.CreateCommand(); cmd.CommandText="SELECT ShiftCode,ShiftDate FROM tblHandoverHeader WHERE HandoverID=$id LIMIT 1"; cmd.Parameters.AddWithValue("$id",sid); using var r=cmd.ExecuteReader(); if(!r.Read()) return null; return (r.IsDBNull(0)?string.Empty:r.GetString(0), DateTime.Parse(r.IsDBNull(1)?"1970-01-01":r.GetString(1), CultureInfo.InvariantCulture)); }
    private static long? FindBudgetHeaderId(SqliteConnection c,long sid){ using var cmd=c.CreateCommand(); cmd.CommandText="SELECT BudgetHeaderID FROM tblBudgetHeader WHERE HandoverID=$sid LIMIT 1"; cmd.Parameters.AddWithValue("$sid",sid); var v=cmd.ExecuteScalar(); return v==null||v==DBNull.Value?null:Convert.ToInt64(v); }
    private static (string? UpdatedAt,string? UpdatedBy) GetBudgetRowLastUpdated(SqliteConnection c,long headerId){ using var cmd=c.CreateCommand(); cmd.CommandText="SELECT UpdatedAt,UpdatedBy FROM tblBudgetRows WHERE BudgetHeaderID=$id ORDER BY UpdatedAt DESC, BudgetRowID DESC LIMIT 1"; cmd.Parameters.AddWithValue("$id",headerId); using var r=cmd.ExecuteReader(); return r.Read()?(r.IsDBNull(0)?null:r.GetString(0),r.IsDBNull(1)?null:r.GetString(1)):(null,null); }
    private static (double? LinesPlanned,double? TotalStaffOnRegister,string Comments) LoadBudgetHeaderMeta(SqliteConnection c,long headerId){ using var cmd=c.CreateCommand(); cmd.CommandText="SELECT LinesPlanned,TotalStaffOnRegister,Comments FROM tblBudgetHeader WHERE BudgetHeaderID=$id LIMIT 1"; cmd.Parameters.AddWithValue("$id",headerId); using var r=cmd.ExecuteReader(); return r.Read()?(r.IsDBNull(0)?null:r.GetDouble(0),r.IsDBNull(1)?null:r.GetDouble(1),r.IsDBNull(2)?string.Empty:r.GetString(2)):(null,null,string.Empty); }
    private static (int HolidayCount,int AbsentCount,int OtherReasonCount,int AgencyUsedCount) CountReasonBuckets(IReadOnlyList<BudgetRowPayload> rows){ int h=0,a=0,o=0,ag=0; foreach(var row in rows){ var t=(row.ReasonText??"").ToLowerInvariant(); if(t.Contains("holiday"))h++; if(t.Contains("absent"))a++; if(t.Contains("agency"))ag++; if(t.Contains("other"))o++; } return (h,a,o,ag);}    
    private static readonly string[] BudgetLabourAreas = ["Injection","MP / MetaPress","Berks","Wilts","FP / Further Processing","Brine operative","Rack cleaner / domestic","Goods in","Dry Goods","Supervisors","Admin","Cleaners","Slam","OH/MH Yard cleaner","Stock controller","Training","Trolley Porter T1/T2","Oak House","Butchery"];
}
