using Fintable.Features.Sync;

namespace Fintable.Tests.Features.Sync;

public class SyncDateRangeCalculatorTests
{
    [Fact]
    public void GetYearRanges_DefaultOptions_ReturnsCorrectCount()
    {
        // Arrange
        var options = new SyncWindowOptions();
        var reference = new DateTime(2025, 6, 15);

        // Act
        var ranges = SyncDateRangeCalculator.GetYearRanges(options, reference);

        // Assert
        var expectedCount = options.YearsBack + options.YearsForward + 1;
        Assert.Equal(expectedCount, ranges.Count);
    }

    [Fact]
    public void GetYearRanges_WithReferenceDate_ReturnsCorrectYears()
    {
        // Arrange
        var options = new SyncWindowOptions { YearsBack = 2, YearsForward = 1 };
        var reference = new DateTime(2025, 3, 1);

        // Act
        var ranges = SyncDateRangeCalculator.GetYearRanges(options, reference);

        // Assert
        Assert.Equal(4, ranges.Count);
        Assert.Equal(2023, ranges[0].Start.Year);
        Assert.Equal(2024, ranges[1].Start.Year);
        Assert.Equal(2025, ranges[2].Start.Year);
        Assert.Equal(2026, ranges[3].Start.Year);
    }

    [Fact]
    public void GetYearRanges_EachRange_StartsJan1AndEndsDec31()
    {
        // Arrange
        var options = new SyncWindowOptions { YearsBack = 0, YearsForward = 2 };
        var reference = new DateTime(2025, 7, 1);

        // Act
        var ranges = SyncDateRangeCalculator.GetYearRanges(options, reference);

        // Assert
        foreach (var (start, end) in ranges)
        {
            Assert.Equal(1, start.Month);
            Assert.Equal(1, start.Day);
            Assert.Equal(0, start.Hour);
            Assert.Equal(0, start.Minute);
            Assert.Equal(0, start.Second);

            Assert.Equal(12, end.Month);
            Assert.Equal(31, end.Day);
            Assert.Equal(23, end.Hour);
            Assert.Equal(59, end.Minute);
            Assert.Equal(59, end.Second);
            Assert.Equal(999, end.Millisecond);
        }
    }

    [Fact]
    public void GetYearRanges_NullReferenceDate_UsesCurrentYear()
    {
        // Arrange
        var options = new SyncWindowOptions { YearsBack = 0, YearsForward = 0 };

        // Act
        var ranges = SyncDateRangeCalculator.GetYearRanges(options);

        // Assert
        Assert.Single(ranges);
        Assert.Equal(DateTime.UtcNow.Year, ranges[0].Start.Year);
    }

    [Fact]
    public void GetYearRanges_ZeroWindow_ReturnsSingleYear()
    {
        // Arrange
        var options = new SyncWindowOptions { YearsBack = 0, YearsForward = 0 };
        var reference = new DateTime(2025, 1, 1);

        // Act
        var ranges = SyncDateRangeCalculator.GetYearRanges(options, reference);

        // Assert
        Assert.Single(ranges);
        Assert.Equal(2025, ranges[0].Start.Year);
        Assert.Equal(2025, ranges[0].End.Year);
    }
}
