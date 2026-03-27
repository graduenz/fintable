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
        var cancellationToken = TestContext.Current.CancellationToken;
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

        await _db.SaveChangesAsync(cancellationToken);

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
        var cancellationToken = TestContext.Current.CancellationToken;
        var faker = new Faker();
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(cancellationToken);

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

    #region GetFintableReportAsync

    [Fact]
    public async Task GetFintableReportAsync_EmptyDb_ReturnsEmptyRows()
    {
        // Arrange
        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        Assert.Equal(2026, report.Year);
        Assert.Empty(report.Rows);
    }

    [Fact]
    public async Task GetFintableReportAsync_IncomeCategory_ReturnsRowWithPositiveValue()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var category = SeedCategory(provider, "Salary", CategoryKind.Income);
        SeedTransaction(account, category, new DateTime(2026, 3, 15), value: 500000);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        var row = Assert.Single(report.Rows);
        Assert.Equal("Salary", row.Category);
        Assert.Equal("Income", row.Kind);
        Assert.Equal(12, row.Months.Count);
        var march = row.Months.Single(m => m.Month == 3);
        Assert.Equal(500000, march.Value);
        Assert.True(march.Paid);
    }

    [Fact]
    public async Task GetFintableReportAsync_ExpenseCategory_ReturnsRowWithPositiveAbsoluteValue()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var category = SeedCategory(provider, "Groceries", CategoryKind.Expense);
        SeedTransaction(account, category, new DateTime(2026, 1, 10), value: -15000);
        SeedTransaction(account, category, new DateTime(2026, 1, 20), value: -8000);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        var row = Assert.Single(report.Rows);
        Assert.Equal("Groceries", row.Category);
        Assert.Equal("Expense", row.Kind);
        var january = row.Months.Single(m => m.Month == 1);
        Assert.Equal(23000, january.Value);
    }

    [Fact]
    public async Task GetFintableReportAsync_ParentChildCategory_ReturnsFlattenedName()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var parentCategory = SeedCategory(provider, "Car", CategoryKind.Expense);
        var childCategory = SeedCategory(provider, "Maintenance", CategoryKind.Expense, parentCategory);
        SeedTransaction(account, parentCategory, new DateTime(2026, 2, 5), value: -5000);
        SeedTransaction(account, childCategory, new DateTime(2026, 2, 10), value: -3000);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        Assert.Equal(2, report.Rows.Count);
        Assert.Contains(report.Rows, r => r.Category == "Car");
        Assert.Contains(report.Rows, r => r.Category == "Car - Maintenance");
    }

    [Fact]
    public async Task GetFintableReportAsync_CreditCardInvoice_ReturnsCreditCardRow()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var creditCard = SeedCreditCard(provider, "Amex");
        SeedInvoice(creditCard, new DateTime(2026, 1, 15), value: 200000, paid: false);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        var row = Assert.Single(report.Rows);
        Assert.Equal("Amex", row.Category);
        Assert.Equal("CreditCard", row.Kind);
        var january = row.Months.Single(m => m.Month == 1);
        Assert.Equal(200000, january.Value);
        Assert.False(january.Paid);
    }

    [Fact]
    public async Task GetFintableReportAsync_CreditCardTransaction_ExcludedFromCategoryRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var creditCard = SeedCreditCard(provider, "Visa");
        var category = SeedCategory(provider, "Shopping", CategoryKind.Expense);
        var invoice = SeedInvoice(creditCard, new DateTime(2026, 5, 10), value: 10000, paid: true);

        var transaction = new Transaction
        {
            Id = Id.New(),
            Description = "Online Purchase",
            Date = new DateTime(2026, 5, 3),
            Paid = true,
            Value = -10000,
            TotalInstallments = 1,
            Installment = 1,
            Recurring = false,
            AccountId = creditCard.Id,
            AccountType = TransactionAccountType.CreditCard,
            CategoryId = category.Id,
            InvoiceId = invoice.Id,
            ExternalId = new Faker().Random.Number(1000, 9999).ToString(),
        };
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        Assert.DoesNotContain(report.Rows, r => r.Category == "Shopping");
        var creditCardRow = Assert.Single(report.Rows);
        Assert.Equal("Visa", creditCardRow.Category);
        Assert.Equal("CreditCard", creditCardRow.Kind);
    }

    [Fact]
    public async Task GetFintableReportAsync_InvoicePaymentTransaction_ExcludedFromCategoryRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var creditCard = SeedCreditCard(provider, "Nubank");
        var category = SeedCategory(provider, "Credit Card Payment", CategoryKind.Expense);
        var invoice = SeedInvoice(creditCard, new DateTime(2026, 3, 10), value: 50000, paid: true);

        var transaction = new Transaction
        {
            Id = Id.New(),
            Description = "Invoice Payment",
            Date = new DateTime(2026, 3, 10),
            Paid = true,
            Value = -50000,
            TotalInstallments = 1,
            Installment = 1,
            Recurring = false,
            AccountId = account.Id,
            AccountType = TransactionAccountType.Account,
            CategoryId = category.Id,
            InvoiceId = invoice.Id,
            ExternalId = new Faker().Random.Number(1000, 9999).ToString(),
        };
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        Assert.DoesNotContain(report.Rows, r => r.Category == "Credit Card Payment");
        var creditCardRow = Assert.Single(report.Rows);
        Assert.Equal("Nubank", creditCardRow.Category);
    }

    [Fact]
    public async Task GetFintableReportAsync_NullCategoryTransaction_ReturnsUncategorizedRow()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        SeedTransaction(account, null, new DateTime(2026, 6, 15), value: -7500);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        var row = Assert.Single(report.Rows);
        Assert.Equal("Uncategorized", row.Category);
        Assert.Equal("Expense", row.Kind);
        var june = row.Months.Single(m => m.Month == 6);
        Assert.Equal(7500, june.Value);
    }

    [Fact]
    public async Task GetFintableReportAsync_UncategorizedIncomeAndExpense_ReturnsTwoUncategorizedRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        SeedTransaction(account, null, new DateTime(2026, 4, 10), value: 10000);
        SeedTransaction(account, null, new DateTime(2026, 4, 20), value: -5000);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        Assert.Equal(2, report.Rows.Count);
        Assert.Contains(report.Rows, r => r.Category == "Uncategorized" && r.Kind == "Income");
        Assert.Contains(report.Rows, r => r.Category == "Uncategorized" && r.Kind == "Expense");
    }

    [Fact]
    public async Task GetFintableReportAsync_DifferentYear_ExcludesTransactionsAndInvoices()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var category = SeedCategory(provider, "Food", CategoryKind.Expense);
        var creditCard = SeedCreditCard(provider, "Amex");
        SeedTransaction(account, category, new DateTime(2025, 6, 15), value: -3000);
        SeedInvoice(creditCard, new DateTime(2025, 6, 10), value: 10000, paid: true);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        Assert.Empty(report.Rows);
    }

    [Fact]
    public async Task GetFintableReportAsync_MixedPaidStatusInMonth_ReturnsPaidFalse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var category = SeedCategory(provider, "Utilities", CategoryKind.Expense);
        SeedTransaction(account, category, new DateTime(2026, 7, 5), value: -2000, paid: true);
        SeedTransaction(account, category, new DateTime(2026, 7, 20), value: -3000, paid: false);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        var row = Assert.Single(report.Rows);
        var july = row.Months.Single(m => m.Month == 7);
        Assert.False(july.Paid);
    }

    [Fact]
    public async Task GetFintableReportAsync_AllPaidInMonth_ReturnsPaidTrue()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var category = SeedCategory(provider, "Rent", CategoryKind.Expense);
        SeedTransaction(account, category, new DateTime(2026, 8, 1), value: -100000, paid: true);
        SeedTransaction(account, category, new DateTime(2026, 8, 15), value: -5000, paid: true);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        var row = Assert.Single(report.Rows);
        var august = row.Months.Single(m => m.Month == 8);
        Assert.True(august.Paid);
    }

    [Fact]
    public async Task GetFintableReportAsync_MonthWithNoData_ReturnsZeroValueNullPaid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);
        var category = SeedCategory(provider, "Gym", CategoryKind.Expense);
        SeedTransaction(account, category, new DateTime(2026, 1, 5), value: -5000);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        var row = Assert.Single(report.Rows);
        Assert.Equal(12, row.Months.Count);
        var february = row.Months.Single(m => m.Month == 2);
        Assert.Equal(0, february.Value);
        Assert.Null(february.Paid);
    }

    [Fact]
    public async Task GetFintableReportAsync_SortOrder_IncomeThenCreditCardThenExpense()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = SeedProvider();
        var account = SeedAccount(provider);

        var incomeCategory = SeedCategory(provider, "Salary", CategoryKind.Income);
        var expenseCategoryA = SeedCategory(provider, "A-Groceries", CategoryKind.Expense);
        var expenseCategoryB = SeedCategory(provider, "B-Rent", CategoryKind.Expense);
        var creditCard = SeedCreditCard(provider, "Amex");

        SeedTransaction(account, incomeCategory, new DateTime(2026, 1, 1), value: 500000);
        SeedTransaction(account, expenseCategoryA, new DateTime(2026, 1, 5), value: -10000);
        SeedTransaction(account, expenseCategoryB, new DateTime(2026, 1, 10), value: -50000);
        SeedInvoice(creditCard, new DateTime(2026, 1, 15), value: 30000, paid: true);
        await _db.SaveChangesAsync(cancellationToken);

        var service = new ReportsService(_db);

        // Act
        var report = await service.GetFintableReportAsync(2026);

        // Assert
        Assert.Equal(4, report.Rows.Count);
        Assert.Equal("Income", report.Rows[0].Kind);
        Assert.Equal("Salary", report.Rows[0].Category);
        Assert.Equal("CreditCard", report.Rows[1].Kind);
        Assert.Equal("Amex", report.Rows[1].Category);
        Assert.Equal("Expense", report.Rows[2].Kind);
        Assert.Equal("A-Groceries", report.Rows[2].Category);
        Assert.Equal("Expense", report.Rows[3].Kind);
        Assert.Equal("B-Rent", report.Rows[3].Category);
    }

    #endregion

    #region Static helper tests

    [Fact]
    public void GetFlattenedCategoryName_WithParent_ReturnsCombinedName()
    {
        // Arrange
        var parent = new Category
        {
            Id = Id.New(), Name = "Car", ProviderId = "p1", ExternalId = "1",
        };
        var child = new Category
        {
            Id = Id.New(), Name = "Maintenance", ProviderId = "p1", ExternalId = "2",
            ParentId = parent.Id, Parent = parent,
        };

        // Act
        var result = ReportsService.GetFlattenedCategoryName(child);

        // Assert
        Assert.Equal("Car - Maintenance", result);
    }

    [Fact]
    public void GetFlattenedCategoryName_WithoutParent_ReturnsOwnName()
    {
        // Arrange
        var category = new Category
        {
            Id = Id.New(), Name = "Food", ProviderId = "p1", ExternalId = "1",
        };

        // Act
        var result = ReportsService.GetFlattenedCategoryName(category);

        // Assert
        Assert.Equal("Food", result);
    }

    [Theory]
    [InlineData(CategoryKind.Income, "Income")]
    [InlineData(CategoryKind.Expense, "Expense")]
    [InlineData(CategoryKind.Unknown, "Unknown")]
    public void MapCategoryKind_ReturnsExpectedString(CategoryKind kind, string expected)
    {
        // Act
        var result = ReportsService.MapCategoryKind(kind);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FillAllMonths_PartialData_FillsMissingWithZeroAndNullPaid()
    {
        // Arrange
        var cells = new List<FintableReportCellDto>
        {
            new() { Month = 3, Value = 100, Paid = true },
            new() { Month = 7, Value = 200, Paid = false },
        };

        // Act
        var result = ReportsService.FillAllMonths(cells);

        // Assert
        Assert.Equal(12, result.Count);
        Assert.Equal(100, result[2].Value);
        Assert.True(result[2].Paid);
        Assert.Equal(200, result[6].Value);
        Assert.False(result[6].Paid);
        Assert.Equal(0, result[0].Value);
        Assert.Null(result[0].Paid);
    }

    [Fact]
    public void FillAllMonths_EmptyInput_ReturnsTwelveEmptyMonths()
    {
        // Arrange
        var cells = new List<FintableReportCellDto>();

        // Act
        var result = ReportsService.FillAllMonths(cells);

        // Assert
        Assert.Equal(12, result.Count);
        Assert.All(result, c =>
        {
            Assert.Equal(0, c.Value);
            Assert.Null(c.Paid);
        });
    }

    [Fact]
    public void SortRows_MixedKinds_SortsCorrectly()
    {
        // Arrange
        var rows = new List<FintableReportRowDto>
        {
            new() { Category = "Rent", Kind = "Expense", Months = [] },
            new() { Category = "Amex", Kind = "CreditCard", Months = [] },
            new() { Category = "Salary", Kind = "Income", Months = [] },
            new() { Category = "Bonus", Kind = "Income", Months = [] },
            new() { Category = "Food", Kind = "Expense", Months = [] },
            new() { Category = "Misc", Kind = "Unknown", Months = [] },
        };

        // Act
        ReportsService.SortRows(rows);

        // Assert
        Assert.Equal("Bonus", rows[0].Category);
        Assert.Equal("Salary", rows[1].Category);
        Assert.Equal("Amex", rows[2].Category);
        Assert.Equal("Food", rows[3].Category);
        Assert.Equal("Rent", rows[4].Category);
        Assert.Equal("Misc", rows[5].Category);
    }

    [Fact]
    public void BuildCategoryRows_UnknownCategoryId_SkipsRow()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Id.New(), Description = "Test", Date = new DateTime(2026, 1, 1),
                Paid = true, Value = -100, TotalInstallments = 1, Installment = 1,
                Recurring = false, AccountId = "a1", AccountType = TransactionAccountType.Account,
                CategoryId = "nonexistent", ExternalId = "e1",
            },
        };
        var categoriesById = new Dictionary<string, Category>();

        // Act
        var result = ReportsService.BuildCategoryRows(transactions, categoriesById);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BuildUncategorizedRows_NoUncategorized_ReturnsEmpty()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Id.New(), Description = "Test", Date = new DateTime(2026, 1, 1),
                Paid = true, Value = -100, TotalInstallments = 1, Installment = 1,
                Recurring = false, AccountId = "a1", AccountType = TransactionAccountType.Account,
                CategoryId = "c1", ExternalId = "e1",
            },
        };

        // Act
        var result = ReportsService.BuildUncategorizedRows(transactions);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BuildCreditCardRows_UnknownCreditCardId_SkipsRow()
    {
        // Arrange
        var creditCards = new List<CreditCard>();
        var invoices = new List<Invoice>
        {
            new()
            {
                Id = Id.New(), Date = new DateTime(2026, 1, 1), Value = 1000,
                Paid = true, CreditCardId = "nonexistent", ExternalId = "e1",
            },
        };

        // Act
        var result = ReportsService.BuildCreditCardRows(creditCards, invoices);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BuildMonthCells_MultipleTransactionsInSameMonth_SumsValues()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Id.New(), Description = "T1", Date = new DateTime(2026, 5, 1),
                Paid = true, Value = -3000, TotalInstallments = 1, Installment = 1,
                Recurring = false, AccountId = "a1", AccountType = TransactionAccountType.Account,
                ExternalId = "e1",
            },
            new()
            {
                Id = Id.New(), Description = "T2", Date = new DateTime(2026, 5, 15),
                Paid = true, Value = -2000, TotalInstallments = 1, Installment = 1,
                Recurring = false, AccountId = "a1", AccountType = TransactionAccountType.Account,
                ExternalId = "e2",
            },
        };

        // Act
        var result = ReportsService.BuildMonthCells(transactions);

        // Assert
        var may = result.Single(c => c.Month == 5);
        Assert.Equal(5000, may.Value);
        Assert.True(may.Paid);
    }

    #endregion

    #region Seed helpers

    private Provider SeedProvider()
    {
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = new Faker().Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        return provider;
    }

    private Account SeedAccount(Provider provider)
    {
        var account = new Account
        {
            Id = Id.New(),
            Name = new Faker().Finance.AccountName(),
            ProviderId = provider.Id,
            ExternalId = new Faker().Random.Number(1000, 9999).ToString(),
        };
        _db.Accounts.Add(account);
        return account;
    }

    private Category SeedCategory(Provider provider, string name, CategoryKind kind, Category? parent = null)
    {
        var category = new Category
        {
            Id = Id.New(),
            Name = name,
            Kind = kind,
            ProviderId = provider.Id,
            ExternalId = new Faker().Random.Number(1000, 9999).ToString(),
            ParentId = parent?.Id,
        };
        _db.Categories.Add(category);
        return category;
    }

    private CreditCard SeedCreditCard(Provider provider, string name)
    {
        var creditCard = new CreditCard
        {
            Id = Id.New(),
            Name = name,
            ProviderId = provider.Id,
            ExternalId = new Faker().Random.Number(1000, 9999).ToString(),
        };
        _db.CreditCards.Add(creditCard);
        return creditCard;
    }

    private Invoice SeedInvoice(CreditCard creditCard, DateTime date, int value, bool paid)
    {
        var invoice = new Invoice
        {
            Id = Id.New(),
            Date = date,
            Value = value,
            Paid = paid,
            CreditCardId = creditCard.Id,
            ExternalId = new Faker().Random.Number(1000, 9999).ToString(),
        };
        _db.Invoices.Add(invoice);
        return invoice;
    }

    private Transaction SeedTransaction(Account account, Category? category, DateTime date, int value, bool paid = true)
    {
        var transaction = new Transaction
        {
            Id = Id.New(),
            Description = new Faker().Commerce.ProductName(),
            Date = date,
            Paid = paid,
            Value = value,
            TotalInstallments = 1,
            Installment = 1,
            Recurring = false,
            AccountId = account.Id,
            AccountType = TransactionAccountType.Account,
            CategoryId = category?.Id,
            ExternalId = new Faker().Random.Number(1000, 9999).ToString(),
        };
        _db.Transactions.Add(transaction);
        return transaction;
    }

    #endregion

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
