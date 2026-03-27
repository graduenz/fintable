using Fintable.Features.Sync;
using Fintable.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintable.Organizze;

public class OrganizzeSyncService
{
    private readonly FintableDb _db;
    private readonly NOrganizze.NOrganizzeClient _client;
    private readonly SyncWindowOptions _windowOptions;

    public OrganizzeSyncService(FintableDb db, NOrganizze.NOrganizzeClient client, SyncWindowOptions windowOptions)
    {
        _db = db;
        _client = client;
        _windowOptions = windowOptions;
    }

    public async Task SyncAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        // Order is important for relationship resolution.
        var accountsMap = await SyncAccountsAsync(provider, cancellationToken);
        var categoriesMap = await SyncCategoriesAsync(provider, cancellationToken);
        var creditCardsMap = await SyncCreditCardsAsync(provider, cancellationToken);
        var invoicesMap = await SyncInvoicesAsync(creditCardsMap, cancellationToken);
        await SyncTransactionsAsync(accountsMap, categoriesMap, creditCardsMap, invoicesMap, cancellationToken);
    }

    private async Task<Dictionary<string, string>> SyncAccountsAsync(Provider provider, CancellationToken cancellationToken)
    {
        var remoteAccounts = await _client.Accounts.ListAsync(cancellationToken: cancellationToken);

        var existing = await _db.Accounts
            .Where(a => a.ProviderId == provider.Id)
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(a => a.ExternalId, a => a);

        foreach (var remote in remoteAccounts)
        {
            var externalId = remote.Id.ToString();
            var name = remote.Name;

            if (byExternalId.TryGetValue(externalId, out var account))
            {
                account.Name = name;
                continue;
            }

            var newAccount = new Account
            {
                Id = Id.New(),
                ProviderId = provider.Id,
                ExternalId = externalId,
                Name = name,
            };

            _db.Accounts.Add(newAccount);
            byExternalId[externalId] = newAccount;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncCategoriesAsync(Provider provider, CancellationToken cancellationToken)
    {
        var remoteCategories = await _client.Categories.ListAsync(cancellationToken: cancellationToken);

        var existing = await _db.Categories
            .Where(c => c.ProviderId == provider.Id)
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(c => c.ExternalId, c => c);

        foreach (var remote in remoteCategories)
        {
            var externalId = remote.Id.ToString();
            var name = remote.Name;

            if (byExternalId.TryGetValue(externalId, out var category))
            {
                category.Name = name;
                continue;
            }

            var newCategory = new Category
            {
                Id = Id.New(),
                ProviderId = provider.Id,
                ExternalId = externalId,
                Name = name,
            };

            _db.Categories.Add(newCategory);
            byExternalId[externalId] = newCategory;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncCreditCardsAsync(Provider provider, CancellationToken cancellationToken)
    {
        var remoteCards = await _client.CreditCards.ListAsync(cancellationToken: cancellationToken);

        var existing = await _db.CreditCards
            .Where(c => c.ProviderId == provider.Id)
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(c => c.ExternalId, c => c);

        foreach (var remote in remoteCards)
        {
            var externalId = remote.Id.ToString();
            var name = remote.Name;

            if (byExternalId.TryGetValue(externalId, out var card))
            {
                card.Name = name;
                continue;
            }

            var newCard = new CreditCard
            {
                Id = Id.New(),
                ProviderId = provider.Id,
                ExternalId = externalId,
                Name = name,
            };

            _db.CreditCards.Add(newCard);
            byExternalId[externalId] = newCard;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncInvoicesAsync(
        Dictionary<string, string> creditCardsMap,
        CancellationToken cancellationToken)
    {
        var yearRanges = SyncDateRangeCalculator.GetYearRanges(_windowOptions);

        var existing = await _db.Invoices
            .Where(i => creditCardsMap.Values.Contains(i.CreditCardId))
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(i => i.ExternalId, i => i);

        // Fetch invoices per credit card and per year to control request sizes.
        foreach (var creditCardExternalId in creditCardsMap.Keys)
        {
            if (!creditCardsMap.TryGetValue(creditCardExternalId, out var localCreditCardId))
            {
                continue;
            }

            var creditCardRemoteId = long.Parse(creditCardExternalId);
            foreach (var (start, end) in yearRanges)
            {
                var remoteInvoices = await _client.Invoices.ListAsync(
                    creditCardRemoteId,
                    new NOrganizze.Invoices.InvoiceListOptions
                    {
                        StartDate = start,
                        EndDate = end,
                    },
                    cancellationToken: cancellationToken);

                foreach (var remote in remoteInvoices)
                {
                    var externalId = remote.Id.ToString();
                    var date = remote.Date;
                    var amountCents = remote.AmountCents;
                    var paid = remote.BalanceCents == 0;

                    if (byExternalId.TryGetValue(externalId, out var invoice))
                    {
                        invoice.Date = date;
                        invoice.Value = amountCents;
                        invoice.Paid = paid;
                        continue;
                    }

                    var newInvoice = new Invoice
                    {
                        Id = Id.New(),
                        CreditCardId = localCreditCardId,
                        ExternalId = externalId,
                        Date = date,
                        Value = amountCents,
                        Paid = paid,
                    };

                    _db.Invoices.Add(newInvoice);
                    byExternalId[externalId] = newInvoice;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task SyncTransactionsAsync(
        Dictionary<string, string> accountsMap,
        Dictionary<string, string> categoriesMap,
        Dictionary<string, string> creditCardsMap,
        Dictionary<string, string> invoicesMap,
        CancellationToken cancellationToken)
    {
        var yearRanges = SyncDateRangeCalculator.GetYearRanges(_windowOptions);

        var providerAccountIds = accountsMap.Values.ToHashSet();
        var providerCreditCardIds = creditCardsMap.Values.ToHashSet();

        var existing = await _db.Transactions
            .Where(t => providerAccountIds.Contains(t.AccountId) || providerCreditCardIds.Contains(t.AccountId))
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(t => t.ExternalId, t => t);

        foreach (var (start, end) in yearRanges)
        {
            var remoteTransactions = await _client.Transactions.ListAsync(
                new NOrganizze.Transactions.TransactionListOptions
                {
                    StartDate = start,
                    EndDate = end,
                },
                cancellationToken: cancellationToken);

            foreach (var remote in remoteTransactions)
            {
                var externalId = remote.Id.ToString();
                var accountExternalId = remote.AccountId.ToString();

                var accountType = Fintable.Models.TransactionAccountType.Account;
                if (!accountsMap.TryGetValue(accountExternalId, out var localAccountId))
                {
                    if (creditCardsMap.TryGetValue(accountExternalId, out var localCreditCardId))
                    {
                        localAccountId = localCreditCardId;
                        accountType = Fintable.Models.TransactionAccountType.CreditCard;
                    }
                    else
                    {
                        // If we do not know the account, skip this transaction for now.
                        continue;
                    }
                }

                var remoteCategoryKey = remote.CategoryId > 0 ? remote.CategoryId.ToString() : null;
                var localCategoryId = remoteCategoryKey is not null && categoriesMap.TryGetValue(remoteCategoryKey, out var mappedId)
                    ? mappedId
                    : null;
                var localInvoiceId = ResolveLocalInvoiceId(remote, invoicesMap);

                if (byExternalId.TryGetValue(externalId, out var transaction))
                {
                    transaction.Description = remote.Description;
                    transaction.Date = remote.Date;
                    transaction.Paid = remote.Paid;
                    transaction.Value = remote.AmountCents;
                    transaction.TotalInstallments = remote.TotalInstallments;
                    transaction.Installment = remote.Installment;
                    transaction.Recurring = remote.Recurring;
                    transaction.AccountId = localAccountId;
                    transaction.AccountType = accountType;
                    transaction.CategoryId = localCategoryId;
                    transaction.InvoiceId = localInvoiceId;

                    continue;
                }

                var newTransaction = new Transaction
                {
                    Id = Id.New(),
                    ExternalId = externalId,
                    Description = remote.Description,
                    Date = remote.Date,
                    Paid = remote.Paid,
                    Value = remote.AmountCents,
                    TotalInstallments = remote.TotalInstallments,
                    Installment = remote.Installment,
                    Recurring = remote.Recurring,
                    AccountId = localAccountId,
                    AccountType = accountType,
                    CategoryId = localCategoryId,
                    InvoiceId = localInvoiceId,
                };

                _db.Transactions.Add(newTransaction);
                byExternalId[externalId] = newTransaction;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    internal static string? ResolveLocalInvoiceId(
        NOrganizze.Transactions.Transaction remoteTransaction,
        IReadOnlyDictionary<string, string> invoicesMap)
    {
        var externalInvoiceId = remoteTransaction.PaidCreditCardInvoiceId
            ?? remoteTransaction.CreditCardInvoiceId;

        if (externalInvoiceId is null || externalInvoiceId <= 0)
        {
            return null;
        }

        return invoicesMap.TryGetValue(externalInvoiceId.Value.ToString(), out var localInvoiceId)
            ? localInvoiceId
            : null;
    }
}
