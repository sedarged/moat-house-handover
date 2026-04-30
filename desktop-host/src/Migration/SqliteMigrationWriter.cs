using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using MoatHouseHandover.Host.Sqlite;

namespace MoatHouseHandover.Host.Migration;

public sealed class SqliteMigrationWriter
{
    public IReadOnlyList<MigrationTableResult> Import(MigrationOptions options, IReadOnlyDictionary<string, DataTable> source)
    {
        if (File.Exists(options.Paths.StagingSqlitePath)) File.Delete(options.Paths.StagingSqlitePath);
        var bootstrapper = new SqliteBootstrapper();
        bootstrapper.EnsureBootstrapped(options.Paths.StagingSqlitePath, Path.GetDirectoryName(options.Paths.TargetSqlitePath)!.Replace("\\Data",""), options.Actor);

        var factory = new SqliteConnectionFactory(Path.GetDirectoryName(options.Paths.TargetSqlitePath)!.Replace("\\Data",""));
        using var conn = factory.Create(options.Paths.StagingSqlitePath);
        conn.Open();
        var list = new List<MigrationTableResult>();
        foreach (var pair in source)
        {
            var imported = 0;
            var failed = 0;
            foreach (DataRow row in pair.Value.Rows)
            {
                try { InsertRow(conn, pair.Key, row); imported++; } catch { failed++; }
            }
            list.Add(new MigrationTableResult(pair.Key, pair.Key, pair.Value.Rows.Count, imported, pair.Value.Rows.Count - imported - failed, failed));
        }
        return list;
    }

    private static void InsertRow(SqliteConnection conn, string table, DataRow row)
    {
        var cols = new List<string>(); var vals = new List<string>();
        using var cmd = conn.CreateCommand();
        var i = 0;
        foreach (DataColumn c in row.Table.Columns)
        {
            cols.Add(c.ColumnName); var p = "$p" + i++; vals.Add(p); cmd.Parameters.AddWithValue(p, NormalizeValue(c.ColumnName,row[c]));
        }
        cmd.CommandText = $"INSERT INTO {table} ({string.Join(",", cols)}) VALUES ({string.Join(",", vals)});";
        cmd.ExecuteNonQuery();
    }

    private static object? NormalizeValue(string column, object value)
    {
        if (value is DBNull) return null;
        if (value is bool b) return b ? 1 : 0;
        if (value is DateTime dt)
        {
            if (column.Contains("ShiftDate", StringComparison.OrdinalIgnoreCase)) return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return dt.ToString("O", CultureInfo.InvariantCulture);
        }
        if (column.Equals("ConfigValue", StringComparison.OrdinalIgnoreCase))
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return text.Replace("C:/MOAT-Handover/shared", @"M:\Moat House\MoatHouse Handover");
        }
        return value;
    }
}
