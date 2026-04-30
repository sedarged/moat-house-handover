using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MoatHouseHandover.Host.Backup;

public sealed record RetentionCandidate(string BackupFolder, DateTimeOffset CreatedUtc, BackupKind Kind, bool DeleteRecommended, string Reason);
public sealed class BackupRetentionService
{
    public IReadOnlyList<RetentionCandidate> BuildDryRun(string backupRoot, BackupRetentionPolicy policy)
    {
        if (!Directory.Exists(backupRoot)) return [];
        var list = new List<RetentionCandidate>();
        foreach (var dir in Directory.GetDirectories(backupRoot))
        {
            var name = Path.GetFileName(dir);
            var kind = Enum.GetValues<BackupKind>().FirstOrDefault(k => name.Contains(k.ToString(), StringComparison.OrdinalIgnoreCase));
            var created = Directory.GetCreationTimeUtc(dir);
            var ageDays = (DateTimeOffset.UtcNow - created).TotalDays;
            var recommend = policy.DeleteEnabled && ageDays > policy.KeepDays;
            list.Add(new(dir, created, kind, recommend, recommend ? "older-than-policy" : "keep-all-safety-default"));
        }
        return list.OrderByDescending(x => x.CreatedUtc).ToList();
    }
}
