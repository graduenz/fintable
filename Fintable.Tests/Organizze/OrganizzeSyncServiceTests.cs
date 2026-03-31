using Bogus;
using Fintable.Organizze;
using Fintable.Persistence;
using OrganizzeCategory = NOrganizze.Categories.Category;
using OrganizzeTransaction = NOrganizze.Transactions.Transaction;

namespace Fintable.Tests.Organizze;

public class OrganizzeSyncServiceTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void ResolveLocalInvoiceId_RemoteTransactionHasPaidCreditCardInvoiceId_ReturnsLocalInvoiceId()
    {
        // Arrange
        var externalInvoiceId = _faker.Random.Int(1000, 9999).ToString();
        var localInvoiceId = Id.New();
        var invoicesMap = new Dictionary<string, string>
        {
            [externalInvoiceId] = localInvoiceId,
        };

        var remoteTransaction = new OrganizzeTransaction
        {
            PaidCreditCardInvoiceId = long.Parse(externalInvoiceId),
        };

        // Act
        var resolvedInvoiceId = OrganizzeSyncService.ResolveLocalInvoiceId(remoteTransaction, invoicesMap);

        // Assert
        Assert.Equal(localInvoiceId, resolvedInvoiceId);
    }

    [Fact]
    public void ResolveLocalInvoiceId_RemoteTransactionHasCreditCardInvoiceId_ReturnsLocalInvoiceId()
    {
        // Arrange
        var externalInvoiceId = _faker.Random.Int(1000, 9999).ToString();
        var localInvoiceId = Id.New();
        var invoicesMap = new Dictionary<string, string>
        {
            [externalInvoiceId] = localInvoiceId,
        };

        var remoteTransaction = new OrganizzeTransaction
        {
            CreditCardInvoiceId = long.Parse(externalInvoiceId),
        };

        // Act
        var resolvedInvoiceId = OrganizzeSyncService.ResolveLocalInvoiceId(remoteTransaction, invoicesMap);

        // Assert
        Assert.Equal(localInvoiceId, resolvedInvoiceId);
    }

    [Fact]
    public void ResolveLocalInvoiceId_RemoteTransactionHasNoInvoiceLink_ReturnsNull()
    {
        // Arrange
        var invoicesMap = new Dictionary<string, string>
        {
            [_faker.Random.Int(1000, 9999).ToString()] = Id.New(),
        };
        var remoteTransaction = new OrganizzeTransaction();

        // Act
        var resolvedInvoiceId = OrganizzeSyncService.ResolveLocalInvoiceId(remoteTransaction, invoicesMap);

        // Assert
        Assert.Null(resolvedInvoiceId);
    }

    [Fact]
    public void ResolveLocalInvoiceId_RemoteTransactionHasUnmappedInvoiceId_ReturnsNull()
    {
        // Arrange
        var mappedExternalInvoiceId = _faker.Random.Int(1000, 9999).ToString();
        var unmappedExternalInvoiceId = _faker.Random.Int(10000, 19999).ToString();
        var invoicesMap = new Dictionary<string, string>
        {
            [mappedExternalInvoiceId] = Id.New(),
        };
        var remoteTransaction = new OrganizzeTransaction
        {
            PaidCreditCardInvoiceId = long.Parse(unmappedExternalInvoiceId),
        };

        // Act
        var resolvedInvoiceId = OrganizzeSyncService.ResolveLocalInvoiceId(remoteTransaction, invoicesMap);

        // Assert
        Assert.Null(resolvedInvoiceId);
    }

    [Fact]
    public void TryGetExternalInvoiceId_RemoteTransactionHasInvoiceLink_ReturnsTrueAndExternalInvoiceId()
    {
        // Arrange
        var externalInvoiceId = _faker.Random.Long(1000, 9999);
        var remoteTransaction = new OrganizzeTransaction
        {
            PaidCreditCardInvoiceId = externalInvoiceId,
        };

        // Act
        var found = OrganizzeSyncService.TryGetExternalInvoiceId(remoteTransaction, out var resolvedExternalInvoiceId);

        // Assert
        Assert.True(found);
        Assert.Equal(externalInvoiceId, resolvedExternalInvoiceId);
    }

    [Fact]
    public void TryGetExternalInvoiceId_RemoteTransactionHasNoInvoiceLink_ReturnsFalse()
    {
        // Arrange
        var remoteTransaction = new OrganizzeTransaction();

        // Act
        var found = OrganizzeSyncService.TryGetExternalInvoiceId(remoteTransaction, out var resolvedExternalInvoiceId);

        // Assert
        Assert.False(found);
        Assert.Equal(default, resolvedExternalInvoiceId);
    }

    [Fact]
    public void GetLatestTransactionDate_EmptyTransactions_ReturnsNull()
    {
        // Arrange
        var transactions = Array.Empty<OrganizzeTransaction>();

        // Act
        var latestDate = OrganizzeSyncService.GetLatestTransactionDate(transactions);

        // Assert
        Assert.Null(latestDate);
    }

    [Fact]
    public void GetLatestTransactionDate_MultipleTransactions_ReturnsLatestDate()
    {
        // Arrange
        var earliestDate = new DateTime(2026, 01, 02);
        var latestDate = new DateTime(2026, 03, 10);
        var transactions = new[]
        {
            new OrganizzeTransaction { Id = 1, Date = earliestDate },
            new OrganizzeTransaction { Id = 2, Date = latestDate },
            new OrganizzeTransaction { Id = 3, Date = new DateTime(2026, 02, 01) },
        };

        // Act
        var result = OrganizzeSyncService.GetLatestTransactionDate(transactions);

        // Assert
        Assert.Equal(latestDate, result);
    }

    [Fact]
    public void DeduplicateTransactionsByExternalId_DuplicateIds_ReturnsDistinctTransactions()
    {
        // Arrange
        var transactions = new[]
        {
            new OrganizzeTransaction { Id = 10, Description = "first" },
            new OrganizzeTransaction { Id = 11, Description = "single" },
            new OrganizzeTransaction { Id = 10, Description = "second" },
        };

        // Act
        var deduped = OrganizzeSyncService.DeduplicateTransactionsByExternalId(transactions);

        // Assert
        Assert.Equal(2, deduped.Count);
        Assert.Contains(deduped, transaction => transaction.Id == 10 && transaction.Description == "second");
        Assert.Contains(deduped, transaction => transaction.Id == 11);
    }

    [Fact]
    public void TryGetNextCursorStart_LatestDateAdvancesCursor_ReturnsTrue()
    {
        // Arrange
        var currentStart = new DateTime(2026, 01, 01);
        var latestDate = new DateTime(2026, 01, 01);

        // Act
        var canAdvance = OrganizzeSyncService.TryGetNextCursorStart(currentStart, latestDate, out var nextStart);

        // Assert
        Assert.True(canAdvance);
        Assert.Equal(new DateTime(2026, 01, 02), nextStart);
    }

    [Fact]
    public void TryGetNextCursorStart_LatestDateDoesNotAdvanceCursor_ReturnsFalse()
    {
        // Arrange
        var currentStart = new DateTime(2026, 01, 02);
        var latestDate = new DateTime(2026, 01, 01);

        // Act
        var canAdvance = OrganizzeSyncService.TryGetNextCursorStart(currentStart, latestDate, out var nextStart);

        // Assert
        Assert.False(canAdvance);
        Assert.Equal(default, nextStart);
    }

    [Fact]
    public void TryGetNextCursorStart_LatestDateIsNull_ReturnsFalse()
    {
        // Arrange
        var currentStart = new DateTime(2026, 01, 02);

        // Act
        var canAdvance = OrganizzeSyncService.TryGetNextCursorStart(currentStart, latestDate: null, out var nextStart);

        // Assert
        Assert.False(canAdvance);
        Assert.Equal(default, nextStart);
    }

    [Fact]
    public void TryGetExternalInvoiceId_OnlyCreditCardInvoiceIdIsZero_ReturnsFalse()
    {
        // Arrange
        var remoteTransaction = new OrganizzeTransaction
        {
            CreditCardInvoiceId = 0,
        };

        // Act
        var found = OrganizzeSyncService.TryGetExternalInvoiceId(remoteTransaction, out var resolvedExternalInvoiceId);

        // Assert
        Assert.False(found);
        Assert.Equal(default, resolvedExternalInvoiceId);
    }

    [Fact]
    public void TryGetExternalInvoiceId_PaidCreditCardInvoiceIdHasPriorityOverCreditCardInvoiceId()
    {
        // Arrange
        var remoteTransaction = new OrganizzeTransaction
        {
            PaidCreditCardInvoiceId = 1001,
            CreditCardInvoiceId = 2002,
        };

        // Act
        var found = OrganizzeSyncService.TryGetExternalInvoiceId(remoteTransaction, out var resolvedExternalInvoiceId);

        // Assert
        Assert.True(found);
        Assert.Equal(1001, resolvedExternalInvoiceId);
    }

    [Fact]
    public void AssignCategoryParentsFromRemote_ChildReferencesParent_SetsParentIdToLocalParent()
    {
        // Arrange
        var providerId = Id.New();
        var parent = new Category
        {
            Id = Id.New(),
            Name = _faker.Commerce.Department(),
            ProviderId = providerId,
            ExternalId = "10",
        };
        var child = new Category
        {
            Id = Id.New(),
            Name = _faker.Commerce.Categories(1)[0],
            ProviderId = providerId,
            ExternalId = "20",
        };
        var byExternalId = new Dictionary<string, Category>
        {
            [parent.ExternalId] = parent,
            [child.ExternalId] = child,
        };
        var remoteCategories = new[]
        {
            new OrganizzeCategory { Id = 10, Name = parent.Name, ParentId = null },
            new OrganizzeCategory { Id = 20, Name = child.Name, ParentId = 10 },
        };

        // Act
        OrganizzeSyncService.AssignCategoryParentsFromRemote(remoteCategories, byExternalId);

        // Assert
        Assert.Equal(parent.Id, child.ParentId);
        Assert.Null(parent.ParentId);
    }

    [Fact]
    public void AssignCategoryParentsFromRemote_ParentMissingInMap_ClearsParentId()
    {
        // Arrange
        var providerId = Id.New();
        var child = new Category
        {
            Id = Id.New(),
            Name = _faker.Commerce.Categories(1)[0],
            ProviderId = providerId,
            ExternalId = "20",
            ParentId = Id.New(),
        };
        var byExternalId = new Dictionary<string, Category>
        {
            [child.ExternalId] = child,
        };
        var remoteCategories = new[]
        {
            new OrganizzeCategory { Id = 20, Name = child.Name, ParentId = 99 },
        };

        // Act
        OrganizzeSyncService.AssignCategoryParentsFromRemote(remoteCategories, byExternalId);

        // Assert
        Assert.Null(child.ParentId);
    }
}
