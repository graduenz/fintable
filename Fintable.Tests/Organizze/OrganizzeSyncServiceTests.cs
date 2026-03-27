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
