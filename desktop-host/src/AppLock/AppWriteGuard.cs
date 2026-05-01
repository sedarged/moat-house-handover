using MoatHouseHandover.Host.AppData;

namespace MoatHouseHandover.Host.AppLock;

public sealed class AppWriteGuard
{
    private readonly AppLockService _lockService;

    public AppWriteGuard(AppDataRoot root)
    {
        _lockService = new AppLockService(root);
    }

    public AppWriteGuardResult EnsureWriteAllowed(DatabaseProviderKind provider)
    {
        if (provider != DatabaseProviderKind.SQLite)
        {
            var accessState = _lockService.CheckStatus(lockRequired: false);
            return new AppWriteGuardResult(true, "AccessLegacy mode: SQLite write lock not required.", accessState);
        }

        var state = _lockService.CheckStatus(lockRequired: true);
        if (state.Status == AppLockStatus.HeldByCurrentProcess || state.Status == AppLockStatus.Acquired)
        {
            return new AppWriteGuardResult(true, "SQLite write lock is held by current process.", state);
        }

        var message = state.Status == AppLockStatus.HeldByOtherProcess
            ? state.Message
            : "SQLite write lock is not held. Another user may be using the app or the app is in read-only mode.";

        return new AppWriteGuardResult(false, message, state);
    }
}
