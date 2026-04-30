using System;
using System.Collections.Generic;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed class AppPathService
{
    public const string ApprovedDataRoot = @"M:\Moat House\MoatHouse Handover\";

    public AppPathResolution ResolveAndValidate(HostConfig config)
    {
        var configuredRoot = string.IsNullOrWhiteSpace(config.DataRoot) ? ApprovedDataRoot : config.DataRoot;
        var dataRoot = EnsureTrailingSeparator(Path.GetFullPath(configuredRoot));
        var approvedRoot = EnsureTrailingSeparator(Path.GetFullPath(ApprovedDataRoot));

        var paths = new AppPaths(
            DataRoot: dataRoot,
            Data: Path.Combine(dataRoot, "Data"),
            Attachments: Path.Combine(dataRoot, "Attachments"),
            Reports: Path.Combine(dataRoot, "Reports"),
            Backups: Path.Combine(dataRoot, "Backups"),
            Logs: Path.Combine(dataRoot, "Logs"),
            Config: Path.Combine(dataRoot, "Config"),
            Imports: Path.Combine(dataRoot, "Imports"),
            Migration: Path.Combine(dataRoot, "Migration"));

        var results = new List<AppPathValidationResult>
        {
            ValidateApprovedDataRoot(paths.DataRoot, approvedRoot)
        };

        if (results[0].IsOk)
        {
            results.Add(ValidateRequiredDirectory("data", paths.Data));
            results.Add(ValidateRequiredDirectory("attachments", paths.Attachments));
            results.Add(ValidateRequiredDirectory("reports", paths.Reports));
            results.Add(ValidateRequiredDirectory("backups", paths.Backups));
            results.Add(ValidateRequiredDirectory("logs", paths.Logs));
            results.Add(ValidateRequiredDirectory("config", paths.Config));
            results.Add(ValidateRequiredDirectory("imports", paths.Imports));
            results.Add(ValidateRequiredDirectory("migration", paths.Migration));
        }

        return new AppPathResolution(paths, results);
    }

    private static AppPathValidationResult ValidateApprovedDataRoot(string actualRoot, string approvedRoot)
    {
        var matches = string.Equals(actualRoot, approvedRoot, StringComparison.OrdinalIgnoreCase);
        if (matches)
        {
            return new AppPathValidationResult("dataRoot.approved", actualRoot, "ok", "Data root matches approved live root.");
        }

        return new AppPathValidationResult(
            "dataRoot.approved",
            actualRoot,
            "failed",
            $"Configured data root is not approved. Expected '{approvedRoot}' but received '{actualRoot}'.");
    }

    private static AppPathValidationResult ValidateRequiredDirectory(string key, string fullPath)
    {
        try
        {
            Directory.CreateDirectory(fullPath);
            var probePath = Path.Combine(fullPath, ".write_probe_" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(probePath, "probe");
            File.Delete(probePath);
            return new AppPathValidationResult(key, fullPath, "ok", "Directory exists and write access is confirmed.");
        }
        catch (Exception ex)
        {
            return new AppPathValidationResult(key, fullPath, "failed", $"Directory validation failed: {ex.Message}");
        }
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
}

public sealed record AppPaths(
    string DataRoot,
    string Data,
    string Attachments,
    string Reports,
    string Backups,
    string Logs,
    string Config,
    string Imports,
    string Migration);

public sealed record AppPathValidationResult(string Key, string FullPath, string Status, string Message)
{
    public bool IsOk => string.Equals(Status, "ok", StringComparison.OrdinalIgnoreCase);
}

public sealed record AppPathResolution(AppPaths Paths, IReadOnlyList<AppPathValidationResult> ValidationResults)
{
    public bool AllValid
    {
        get
        {
            foreach (var result in ValidationResults)
            {
                if (!result.IsOk)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
