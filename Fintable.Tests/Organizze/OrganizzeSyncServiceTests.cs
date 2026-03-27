using Bogus;
using Fintable.Organizze;
using Fintable.Persistence;
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
}
