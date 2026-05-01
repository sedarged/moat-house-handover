using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MoatHouseHandover.Host.AppData;

namespace MoatHouseHandover.Host.AppLock;

public sealed class AppLockService
{
    public static readonly TimeSpan DefaultStaleThreshold = TimeSpan.FromMinutes(15);

    private readonly string _dataRoot;
    private readonly string _sqlitePath;
    private readonly string _lockFilePath;
    private readonly string _appVersion;

    public AppLockService(AppDataRoot root, string? appVersion = null)
    {
        _dataRoot = root.DataRoot;
        _sqlitePath = root.SqliteDatabasePath;
        _lockFilePath = Path.Combine(root.DataFolder, "moat-house.write.lock");
        _appVersion = string.IsNullOrWhiteSpace(appVersion) ? "unknown" : appVersion;
    }

    public string GetLockFilePath() => _lockFilePath;

    public AppLockState CheckStatus(TimeSpan? staleThreshold = null, bool lockRequired = true)
    {
        if (!lockRequired)
        {
            return new AppLockState(_lockFilePath, AppLockStatus.NotRequired, true, true, false, "Write lock not required while AccessLegacy provider is active.", null, File.Exists(_lockFilePath), null, false, null);
        }

        if (!File.Exists(_lockFilePath))
        {
            return new AppLockState(_lockFilePath, AppLockStatus.Available, true, false, false, "SQLite write lock is available but not acquired.", null, false, null, false, null);
        }

        var payload = ReadLockFile();
        if (payload is null)
        {
            return new AppLockState(_lockFilePath, AppLockStatus.Broken, true, false, true, "Write lock file is unreadable or invalid.", null, true, null, false, new AppLockIssue(AppLockIssueSeverity.Error, "app_lock.file.invalid", "Lock file cannot be parsed."));
        }

        var owner = ToOwner(payload);
        var age = DateTime.UtcNow - owner.HeartbeatAtUtc;
        var stale = age > (staleThreshold ?? DefaultStaleThreshold);
        var currentPid = Environment.ProcessId;
        var currentMachine = Environment.MachineName;

        if (stale)
        {
            return new AppLockState(_lockFilePath, AppLockStatus.Stale, true, false, true, "SQLite write lock appears stale and requires admin/user action.", owner, true, age, true, new AppLockIssue(AppLockIssueSeverity.Warning, "app_lock.stale", "Heartbeat exceeded stale threshold."));
        }

        if (owner.ProcessId == currentPid && string.Equals(owner.MachineName, currentMachine, StringComparison.OrdinalIgnoreCase))
        {
            return new AppLockState(_lockFilePath, AppLockStatus.HeldByCurrentProcess, true, true, false, "SQLite write lock is held by current process.", owner, true, age, false, null);
        }

        return new AppLockState(_lockFilePath, AppLockStatus.HeldByOtherProcess, true, false, true, $"SQLite write lock is held by {owner.UserName}@{owner.MachineName} (pid {owner.ProcessId}).", owner, true, age, false, new AppLockIssue(AppLockIssueSeverity.Warning, "app_lock.held_by_other", "Another process owns the write lock."));
    }

    public AppLockState AcquireWriteLock(TimeSpan? staleThreshold = null)
    {
        var owner = BuildCurrentOwner(AppLockMode.Write);
        var payload = ToPayload(owner);
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        try
        {
            using var stream = new FileStream(_lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();
            return CheckStatus(staleThreshold, lockRequired: true);
        }
        catch (IOException)
        {
            return CheckStatus(staleThreshold, lockRequired: true);
        }
        catch (Exception ex)
        {
            return new AppLockState(_lockFilePath, AppLockStatus.Blocked, true, false, true, $"Unable to acquire write lock: {ex.Message}", null, File.Exists(_lockFilePath), null, false, new AppLockIssue(AppLockIssueSeverity.Error, "app_lock.acquire.failed", ex.Message));
        }
    }

    public AppLockState RefreshHeartbeat(TimeSpan? staleThreshold = null)
    {
        var existing = ReadLockFile();
        if (existing is null) return CheckStatus(staleThreshold, lockRequired: true);
        if (existing.ProcessId != Environment.ProcessId || !string.Equals(existing.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return CheckStatus(staleThreshold, lockRequired: true);

        existing.HeartbeatAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        WriteLockFile(existing);
        return CheckStatus(staleThreshold, lockRequired: true);
    }

    public AppLockState ReleaseWriteLock(TimeSpan? staleThreshold = null)
    {
        var existing = ReadLockFile();
        if (existing is null) return CheckStatus(staleThreshold, lockRequired: true);
        if (existing.ProcessId != Environment.ProcessId || !string.Equals(existing.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return CheckStatus(staleThreshold, lockRequired: true);

        File.Delete(_lockFilePath);
        return CheckStatus(staleThreshold, lockRequired: true);
    }

    private AppLockOwner BuildCurrentOwner(AppLockMode mode)
    {
        var now = DateTime.UtcNow;
        return new AppLockOwner(Environment.MachineName, Environment.UserName, Environment.ProcessId, _appVersion, _dataRoot, _sqlitePath, now, now, mode);
    }

    private LockFilePayload? ReadLockFile()
    {
        try
        {
            var json = File.ReadAllText(_lockFilePath);
            return JsonSerializer.Deserialize<LockFilePayload>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void WriteLockFile(LockFilePayload payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(_lockFilePath, json);
    }

    private static AppLockOwner ToOwner(LockFilePayload payload)
    {
        return new AppLockOwner(
            payload.MachineName ?? string.Empty,
            payload.UserName ?? string.Empty,
            payload.ProcessId,
            payload.AppVersion ?? "unknown",
            payload.DataRoot ?? string.Empty,
            payload.SqlitePath ?? string.Empty,
            ParseUtc(payload.CreatedAtUtc),
            ParseUtc(payload.HeartbeatAtUtc),
            Enum.TryParse<AppLockMode>(payload.Mode, true, out var mode) ? mode : AppLockMode.Write);
    }

    private static LockFilePayload ToPayload(AppLockOwner owner) => new()
    {
        MachineName = owner.MachineName,
        UserName = owner.UserName,
        ProcessId = owner.ProcessId,
        AppVersion = owner.AppVersion,
        DataRoot = owner.DataRoot,
        SqlitePath = owner.SqlitePath,
        Mode = owner.Mode.ToString(),
        CreatedAtUtc = owner.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
        HeartbeatAtUtc = owner.HeartbeatAtUtc.ToString("O", CultureInfo.InvariantCulture)
    };

    private static DateTime ParseUtc(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? dt : DateTime.UnixEpoch;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed class LockFilePayload
    {
        public string? MachineName { get; set; }
        public string? UserName { get; set; }
        public int ProcessId { get; set; }
        public string? AppVersion { get; set; }
        public string? DataRoot { get; set; }
        public string? SqlitePath { get; set; }
        public string? Mode { get; set; }
        public string? CreatedAtUtc { get; set; }
        public string? HeartbeatAtUtc { get; set; }
    }
}
