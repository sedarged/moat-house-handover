using System;
using System.Data.OleDb;

namespace MoatHouseHandover.Host;

public sealed class EmailProfileRepository
{
    private readonly string _connectionString;

    public EmailProfileRepository(string accessDatabasePath)
    {
        _connectionString = AccessBootstrapper.BuildConnectionString(accessDatabasePath);
    }

    public EmailProfilePayload? LoadByShiftCode(string shiftCode)
    {
        using var connection = new OleDbConnection(_connectionString);
        connection.Open();

        using var command = new OleDbCommand(@"SELECT TOP 1 s.ShiftCode, s.EmailProfileKey, p.ToList, p.CcList, p.SubjectTemplate, p.BodyTemplate, p.IsActive
FROM tblShiftRules AS s
LEFT JOIN tblEmailProfiles AS p ON p.EmailProfileKey = s.EmailProfileKey
WHERE s.ShiftCode = ?", connection);
        command.Parameters.AddWithValue("@p1", shiftCode);

        using var reader = command.ExecuteReader();
        if (!reader!.Read())
        {
            return null;
        }

        var emailProfileKey = Convert.ToString(reader["EmailProfileKey"]) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(emailProfileKey))
        {
            return null;
        }

        return new EmailProfilePayload(
            EmailProfileKey: emailProfileKey,
            ShiftCode: Convert.ToString(reader["ShiftCode"]) ?? string.Empty,
            ToList: Convert.ToString(reader["ToList"]) ?? string.Empty,
            CcList: Convert.ToString(reader["CcList"]) ?? string.Empty,
            SubjectTemplate: Convert.ToString(reader["SubjectTemplate"]) ?? string.Empty,
            BodyTemplate: Convert.ToString(reader["BodyTemplate"]) ?? string.Empty,
            IsActive: reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]));
    }
}
