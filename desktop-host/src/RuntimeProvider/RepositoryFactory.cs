using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoatHouseHandover.Host.DualRun;
using MoatHouseHandover.Host.AppData;
using MoatHouseHandover.Host.AppLock;

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
        var root = !string.IsNullOrWhiteSpace(runtimeStatus.ApprovedDataRoot)
            ? runtimeStatus.ApprovedDataRoot
            : config.DataRoot ?? string.Empty;

        var appRoot = AppDataRootInitializer.BuildRoot(root);
        var guard = new AppWriteGuard(appRoot);

        var session = new GuardedSqliteSessionRepository(new Sqlite.Repositories.SqliteSessionRepository(runtimeStatus.TargetSqlitePath, root), guard);
        var department = new GuardedSqliteDepartmentRepository(new Sqlite.Repositories.SqliteDepartmentRepository(runtimeStatus.TargetSqlitePath, root), guard);
        var attachment = new GuardedSqliteAttachmentRepository(new Sqlite.Repositories.SqliteAttachmentRepository(runtimeStatus.TargetSqlitePath, root), guard);
        var budget = new GuardedSqliteBudgetRepository(new Sqlite.Repositories.SqliteBudgetRepository(runtimeStatus.TargetSqlitePath, root), guard);
        var preview = new Sqlite.Repositories.SqlitePreviewRepository(runtimeStatus.TargetSqlitePath, root);
        var audit = new GuardedSqliteAuditLogRepository(new Sqlite.Repositories.SqliteAuditLogRepository(runtimeStatus.TargetSqlitePath, root), guard);
        var email = new Sqlite.Repositories.SqliteEmailProfileRepository(runtimeStatus.TargetSqlitePath, root);

        return new AppRepositorySet(session, department, attachment, budget, preview, audit, email);
    }
}

public sealed class RuntimeProviderSelector
{
    public RuntimeProviderSelection Select(HostRuntimeStatus runtimeStatus, HostConfig config, AppPathResolution paths)
    {
        var options = ResolveRequest(config);
        var parse = ParseProvider(options.RequestedProviderRaw);
        var gate = EvaluateGate(parse.RequestedProvider, runtimeStatus, config, paths, options.DeveloperOverrideEnabled, parse);
        var effective = gate.Status == RuntimeProviderGateStatus.Allowed ? parse.RequestedProvider : DatabaseProviderKind.AccessLegacy;
        var message = effective == DatabaseProviderKind.AccessLegacy
            ? (gate.FallbackReason is null ? "AccessLegacy is active default provider." : $"AccessLegacy fallback active: {gate.FallbackReason}")
            : "SQLite runtime provider explicitly enabled and gate approved.";
        return new RuntimeProviderSelection(parse.RequestedProvider, effective, options.Source, gate, message, effective == DatabaseProviderKind.SQLite);
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

        return new RuntimeProviderOptions(null, RuntimeProviderSource.Default, IsDevOverride());
    }

    private static bool IsDevOverride() => string.Equals(Environment.GetEnvironmentVariable("MOAT_HOUSE_RUNTIME_PROVIDER_DEV_OVERRIDE"), "true", StringComparison.OrdinalIgnoreCase);

    private static ParsedProviderRequest ParseProvider(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ParsedProviderRequest(DatabaseProviderKind.AccessLegacy, true, null);
        }

        if (string.Equals(raw, "AccessLegacy", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedProviderRequest(DatabaseProviderKind.AccessLegacy, true, raw);
        }

        if (string.Equals(raw, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedProviderRequest(DatabaseProviderKind.SQLite, true, raw);
        }

        return new ParsedProviderRequest(DatabaseProviderKind.AccessLegacy, false, raw);
    }

    private static RuntimeProviderGateResult EvaluateGate(DatabaseProviderKind requested, HostRuntimeStatus runtimeStatus, HostConfig config, AppPathResolution paths, bool overrideEnabled, ParsedProviderRequest parse)
    {
        var issues = new List<RuntimeProviderIssue>();
        var accessAvailable = File.Exists(runtimeStatus.AccessDatabasePath);
        var sqliteExists = File.Exists(runtimeStatus.TargetSqlitePath);
        var schemaReady = runtimeStatus.SqliteBootstrapSucceeded;
        var reposReady = !string.IsNullOrWhiteSpace(config.DataRoot);

        var validator = new DualRunEvidenceValidator();
        var dualRun = validator.ValidateLatest(Path.Combine(paths.Paths.Migration, "DualRun"), TimeSpan.FromDays(14));

        if (!parse.IsValid)
        {
            issues.Add(new RuntimeProviderIssue("runtime_provider.invalid_value", RuntimeProviderSeverity.Warning, "Invalid runtimeProvider value; falling back to AccessLegacy.", parse.RawValue));
        }

        if (requested == DatabaseProviderKind.AccessLegacy)
        {
            var fallback = parse.IsValid ? null : "Invalid runtimeProvider value requested.";
            return new RuntimeProviderGateResult(RuntimeProviderGateStatus.Allowed, sqliteExists, schemaReady, reposReady, accessAvailable, dualRun.ReportPath, dualRun.Status == DualRunEvidenceStatus.Accepted, fallback, issues);
        }

        if (!sqliteExists) issues.Add(new RuntimeProviderIssue("sqlite.db.missing", RuntimeProviderSeverity.Error, "SQLite DB file is missing."));
        if (!schemaReady) issues.Add(new RuntimeProviderIssue("sqlite.schema.not_ready", RuntimeProviderSeverity.Error, "SQLite schema marker/bootstrap not ready."));
        if (!reposReady) issues.Add(new RuntimeProviderIssue("sqlite.repositories.not_ready", RuntimeProviderSeverity.Error, "SQLite repository prerequisites are not ready."));
        if (!accessAvailable) issues.Add(new RuntimeProviderIssue("access.fallback.unavailable", RuntimeProviderSeverity.Error, "AccessLegacy fallback DB is not available."));

        if (dualRun.Status == DualRunEvidenceStatus.Missing) issues.Add(new RuntimeProviderIssue("sqlite.dualrun.missing", RuntimeProviderSeverity.Warning, "Latest dual-run report was not found."));
        else if (dualRun.Status == DualRunEvidenceStatus.Unreadable) issues.Add(new RuntimeProviderIssue("sqlite.dualrun.unreadable", RuntimeProviderSeverity.Warning, "Latest dual-run report is unreadable or malformed.", dualRun.Issues.FirstOrDefault()?.Detail));
        else if (dualRun.Status != DualRunEvidenceStatus.Accepted) issues.Add(new RuntimeProviderIssue("sqlite.dualrun.not_accepted", RuntimeProviderSeverity.Warning, "Latest dual-run recommendation is not accepted for runtime switch.", dualRun.Recommendation));

        var hasErrors = issues.Any(i => i.Severity == RuntimeProviderSeverity.Error);
        var hasDualRunWarning = issues.Any(i => i.Code.StartsWith("sqlite.dualrun", StringComparison.Ordinal));
        if (hasErrors)
        {
            return new RuntimeProviderGateResult(RuntimeProviderGateStatus.WarningFallback, sqliteExists, schemaReady, reposReady, accessAvailable, dualRun.ReportPath, dualRun.Status == DualRunEvidenceStatus.Accepted, "SQLite request failed gate checks.", issues);
        }

        if (hasDualRunWarning && !overrideEnabled)
        {
            return new RuntimeProviderGateResult(RuntimeProviderGateStatus.WarningFallback, sqliteExists, schemaReady, reposReady, accessAvailable, dualRun.ReportPath, dualRun.Status == DualRunEvidenceStatus.Accepted, "Dual-run evidence is missing/unreadable/not accepted; fallback to AccessLegacy.", issues);
        }

        return new RuntimeProviderGateResult(RuntimeProviderGateStatus.Allowed, sqliteExists, schemaReady, reposReady, accessAvailable, dualRun.ReportPath, dualRun.Status == DualRunEvidenceStatus.Accepted, null, issues);
    }

    private sealed record ParsedProviderRequest(DatabaseProviderKind RequestedProvider, bool IsValid, string? RawValue);
}
