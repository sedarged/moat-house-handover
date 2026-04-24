using System;
using System.Collections.Generic;
using System.IO;

namespace MoatHouseHandover.Host;

public sealed class BootstrapLogger
{
    private readonly string _logFilePath;
    private readonly List<string> _entries = new();

    public BootstrapLogger(string logRoot)
    {
        Directory.CreateDirectory(logRoot);
        _logFilePath = Path.Combine(logRoot, $"startup-{DateTime.UtcNow:yyyyMMdd}.log");
    }

    public IReadOnlyList<string> Entries => _entries;

    public void Log(string message)
    {
        var entry = $"[{DateTime.UtcNow:O}] {message}";
        _entries.Add(entry);
        File.AppendAllLines(_logFilePath, new[] { entry });
    }
}
