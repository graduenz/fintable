using Fintable.Features.Sync;
using Fintable.Models;
using Fintable.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintable.Organizze;

public class OrganizzeSyncService
{
    internal const int TransactionFetchCap = 500;

    private readonly FintableDb _db;
    private readonly NOrganizze.NOrganizzeClient? _client;
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

    // Testing seam: allows subclass-based tests without coupling to NOrganizze types at constructor boundary.
    protected OrganizzeSyncService(
        FintableDb db,
        SyncWindowOptions windowOptions,
        ILogger<OrganizzeSyncService> logger)
    {
        _db = db;
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
        var remoteAccounts = await ListAccountsAsync(cancellationToken);

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
        var remoteCategories = await ListCategoriesAsync(cancellationToken);

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
        Dictionary<string, Category> byExternalId)
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
        var remoteCards = await ListCreditCardsAsync(cancellationToken);

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

                var remoteInvoices = await ListInvoicesAsync(
                    creditCardRemoteId,
                    new NOrganizze.Invoices.InvoiceListOptions
                    {
                        StartDate = start,
                        EndDate = end,
                    },
                    cancellationToken);

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

            var remoteTransactions = await FetchTransactionsByDateCursorAsync(start, end, collector, cancellationToken);
            fetchedTransactionsCount += remoteTransactions.Count;

            foreach (var remote in remoteTransactions)
            {
                var externalId = remote.Id.ToString();
                if (!TryResolveLocalAccount(
                        remote,
                        accountsMap,
                        creditCardsMap,
                        collector,
                        out var localAccountId,
                        out var accountType))
                {
                    skippedUnknownAccountCount++;
                    continue;
                }

                var localCategoryId = ResolveLocalCategoryIdAndReportMissing(remote, categoriesMap, collector);
                var localInvoiceId = ResolveLocalInvoiceIdAndReportMissing(remote, invoicesMap, collector);
                ApplyCategoryKindInference(localCategoryId, remote.AmountCents, categoriesById);
                UpsertTransaction(
                    byExternalId,
                    remote,
                    externalId,
                    localAccountId,
                    accountType,
                    localCategoryId,
                    localInvoiceId);
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

    private static bool TryResolveLocalAccount(
        NOrganizze.Transactions.Transaction remoteTransaction,
        Dictionary<string, string> accountsMap,
        Dictionary<string, string> creditCardsMap,
        SyncWarningCollector collector,
        out string localAccountId,
        out TransactionAccountType accountType)
    {
        var externalId = remoteTransaction.AccountId.ToString();
        if (accountsMap.TryGetValue(externalId, out var mappedAccountId) && mappedAccountId is not null)
        {
            localAccountId = mappedAccountId;
            accountType = TransactionAccountType.Account;
            return true;
        }

        if (creditCardsMap.TryGetValue(externalId, out var mappedCreditCardId) && mappedCreditCardId is not null)
        {
            localAccountId = mappedCreditCardId;
            accountType = TransactionAccountType.CreditCard;
            return true;
        }

        collector.ReportWarning(
            SyncWarningCodes.TransactionUnknownAccountSkipped,
            $"Transaction \"{remoteTransaction.Description}\" ({remoteTransaction.Id}) on {remoteTransaction.Date:yyyy-MM-dd} skipped because account {externalId} is not mapped locally.");
        localAccountId = string.Empty;
        accountType = default;
        return false;
    }

    private static string? ResolveLocalCategoryIdAndReportMissing(
        NOrganizze.Transactions.Transaction remoteTransaction,
        Dictionary<string, string> categoriesMap,
        SyncWarningCollector collector)
    {
        if (remoteTransaction.CategoryId <= 0)
        {
            return null;
        }

        var externalCategoryId = remoteTransaction.CategoryId.ToString();
        if (categoriesMap.TryGetValue(externalCategoryId, out var localCategoryId))
        {
            return localCategoryId;
        }

        collector.ReportWarning(
            SyncWarningCodes.CategoryMappingMissing,
            $"Transaction \"{remoteTransaction.Description}\" ({remoteTransaction.Id}) on {remoteTransaction.Date:yyyy-MM-dd} references category {externalCategoryId} with no local mapping.");
        return null;
    }

    private static string? ResolveLocalInvoiceIdAndReportMissing(
        NOrganizze.Transactions.Transaction remoteTransaction,
        Dictionary<string, string> invoicesMap,
        SyncWarningCollector collector)
    {
        var localInvoiceId = ResolveLocalInvoiceId(remoteTransaction, invoicesMap);
        if (!TryGetExternalInvoiceId(remoteTransaction, out var externalInvoiceId) || localInvoiceId is not null)
        {
            return localInvoiceId;
        }

        collector.ReportWarning(
            SyncWarningCodes.InvoiceMappingMissing,
            $"Transaction \"{remoteTransaction.Description}\" ({remoteTransaction.Id}) on {remoteTransaction.Date:yyyy-MM-dd} references invoice {externalInvoiceId} with no local mapping.");
        return null;
    }

    private void UpsertTransaction(
        Dictionary<string, Transaction> byExternalId,
        NOrganizze.Transactions.Transaction remoteTransaction,
        string externalId,
        string localAccountId,
        TransactionAccountType accountType,
        string? localCategoryId,
        string? localInvoiceId)
    {
        if (byExternalId.TryGetValue(externalId, out var transaction))
        {
            ApplyRemoteTransactionToEntity(
                transaction,
                remoteTransaction,
                localAccountId,
                accountType,
                localCategoryId,
                localInvoiceId);
            return;
        }

        var newTransaction = new Transaction
        {
            Id = Id.New(),
            ExternalId = externalId,
            Description = remoteTransaction.Description,
            AccountId = localAccountId,
        };
        ApplyRemoteTransactionToEntity(
            newTransaction,
            remoteTransaction,
            localAccountId,
            accountType,
            localCategoryId,
            localInvoiceId);

        _db.Transactions.Add(newTransaction);
        byExternalId[externalId] = newTransaction;
    }

    private static void ApplyRemoteTransactionToEntity(
        Transaction transaction,
        NOrganizze.Transactions.Transaction remoteTransaction,
        string localAccountId,
        TransactionAccountType accountType,
        string? localCategoryId,
        string? localInvoiceId)
    {
        transaction.Description = remoteTransaction.Description;
        transaction.Date = remoteTransaction.Date;
        transaction.Paid = remoteTransaction.Paid;
        transaction.Value = remoteTransaction.AmountCents;
        transaction.TotalInstallments = remoteTransaction.TotalInstallments;
        transaction.Installment = remoteTransaction.Installment;
        transaction.Recurring = remoteTransaction.Recurring;
        transaction.AccountId = localAccountId;
        transaction.AccountType = accountType;
        transaction.CategoryId = localCategoryId;
        transaction.InvoiceId = localInvoiceId;
    }

    internal async Task<List<NOrganizze.Transactions.Transaction>> FetchTransactionsByDateCursorAsync(
        DateTime start,
        DateTime end,
        SyncWarningCollector collector,
        CancellationToken cancellationToken)
    {
        var allFetched = new List<NOrganizze.Transactions.Transaction>();
        var currentStart = start;

        while (currentStart <= end)
        {
            var chunk = await ListTransactionsAsync(
                new NOrganizze.Transactions.TransactionListOptions
                {
                    StartDate = currentStart,
                    EndDate = end,
                },
                cancellationToken);

            allFetched.AddRange(chunk);

            if (chunk.Count < TransactionFetchCap)
            {
                break;
            }

            collector.ReportWarning(
                SyncWarningCodes.TransactionFetchCapDetected,
                $"Transaction fetch reached {chunk.Count} items for {currentStart:yyyy-MM-dd}..{end:yyyy-MM-dd}; continuing from latest fetched date.");

            var latestDate = GetLatestTransactionDate(chunk);
            if (!TryGetNextCursorStart(currentStart, latestDate, out var nextStart))
            {
                collector.ReportCritical(
                    SyncWarningCodes.TransactionFetchCursorStalled,
                    $"Transaction fetch cursor stalled at {currentStart:yyyy-MM-dd}..{end:yyyy-MM-dd}; unable to advance range safely.");
                break;
            }

            currentStart = nextStart;
        }

        return DeduplicateTransactionsByExternalId(allFetched);
    }

    protected virtual async Task<IReadOnlyList<NOrganizze.Accounts.Account>> ListAccountsAsync(CancellationToken cancellationToken)
        => await _client!.Accounts.ListAsync(cancellationToken: cancellationToken);

    protected virtual async Task<IReadOnlyList<NOrganizze.Categories.Category>> ListCategoriesAsync(CancellationToken cancellationToken)
        => await _client!.Categories.ListAsync(cancellationToken: cancellationToken);

    protected virtual async Task<IReadOnlyList<NOrganizze.CreditCards.CreditCard>> ListCreditCardsAsync(CancellationToken cancellationToken)
        => await _client!.CreditCards.ListAsync(cancellationToken: cancellationToken);

    protected virtual async Task<IReadOnlyList<NOrganizze.Invoices.Invoice>> ListInvoicesAsync(
        long creditCardId,
        NOrganizze.Invoices.InvoiceListOptions options,
        CancellationToken cancellationToken)
        => await _client!.Invoices.ListAsync(creditCardId, options, cancellationToken: cancellationToken);

    protected virtual async Task<IReadOnlyList<NOrganizze.Transactions.Transaction>> ListTransactionsAsync(
        NOrganizze.Transactions.TransactionListOptions options,
        CancellationToken cancellationToken)
        => await _client!.Transactions.ListAsync(options, cancellationToken: cancellationToken);

    internal static DateTime? GetLatestTransactionDate(IEnumerable<NOrganizze.Transactions.Transaction> transactions)
    {
        var dates = transactions.Select(transaction => transaction.Date).ToList();
        return dates.Count == 0 ? null : dates.Max();
    }

    internal static List<NOrganizze.Transactions.Transaction> DeduplicateTransactionsByExternalId(
        IEnumerable<NOrganizze.Transactions.Transaction> transactions)
    {
        var dedup = new Dictionary<long, NOrganizze.Transactions.Transaction>();
        foreach (var transaction in transactions)
        {
            dedup[transaction.Id] = transaction;
        }

        return dedup.Values.ToList();
    }

    internal static bool TryGetNextCursorStart(DateTime currentStart, DateTime? latestDate, out DateTime nextStart)
    {
        if (latestDate is null)
        {
            nextStart = default;
            return false;
        }

        nextStart = latestDate.Value.Date.AddDays(1);
        if (nextStart <= currentStart.Date)
        {
            nextStart = default;
            return false;
        }

        return true;
    }

    private static void ApplyCategoryKindInference(
        string? localCategoryId,
        int amountCents,
        Dictionary<string, Category> categoriesById)
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
        Dictionary<string, string> invoicesMap)
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
