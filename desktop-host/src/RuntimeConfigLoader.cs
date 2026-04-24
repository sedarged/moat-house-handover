using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MoatHouseHandover.Host;

public sealed class RuntimeConfigLoader
{
    private const string ConfigEnvironmentVariable = "MOAT_HANDOVER_CONFIG";

    public RuntimeConfigResult Load()
    {
        var candidates = GetConfigCandidates();
        var errors = new List<string>();

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(candidate);
                var config = JsonSerializer.Deserialize<HostConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config is null)
                {
                    errors.Add($"Config file '{candidate}' was empty or invalid JSON.");
                    continue;
                }

                Validate(config, candidate);
                return new RuntimeConfigResult(candidate, config, candidates);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed reading '{candidate}': {ex.Message}");
            }
        }

        throw new InvalidOperationException(
            "Unable to load runtime config. Checked paths:\n"
            + string.Join("\n", candidates)
            + (errors.Count == 0 ? string.Empty : "\nErrors:\n" + string.Join("\n", errors)));
    }

    public IReadOnlyList<string> GetConfigCandidates()
    {
        var paths = new List<string>();

        var envPath = Environment.GetEnvironmentVariable(ConfigEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            paths.Add(Path.GetFullPath(envPath));
        }

        paths.Add(Path.Combine(AppContext.BaseDirectory, "config", "runtime.config.json"));

        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (!string.IsNullOrWhiteSpace(commonAppData))
        {
            paths.Add(Path.Combine(commonAppData, "MoatHouseHandover", "config", "runtime.config.json"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            paths.Add(Path.Combine(localAppData, "MoatHouseHandover", "config", "runtime.config.json"));
        }

        // Development fallback for repository execution.
        paths.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "config", "runtime.config.json")));

        return paths;
    }

    private static void Validate(HostConfig config, string sourcePath)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(config.AccessDatabasePath))
        {
            missing.Add("accessDatabasePath");
        }

        if (string.IsNullOrWhiteSpace(config.AttachmentsRoot))
        {
            missing.Add("attachmentsRoot");
        }

        if (string.IsNullOrWhiteSpace(config.ReportsOutputRoot))
        {
            missing.Add("reportsOutputRoot");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Config '{sourcePath}' is missing required values: {string.Join(", ", missing)}");
        }
    }
}

public sealed record RuntimeConfigResult(string SourcePath, HostConfig Config, IReadOnlyList<string> CheckedPaths);
