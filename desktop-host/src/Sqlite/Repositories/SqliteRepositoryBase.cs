using Microsoft.Data.Sqlite;
using MoatHouseHandover.Host.Sqlite;

namespace MoatHouseHandover.Host.Sqlite.Repositories;

public abstract class SqliteRepositoryBase
{
    private readonly SqliteConnectionFactory _factory;
    private readonly string _sqlitePath;

    protected SqliteRepositoryBase(string sqlitePath, string dataRoot)
    {
        _sqlitePath = sqlitePath;
        _factory = new SqliteConnectionFactory(dataRoot);
    }

    protected SqliteConnection OpenConnection()
    {
        var c = _factory.Create(_sqlitePath);
        c.Open();
        using var pragma = c.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000; PRAGMA journal_mode = DELETE;";
        pragma.ExecuteNonQuery();
        return c;
    }
}
