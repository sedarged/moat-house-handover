using System;

namespace MoatHouseHandover.Host.AppLock;

public enum AppLockStatus { NotRequired = 1, Available = 2, Acquired = 3, HeldByCurrentProcess = 4, HeldByOtherProcess = 5, Stale = 6, Broken = 7, Blocked = 8 }
public enum AppLockMode { ReadOnly = 1, Write = 2, Maintenance = 3 }
public enum AppLockIssueSeverity { Info = 1, Warning = 2, Error = 3 }

public sealed record AppLockIssue(AppLockIssueSeverity Severity, string Code, string Message);

public sealed record AppLockOwner(
    string MachineName,
    string UserName,
    int ProcessId,
    string AppVersion,
    string DataRoot,
    string SqlitePath,
    DateTime CreatedAtUtc,
    DateTime HeartbeatAtUtc,
    AppLockMode Mode);

public sealed record AppLockState(
    string LockFilePath,
    AppLockStatus Status,
    bool CanRead,
    bool CanWrite,
    bool RequiresUserAction,
    string Message,
    AppLockOwner? Owner,
    bool LockFileExists,
    TimeSpan? HeartbeatAge,
    bool IsStale,
    AppLockIssue? Issue);

public sealed record AppWriteGuardResult(bool Allowed, string Message, AppLockState LockState);
