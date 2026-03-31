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
}

public class SyncExecutionResultDto
{
    public List<SyncedProviderDto> SyncedProviders { get; set; } = [];
    public List<SyncWarningGroupDto> WarningGroups { get; set; } = [];
}

public sealed class SyncWarningCollector : IDisposable
{
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

    public IReadOnlyList<SyncWarningGroupDto> WarningGroups => _issues
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
            _logger.LogCritical("{CriticalCount} critical issue(s) detected during {Scope}.", criticalCount, _scope);
        }

        if (warningCount > 0)
        {
            _logger.LogWarning("{WarningCount} warning issue(s) detected during {Scope}.", warningCount, _scope);
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
