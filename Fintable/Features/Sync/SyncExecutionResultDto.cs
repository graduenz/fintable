using Microsoft.Extensions.Logging;

namespace Fintable.Features.Sync;

public enum SyncWarningSeverity
{
    Warning,
    Critical,
}

public class SyncWarningGroupDto
{
    public required string Code { get; set; }
    public required SyncWarningSeverity Severity { get; set; }
    public int Count { get; set; }
    public List<string> Warnings { get; set; } = [];

    public override string ToString()
    {
        return $"[{Code}] [Severity: {Severity}] [Count: {Count}]";
    }
}

public enum SyncProviderOutcome
{
    Synced,
    Skipped,
}

public class SyncedProviderDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required SyncProviderOutcome Outcome { get; set; }

    public override string ToString()
    {
        return $"[{Outcome}] [{Type}] [{Name}] [Id: {Id}]";
    }
}

public class SyncExecutionResultDto
{
    public List<SyncedProviderDto> SyncedProviders { get; set; } = [];
    public List<SyncWarningGroupDto> WarningGroups { get; set; } = [];

    public override string ToString()
    {
        return $"[SyncedProviders: {SyncedProviders.Count}] [WarningGroups: {WarningGroups.Count}]";
    }
}

public sealed class SyncWarningCollector : IDisposable
{
    private static readonly Action<Microsoft.Extensions.Logging.ILogger, int, string, Exception?> LogCriticalSummary =
        LoggerMessage.Define<int, string>(
            LogLevel.Critical,
            new EventId(1, nameof(LogCriticalSummary)),
            "{CriticalCount} critical issue(s) detected during {Scope}.");

    private static readonly Action<Microsoft.Extensions.Logging.ILogger, int, string, Exception?> LogWarningSummary =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(2, nameof(LogWarningSummary)),
            "{WarningCount} warning issue(s) detected during {Scope}.");

    private sealed class SyncIssue
    {
        public required string Code { get; init; }
        public required SyncWarningSeverity Severity { get; init; }
        public required string Message { get; init; }
    }

    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly string _scope;
    private readonly List<SyncIssue> _issues = [];
    private bool _summaryLogged;
    private bool _disposed;

    public SyncWarningCollector(Microsoft.Extensions.Logging.ILogger logger, string scope)
    {
        _logger = logger;
        _scope = scope;
    }

    public IReadOnlyList<SyncWarningGroupDto> GetWarningGroups()
    {
        return _issues
            .GroupBy(issue => new { issue.Code, issue.Severity })
            .OrderBy(group => group.Key.Code, StringComparer.Ordinal)
            .Select(group => new SyncWarningGroupDto
            {
                Code = group.Key.Code,
                Severity = group.Key.Severity,
                Count = group.Count(),
                Warnings = group.Select(issue => issue.Message).ToList(),
            })
            .ToList();
    }

    public void ReportWarning(string code, string message)
    {
        Report(SyncWarningSeverity.Warning, code, message);
    }

    public void ReportCritical(string code, string message)
    {
        Report(SyncWarningSeverity.Critical, code, message);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        LogSummary();
        _disposed = true;
    }

    private void LogSummary()
    {
        if (_summaryLogged)
        {
            return;
        }

        var criticalCount = _issues.Count(issue => issue.Severity == SyncWarningSeverity.Critical);
        var warningCount = _issues.Count(issue => issue.Severity == SyncWarningSeverity.Warning);

        if (criticalCount > 0)
        {
            LogCriticalSummary(_logger, criticalCount, _scope, null);
        }

        if (warningCount > 0)
        {
            LogWarningSummary(_logger, warningCount, _scope, null);
        }

        _summaryLogged = true;
    }

    private void Report(SyncWarningSeverity severity, string code, string message)
    {
        _issues.Add(new SyncIssue
        {
            Code = code,
            Severity = severity,
            Message = message,
        });
    }
}
