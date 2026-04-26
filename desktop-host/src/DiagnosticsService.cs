using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed class DiagnosticsService
{
    private readonly HostRuntimeStatus _runtimeStatus;
    private readonly HostConfig _config;

    public DiagnosticsService(HostRuntimeStatus runtimeStatus, HostConfig config)
    {
        _runtimeStatus = runtimeStatus;
        _config = config;
    }

    public DiagnosticsPayload Run(DiagnosticsRunRequest _)
    {
        var checks = new List<DiagnosticsCheckResult>();

        AddCheck(checks, "os.info", () =>
        {
            var isWindows = OperatingSystem.IsWindows();
            var os = Environment.OSVersion.VersionString;
            var status = isWindows ? "ok" : "warning";
            return new DiagnosticsCheckResult("os.info", status, isWindows ? "Running on Windows." : "Running on non-Windows environment.", os);
        });

        AddCheck(checks, "config.values.loaded", () =>
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(_config.AccessDatabasePath))
            {
                missing.Add("accessDatabasePath");
            }

            if (string.IsNullOrWhiteSpace(_config.AttachmentsRoot))
            {
                missing.Add("attachmentsRoot");
            }

            if (string.IsNullOrWhiteSpace(_config.ReportsOutputRoot))
            {
                missing.Add("reportsOutputRoot");
            }

            if (missing.Count > 0)
            {
                return new DiagnosticsCheckResult("config.values.loaded", "failed", "Required runtime config values are missing.", string.Join(", ", missing));
            }

            return new DiagnosticsCheckResult("config.values.loaded", "ok", "Runtime config values are loaded.", _runtimeStatus.ConfigPath);
        });

        AddCheck(checks, "access.database.path", () =>
        {
            var exists = File.Exists(_runtimeStatus.AccessDatabasePath);
            return new DiagnosticsCheckResult(
                "access.database.path",
                exists ? "ok" : "failed",
                exists ? "Access database path exists." : "Access database path was not found.",
                _runtimeStatus.AccessDatabasePath);
        });

        AddCheck(checks, "access.connection.open", () =>
        {
            using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
            connection.Open();
            return new DiagnosticsCheckResult("access.connection.open", "ok", "Access connection opened successfully.", connection.DataSource ?? _runtimeStatus.AccessDatabasePath);
        });

        AddCheck(checks, "access.config.seed", CheckConfigSeed);

        AddCheck(checks, "attachments.root.exists", () => EnsureDirectory("attachments.root.exists", _runtimeStatus.AttachmentsRoot));
        AddCheck(checks, "reports.root.exists", () => EnsureDirectory("reports.root.exists", _runtimeStatus.ReportsOutputRoot));

        AddCheck(checks, "attachments.root.write", () => EnsureWriteAccess("attachments.root.write", _runtimeStatus.AttachmentsRoot));
        AddCheck(checks, "reports.root.write", () => EnsureWriteAccess("reports.root.write", _runtimeStatus.ReportsOutputRoot));

        AddCheck(checks, "email.profiles.exist", CheckEmailProfilesExist);
        AddCheck(checks, "email.profiles.active.valid", CheckActiveProfilesValid);

        AddCheck(checks, "outlook.com.available", () =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return new DiagnosticsCheckResult("outlook.com.available", "warning", "Outlook COM check requires Windows.", "Non-Windows environment");
            }

            var outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType is null)
            {
                return new DiagnosticsCheckResult("outlook.com.available", "warning", "Outlook COM type is unavailable.", "ProgID Outlook.Application was not found");
            }

            return new DiagnosticsCheckResult("outlook.com.available", "ok", "Outlook COM type is available.", outlookType.FullName ?? "Outlook.Application");
        });

        var overall = ComputeOverallStatus(checks);
        return new DiagnosticsPayload(overall, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), checks);
    }

    private DiagnosticsCheckResult CheckConfigSeed()
    {
        using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
        connection.Open();

        var keys = new[] { "accessDatabasePath", "attachmentsRoot", "reportsOutputRoot" };
        var missing = new List<string>();

        foreach (var key in keys)
        {
            using var cmd = new OleDbCommand("SELECT TOP 1 ConfigValue FROM tblConfig WHERE ConfigKey = ?", connection);
            cmd.Parameters.AddWithValue("@p1", key);
            var value = cmd.ExecuteScalar();
            if (value is null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))
            {
                missing.Add(key);
            }
        }

        if (missing.Count > 0)
        {
            return new DiagnosticsCheckResult("access.config.seed", "warning", "One or more tblConfig values are missing/blank.", string.Join(", ", missing));
        }

        return new DiagnosticsCheckResult("access.config.seed", "ok", "tblConfig values were found.", string.Join(", ", keys));
    }

    private DiagnosticsCheckResult CheckEmailProfilesExist()
    {
        using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
        connection.Open();

        var missing = new List<string>();
        foreach (var shift in new[] { "AM", "PM", "NS" })
        {
            using var cmd = new OleDbCommand(@"SELECT TOP 1 p.EmailProfileKey
FROM tblShiftRules AS s
LEFT JOIN tblEmailProfiles AS p ON p.EmailProfileKey = s.EmailProfileKey
WHERE s.ShiftCode = ?", connection);
            cmd.Parameters.AddWithValue("@p1", shift);
            var value = cmd.ExecuteScalar();
            if (value is null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))
            {
                missing.Add(shift);
            }
        }

        if (missing.Count > 0)
        {
            return new DiagnosticsCheckResult("email.profiles.exist", "failed", "Missing email profile mapping for one or more shifts.", string.Join(", ", missing));
        }

        return new DiagnosticsCheckResult("email.profiles.exist", "ok", "Email profile mappings exist for AM/PM/NS.", "AM, PM, NS");
    }

    private DiagnosticsCheckResult CheckActiveProfilesValid()
    {
        using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
        connection.Open();

        var issues = new List<string>();

        foreach (var shift in new[] { "AM", "PM", "NS" })
        {
            using var cmd = new OleDbCommand(@"SELECT TOP 1 p.EmailProfileKey, p.IsActive, p.ToList, p.CcList
FROM tblShiftRules AS s
LEFT JOIN tblEmailProfiles AS p ON p.EmailProfileKey = s.EmailProfileKey
WHERE s.ShiftCode = ?", connection);
            cmd.Parameters.AddWithValue("@p1", shift);
            using var reader = cmd.ExecuteReader();
            if (!reader!.Read())
            {
                issues.Add(shift + ": mapping missing");
                continue;
            }

            var key = Convert.ToString(reader["EmailProfileKey"]) ?? string.Empty;
            var isActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]);
            var toList = Convert.ToString(reader["ToList"]) ?? string.Empty;
            var ccList = Convert.ToString(reader["CcList"]) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                issues.Add(shift + ": profile key missing");
                continue;
            }

            if (!isActive)
            {
                issues.Add(shift + ": profile inactive");
                continue;
            }

            if (string.IsNullOrWhiteSpace(toList) && string.IsNullOrWhiteSpace(ccList))
            {
                issues.Add(shift + ": no To/CC recipients");
            }
        }

        if (issues.Count > 0)
        {
            return new DiagnosticsCheckResult("email.profiles.active.valid", "warning", "One or more active profile checks failed.", string.Join(" | ", issues));
        }

        return new DiagnosticsCheckResult("email.profiles.active.valid", "ok", "Active email profiles look valid.", "AM, PM, NS");
    }

    private static DiagnosticsCheckResult EnsureDirectory(string checkName, string path)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(fullPath);
        return new DiagnosticsCheckResult(checkName, "ok", "Directory exists or was created.", fullPath);
    }

    private static DiagnosticsCheckResult EnsureWriteAccess(string checkName, string path)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(fullPath);

        var probePath = Path.Combine(fullPath, ".diagnostic_write_probe_" + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(probePath, "probe");
        File.Delete(probePath);

        return new DiagnosticsCheckResult(checkName, "ok", "Write access confirmed.", fullPath);
    }

    private static void AddCheck(List<DiagnosticsCheckResult> checks, string checkName, Func<DiagnosticsCheckResult> checker)
    {
        try
        {
            checks.Add(checker());
        }
        catch (Exception ex)
        {
            checks.Add(new DiagnosticsCheckResult(checkName, "failed", ex.Message, ex.GetType().Name));
        }
    }

    private static string ComputeOverallStatus(IEnumerable<DiagnosticsCheckResult> checks)
    {
        var hasFailed = false;
        var hasWarning = false;

        foreach (var check in checks)
        {
            if (string.Equals(check.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                hasFailed = true;
                continue;
            }

            if (string.Equals(check.Status, "warning", StringComparison.OrdinalIgnoreCase))
            {
                hasWarning = true;
            }
        }

        if (hasFailed)
        {
            return "failed";
        }

        return hasWarning ? "warning" : "ok";
    }
}
