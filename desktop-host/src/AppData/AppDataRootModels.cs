using System;
using System.Collections.Generic;

namespace MoatHouseHandover.Host.AppData;

public enum AppDataOwnershipStatus { Ready = 1, ReadyWithWarnings = 2, FirstRunInitialized = 3, Blocked = 4 }

public sealed record AppDataRoot(
    string DataRoot,
    string DataFolder,
    string AttachmentsFolder,
    string ReportsFolder,
    string BackupsFolder,
    string MigrationFolder,
    string DualRunFolder,
    string LogsFolder,
    string ConfigFolder,
    string SqliteDatabasePath,
    string AccessLegacyDatabasePath);

public sealed record AppDataRootIssue(string Code, string Message, bool IsBlocking);

public sealed record AppDataRootStatus(
    AppDataRoot Root,
    bool IsFirstRun,
    IReadOnlyList<string> CreatedFolders,
    IReadOnlyList<string> ExistingFolders,
    IReadOnlyList<AppDataRootIssue> BlockingIssues,
    IReadOnlyList<AppDataRootIssue> Warnings,
    AppDataOwnershipStatus OwnershipStatus,
    bool SqliteBootstrapSucceeded,
    string? SqliteBootstrapMessage);
