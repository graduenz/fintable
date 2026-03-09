namespace Fintable.Features.Providers.Organizze;

public class OrganizzeFetchClient
{
    private readonly NOrganizze.NOrganizzeClient _client;

    public OrganizzeFetchClient(OrganizzeMetadata metadata)
    {
        _client = new NOrganizze.NOrganizzeClient(metadata.ToCredentials);
    }

    public async Task<IReadOnlyList<OrganizzeAccountDto>> GetAccountsAsync()
    {
        var accounts = await _client.Accounts.ListAsync();

        return accounts
            .Select(a => new OrganizzeAccountDto
            {
                ExternalId = a.Id.ToString(),
                Name = a.Name,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<OrganizzeCategoryDto>> GetCategoriesAsync()
    {
        var categories = await _client.Categories.ListAsync();

        return categories
            .Select(c => new OrganizzeCategoryDto
            {
                ExternalId = c.Id.ToString(),
                Name = c.Name,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<OrganizzeCreditCardDto>> GetCreditCardsAsync()
    {
        var creditCards = await _client.CreditCards.ListAsync();

        return creditCards
            .Select(c => new OrganizzeCreditCardDto
            {
                ExternalId = c.Id.ToString(),
                Name = c.Name,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<OrganizzeInvoiceDto>> GetInvoicesAsync(string creditCardExternalId)
    {
        var creditCardId = long.Parse(creditCardExternalId);
        var invoices = await _client.Invoices.ListAsync(creditCardId);

        return invoices
            .Select(i => new OrganizzeInvoiceDto
            {
                ExternalId = i.Id.ToString(),
                Date = i.Date,
                AmountCents = i.AmountCents,
                Paid = i.BalanceCents == 0,
                CreditCardExternalId = creditCardExternalId,
            })
            .ToList();
    }

    public async Task<IReadOnlyList<OrganizzeTransactionDto>> GetTransactionsAsync()
    {
        var transactions = await _client.Transactions.ListAsync();

        return transactions
            .Select(t => new OrganizzeTransactionDto
            {
                ExternalId = t.Id.ToString(),
                AccountExternalId = t.AccountId.ToString(),
                // TODO: Map CategoryExternalId when available from NOrganizze
                CategoryExternalId = null,
                Description = t.Description,
                Date = t.Date,
                Paid = t.Paid,
                AmountCents = t.AmountCents,
                Installment = t.Installment,
                TotalInstallments = t.TotalInstallments,
                Recurring = t.Recurring,
            })
            .ToList();
    }
}

