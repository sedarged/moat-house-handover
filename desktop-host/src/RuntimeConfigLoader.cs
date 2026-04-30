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
            + (errors.Count == 0 ? string.Empty : "\nErrors:\n" + string.Join("\n", errors))
            + "\nSet MOAT_HANDOVER_CONFIG or place runtime.config.json in the packaged config folder.");
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

        AddMissingIfBlank(config.DataRoot, "dataRoot", missing);
        AddMissingIfBlank(config.AccessDatabasePath, "accessDatabasePath", missing);
        AddMissingIfBlank(config.AttachmentsRoot, "attachmentsRoot", missing);
        AddMissingIfBlank(config.ReportsOutputRoot, "reportsOutputRoot", missing);
        AddMissingIfBlank(config.BackupsRoot, "backupsRoot", missing);
        AddMissingIfBlank(config.LogRoot, "logRoot", missing);
        AddMissingIfBlank(config.ConfigRoot, "configRoot", missing);
        AddMissingIfBlank(config.ImportsRoot, "importsRoot", missing);
        AddMissingIfBlank(config.MigrationRoot, "migrationRoot", missing);

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Config '{sourcePath}' is missing required values: {string.Join(", ", missing)}");
        }

        // Phase 2 guardrail: config must resolve to the approved M:\ live root and child paths.
        _ = new AppPathService().Resolve(config);
    }

    private static void AddMissingIfBlank(string? value, string keyName, List<string> missing)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(keyName);
        }
    }
}

public sealed record RuntimeConfigResult(string SourcePath, HostConfig Config, IReadOnlyList<string> CheckedPaths);
