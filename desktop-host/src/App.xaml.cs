using MoatHouseHandover.Host.DualRun;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace MoatHouseHandover.Host;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (!DualRunCli.TryHandle(e.Args, out var exitCode))
        {
            base.OnStartup(e);
            return;
        }

        Shutdown(exitCode);
    }
}

internal static class DualRunCli
{
    public static bool TryHandle(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (!args.Contains("--dualrun-evidence", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var parsed = Parse(args);
            var result = new DualRunEvidenceRunner().Run(new DualRunEvidenceRunRequest(parsed.ShiftCode, parsed.ShiftDate, parsed.UserName, parsed.Departments, parsed.DataRoot));

            Console.WriteLine($"Dual-run evidence report generated. JSON: {result.JsonReportPath}");
            Console.WriteLine($"Dual-run evidence report generated. TXT: {result.TextReportPath}");
            Console.WriteLine($"Counts: match={result.MatchCount} warning={result.WarningCount} mismatch={result.MismatchCount} skipped={result.SkippedCount} failed={result.FailedCount}");
            Console.WriteLine($"Recommendation: {result.Recommendation}");
            Console.WriteLine($"EvidenceStatus: {result.EvidenceStatus}");
            Console.WriteLine($"NextAction: {result.NextAction}");
            foreach (var issue in result.Issues)
            {
                Console.WriteLine($"Issue: {issue}");
            }

            exitCode = result.Success ? 0 : 1;
            return true;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"dualrun-evidence argument error: {ex.Message}");
            PrintUsage();
            exitCode = 2;
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"dualrun-evidence failed: {ex.Message}");
            exitCode = 2;
            return true;
        }
    }

    private static (string ShiftCode, DateTime ShiftDate, string UserName, IReadOnlyList<string> Departments, string? DataRoot) Parse(string[] args)
    {
        var shiftCode = ReadRequired(args, "--shift-code");
        var shiftDateRaw = ReadRequired(args, "--shift-date");
        if (!DateTime.TryParseExact(shiftDateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shiftDate))
        {
            throw new ArgumentException("--shift-date must be in yyyy-MM-dd format.");
        }

        var departmentsRaw = ReadOptional(args, "--departments");
        var departments = string.IsNullOrWhiteSpace(departmentsRaw)
            ? Array.Empty<string>()
            : departmentsRaw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var userName = ReadOptional(args, "--user-name") ?? "dualrun";
        var dataRoot = ReadOptional(args, "--data-root");
        return (shiftCode, shiftDate, userName, departments, dataRoot);
    }

    private static string ReadRequired(string[] args, string name)
        => ReadOptional(args, name) ?? throw new ArgumentException($"Missing required argument: {name}");

    private static string? ReadOptional(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Length) throw new ArgumentException($"Missing value for argument: {name}");
            return args[i + 1];
        }

        return null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: MoatHouseHandover.Host.exe --dualrun-evidence --shift-code <AM|PM|NS> --shift-date <yyyy-MM-dd> [--departments Injection,MetaPress,Slicing] [--user-name dualrun] [--data-root <path>]");
    }
}
