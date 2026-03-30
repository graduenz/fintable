using System.Net;
using Fintable.Models;
using Fintable.Persistence;

namespace Fintable.Tests.Features.Reports;

public class ReportsControllerTests : BaseControllerTests
{
    [Fact]
    public async Task FintablePdf_NoData_ReturnsOkWithPdfContentType()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync("/v1/reports/fintable/pdf?year=2026", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        Assert.True(bytes.Length > 0);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task FintablePdf_WithData_ReturnsOkWithPdf()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = "Test Provider",
        };
        Db.Providers.Add(provider);

        var account = new Account
        {
            Id = Id.New(),
            Name = "Checking",
            ProviderId = provider.Id,
            ExternalId = "1001",
        };
        Db.Accounts.Add(account);

        var category = new Category
        {
            Id = Id.New(),
            Name = "Salary",
            Kind = CategoryKind.Income,
            ProviderId = provider.Id,
            ExternalId = "2001",
        };
        Db.Categories.Add(category);

        Db.Transactions.Add(new Transaction
        {
            Id = Id.New(),
            Description = "Monthly Salary",
            Date = new DateTime(2026, 1, 15),
            Paid = true,
            Value = 500000,
            TotalInstallments = 1,
            Installment = 1,
            Recurring = false,
            AccountId = account.Id,
            AccountType = TransactionAccountType.Account,
            CategoryId = category.Id,
            ExternalId = "3001",
        });

        await Db.SaveChangesAsync(cancellationToken);

        // Act
        var response = await Client.GetAsync("/v1/reports/fintable/pdf?year=2026", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("fintable-2026.pdf", response.Content.Headers.ContentDisposition?.FileName);

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task FintablePdf_DefaultYear_ReturnsOk()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync("/v1/reports/fintable/pdf", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Fintable_NoData_ReturnsOkWithEmptyRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.GetAsync("/v1/reports/fintable?year=2026", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
