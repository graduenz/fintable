namespace Fintable.Features.Reports;

public class FintableReportDto
{
    public int Year { get; set; }
    public List<FintableReportRowDto> Rows { get; set; } = [];
}

public class FintableReportRowDto
{
    public required string Category { get; set; }
    public required string Kind { get; set; }
    public List<FintableReportCellDto> Months { get; set; } = [];
}

public class FintableReportCellDto
{
    public int Month { get; set; }
    public int Value { get; set; }
    public bool? Paid { get; set; }
}
