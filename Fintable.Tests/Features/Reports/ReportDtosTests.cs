using Fintable.Features.Reports;

namespace Fintable.Tests.Features.Reports;

public class ReportDtosTests
{
    [Fact]
    public void ToString_FintableReportCellDto_WithNullPaid_UsesNullLiteral()
    {
        // Arrange
        var dto = new FintableReportCellDto
        {
            Month = 2,
            Value = 123.45m,
            Paid = null,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.StartsWith("[Month: 2] [Value: ", text);
        Assert.EndsWith("] [Paid: null]", text);
    }

    [Fact]
    public void ToString_FintableReportCellDto_WithPaidValue_UsesBooleanText()
    {
        // Arrange
        var dto = new FintableReportCellDto
        {
            Month = 3,
            Value = 10m,
            Paid = true,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[Month: 3] [Value: 10] [Paid: True]", text);
    }

    [Fact]
    public void ToString_FintableReportRowDto_SumsMonthValues()
    {
        // Arrange
        var dto = new FintableReportRowDto
        {
            Kind = "expense",
            Category = "Food",
            Months =
            [
                new() { Month = 1, Value = 10.5m },
                new() { Month = 2, Value = 20m },
            ],
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.StartsWith("[expense] [Food] [", text);
        Assert.EndsWith("]", text);
        Assert.Contains("30", text);
    }

    [Fact]
    public void ToString_FintableReportDto_UsesRowsCount()
    {
        // Arrange
        var dto = new FintableReportDto
        {
            Year = 2026,
            Rows = [new() { Kind = "expense", Category = "Food" }],
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[2026] [Rows: 1]", text);
    }

    [Fact]
    public void ToString_StatsReportDto_WithNullProviders_UsesZeroProvidersCount()
    {
        // Arrange
        var dto = new StatsReportDto
        {
            Providers = null,
            TotalProviders = 1,
            TotalAccounts = 2,
            TotalCategories = 3,
            TotalCreditCards = 4,
            TotalInvoices = 5,
            TotalTransactions = 6,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[Providers: 0] [TotalProviders: 1] [Accounts: 2] [Categories: 3] [CreditCards: 4] [Invoices: 5] [Transactions: 6]", text);
    }

    [Fact]
    public void ToString_ProviderStatsReportDto_WithNullNestedDtos_UsesZeroCounts()
    {
        // Arrange
        var dto = new ProviderStatsReportDto
        {
            Name = "Main",
            Accounts = 1,
            Categories = 2,
            CreditCards = 3,
            Invoices = null,
            Transactions = null,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[Main] [Accounts: 1] [Categories: 2] [CreditCards: 3] [Invoices: 0] [Transactions: 0]", text);
    }

    [Fact]
    public void ToString_ProviderStatsReportDto_WithNestedDtos_UsesNestedCounts()
    {
        // Arrange
        var dto = new ProviderStatsReportDto
        {
            Name = "Main",
            Accounts = 1,
            Categories = 2,
            CreditCards = 3,
            Invoices = new InvoiceStatsReportDto { Count = 4 },
            Transactions = new TransactionStatsReportDto { Count = 5 },
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[Main] [Accounts: 1] [Categories: 2] [CreditCards: 3] [Invoices: 4] [Transactions: 5]", text);
    }

    [Fact]
    public void ToString_InvoiceStatsReportDto_IncludesDatesAndCount()
    {
        // Arrange
        var dto = new InvoiceStatsReportDto
        {
            FirstDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            LastDate = new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc),
            Count = 9,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Contains("[Count: 9]", text);
        Assert.Contains("2026-01-01T10:00:00.0000000Z", text);
        Assert.Contains("2026-01-31T10:00:00.0000000Z", text);
    }

    [Fact]
    public void ToString_TransactionStatsReportDto_IncludesDatesAndCount()
    {
        // Arrange
        var dto = new TransactionStatsReportDto
        {
            FirstDate = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc),
            LastDate = new DateTime(2026, 2, 28, 10, 0, 0, DateTimeKind.Utc),
            Count = 7,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Contains("[Count: 7]", text);
        Assert.Contains("2026-02-01T10:00:00.0000000Z", text);
        Assert.Contains("2026-02-28T10:00:00.0000000Z", text);
    }
}
