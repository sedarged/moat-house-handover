using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;

namespace MoatHouseHandover.Host;

public sealed class BudgetRepository
{
    private readonly string _connectionString;

    public BudgetRepository(string accessDatabasePath)
    {
        _connectionString = AccessBootstrapper.BuildConnectionString(accessDatabasePath);
    }

    public BudgetPayload LoadBudget(long sessionId, string userName)
    {
        using var connection = OpenConnection();
        var header = EnsureBudgetHeaderAndRows(connection, sessionId, userName);
        var rows = LoadBudgetRows(connection, header.BudgetHeaderId, sessionId, header.ShiftCode, header.ShiftDateIso);
        var totals = CalculateTotals(rows);
        var rowUpdated = GetBudgetRowLastUpdated(connection, header.BudgetHeaderId);
        var meta = LoadBudgetHeaderMeta(connection, header.BudgetHeaderId);
        var counts = CountReasonBuckets(rows);
        var summary = new BudgetSummaryPayload(
            totals.PlannedTotal,
            totals.UsedTotal,
            totals.VarianceTotal,
            totals.Status,
            rowUpdated.UpdatedAt,
            rowUpdated.UpdatedBy,
            rows.Count,
            meta.LinesPlanned,
            meta.TotalStaffOnRegister,
            counts.HolidayCount,
            counts.AbsentCount,
            counts.OtherReasonCount,
            counts.AgencyUsedCount,
            meta.Comments);

        return new BudgetPayload(
            header.BudgetHeaderId,
            sessionId,
            header.ShiftCode,
            header.ShiftDateIso,
            rows,
            totals,
            summary,
            header.UpdatedAt,
            header.UpdatedBy);
    }

    public BudgetPayload SaveBudget(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta, string userName)
    {
        using var connection = OpenConnection();
        var header = EnsureBudgetHeaderAndRows(connection, sessionId, userName);
        var now = DateTime.Now;

        using var tx = connection.BeginTransaction();

        foreach (var row in rows)
        {
            var planned = row.PlannedQty;
            var used = row.UsedQty;
            var variance = (used ?? 0) - (planned ?? 0);
            var reason = row.ReasonText?.Trim() ?? string.Empty;
            var deptName = row.DeptName?.Trim() ?? string.Empty;

            if (row.BudgetRowId.HasValue && row.BudgetRowId.Value > 0)
            {
                using var update = new OleDbCommand(@"UPDATE tblBudgetRows
SET DeptName = ?, PlannedQty = ?, UsedQty = ?, VarianceQty = ?, ReasonText = ?, UpdatedAt = ?, UpdatedBy = ?
WHERE BudgetRowID = ? AND BudgetHeaderID = ?", connection, tx);
                update.Parameters.AddWithValue("@p1", deptName);
                update.Parameters.AddWithValue("@p2", ToDbNullable(planned));
                update.Parameters.AddWithValue("@p3", ToDbNullable(used));
                update.Parameters.AddWithValue("@p4", variance);
                update.Parameters.AddWithValue("@p5", reason);
                update.Parameters.AddWithValue("@p6", now);
                update.Parameters.AddWithValue("@p7", userName);
                update.Parameters.AddWithValue("@p8", row.BudgetRowId.Value);
                update.Parameters.AddWithValue("@p9", header.BudgetHeaderId);

                var affected = update.ExecuteNonQuery();
                if (affected > 0)
                {
                    continue;
                }
            }

            using var insert = new OleDbCommand(@"INSERT INTO tblBudgetRows
(BudgetHeaderID, DeptName, PlannedQty, UsedQty, VarianceQty, ReasonText, UpdatedAt, UpdatedBy)
VALUES (?, ?, ?, ?, ?, ?, ?, ?)", connection, tx);
            insert.Parameters.AddWithValue("@p1", header.BudgetHeaderId);
            insert.Parameters.AddWithValue("@p2", deptName);
            insert.Parameters.AddWithValue("@p3", ToDbNullable(planned));
            insert.Parameters.AddWithValue("@p4", ToDbNullable(used));
            insert.Parameters.AddWithValue("@p5", variance);
            insert.Parameters.AddWithValue("@p6", reason);
            insert.Parameters.AddWithValue("@p7", now);
            insert.Parameters.AddWithValue("@p8", userName);
            insert.ExecuteNonQuery();
        }

        using (var updateHeader = new OleDbCommand(@"UPDATE tblBudgetHeader
SET UpdatedAt = ?, UpdatedBy = ?
WHERE BudgetHeaderID = ?", connection, tx))
        {
            updateHeader.Parameters.AddWithValue("@p1", now);
            updateHeader.Parameters.AddWithValue("@p2", userName);
            updateHeader.Parameters.AddWithValue("@p3", header.BudgetHeaderId);
            updateHeader.ExecuteNonQuery();
        }

        if (meta is not null)
        {
            using var updateMeta = new OleDbCommand(@"UPDATE tblBudgetHeader SET LinesPlanned = ?, TotalStaffOnRegister = ?, Comments = ? WHERE BudgetHeaderID = ?", connection, tx);
            updateMeta.Parameters.AddWithValue("@p1", ToDbNullable(meta.LinesPlanned));
            updateMeta.Parameters.AddWithValue("@p2", ToDbNullable(meta.TotalStaffOnRegister));
            updateMeta.Parameters.AddWithValue("@p3", meta.Comments?.Trim() ?? string.Empty);
            updateMeta.Parameters.AddWithValue("@p4", header.BudgetHeaderId);
            updateMeta.ExecuteNonQuery();
        }

        tx.Commit();
        return LoadBudget(sessionId, userName);
    }

    public BudgetSummaryPayload LoadBudgetSummary(long sessionId)
    {
        using var connection = OpenConnection();
        _ = GetSessionContext(connection, sessionId)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        var budgetHeaderId = FindBudgetHeaderId(connection, sessionId);
        if (budgetHeaderId is null)
        {
            return new BudgetSummaryPayload(0, 0, 0, "not set", null, null, 0, null, null, 0, 0, 0, 0, string.Empty);
        }

        const string sql = @"SELECT
SUM(IIF(PlannedQty IS NULL, 0, PlannedQty)) AS PlannedTotal,
SUM(IIF(UsedQty IS NULL, 0, UsedQty)) AS UsedTotal,
SUM(IIF(VarianceQty IS NULL, IIF(UsedQty IS NULL, 0, UsedQty) - IIF(PlannedQty IS NULL, 0, PlannedQty), VarianceQty)) AS VarianceTotal,
COUNT(*) AS RowCount
FROM tblBudgetRows
WHERE BudgetHeaderID = ?";

        using var cmd = new OleDbCommand(sql, connection);
        cmd.Parameters.AddWithValue("@p1", budgetHeaderId.Value);
        using var reader = cmd.ExecuteReader();
        if (!reader!.Read())
        {
            return new BudgetSummaryPayload(0, 0, 0, "not set", null, null, 0, null, null, 0, 0, 0, 0, string.Empty);
        }

        var planned = reader["PlannedTotal"] == DBNull.Value ? 0 : Convert.ToDouble(reader["PlannedTotal"]);
        var used = reader["UsedTotal"] == DBNull.Value ? 0 : Convert.ToDouble(reader["UsedTotal"]);
        var variance = reader["VarianceTotal"] == DBNull.Value ? used - planned : Convert.ToDouble(reader["VarianceTotal"]);
        var status = ResolveBudgetStatus(planned, used, variance);
        var updated = GetBudgetRowLastUpdated(connection, budgetHeaderId.Value);
        var rowCount = reader["RowCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RowCount"]);

        var meta = LoadBudgetHeaderMeta(connection, budgetHeaderId.Value);
        var rows = LoadBudgetRows(connection, budgetHeaderId.Value, sessionId, "", "");
        var counts = CountReasonBuckets(rows);
        return new BudgetSummaryPayload(planned, used, variance, status, updated.UpdatedAt, updated.UpdatedBy, rowCount, meta.LinesPlanned, meta.TotalStaffOnRegister, counts.HolidayCount, counts.AbsentCount, counts.OtherReasonCount, counts.AgencyUsedCount, meta.Comments);
    }

    public BudgetPayload Recalculate(long sessionId, IReadOnlyList<BudgetRowUpsertRequest> rows, BudgetMetaUpsertRequest? meta)
    {
        using var connection = OpenConnection();
        var context = GetSessionContext(connection, sessionId)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        var headerId = FindBudgetHeaderId(connection, sessionId) ?? 0;
        var projectedRows = new List<BudgetRowPayload>(rows.Count);
        foreach (var row in rows)
        {
            var planned = row.PlannedQty;
            var used = row.UsedQty;
            var variance = (used ?? 0) - (planned ?? 0);
            projectedRows.Add(new BudgetRowPayload(
                BudgetRowId: row.BudgetRowId ?? 0,
                BudgetHeaderId: headerId,
                SessionId: sessionId,
                ShiftCode: context.ShiftCode,
                ShiftDate: context.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DeptName: row.DeptName?.Trim() ?? string.Empty,
                PlannedQty: planned,
                UsedQty: used,
                Variance: variance,
                Status: ResolveRowStatus(planned, used, variance),
                ReasonText: row.ReasonText?.Trim() ?? string.Empty,
                UpdatedAt: null,
                UpdatedBy: null));
        }

        var totals = CalculateTotals(projectedRows);
        var counts = CountReasonBuckets(projectedRows);
        var summary = new BudgetSummaryPayload(totals.PlannedTotal, totals.UsedTotal, totals.VarianceTotal, totals.Status, null, null, projectedRows.Count, meta?.LinesPlanned, meta?.TotalStaffOnRegister, counts.HolidayCount, counts.AbsentCount, counts.OtherReasonCount, counts.AgencyUsedCount, meta?.Comments?.Trim() ?? string.Empty);

        return new BudgetPayload(
            headerId,
            sessionId,
            context.ShiftCode,
            context.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            projectedRows,
            totals,
            summary,
            null,
            null);
    }

    private (long BudgetHeaderId, string ShiftCode, string ShiftDateIso, string? UpdatedAt, string? UpdatedBy) EnsureBudgetHeaderAndRows(OleDbConnection connection, long sessionId, string userName)
    {
        var context = GetSessionContext(connection, sessionId)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        var budgetHeaderId = FindBudgetHeaderId(connection, sessionId);
        if (budgetHeaderId is null)
        {
            var now = DateTime.Now;
            using var insertHeader = new OleDbCommand(@"INSERT INTO tblBudgetHeader
(HandoverID, ShiftDate, ShiftCode, LinesPlanned, TotalStaffOnRegister, Comments, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", connection);
            insertHeader.Parameters.AddWithValue("@p1", sessionId);
            insertHeader.Parameters.AddWithValue("@p2", context.ShiftDate);
            insertHeader.Parameters.AddWithValue("@p3", context.ShiftCode);
            insertHeader.Parameters.AddWithValue("@p4", DBNull.Value);
            insertHeader.Parameters.AddWithValue("@p5", DBNull.Value);
            insertHeader.Parameters.AddWithValue("@p6", string.Empty);
            insertHeader.Parameters.AddWithValue("@p7", now);
            insertHeader.Parameters.AddWithValue("@p8", userName);
            insertHeader.Parameters.AddWithValue("@p9", now);
            insertHeader.Parameters.AddWithValue("@p10", userName);
            insertHeader.ExecuteNonQuery();

            budgetHeaderId = FindBudgetHeaderId(connection, sessionId);
        }

        if (budgetHeaderId is null)
        {
            throw new InvalidOperationException("Budget header could not be created.");
        }

        EnsureBudgetRows(connection, budgetHeaderId.Value, userName);

        using var headerCmd = new OleDbCommand("SELECT TOP 1 UpdatedAt, UpdatedBy FROM tblBudgetHeader WHERE BudgetHeaderID = ?", connection);
        headerCmd.Parameters.AddWithValue("@p1", budgetHeaderId.Value);
        using var reader = headerCmd.ExecuteReader();

        string? updatedAt = null;
        string? updatedBy = null;
        if (reader!.Read())
        {
            updatedAt = reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]);
            updatedBy = reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"]);
        }

        return (
            budgetHeaderId.Value,
            context.ShiftCode,
            context.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            updatedAt,
            updatedBy);
    }

    private static void EnsureBudgetRows(OleDbConnection connection, long budgetHeaderId, string userName)
    {
        using var countCmd = new OleDbCommand("SELECT COUNT(*) FROM tblBudgetRows WHERE BudgetHeaderID = ?", connection);
        countCmd.Parameters.AddWithValue("@p1", budgetHeaderId);
        var count = Convert.ToInt32(countCmd.ExecuteScalar());
        if (count > 0)
        {
            return;
        }

        var departments = new List<string>(BudgetLabourAreas);

        var now = DateTime.Now;
        foreach (var dept in departments)
        {
            using var insert = new OleDbCommand(@"INSERT INTO tblBudgetRows
(BudgetHeaderID, DeptName, PlannedQty, UsedQty, VarianceQty, ReasonText, UpdatedAt, UpdatedBy)
VALUES (?, ?, NULL, NULL, 0, '', ?, ?)", connection);
            insert.Parameters.AddWithValue("@p1", budgetHeaderId);
            insert.Parameters.AddWithValue("@p2", dept);
            insert.Parameters.AddWithValue("@p3", now);
            insert.Parameters.AddWithValue("@p4", userName);
            insert.ExecuteNonQuery();
        }
    }

    private static List<BudgetRowPayload> LoadBudgetRows(OleDbConnection connection, long budgetHeaderId, long sessionId, string shiftCode, string shiftDate)
    {
        var rows = new List<BudgetRowPayload>();
        const string sql = @"SELECT r.BudgetRowID, r.BudgetHeaderID, r.DeptName, r.PlannedQty, r.UsedQty, r.VarianceQty, r.ReasonText, r.UpdatedAt, r.UpdatedBy, d.DisplayOrder
FROM tblBudgetRows AS r
LEFT JOIN tblDepartments AS d ON d.DeptName = r.DeptName
WHERE r.BudgetHeaderID = ?
ORDER BY IIF(d.DisplayOrder IS NULL, 999, d.DisplayOrder), r.BudgetRowID, r.DeptName";

        using var cmd = new OleDbCommand(sql, connection);
        cmd.Parameters.AddWithValue("@p1", budgetHeaderId);
        using var reader = cmd.ExecuteReader();
        while (reader!.Read())
        {
            double? planned = reader["PlannedQty"] == DBNull.Value ? null : Convert.ToDouble(reader["PlannedQty"]);
            double? used = reader["UsedQty"] == DBNull.Value ? null : Convert.ToDouble(reader["UsedQty"]);
            var variance = reader["VarianceQty"] == DBNull.Value
                ? (used ?? 0) - (planned ?? 0)
                : Convert.ToDouble(reader["VarianceQty"]);

            rows.Add(new BudgetRowPayload(
                BudgetRowId: Convert.ToInt64(reader["BudgetRowID"]),
                BudgetHeaderId: Convert.ToInt64(reader["BudgetHeaderID"]),
                SessionId: sessionId,
                ShiftCode: shiftCode,
                ShiftDate: shiftDate,
                DeptName: Convert.ToString(reader["DeptName"]) ?? string.Empty,
                PlannedQty: planned,
                UsedQty: used,
                Variance: variance,
                Status: ResolveRowStatus(planned, used, variance),
                ReasonText: reader["ReasonText"] == DBNull.Value ? string.Empty : (Convert.ToString(reader["ReasonText"]) ?? string.Empty),
                UpdatedAt: reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]),
                UpdatedBy: reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"])));
        }

        return rows;
    }

    private static BudgetTotalsPayload CalculateTotals(IReadOnlyList<BudgetRowPayload> rows)
    {
        double planned = 0;
        double used = 0;
        foreach (var row in rows)
        {
            planned += row.PlannedQty ?? 0;
            used += row.UsedQty ?? 0;
        }

        var variance = used - planned;
        var status = ResolveBudgetStatus(planned, used, variance);

        return new BudgetTotalsPayload(planned, used, variance, status);
    }

    private static string ResolveBudgetStatus(double planned, double used, double variance)
    {
        if (planned == 0 && used == 0)
        {
            return "not set";
        }

        if (Math.Abs(variance) < 0.0001)
        {
            return "on target";
        }

        return variance > 0 ? "over" : "under";
    }

    private static string ResolveRowStatus(double? planned, double? used, double variance)
    {
        if (!planned.HasValue && !used.HasValue)
        {
            return "not set";
        }

        if (Math.Abs(variance) < 0.0001)
        {
            return "on target";
        }

        return variance > 0 ? "over" : "under";
    }

    private static (string ShiftCode, DateTime ShiftDate)? GetSessionContext(OleDbConnection connection, long sessionId)
    {
        using var cmd = new OleDbCommand("SELECT TOP 1 ShiftCode, ShiftDate FROM tblHandoverHeader WHERE HandoverID = ?", connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);
        using var reader = cmd.ExecuteReader();
        if (!reader!.Read())
        {
            return null;
        }

        return (
            ShiftCode: Convert.ToString(reader["ShiftCode"]) ?? string.Empty,
            ShiftDate: Convert.ToDateTime(reader["ShiftDate"]).Date);
    }

    private static long? FindBudgetHeaderId(OleDbConnection connection, long sessionId)
    {
        using var cmd = new OleDbCommand("SELECT TOP 1 BudgetHeaderID FROM tblBudgetHeader WHERE HandoverID = ?", connection);
        cmd.Parameters.AddWithValue("@p1", sessionId);
        var value = cmd.ExecuteScalar();
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt64(value);
    }

    private static (string? UpdatedAt, string? UpdatedBy) GetBudgetRowLastUpdated(OleDbConnection connection, long budgetHeaderId)
    {
        using var cmd = new OleDbCommand(@"SELECT TOP 1 UpdatedAt, UpdatedBy
FROM tblBudgetRows
WHERE BudgetHeaderID = ?
ORDER BY UpdatedAt DESC, BudgetRowID DESC", connection);
        cmd.Parameters.AddWithValue("@p1", budgetHeaderId);
        using var reader = cmd.ExecuteReader();
        if (!reader!.Read())
        {
            return (null, null);
        }

        var updatedAt = reader["UpdatedAt"] == DBNull.Value ? null : ToIso(reader["UpdatedAt"]);
        var updatedBy = reader["UpdatedBy"] == DBNull.Value ? null : Convert.ToString(reader["UpdatedBy"]);
        return (updatedAt, updatedBy);
    }

    private static (double? LinesPlanned, double? TotalStaffOnRegister, string Comments) LoadBudgetHeaderMeta(OleDbConnection connection, long budgetHeaderId)
    {
        using var cmd = new OleDbCommand("SELECT TOP 1 LinesPlanned, TotalStaffOnRegister, Comments FROM tblBudgetHeader WHERE BudgetHeaderID = ?", connection);
        cmd.Parameters.AddWithValue("@p1", budgetHeaderId);
        using var reader = cmd.ExecuteReader();
        if (reader != null && reader.Read())
        {
            double? lines = reader["LinesPlanned"] == DBNull.Value ? null : Convert.ToDouble(reader["LinesPlanned"]);
            double? reg = reader["TotalStaffOnRegister"] == DBNull.Value ? null : Convert.ToDouble(reader["TotalStaffOnRegister"]);
            var comments = reader["Comments"] == DBNull.Value ? string.Empty : (Convert.ToString(reader["Comments"]) ?? string.Empty);
            return (lines, reg, comments);
        }
        return (null, null, string.Empty);
    }

    private static (int HolidayCount, int AbsentCount, int OtherReasonCount, int AgencyUsedCount) CountReasonBuckets(IReadOnlyList<BudgetRowPayload> rows)
    {
        var holiday = 0; var absent = 0; var other = 0; var agency = 0;
        foreach (var row in rows)
        {
            var t = (row.ReasonText ?? string.Empty).ToLowerInvariant();
            if (t.Contains("holiday")) holiday++;
            if (t.Contains("absent")) absent++;
            if (t.Contains("agency")) agency++;
            if (t.Contains("other")) other++;
        }
        return (holiday, absent, other, agency);
    }

    private static readonly string[] BudgetLabourAreas = new[]
    {
        "Injection", "MP / MetaPress", "Berks", "Wilts", "FP / Further Processing", "Brine operative",
        "Rack cleaner / domestic", "Goods in", "Dry Goods", "Supervisors", "Admin", "Cleaners",
        "Slam", "OH/MH Yard cleaner", "Stock controller", "Training", "Trolley Porter T1/T2", "Oak House", "Butchery"
    };

    private OleDbConnection OpenConnection()
    {
        var connection = new OleDbConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static object ToDbNullable(double? value)
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }

    private static string ToIso(object dateValue)
    {
        var value = Convert.ToDateTime(dateValue);
        return value.ToString("O", CultureInfo.InvariantCulture);
    }
}
