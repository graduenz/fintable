namespace Fintable.Features.Reports;

public class FintableReportDto
{
    public int Year { get; set; }
    public List<FintableReportRowDto> Rows { get; set; } = [];

    public override string ToString()
    {
        return $"[{Year}] [Rows: {Rows.Count}]";
    }
}

public class FintableReportRowDto
{
    public required string Category { get; set; }
    public required string Kind { get; set; }
    public List<FintableReportCellDto> Months { get; set; } = [];

    public override string ToString()
    {
        var sumOfMonthsValue = Months.Sum(month => month.Value);
        return $"[{Kind}] [{Category}] [{sumOfMonthsValue}]";
    }
}

public class FintableReportCellDto
{
    public int Month { get; set; }
    public decimal Value { get; set; }
    public bool? Paid { get; set; }

    public override string ToString()
    {
        var paid = Paid.HasValue ? Paid.Value.ToString() : "null";
        return $"[Month: {Month}] [Value: {Value}] [Paid: {paid}]";
    }
}
