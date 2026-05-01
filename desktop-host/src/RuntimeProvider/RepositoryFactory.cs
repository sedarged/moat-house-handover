using System;
using System.Collections.Generic;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed record AppRepositorySet(
    ISessionRepository SessionRepository,
    IDepartmentRepository DepartmentRepository,
    IAttachmentRepository AttachmentRepository,
    IBudgetRepository BudgetRepository,
    IPreviewRepository PreviewRepository,
    IAuditLogRepository AuditLogRepository,
    IEmailProfileRepository EmailProfileRepository);

public interface IAppRepositoryFactory
{
    AppRepositorySet Create(HostRuntimeStatus runtimeStatus, HostConfig config);
}

public sealed class AccessRepositoryFactory : IAppRepositoryFactory
{
    public AppRepositorySet Create(HostRuntimeStatus runtimeStatus, HostConfig config) => new(
        new SessionRepository(runtimeStatus.AccessDatabasePath),
        new DepartmentRepository(runtimeStatus.AccessDatabasePath),
        new AttachmentRepository(runtimeStatus.AccessDatabasePath),
        new BudgetRepository(runtimeStatus.AccessDatabasePath),
        new PreviewRepository(runtimeStatus.AccessDatabasePath),
        new AuditLogRepository(runtimeStatus.AccessDatabasePath),
        new EmailProfileRepository(runtimeStatus.AccessDatabasePath));
}

public sealed class SqliteRepositoryFactory : IAppRepositoryFactory
{
    public AppRepositorySet Create(HostRuntimeStatus runtimeStatus, HostConfig config)
    {
        var root = config.DataRoot ?? string.Empty;
        return new AppRepositorySet(
            new Sqlite.Repositories.SqliteSessionRepository(runtimeStatus.TargetSqlitePath, root),
            new Sqlite.Repositories.SqliteDepartmentRepository(runtimeStatus.TargetSqlitePath, root),
            new Sqlite.Repositories.SqliteAttachmentRepository(runtimeStatus.TargetSqlitePath, root),
            new Sqlite.Repositories.SqliteBudgetRepository(runtimeStatus.TargetSqlitePath, root),
            new Sqlite.Repositories.SqlitePreviewRepository(runtimeStatus.TargetSqlitePath, root),
            new Sqlite.Repositories.SqliteAuditLogRepository(runtimeStatus.TargetSqlitePath, root),
            new Sqlite.Repositories.SqliteEmailProfileRepository(runtimeStatus.TargetSqlitePath, root));
    }
}

public sealed class RuntimeProviderSelector
{
    public RuntimeProviderSelection Select(HostRuntimeStatus runtimeStatus, HostConfig config, AppPathResolution paths)
    {
        var options = ResolveRequest(config);
        var requested = ParseProvider(options.RequestedProviderRaw);
        var gate = EvaluateGate(requested, runtimeStatus, config, paths, options.DeveloperOverrideEnabled);
        var effective = gate.Status == RuntimeProviderGateStatus.Allowed ? requested : DatabaseProviderKind.AccessLegacy;
        var message = effective == DatabaseProviderKind.AccessLegacy
            ? (gate.FallbackReason is null ? "AccessLegacy is active default provider." : $"AccessLegacy fallback active: {gate.FallbackReason}")
            : "SQLite runtime provider explicitly enabled and gate approved.";
        return new RuntimeProviderSelection(requested, effective, options.Source, gate, message, effective == DatabaseProviderKind.SQLite);
    }

    private static RuntimeProviderOptions ResolveRequest(HostConfig config)
    {
        var env = Environment.GetEnvironmentVariable("MOAT_HOUSE_RUNTIME_PROVIDER");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return new RuntimeProviderOptions(env, RuntimeProviderSource.EnvironmentVariable, IsDevOverride());
        }

        if (!string.IsNullOrWhiteSpace(config.RuntimeProvider))
        {
            return new RuntimeProviderOptions(config.RuntimeProvider, RuntimeProviderSource.Config, IsDevOverride());
        }

        return new RuntimeProviderOptions("AccessLegacy", RuntimeProviderSource.Default, IsDevOverride());
    }

    private static bool IsDevOverride() => string.Equals(Environment.GetEnvironmentVariable("MOAT_HOUSE_RUNTIME_PROVIDER_DEV_OVERRIDE"), "true", StringComparison.OrdinalIgnoreCase);

    private static DatabaseProviderKind ParseProvider(string? raw)
        => string.Equals(raw, "SQLite", StringComparison.OrdinalIgnoreCase) ? DatabaseProviderKind.SQLite : DatabaseProviderKind.AccessLegacy;

    private static RuntimeProviderGateResult EvaluateGate(DatabaseProviderKind requested, HostRuntimeStatus runtimeStatus, HostConfig config, AppPathResolution paths, bool overrideEnabled)
    {
        var issues = new List<RuntimeProviderIssue>();
        var accessAvailable = File.Exists(runtimeStatus.AccessDatabasePath);
        var sqliteExists = File.Exists(runtimeStatus.TargetSqlitePath);
        var schemaReady = runtimeStatus.SqliteBootstrapSucceeded;
        var reposReady = !string.IsNullOrWhiteSpace(config.DataRoot);
        var dualRunDir = Path.Combine(paths.Paths.Migration, "DualRun");
        var latest = Directory.Exists(dualRunDir) ? Array.Find(Directory.GetFiles(dualRunDir, "dualrun_*.json"), _ => true) : null;

        if (requested == DatabaseProviderKind.AccessLegacy)
        {
            return new RuntimeProviderGateResult(RuntimeProviderGateStatus.Allowed, sqliteExists, schemaReady, reposReady, accessAvailable, latest, null, issues);
        }

        if (!sqliteExists) issues.Add(new RuntimeProviderIssue("sqlite.db.missing", RuntimeProviderSeverity.Error, "SQLite DB file is missing."));
        if (!schemaReady) issues.Add(new RuntimeProviderIssue("sqlite.schema.not_ready", RuntimeProviderSeverity.Error, "SQLite schema marker/bootstrap not ready."));
        if (!reposReady) issues.Add(new RuntimeProviderIssue("sqlite.repositories.not_ready", RuntimeProviderSeverity.Error, "SQLite repository prerequisites are not ready."));
        if (!accessAvailable) issues.Add(new RuntimeProviderIssue("access.fallback.unavailable", RuntimeProviderSeverity.Error, "AccessLegacy fallback DB is not available."));
        if (string.IsNullOrWhiteSpace(latest)) issues.Add(new RuntimeProviderIssue("sqlite.dualrun.missing", RuntimeProviderSeverity.Warning, "Latest dual-run report was not found."));

        var hasErrors = issues.Exists(i => i.Severity == RuntimeProviderSeverity.Error);
        var hasDualRunWarning = issues.Exists(i => i.Code == "sqlite.dualrun.missing");
        if (hasErrors)
        {
            return new RuntimeProviderGateResult(RuntimeProviderGateStatus.WarningFallback, sqliteExists, schemaReady, reposReady, accessAvailable, latest, "SQLite request failed gate checks.", issues);
        }

        if (hasDualRunWarning && !overrideEnabled)
        {
            return new RuntimeProviderGateResult(RuntimeProviderGateStatus.WarningFallback, sqliteExists, schemaReady, reposReady, accessAvailable, latest, "Dual-run evidence missing; fallback to AccessLegacy.", issues);
        }

        return new RuntimeProviderGateResult(RuntimeProviderGateStatus.Allowed, sqliteExists, schemaReady, reposReady, accessAvailable, latest, null, issues);
    }
}
