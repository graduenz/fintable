using Bogus;
using Fintable.Features.Reports;
using Fintable.Models;
using Fintable.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fintable.Tests.Features.Reports;

public class ReportsServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FintableDb _db;

    public ReportsServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FintableDb>()
            .UseSqlite(_connection)
            .Options;

        _db = new FintableDb(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetStatsReportAsync_WithSeededData_ReturnsCorrectCounts()
    {
        // Arrange
        var faker = new Faker();

        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);

        var account = new Account
        {
            Id = Id.New(),
            Name = faker.Finance.AccountName(),
            ProviderId = provider.Id,
            ExternalId = faker.Random.Number(1000, 9999).ToString(),
        };
        _db.Accounts.Add(account);

        var category = new Category
        {
            Id = Id.New(),
            Name = faker.Commerce.Categories(1)[0],
            ProviderId = provider.Id,
            ExternalId = faker.Random.Number(1000, 9999).ToString(),
        };
        _db.Categories.Add(category);

        var creditCard = new CreditCard
        {
            Id = Id.New(),
            Name = faker.Finance.CreditCardNumber(),
            ProviderId = provider.Id,
            ExternalId = faker.Random.Number(1000, 9999).ToString(),
        };
        _db.CreditCards.Add(creditCard);

        var invoice = new Invoice
        {
            Id = Id.New(),
            Date = faker.Date.Past(),
            Value = faker.Random.Int(100, 10000),
            Paid = true,
            CreditCardId = creditCard.Id,
            ExternalId = faker.Random.Number(1000, 9999).ToString(),
        };
        _db.Invoices.Add(invoice);

        var transaction = new Transaction
        {
            Id = Id.New(),
            Description = faker.Commerce.ProductName(),
            Date = faker.Date.Past(),
            Paid = true,
            Value = faker.Random.Int(100, 10000),
            TotalInstallments = 1,
            Installment = 1,
            Recurring = false,
            AccountId = account.Id,
            AccountType = TransactionAccountType.Account,
            CategoryId = category.Id,
            ExternalId = faker.Random.Number(1000, 9999).ToString(),
        };
        _db.Transactions.Add(transaction);

        await _db.SaveChangesAsync();

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetStatsReportAsync();

        // Assert
        Assert.Equal(1, report.TotalProviders);
        Assert.Equal(1, report.TotalAccounts);
        Assert.Equal(1, report.TotalCategories);
        Assert.Equal(1, report.TotalCreditCards);
        Assert.Equal(1, report.TotalInvoices);
        Assert.Equal(1, report.TotalTransactions);

        Assert.NotNull(report.Providers);
        Assert.Single(report.Providers);

        var providerStats = report.Providers[provider.Name];
        Assert.Equal(provider.Name, providerStats.Name);
        Assert.Equal(1, providerStats.Accounts);
        Assert.Equal(1, providerStats.Categories);
        Assert.Equal(1, providerStats.CreditCards);
        Assert.NotNull(providerStats.Invoices);
        Assert.Equal(1, providerStats.Invoices.Count);
        Assert.NotNull(providerStats.Invoices.FirstDate);
        Assert.NotNull(providerStats.Invoices.LastDate);
        Assert.NotNull(providerStats.Transactions);
        Assert.Equal(1, providerStats.Transactions.Count);
        Assert.NotNull(providerStats.Transactions.FirstDate);
        Assert.NotNull(providerStats.Transactions.LastDate);
    }

    [Fact]
    public async Task GetStatsReportAsync_WithProviderButNoData_ReturnsNullDates()
    {
        // Arrange
        var faker = new Faker();
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync();

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetStatsReportAsync();

        // Assert
        Assert.NotNull(report.Providers);
        var providerStats = report.Providers[provider.Name];
        Assert.NotNull(providerStats.Invoices);
        Assert.Equal(0, providerStats.Invoices.Count);
        Assert.Null(providerStats.Invoices.FirstDate);
        Assert.Null(providerStats.Invoices.LastDate);
        Assert.NotNull(providerStats.Transactions);
        Assert.Equal(0, providerStats.Transactions.Count);
        Assert.Null(providerStats.Transactions.FirstDate);
        Assert.Null(providerStats.Transactions.LastDate);
    }

    [Fact]
    public async Task GetStatsReportAsync_EmptyDb_ReturnsZeroCounts()
    {
        // Arrange
        var service = new ReportsService(_db);

        // Act
        var report = await service.GetStatsReportAsync();

        // Assert
        Assert.Equal(0, report.TotalProviders);
        Assert.Equal(0, report.TotalAccounts);
        Assert.Equal(0, report.TotalCategories);
        Assert.Equal(0, report.TotalCreditCards);
        Assert.Equal(0, report.TotalInvoices);
        Assert.Equal(0, report.TotalTransactions);
        Assert.NotNull(report.Providers);
        Assert.Empty(report.Providers);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
