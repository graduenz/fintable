using Fintable.Features.Sync;

namespace Fintable.Tests.Features.Sync;

public class SyncExecutionResultDtosTests
{
    [Fact]
    public void ToString_SyncWarningGroupDto_IncludesCodeSeverityAndCount()
    {
        // Arrange
        var dto = new SyncWarningGroupDto
        {
            Code = "warning_code",
            Severity = SyncWarningSeverity.Warning,
            Count = 2,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[warning_code] [Severity: Warning] [Count: 2]", text);
    }

    [Fact]
    public void ToString_SyncedProviderDto_IncludesOutcomeTypeNameAndId()
    {
        // Arrange
        var dto = new SyncedProviderDto
        {
            Id = "provider-1",
            Name = "Main",
            Type = "organizze",
            Outcome = SyncProviderOutcome.Synced,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[Synced] [organizze] [Main] [Id: provider-1]", text);
    }

    [Fact]
    public void ToString_SyncExecutionResultDto_UsesCollectionCounts()
    {
        // Arrange
        var dto = new SyncExecutionResultDto
        {
            SyncedProviders =
            [
                new()
                {
                    Id = "provider-1",
                    Name = "Main",
                    Type = "organizze",
                    Outcome = SyncProviderOutcome.Synced,
                },
            ],
            WarningGroups =
            [
                new()
                {
                    Code = "warning_code",
                    Severity = SyncWarningSeverity.Warning,
                    Count = 1,
                },
            ],
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[SyncedProviders: 1] [WarningGroups: 1]", text);
    }
}
