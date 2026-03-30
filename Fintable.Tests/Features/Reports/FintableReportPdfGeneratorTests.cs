using Fintable.Features.Reports;

namespace Fintable.Tests.Features.Reports;

public class FintableReportPdfGeneratorTests
{
    [Fact]
    public void Generate_EmptyReport_ReturnsValidPdf()
    {
        // Arrange
        var report = new FintableReportDto { Year = 2026, Rows = [] };

        // Act
        var pdf = FintableReportPdfGenerator.Generate(report);

        // Assert
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    [Fact]
    public void Generate_WithRows_ReturnsValidPdf()
    {
        // Arrange
        var report = new FintableReportDto
        {
            Year = 2026,
            Rows =
            [
                new FintableReportRowDto
                {
                    Category = "Salary",
                    Kind = "Income",
                    Months = BuildMonths(5000m, true),
                },
                new FintableReportRowDto
                {
                    Category = "Amex",
                    Kind = "CreditCard",
                    Months = BuildMonths(2000m, false),
                },
                new FintableReportRowDto
                {
                    Category = "Groceries",
                    Kind = "Expense",
                    Months = BuildMonths(800m, true),
                },
                new FintableReportRowDto
                {
                    Category = "Misc",
                    Kind = "Unknown",
                    Months = BuildMonths(100m, null),
                },
            ],
        };

        // Act
        var pdf = FintableReportPdfGenerator.Generate(report);

        // Assert
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
    }

    [Fact]
    public void Generate_MixedPaidAndUnpaidCells_ReturnsValidPdf()
    {
        // Arrange
        var months = new List<FintableReportCellDto>();
        for (var m = 1; m <= 12; m++)
        {
            months.Add(new FintableReportCellDto
            {
                Month = m,
                Value = m % 2 == 0 ? 150.50m : 0m,
                Paid = m % 2 == 0 ? true : null,
            });
        }

        var report = new FintableReportDto
        {
            Year = 2026,
            Rows = [new FintableReportRowDto { Category = "Test", Kind = "Expense", Months = months }],
        };

        // Act
        var pdf = FintableReportPdfGenerator.Generate(report);

        // Assert
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
    }

    private static List<FintableReportCellDto> BuildMonths(decimal value, bool? paid)
    {
        var months = new List<FintableReportCellDto>();
        for (var m = 1; m <= 12; m++)
            months.Add(new FintableReportCellDto { Month = m, Value = value, Paid = paid });
        return months;
    }
}
