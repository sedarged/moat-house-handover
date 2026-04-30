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
            ValidateDirectory("dataRoot", paths.DataRoot),
            ValidateDirectory("data", paths.Data),
            ValidateDirectory("attachments", paths.Attachments),
            ValidateDirectory("reports", paths.Reports),
            ValidateDirectory("backups", paths.Backups),
            ValidateDirectory("logs", paths.Logs),
            ValidateDirectory("config", paths.Config),
            ValidateDirectory("imports", paths.Imports),
            ValidateDirectory("migration", paths.Migration)
        };

        return new AppPathResolution(paths, results);
    }

    private static AppPathValidationResult ValidateDirectory(string key, string fullPath)
    {
        try
        {
            Directory.CreateDirectory(fullPath);
            return new AppPathValidationResult(key, fullPath, "ok", "Directory exists or was created.");
        }
        catch (Exception ex)
        {
            return new AppPathValidationResult(key, fullPath, "failed", ex.Message);
        }
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
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

public sealed record AppPathValidationResult(string Key, string FullPath, string Status, string Message);

public sealed record AppPathResolution(AppPaths Paths, IReadOnlyList<AppPathValidationResult> ValidationResults)
{
    public bool AllValid
    {
        get
        {
            foreach (var result in ValidationResults)
            {
                if (!string.Equals(result.Status, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
