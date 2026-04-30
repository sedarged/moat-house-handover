using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite;

public sealed class SqliteBootstrapper
{
    private static readonly (int Order, string DeptName, int IsMetricDept)[] Departments =
    [
        (1, "Injection", 1), (2, "MetaPress", 1), (3, "Berks", 1), (4, "Wilts", 1),
        (5, "Racking", 0), (6, "Butchery", 0), (7, "Further Processing", 0), (8, "Tumblers", 0),
        (9, "Smoke Tumbler", 0), (10, "Minimums & Samples", 0), (11, "Goods In & Despatch", 0), (12, "Dry Goods", 0), (13, "Additional", 0)
    ];

    public BootstrapResult EnsureBootstrapped(string sqlitePath, string dataRoot, string actor)
    {
        var factory = new SqliteConnectionFactory(dataRoot);
        using var connection = factory.Create(sqlitePath);
        connection.Open();

        ExecutePragmaNonQuery(connection, "PRAGMA foreign_keys = ON;");
        ExecutePragmaNonQuery(connection, "PRAGMA busy_timeout = 5000;");
        var journalMode = ReadPragmaString(connection, "PRAGMA journal_mode = DELETE;");

        using var tx = connection.BeginTransaction();

        foreach (var statement in SqliteSchema.CreateStatements)
        {
            Execute(connection, tx, statement);
        }

        SeedDepartments(connection, tx);
        SeedShiftRules(connection, tx);
        SeedConfig(connection, tx, dataRoot, sqlitePath);
        SeedInitialMigration(connection, tx, actor);
        tx.Commit();

        var tablesPresent = CountExistingTables(connection, SqliteSchema.RequiredTables);
        var migrationExists = HasMigration(connection, SqliteSchema.InitialMigrationId);
        return new BootstrapResult(true, $"SQLite bootstrap completed at '{sqlitePath}' with journal_mode={journalMode}.", tablesPresent, migrationExists);
    }

    private static void SeedDepartments(SqliteConnection connection, SqliteTransaction tx)
    {
        foreach (var item in Departments)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT OR IGNORE INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive)
VALUES ($name, $order, $metric, 0, 1);";
            cmd.Parameters.AddWithValue("$name", item.DeptName);
            cmd.Parameters.AddWithValue("$order", item.Order);
            cmd.Parameters.AddWithValue("$metric", item.IsMetricDept);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedShiftRules(SqliteConnection connection, SqliteTransaction tx)
    {
        var shifts = new[] { ("AM", "Morning", "default_am", 1), ("PM", "Afternoon", "default_pm", 2), ("NS", "Night", "default_ns", 3) };
        foreach (var shift in shifts)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT OR IGNORE INTO tblShiftRules (ShiftCode, ShiftName, EmailProfileKey, DisplayOrder)
VALUES ($code, $name, $profile, $order);";
            cmd.Parameters.AddWithValue("$code", shift.Item1);
            cmd.Parameters.AddWithValue("$name", shift.Item2);
            cmd.Parameters.AddWithValue("$profile", shift.Item3);
            cmd.Parameters.AddWithValue("$order", shift.Item4);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedConfig(SqliteConnection connection, SqliteTransaction tx, string dataRoot, string sqlitePath)
    {
        var values = new Dictionary<string, string>
        {
            ["databaseProvider"] = "SQLite",
            ["dataRoot"] = dataRoot.TrimEnd('\\'),
            ["sqliteDatabasePath"] = sqlitePath,
            ["attachmentsRoot"] = System.IO.Path.Combine(dataRoot, "Attachments"),
            ["reportsOutputRoot"] = System.IO.Path.Combine(dataRoot, "Reports"),
            ["backupsRoot"] = System.IO.Path.Combine(dataRoot, "Backups"),
            ["logsRoot"] = System.IO.Path.Combine(dataRoot, "Logs"),
            ["importsRoot"] = System.IO.Path.Combine(dataRoot, "Imports"),
            ["migrationRoot"] = System.IO.Path.Combine(dataRoot, "Migration")
        };

        foreach (var pair in values)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR IGNORE INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ($key, $value, 'Phase 4 SQLite bootstrap seed');";
            cmd.Parameters.AddWithValue("$key", pair.Key);
            cmd.Parameters.AddWithValue("$value", pair.Value);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedInitialMigration(SqliteConnection connection, SqliteTransaction tx, string actor)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"INSERT OR IGNORE INTO tblSchemaMigrations (MigrationId, AppliedAt, AppliedBy, Notes)
VALUES ($id, $at, $by, 'Initial SQLite schema bootstrap');";
        cmd.Parameters.AddWithValue("$id", SqliteSchema.InitialMigrationId);
        cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$by", actor);
        cmd.ExecuteNonQuery();
    }

    private static int CountExistingTables(SqliteConnection connection, IReadOnlyList<string> required)
    {
        var count = 0;
        foreach (var table in required)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            cmd.Parameters.AddWithValue("$name", table);
            var exists = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            if (exists) count++;
        }

        return count;
    }

    private static bool HasMigration(SqliteConnection connection, string migrationId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM tblSchemaMigrations WHERE MigrationId = $id;";
        cmd.Parameters.AddWithValue("$id", migrationId);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static void ExecutePragmaNonQuery(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string ReadPragmaString(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction tx, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

public sealed record BootstrapResult(bool Success, string Message, int ExistingRequiredTableCount, bool InitialMigrationExists);
