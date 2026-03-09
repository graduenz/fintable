using Fintable.Persistence;
using Fintable.Providers.Organizze;
using Microsoft.EntityFrameworkCore;

namespace Fintable.Features.Sync;

public class OrganizzeSyncService
{
    private readonly FintableDb _db;
    private readonly OrganizzeFetchClient _client;

    public OrganizzeSyncService(FintableDb db, OrganizzeFetchClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task SyncAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        // Order is important for relationship resolution.
        var accountsMap = await SyncAccountsAsync(provider, cancellationToken);
        var categoriesMap = await SyncCategoriesAsync(provider, cancellationToken);
        var creditCardsMap = await SyncCreditCardsAsync(provider, cancellationToken);
        var invoicesMap = await SyncInvoicesAsync(provider, creditCardsMap, cancellationToken);
        await SyncTransactionsAsync(provider, accountsMap, categoriesMap, cancellationToken);
    }

    private async Task<Dictionary<string, string>> SyncAccountsAsync(Provider provider, CancellationToken cancellationToken)
    {
        var dtos = await _client.GetAccountsAsync();

        var existing = await _db.Accounts
            .Where(a => a.ProviderId == provider.Id)
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(a => a.ExternalId, a => a);

        foreach (var dto in dtos)
        {
            if (byExternalId.TryGetValue(dto.ExternalId, out var account))
            {
                account.Name = dto.Name;
                continue;
            }

            var newAccount = new Account
            {
                Id = Id.New(),
                ProviderId = provider.Id,
                ExternalId = dto.ExternalId,
                Name = dto.Name,
            };

            _db.Accounts.Add(newAccount);
            byExternalId[dto.ExternalId] = newAccount;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncCategoriesAsync(Provider provider, CancellationToken cancellationToken)
    {
        var dtos = await _client.GetCategoriesAsync();

        var existing = await _db.Categories
            .Where(c => c.ProviderId == provider.Id)
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(c => c.ExternalId, c => c);

        foreach (var dto in dtos)
        {
            if (byExternalId.TryGetValue(dto.ExternalId, out var category))
            {
                category.Name = dto.Name;
                continue;
            }

            var newCategory = new Category
            {
                Id = Id.New(),
                ProviderId = provider.Id,
                ExternalId = dto.ExternalId,
                Name = dto.Name,
            };

            _db.Categories.Add(newCategory);
            byExternalId[dto.ExternalId] = newCategory;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncCreditCardsAsync(Provider provider, CancellationToken cancellationToken)
    {
        var dtos = await _client.GetCreditCardsAsync();

        var existing = await _db.CreditCards
            .Where(c => c.ProviderId == provider.Id)
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(c => c.ExternalId, c => c);

        foreach (var dto in dtos)
        {
            if (byExternalId.TryGetValue(dto.ExternalId, out var card))
            {
                card.Name = dto.Name;
                continue;
            }

            var newCard = new CreditCard
            {
                Id = Id.New(),
                ProviderId = provider.Id,
                ExternalId = dto.ExternalId,
                Name = dto.Name,
            };

            _db.CreditCards.Add(newCard);
            byExternalId[dto.ExternalId] = newCard;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncInvoicesAsync(
        Provider provider,
        Dictionary<string, string> creditCardsMap,
        CancellationToken cancellationToken)
    {
        var allInvoices = new List<OrganizzeInvoiceDto>();

        // Fetch invoices per credit card to mirror existing API.
        foreach (var creditCardExternalId in creditCardsMap.Keys)
        {
            var invoices = await _client.GetInvoicesAsync(creditCardExternalId);
            allInvoices.AddRange(invoices);
        }

        var existing = await _db.Invoices
            .Where(i => creditCardsMap.Values.Contains(i.CreditCardId))
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(i => i.ExternalId, i => i);

        foreach (var dto in allInvoices)
        {
            if (!creditCardsMap.TryGetValue(dto.CreditCardExternalId, out var localCreditCardId))
            {
                continue;
            }

            if (byExternalId.TryGetValue(dto.ExternalId, out var invoice))
            {
                invoice.Date = dto.Date;
                invoice.Value = dto.AmountCents;
                invoice.Paid = dto.Paid;
                continue;
            }

            var newInvoice = new Invoice
            {
                Id = Id.New(),
                CreditCardId = localCreditCardId,
                ExternalId = dto.ExternalId,
                Date = dto.Date,
                Value = dto.AmountCents,
                Paid = dto.Paid,
            };

            _db.Invoices.Add(newInvoice);
            byExternalId[dto.ExternalId] = newInvoice;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task SyncTransactionsAsync(
        Provider provider,
        Dictionary<string, string> accountsMap,
        Dictionary<string, string> categoriesMap,
        CancellationToken cancellationToken)
    {
        var dtos = await _client.GetTransactionsAsync();

        var existing = await _db.Transactions
            .Where(t => t.Account.Accounts.Any(a => a.ProviderId == provider.Id))
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(t => t.ExternalId, t => t);

        foreach (var dto in dtos)
        {
            if (!accountsMap.TryGetValue(dto.AccountExternalId, out var localAccountId))
            {
                // If we do not know the account, skip this transaction for now.
                continue;
            }

            categoriesMap.TryGetValue(dto.CategoryExternalId ?? string.Empty, out var localCategoryId);

            if (byExternalId.TryGetValue(dto.ExternalId, out var transaction))
            {
                transaction.Description = dto.Description;
                transaction.Date = dto.Date;
                transaction.Paid = dto.Paid;
                transaction.Value = dto.AmountCents;
                transaction.TotalInstallments = dto.TotalInstallments;
                transaction.Installment = dto.Installment;
                transaction.Recurring = dto.Recurring;
                transaction.AccountId = localAccountId;
                transaction.AccountType = Fintable.Models.TransactionAccountType.Account;
                if (localCategoryId is not null)
                {
                    transaction.CategoryId = localCategoryId;
                }

                continue;
            }

            var newTransaction = new Transaction
            {
                Id = Id.New(),
                ExternalId = dto.ExternalId,
                Description = dto.Description,
                Date = dto.Date,
                Paid = dto.Paid,
                Value = dto.AmountCents,
                TotalInstallments = dto.TotalInstallments,
                Installment = dto.Installment,
                Recurring = dto.Recurring,
                AccountId = localAccountId,
                AccountType = Fintable.Models.TransactionAccountType.Account,
                CategoryId = localCategoryId ?? string.Empty,
            };

            _db.Transactions.Add(newTransaction);
            byExternalId[dto.ExternalId] = newTransaction;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

