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

        AddCheck(checks, "config.values.loaded", CheckConfigValuesLoaded);
        AddCheck(checks, "storage.data_root.policy", CheckDataRootPolicy);

        AddCheck(checks, "storage.data.exists", () => EnsureDirectory("storage.data.exists", _runtimeStatus.DataDirectory));
        AddCheck(checks, "storage.attachments.exists", () => EnsureDirectory("storage.attachments.exists", _runtimeStatus.AttachmentsRoot));
        AddCheck(checks, "storage.reports.exists", () => EnsureDirectory("storage.reports.exists", _runtimeStatus.ReportsOutputRoot));
        AddCheck(checks, "storage.backups.exists", () => EnsureDirectory("storage.backups.exists", _runtimeStatus.BackupsRoot));
        AddCheck(checks, "storage.logs.exists", () => EnsureDirectory("storage.logs.exists", _runtimeStatus.LogRoot));
        AddCheck(checks, "storage.config.exists", () => EnsureDirectory("storage.config.exists", _runtimeStatus.ConfigRoot));
        AddCheck(checks, "storage.imports.exists", () => EnsureDirectory("storage.imports.exists", _runtimeStatus.ImportsRoot));
        AddCheck(checks, "storage.migration.exists", () => EnsureDirectory("storage.migration.exists", _runtimeStatus.MigrationRoot));

        AddCheck(checks, "storage.data.write", () => EnsureWriteAccess("storage.data.write", _runtimeStatus.DataDirectory));
        AddCheck(checks, "storage.attachments.write", () => EnsureWriteAccess("storage.attachments.write", _runtimeStatus.AttachmentsRoot));
        AddCheck(checks, "storage.reports.write", () => EnsureWriteAccess("storage.reports.write", _runtimeStatus.ReportsOutputRoot));
        AddCheck(checks, "storage.logs.write", () => EnsureWriteAccess("storage.logs.write", _runtimeStatus.LogRoot));

        AddCheck(checks, "access.database.path", () =>
        {
            var exists = File.Exists(_runtimeStatus.AccessDatabasePath);
            return new DiagnosticsCheckResult(
                "access.database.path",
                exists ? "ok" : "failed",
                exists ? "Access database path exists." : "Access database path was not found.",
                _runtimeStatus.AccessDatabasePath);
        });

        AddCheck(checks, "sqlite.target.path", () =>
        {
            return new DiagnosticsCheckResult(
                "sqlite.target.path",
                "ok",
                "SQLite target path is resolved for future migration phases; runtime is not switched to SQLite yet.",
                _runtimeStatus.SQLiteDatabasePath);
        });

        AddCheck(checks, "access.connection.open", () =>
        {
            try
            {
                using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
                connection.Open();
                return new DiagnosticsCheckResult("access.connection.open", "ok", "Access connection opened successfully.", connection.DataSource ?? _runtimeStatus.AccessDatabasePath);
            }
            catch (Exception ex)
            {
                return new DiagnosticsCheckResult(
                    "access.connection.open",
                    "failed",
                    "Access connection failed. Verify Access Database Engine (ACE/OLEDB) is installed and the DB path is reachable.",
                    ex.Message);
            }
        });

        AddCheck(checks, "access.config.seed", CheckConfigSeed);

        AddCheck(checks, "email.profiles.exist", CheckEmailProfilesExist);
        AddCheck(checks, "email.profiles.active.valid", CheckActiveProfilesValid);

        AddCheck(checks, "outlook.com.available", () =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return new DiagnosticsCheckResult("outlook.com.available", "warning", "Outlook draft creation requires Windows + Outlook desktop.", "Non-Windows environment");
            }

            var outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType is null)
            {
                return new DiagnosticsCheckResult("outlook.com.available", "warning", "Outlook COM type is unavailable.", "ProgID Outlook.Application was not found");
            }

            return new DiagnosticsCheckResult("outlook.com.available", "ok", "Outlook COM type is available.", outlookType.FullName ?? "Outlook.Application");
        });

        AddCheck(checks, "runtime.boundary", () =>
        {
            if (!OperatingSystem.IsWindows())
            {
                return new DiagnosticsCheckResult(
                    "runtime.boundary",
                    "warning",
                    "This app targets Windows workstation runtime (WebView2 + ACE/OLEDB + Outlook desktop draft flow).",
                    "Current environment is non-Windows.");
            }

            return new DiagnosticsCheckResult(
                "runtime.boundary",
                "ok",
                "Windows runtime detected. Continue with diagnostics and workstation checklist verification.",
                "WebView2 runtime, ACE provider, and Outlook desktop still require local validation.");
        });

        var overall = ComputeOverallStatus(checks);
        return new DiagnosticsPayload(overall, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), checks);
    }

    private DiagnosticsCheckResult CheckConfigValuesLoaded()
    {
        var missing = new List<string>();
        AddMissingIfBlank(_config.DataRoot, "dataRoot", missing);
        AddMissingIfBlank(_config.AccessDatabasePath, "accessDatabasePath", missing);
        AddMissingIfBlank(_config.AttachmentsRoot, "attachmentsRoot", missing);
        AddMissingIfBlank(_config.ReportsOutputRoot, "reportsOutputRoot", missing);
        AddMissingIfBlank(_config.BackupsRoot, "backupsRoot", missing);
        AddMissingIfBlank(_config.LogRoot, "logRoot", missing);
        AddMissingIfBlank(_config.ConfigRoot, "configRoot", missing);
        AddMissingIfBlank(_config.ImportsRoot, "importsRoot", missing);
        AddMissingIfBlank(_config.MigrationRoot, "migrationRoot", missing);

        if (missing.Count > 0)
        {
            return new DiagnosticsCheckResult("config.values.loaded", "failed", "Required runtime config values are missing.", string.Join(", ", missing));
        }

        return new DiagnosticsCheckResult(
            "config.values.loaded",
            "ok",
            "Runtime config values are loaded.",
            $"config={_runtimeStatus.ConfigPath} | dataRoot={_runtimeStatus.DataRoot} | accessDb={_runtimeStatus.AccessDatabasePath} | sqliteTarget={_runtimeStatus.SQLiteDatabasePath}");
    }

    private DiagnosticsCheckResult CheckDataRootPolicy()
    {
        var valid = AppPathService.IsConfiguredPrimaryDataRoot(_runtimeStatus.DataRoot);
        return new DiagnosticsCheckResult(
            "storage.data_root.policy",
            valid ? "ok" : "failed",
            valid
                ? "Primary live data root matches the approved M: storage policy."
                : "Primary live data root does not match the approved M: storage policy.",
            $"actual={_runtimeStatus.DataRoot} | expected={AppPathDefaults.PrimaryDataRoot}");
    }

    private DiagnosticsCheckResult CheckConfigSeed()
    {
        using var connection = new OleDbConnection(AccessBootstrapper.BuildConnectionString(_runtimeStatus.AccessDatabasePath));
        connection.Open();

        var keys = new[] { "accessDatabasePath", "attachmentsRoot", "reportsOutputRoot" };
        var missing = new List<string>();
        var legacyValues = new List<string>();

        foreach (var key in keys)
        {
            using var cmd = new OleDbCommand("SELECT TOP 1 ConfigValue FROM tblConfig WHERE ConfigKey = ?", connection);
            cmd.Parameters.AddWithValue("@p1", key);
            var value = cmd.ExecuteScalar();
            var textValue = value is null || value == DBNull.Value ? string.Empty : Convert.ToString(value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(textValue))
            {
                missing.Add(key);
                continue;
            }

            if (textValue.Contains("C:/MOAT-Handover", StringComparison.OrdinalIgnoreCase)
                || textValue.Contains("C:\\MOAT-Handover", StringComparison.OrdinalIgnoreCase))
            {
                legacyValues.Add(key + "=" + textValue);
            }
        }

        if (missing.Count > 0)
        {
            return new DiagnosticsCheckResult("access.config.seed", "warning", "One or more tblConfig values are missing/blank.", string.Join(", ", missing));
        }

        if (legacyValues.Count > 0)
        {
            return new DiagnosticsCheckResult(
                "access.config.seed",
                "warning",
                "tblConfig still contains legacy C:/MOAT-Handover seed values. Runtime path resolution now uses HostConfig/AppPathService; config seed cleanup is a later migration item.",
                string.Join(" | ", legacyValues));
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

    private static void AddMissingIfBlank(string? value, string keyName, List<string> missing)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(keyName);
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
