using Fintable.Features.Sync;
using Fintable.Models;
using Fintable.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fintable.Organizze;

public class OrganizzeSyncService
{
    private readonly FintableDb _db;
    private readonly NOrganizze.NOrganizzeClient _client;
    private readonly SyncWindowOptions _windowOptions;
    private readonly ILogger<OrganizzeSyncService> _logger;

    public OrganizzeSyncService(
        FintableDb db,
        NOrganizze.NOrganizzeClient client,
        SyncWindowOptions windowOptions,
        ILogger<OrganizzeSyncService> logger)
    {
        _db = db;
        _client = client;
        _windowOptions = windowOptions;
        _logger = logger;
    }

    public async Task SyncAsync(
        Provider provider,
        SyncWarningCollector collector,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting Organizze sync for provider {ProviderId} ({ProviderName}).",
            provider.Id,
            provider.Name);

        // Order is important for relationship resolution.
        var accountsMap = await SyncAccountsAsync(provider, cancellationToken);
        var categoriesMap = await SyncCategoriesAsync(provider, cancellationToken);
        var creditCardsMap = await SyncCreditCardsAsync(provider, cancellationToken);
        var invoicesMap = await SyncInvoicesAsync(creditCardsMap, cancellationToken);
        await SyncTransactionsAsync(accountsMap, categoriesMap, creditCardsMap, invoicesMap, collector, cancellationToken);

        _logger.LogInformation(
            "Finished Organizze sync for provider {ProviderId} ({ProviderName}). Synced Accounts={AccountsCount}, Categories={CategoriesCount}, CreditCards={CreditCardsCount}, Invoices={InvoicesCount}.",
            provider.Id,
            provider.Name,
            accountsMap.Count,
            categoriesMap.Count,
            creditCardsMap.Count,
            invoicesMap.Count);
    }

    private async Task<Dictionary<string, string>> SyncAccountsAsync(Provider provider, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing accounts for provider {ProviderId}...", provider.Id);
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
        _logger.LogInformation(
            "Accounts sync completed for provider {ProviderId}. Remote={RemoteCount}, LocalMapped={MappedCount}.",
            provider.Id,
            remoteAccounts.Count,
            byExternalId.Count);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncCategoriesAsync(Provider provider, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing categories for provider {ProviderId}...", provider.Id);
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

        AssignCategoryParentsFromRemote(remoteCategories, byExternalId);

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Categories sync completed for provider {ProviderId}. Remote={RemoteCount}, LocalMapped={MappedCount}.",
            provider.Id,
            remoteCategories.Count,
            byExternalId.Count);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    internal static void AssignCategoryParentsFromRemote(
        IEnumerable<NOrganizze.Categories.Category> remoteCategories,
        IDictionary<string, Category> byExternalId)
    {
        foreach (var remote in remoteCategories)
        {
            if (!byExternalId.TryGetValue(remote.Id.ToString(), out var category))
            {
                continue;
            }

            if (remote.ParentId is { } parentExternalId && parentExternalId > 0
                && byExternalId.TryGetValue(parentExternalId.ToString(), out var parentCategory))
            {
                category.ParentId = parentCategory.Id;
            }
            else
            {
                category.ParentId = null;
            }
        }
    }

    private async Task<Dictionary<string, string>> SyncCreditCardsAsync(Provider provider, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing credit cards for provider {ProviderId}...", provider.Id);
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
        _logger.LogInformation(
            "Credit cards sync completed for provider {ProviderId}. Remote={RemoteCount}, LocalMapped={MappedCount}.",
            provider.Id,
            remoteCards.Count,
            byExternalId.Count);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task<Dictionary<string, string>> SyncInvoicesAsync(
        Dictionary<string, string> creditCardsMap,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Syncing invoices for {CreditCardsCount} credit card(s)...",
            creditCardsMap.Count);
        var yearRanges = SyncDateRangeCalculator.GetYearRanges(_windowOptions);

        var existing = await _db.Invoices
            .Where(i => creditCardsMap.Values.Contains(i.CreditCardId))
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(i => i.ExternalId, i => i);
        var fetchedInvoicesCount = 0;
        var creditCardIndex = 0;

        // Fetch invoices per credit card and per year to control request sizes.
        foreach (var creditCardExternalId in creditCardsMap.Keys)
        {
            creditCardIndex++;
            if (!creditCardsMap.TryGetValue(creditCardExternalId, out var localCreditCardId))
            {
                continue;
            }

            var creditCardRemoteId = long.Parse(creditCardExternalId);
            _logger.LogInformation(
                "Fetching invoices for credit card {CreditCardIndex}/{CreditCardsCount} (ExternalId={CreditCardExternalId}) across {YearRangeCount} range(s).",
                creditCardIndex,
                creditCardsMap.Count,
                creditCardExternalId,
                yearRanges.Count);

            var yearRangeIndex = 0;
            foreach (var (start, end) in yearRanges)
            {
                yearRangeIndex++;
                _logger.LogInformation(
                    "Fetching invoices for card {CreditCardExternalId}, range {YearRangeIndex}/{YearRangeCount}: {StartDate:yyyy-MM-dd}..{EndDate:yyyy-MM-dd}.",
                    creditCardExternalId,
                    yearRangeIndex,
                    yearRanges.Count,
                    start,
                    end);

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
                    fetchedInvoicesCount++;
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
        _logger.LogInformation(
            "Invoices sync completed. RemoteFetched={RemoteFetchedCount}, LocalMapped={MappedCount}, CreditCards={CreditCardsCount}, YearRanges={YearRangeCount}.",
            fetchedInvoicesCount,
            byExternalId.Count,
            creditCardsMap.Count,
            yearRanges.Count);

        return byExternalId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id);
    }

    private async Task SyncTransactionsAsync(
        Dictionary<string, string> accountsMap,
        Dictionary<string, string> categoriesMap,
        Dictionary<string, string> creditCardsMap,
        Dictionary<string, string> invoicesMap,
        SyncWarningCollector collector,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Syncing transactions with Accounts={AccountsCount}, Categories={CategoriesCount}, CreditCards={CreditCardsCount}, Invoices={InvoicesCount}.",
            accountsMap.Count,
            categoriesMap.Count,
            creditCardsMap.Count,
            invoicesMap.Count);
        var yearRanges = SyncDateRangeCalculator.GetYearRanges(_windowOptions);

        var providerAccountIds = accountsMap.Values.ToHashSet();
        var providerCreditCardIds = creditCardsMap.Values.ToHashSet();
        var providerCategoryIds = categoriesMap.Values.ToHashSet();

        var existing = await _db.Transactions
            .Where(t => providerAccountIds.Contains(t.AccountId) || providerCreditCardIds.Contains(t.AccountId))
            .ToListAsync(cancellationToken);

        var categoriesById = await _db.Categories
            .Where(c => providerCategoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var byExternalId = existing.ToDictionary(t => t.ExternalId, t => t);
        var fetchedTransactionsCount = 0;
        var skippedUnknownAccountCount = 0;
        var yearRangeIndex = 0;

        foreach (var (start, end) in yearRanges)
        {
            yearRangeIndex++;
            _logger.LogInformation(
                "Fetching transactions for range {YearRangeIndex}/{YearRangeCount}: {StartDate:yyyy-MM-dd}..{EndDate:yyyy-MM-dd}.",
                yearRangeIndex,
                yearRanges.Count,
                start,
                end);

            var remoteTransactions = await _client.Transactions.ListAsync(
                new NOrganizze.Transactions.TransactionListOptions
                {
                    StartDate = start,
                    EndDate = end,
                },
                cancellationToken: cancellationToken);

            foreach (var remote in remoteTransactions)
            {
                fetchedTransactionsCount++;
                var externalId = remote.Id.ToString();
                var accountExternalId = remote.AccountId.ToString();

                var accountType = TransactionAccountType.Account;
                if (!accountsMap.TryGetValue(accountExternalId, out var localAccountId))
                {
                    if (creditCardsMap.TryGetValue(accountExternalId, out var localCreditCardId))
                    {
                        localAccountId = localCreditCardId;
                        accountType = TransactionAccountType.CreditCard;
                    }
                    else
                    {
                        // If we do not know the account, skip this transaction for now.
                        skippedUnknownAccountCount++;
                        collector.ReportWarning(
                            SyncWarningCodes.TransactionUnknownAccountSkipped,
                            $"Transaction \"{remote.Description}\" ({externalId}) on {remote.Date:yyyy-MM-dd} skipped because account {accountExternalId} is not mapped locally.");
                        continue;
                    }
                }

                var remoteCategoryKey = remote.CategoryId > 0 ? remote.CategoryId.ToString() : null;
                var localCategoryId = remoteCategoryKey is not null && categoriesMap.TryGetValue(remoteCategoryKey, out var mappedId)
                    ? mappedId
                    : null;
                if (remoteCategoryKey is not null && localCategoryId is null)
                {
                    collector.ReportWarning(
                        SyncWarningCodes.CategoryMappingMissing,
                        $"Transaction \"{remote.Description}\" ({externalId}) on {remote.Date:yyyy-MM-dd} references category {remoteCategoryKey} with no local mapping.");
                }

                var localInvoiceId = ResolveLocalInvoiceId(remote, invoicesMap);
                if (TryGetExternalInvoiceId(remote, out var externalInvoiceId) && localInvoiceId is null)
                {
                    collector.ReportWarning(
                        SyncWarningCodes.InvoiceMappingMissing,
                        $"Transaction \"{remote.Description}\" ({externalId}) on {remote.Date:yyyy-MM-dd} references invoice {externalInvoiceId} with no local mapping.");
                }
                ApplyCategoryKindInference(localCategoryId, remote.AmountCents, categoriesById);

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
        _logger.LogInformation(
            "Transactions sync completed. RemoteFetched={RemoteFetchedCount}, LocalMapped={MappedCount}, SkippedUnknownAccount={SkippedUnknownAccountCount}, YearRanges={YearRangeCount}.",
            fetchedTransactionsCount,
            byExternalId.Count,
            skippedUnknownAccountCount,
            yearRanges.Count);

        if (fetchedTransactionsCount > 0 && skippedUnknownAccountCount == fetchedTransactionsCount)
        {
            collector.ReportCritical(
                SyncWarningCodes.SyncDataConsistencyRisk,
                "All fetched transactions were skipped due to unmapped accounts. Local sync can be incomplete.");
        }
    }

    private static void ApplyCategoryKindInference(
        string? localCategoryId,
        int amountCents,
        IReadOnlyDictionary<string, Category> categoriesById)
    {
        if (localCategoryId is null || !categoriesById.TryGetValue(localCategoryId, out var category))
        {
            return;
        }

        var inferredKind = InferCategoryKind(amountCents);
        if (inferredKind == CategoryKind.Unknown)
        {
            return;
        }

        if (category.Kind == CategoryKind.Unknown)
        {
            category.Kind = inferredKind;
            return;
        }

        if (category.Kind != inferredKind)
        {
            category.Kind = CategoryKind.Unknown;
        }
    }

    private static CategoryKind InferCategoryKind(int amountCents)
    {
        if (amountCents < 0)
        {
            return CategoryKind.Expense;
        }

        if (amountCents > 0)
        {
            return CategoryKind.Income;
        }

        return CategoryKind.Unknown;
    }

    internal static string? ResolveLocalInvoiceId(
        NOrganizze.Transactions.Transaction remoteTransaction,
        IReadOnlyDictionary<string, string> invoicesMap)
    {
        if (!TryGetExternalInvoiceId(remoteTransaction, out var externalInvoiceId))
        {
            return null;
        }

        return invoicesMap.TryGetValue(externalInvoiceId.ToString(), out var localInvoiceId)
            ? localInvoiceId
            : null;
    }

    internal static bool TryGetExternalInvoiceId(
        NOrganizze.Transactions.Transaction remoteTransaction,
        out long externalInvoiceId)
    {
        var externalInvoiceIdNullable = remoteTransaction.PaidCreditCardInvoiceId
            ?? remoteTransaction.CreditCardInvoiceId;

        if (externalInvoiceIdNullable is null || externalInvoiceIdNullable <= 0)
        {
            externalInvoiceId = default;
            return false;
        }

        externalInvoiceId = externalInvoiceIdNullable.Value;
        return true;
    }
}
