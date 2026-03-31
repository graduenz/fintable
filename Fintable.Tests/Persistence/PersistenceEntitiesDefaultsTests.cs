using Fintable.Models;
using Fintable.Persistence;

namespace Fintable.Tests.Persistence;

public class PersistenceEntitiesDefaultsTests
{
    [Fact]
    public void Category_DefaultCollections_AreInitialized()
    {
        // Arrange
        var category = new Category
        {
            Id = Id.New(),
            Name = "Food",
            Kind = CategoryKind.Unknown,
            ProviderId = Id.New(),
            ExternalId = "external-1",
        };

        // Assert
        Assert.NotNull(category.Children);
        Assert.NotNull(category.Transactions);
        Assert.Empty(category.Children);
        Assert.Empty(category.Transactions);
    }

    [Fact]
    public void CreditCard_DefaultInvoicesCollection_IsInitialized()
    {
        // Arrange
        var card = new CreditCard
        {
            Id = Id.New(),
            Name = "Nubank",
            ProviderId = Id.New(),
            ExternalId = "external-1",
        };

        // Assert
        Assert.NotNull(card.Invoices);
        Assert.Empty(card.Invoices);
    }

    [Fact]
    public void Invoice_DefaultTransactionsCollection_IsInitialized()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Id.New(),
            Date = DateTime.UtcNow,
            Value = 1000,
            Paid = false,
            CreditCardId = Id.New(),
            ExternalId = "external-1",
        };

        // Assert
        Assert.NotNull(invoice.Transactions);
        Assert.Empty(invoice.Transactions);
    }

    [Fact]
    public void Transaction_AllowsOptionalCategoryAndInvoiceIds()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Id.New(),
            Description = "Lunch",
            Date = DateTime.UtcNow,
            Paid = true,
            Value = -2500,
            TotalInstallments = 1,
            Installment = 1,
            Recurring = false,
            AccountId = Id.New(),
            AccountType = TransactionAccountType.Account,
            CategoryId = null,
            InvoiceId = null,
            ExternalId = "external-1",
        };

        // Assert
        Assert.Null(transaction.CategoryId);
        Assert.Null(transaction.InvoiceId);
    }
}
