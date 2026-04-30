using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MoatHouseHandover.Host.Sqlite;

public sealed class SqliteConnectionFactory
{
    private readonly string _approvedDataRoot;

    public SqliteConnectionFactory(string approvedDataRoot)
    {
        _approvedDataRoot = EnsureTrailingSeparator(Path.GetFullPath(approvedDataRoot));
    }

    public SqliteConnection Create(string sqlitePath)
    {
        var fullPath = Path.GetFullPath(sqlitePath);
        var parent = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("SQLite path has no parent directory.");

        if (!fullPath.StartsWith(_approvedDataRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"SQLite target must stay under approved data root. Path '{fullPath}' is outside '{_approvedDataRoot}'.");
        }

        Directory.CreateDirectory(parent);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
            Cache = SqliteCacheMode.Shared
        };

        return new SqliteConnection(builder.ConnectionString);
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
}
