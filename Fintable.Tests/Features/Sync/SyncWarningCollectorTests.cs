using Fintable.Features.Sync;
using Microsoft.Extensions.Logging;

namespace Fintable.Tests.Features.Sync;

public class SyncWarningCollectorTests
{
    [Fact]
    public void Dispose_CalledTwice_LogsSummaryOnlyOnce()
    {
        // Arrange
        var logger = new CountingLogger();
        var collector = new SyncWarningCollector(logger, "test-scope");
        collector.ReportWarning(SyncWarningCodes.NoProvidersToSync, "No providers.");
        collector.ReportCritical(SyncWarningCodes.SyncDataConsistencyRisk, "Consistency risk.");

        // Act
        collector.Dispose();
        collector.Dispose();

        // Assert
        Assert.Equal(1, logger.WarningCount);
        Assert.Equal(1, logger.CriticalCount);
    }

    private sealed class CountingLogger : ILogger
    {
        public int WarningCount { get; private set; }
        public int CriticalCount { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningCount++;
            }

            if (logLevel == LogLevel.Critical)
            {
                CriticalCount++;
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
