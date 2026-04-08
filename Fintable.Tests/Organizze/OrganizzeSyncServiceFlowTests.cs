using Bogus;
using Fintable.Features.Sync;
using Fintable.Models;
using Fintable.Organizze;
using Fintable.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrganizzeAccount = NOrganizze.Accounts.Account;
using OrganizzeCategory = NOrganizze.Categories.Category;
using OrganizzeCreditCard = NOrganizze.CreditCards.CreditCard;
using OrganizzeInvoice = NOrganizze.Invoices.Invoice;
using OrganizzeTransaction = NOrganizze.Transactions.Transaction;

namespace Fintable.Tests.Organizze;

public sealed class OrganizzeSyncServiceFlowTests : IDisposable
{
    private readonly Faker _faker = new();
    private readonly SqliteConnection _connection;
    private readonly FintableDb _db;

    public OrganizzeSyncServiceFlowTests()
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
    public async Task SyncAsync_WithExistingAndNewEntities_UpdatesAndInsertsExpectedData()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
        };

        var existingAccount = new Account
        {
            Id = Id.New(),
            ProviderId = provider.Id,
            ExternalId = "10",
            Name = "Old account",
        };
        var existingCategory = new Category
        {
            Id = Id.New(),
            ProviderId = provider.Id,
            ExternalId = "20",
            Name = "Old category",
            Kind = CategoryKind.Unknown,
        };
        var existingCard = new CreditCard
        {
            Id = Id.New(),
            ProviderId = provider.Id,
            ExternalId = "30",
            Name = "Old card",
        };
        var existingInvoice = new Invoice
        {
            Id = Id.New(),
            CreditCardId = existingCard.Id,
            ExternalId = "40",
            Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Value = 1000,
            Paid = false,
        };
        var existingTransaction = new Transaction
        {
            Id = Id.New(),
            ExternalId = "50",
            Description = "Old transaction",
            Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Paid = false,
            Value = -1000,
            TotalInstallments = 1,
            Installment = 1,
            Recurring = false,
            AccountId = existingAccount.Id,
            AccountType = TransactionAccountType.Account,
            CategoryId = existingCategory.Id,
            InvoiceId = existingInvoice.Id,
        };

        _db.Providers.Add(provider);
        _db.Accounts.Add(existingAccount);
        _db.Categories.Add(existingCategory);
        _db.CreditCards.Add(existingCard);
        _db.Invoices.Add(existingInvoice);
        _db.Transactions.Add(existingTransaction);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new TestableOrganizzeSyncService(
            _db,
            new SyncWindowOptions { YearsBack = 0, YearsForward = 0 },
            NullLogger<OrganizzeSyncService>.Instance)
        {
            Accounts =
            [
                new OrganizzeAccount { Id = 10, Name = "Updated account" },
                new OrganizzeAccount { Id = 11, Name = "New account" },
            ],
            Categories =
            [
                new OrganizzeCategory { Id = 20, Name = "Updated category", ParentId = null },
                new OrganizzeCategory { Id = 21, Name = "New category", ParentId = 20 },
            ],
            CreditCards =
            [
                new OrganizzeCreditCard { Id = 30, Name = "Updated card" },
            ],
            Invoices =
            [
                new OrganizzeInvoice { Id = 40, CreditCardId = 30, Date = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), AmountCents = 2500, BalanceCents = 0 },
            ],
            TransactionChunks =
            [
                [
                    new OrganizzeTransaction
                    {
                        Id = 50,
                        Description = "Updated transaction",
                        Date = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                        Paid = true,
                        AmountCents = -2500,
                        TotalInstallments = 1,
                        Installment = 1,
                        Recurring = false,
                        AccountId = 10,
                        CategoryId = 20,
                        CreditCardId = 30,
                        CreditCardInvoiceId = 40,
                    },
                ],
            ],
        };

        using var collector = new SyncWarningCollector(NullLogger.Instance, "test");

        // Act
        await sut.SyncAsync(provider, collector, TestContext.Current.CancellationToken);
        _db.ChangeTracker.Clear();

        // Assert
        var account = await _db.Accounts.SingleAsync(a => a.ExternalId == "10", TestContext.Current.CancellationToken);
        Assert.Equal("Updated account", account.Name);
        Assert.Equal(2, await _db.Accounts.CountAsync(TestContext.Current.CancellationToken));

        var category = await _db.Categories.SingleAsync(c => c.ExternalId == "20", TestContext.Current.CancellationToken);
        Assert.Equal("Updated category", category.Name);

        var childCategory = await _db.Categories.SingleAsync(c => c.ExternalId == "21", TestContext.Current.CancellationToken);
        Assert.Equal(category.Id, childCategory.ParentId);

        var card = await _db.CreditCards.SingleAsync(c => c.ExternalId == "30", TestContext.Current.CancellationToken);
        Assert.Equal("Updated card", card.Name);

        var invoice = await _db.Invoices.SingleAsync(i => i.ExternalId == "40", TestContext.Current.CancellationToken);
        Assert.Equal(2500, invoice.Value);
        Assert.True(invoice.Paid);

        var transaction = await _db.Transactions.SingleAsync(t => t.ExternalId == "50", TestContext.Current.CancellationToken);
        Assert.Equal("Updated transaction", transaction.Description);
        Assert.True(transaction.Paid);
        Assert.Equal(account.Id, transaction.AccountId);
        Assert.Equal(category.Id, transaction.CategoryId);
        Assert.Equal(invoice.Id, transaction.InvoiceId);
        Assert.Empty(collector.GetWarningGroups());
    }

    [Fact]
    public async Task SyncAsync_AllFetchedTransactionsHaveUnknownAccount_ReportsWarningAndCritical()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new TestableOrganizzeSyncService(
            _db,
            new SyncWindowOptions { YearsBack = 0, YearsForward = 0 },
            NullLogger<OrganizzeSyncService>.Instance)
        {
            Accounts = [],
            Categories = [],
            CreditCards = [],
            Invoices = [],
            TransactionChunks =
            [
                [
                    new OrganizzeTransaction
                    {
                        Id = 90,
                        Description = _faker.Lorem.Sentence(),
                        Date = DateTime.UtcNow,
                        AccountId = 999,
                        AmountCents = -100,
                    },
                ],
            ],
        };

        using var collector = new SyncWarningCollector(NullLogger.Instance, "test");

        // Act
        await sut.SyncAsync(provider, collector, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(collector.GetWarningGroups(), group => group.Code == SyncWarningCodes.TransactionUnknownAccountSkipped);
        Assert.Contains(collector.GetWarningGroups(), group => group.Code == SyncWarningCodes.SyncDataConsistencyRisk);
        Assert.Empty(await _db.Transactions.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SyncAsync_TransactionHasMissingCategoryAndInvoiceMappings_ReportsWarnings()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new TestableOrganizzeSyncService(
            _db,
            new SyncWindowOptions { YearsBack = 0, YearsForward = 0 },
            NullLogger<OrganizzeSyncService>.Instance)
        {
            Accounts = [new OrganizzeAccount { Id = 10, Name = "Account" }],
            Categories = [],
            CreditCards = [],
            Invoices = [],
            TransactionChunks =
            [
                [
                    new OrganizzeTransaction
                    {
                        Id = 91,
                        Description = _faker.Lorem.Sentence(),
                        Date = DateTime.UtcNow,
                        AccountId = 10,
                        CategoryId = 777,
                        CreditCardId = 999,
                        CreditCardInvoiceId = 888,
                        AmountCents = -1000,
                        Paid = true,
                    },
                ],
            ],
        };

        using var collector = new SyncWarningCollector(NullLogger.Instance, "test");

        // Act
        await sut.SyncAsync(provider, collector, TestContext.Current.CancellationToken);
        _db.ChangeTracker.Clear();

        // Assert
        Assert.Contains(collector.GetWarningGroups(), group => group.Code == SyncWarningCodes.CategoryMappingMissing);
        Assert.Contains(collector.GetWarningGroups(), group => group.Code == SyncWarningCodes.InvoiceMappingMissing);

        var transaction = await _db.Transactions.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(transaction.CategoryId);
        Assert.Null(transaction.InvoiceId);
    }

    [Fact]
    public async Task SyncAsync_TransactionAccountMatchesCreditCard_AssignsCreditCardAccountType()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new TestableOrganizzeSyncService(
            _db,
            new SyncWindowOptions { YearsBack = 0, YearsForward = 0 },
            NullLogger<OrganizzeSyncService>.Instance)
        {
            Accounts = [],
            Categories = [],
            CreditCards = [new OrganizzeCreditCard { Id = 30, Name = "Card account" }],
            Invoices = [],
            TransactionChunks =
            [
                [
                    new OrganizzeTransaction
                    {
                        Id = 92,
                        Description = _faker.Lorem.Sentence(),
                        Date = DateTime.UtcNow,
                        AccountId = 30,
                        AmountCents = -2000,
                        Paid = true,
                    },
                ],
            ],
        };

        using var collector = new SyncWarningCollector(NullLogger.Instance, "test");

        // Act
        await sut.SyncAsync(provider, collector, TestContext.Current.CancellationToken);
        _db.ChangeTracker.Clear();

        // Assert
        var creditCard = await _db.CreditCards.SingleAsync(c => c.ExternalId == "30", TestContext.Current.CancellationToken);
        var transaction = await _db.Transactions.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(creditCard.Id, transaction.AccountId);
        Assert.Equal(TransactionAccountType.CreditCard, transaction.AccountType);
        Assert.DoesNotContain(collector.GetWarningGroups(), group => group.Code == SyncWarningCodes.TransactionUnknownAccountSkipped);
    }

    [Fact]
    public async Task FetchTransactionsByDateCursorAsync_WhenCapIsReachedAndCursorStalls_ReportsWarningAndCritical()
    {
        // Arrange
        var start = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var cappedChunk = Enumerable.Range(1, OrganizzeSyncService.TransactionFetchCap)
            .Select(index => new OrganizzeTransaction
            {
                Id = index,
                Date = start,
                Description = _faker.Lorem.Sentence(),
                AccountId = 1,
                AmountCents = -1,
            })
            .ToList();

        var sut = new TestableOrganizzeSyncService(
            _db,
            new SyncWindowOptions(),
            NullLogger<OrganizzeSyncService>.Instance)
        {
            TransactionChunks = [cappedChunk, cappedChunk],
        };

        using var collector = new SyncWarningCollector(NullLogger.Instance, "test");

        // Act
        var transactions = await sut.FetchTransactionsByDateCursorAsync(start, end, collector, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(OrganizzeSyncService.TransactionFetchCap, transactions.Count);
        Assert.Contains(collector.GetWarningGroups(), group => group.Code == SyncWarningCodes.TransactionFetchCapDetected);
        Assert.Contains(collector.GetWarningGroups(), group => group.Code == SyncWarningCodes.TransactionFetchCursorStalled);
        Assert.Equal(2, sut.TransactionOptionsHistory.Count);
        Assert.Equal(start, sut.TransactionOptionsHistory[0].StartDate);
        Assert.Equal(end, sut.TransactionOptionsHistory[0].EndDate);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestableOrganizzeSyncService(
        FintableDb db,
        SyncWindowOptions windowOptions,
        Microsoft.Extensions.Logging.ILogger<OrganizzeSyncService> logger) : OrganizzeSyncService(db, windowOptions, logger)
    {
        public IReadOnlyList<OrganizzeAccount> Accounts { get; set; } = [];
        public IReadOnlyList<OrganizzeCategory> Categories { get; set; } = [];
        public IReadOnlyList<OrganizzeCreditCard> CreditCards { get; set; } = [];
        public IReadOnlyList<OrganizzeInvoice> Invoices { get; set; } = [];
        public IReadOnlyList<IReadOnlyList<OrganizzeTransaction>> TransactionChunks { get; set; } = [];
        public List<NOrganizze.Transactions.TransactionListOptions> TransactionOptionsHistory { get; } = [];

        private int _transactionChunkIndex;

        protected override Task<IReadOnlyList<OrganizzeAccount>> ListAccountsAsync(CancellationToken cancellationToken)
            => Task.FromResult(Accounts);

        protected override Task<IReadOnlyList<OrganizzeCategory>> ListCategoriesAsync(CancellationToken cancellationToken)
            => Task.FromResult(Categories);

        protected override Task<IReadOnlyList<OrganizzeCreditCard>> ListCreditCardsAsync(CancellationToken cancellationToken)
            => Task.FromResult(CreditCards);

        protected override Task<IReadOnlyList<OrganizzeInvoice>> ListInvoicesAsync(
            long creditCardId,
            NOrganizze.Invoices.InvoiceListOptions options,
            CancellationToken cancellationToken)
            => Task.FromResult(Invoices);

        protected override Task<IReadOnlyList<OrganizzeTransaction>> ListTransactionsAsync(
            NOrganizze.Transactions.TransactionListOptions options,
            CancellationToken cancellationToken)
        {
            TransactionOptionsHistory.Add(options);
            if (_transactionChunkIndex >= TransactionChunks.Count)
            {
                return Task.FromResult<IReadOnlyList<OrganizzeTransaction>>([]);
            }

            var chunk = TransactionChunks[_transactionChunkIndex];
            _transactionChunkIndex++;
            return Task.FromResult(chunk);
        }
    }
}
