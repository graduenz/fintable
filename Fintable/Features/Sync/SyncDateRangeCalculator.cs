namespace Fintable.Features.Sync;

internal static class SyncDateRangeCalculator
{
    public static IReadOnlyList<(DateTime Start, DateTime End)> GetYearRanges(SyncWindowOptions options, DateTime? referenceDate = null)
    {
        var reference = referenceDate?.Date ?? DateTime.UtcNow.Date;
        var startYear = reference.Year - options.YearsBack;
        var endYear = reference.Year + options.YearsForward;

        var ranges = new List<(DateTime Start, DateTime End)>();

        for (var year = startYear; year <= endYear; year++)
        {
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
            ranges.Add((start, end));
        }

        return ranges;
    }
}

