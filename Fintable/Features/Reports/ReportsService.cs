using Fintable.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintable.Features.Reports
{
    public class ReportsService(FintableDb db) : IReportsService
    {
        public async Task<StatsReportDto> GetStatsReportAsync()
        {
            var providers = await db.Providers.ToListAsync();
            var accounts = await db.Accounts.ToListAsync();
            var categories = await db.Categories.ToListAsync();
            var creditCards = await db.CreditCards.ToListAsync();
            var invoices = await db.Invoices.ToListAsync();
            var transactions = await db.Transactions.ToListAsync();

            return new StatsReportDto
            {
                TotalProviders = providers.Count,
                TotalAccounts = accounts.Count,
                TotalCategories = categories.Count,
                TotalCreditCards = creditCards.Count,
                TotalInvoices = invoices.Count,
                TotalTransactions = transactions.Count,
                Providers = providers.ToDictionary(p => p.Name, p =>
                {
                    var providerAccounts = accounts.Where(a => a.ProviderId == p.Id).ToList();
                    var providerCreditCards = creditCards.Where(c => c.ProviderId == p.Id).ToList();
                    var providerInvoices = invoices.Where(i => providerCreditCards.Any(c => c.Id == i.CreditCardId)).ToList();
                    var providerTransactions = transactions.Where(t => providerAccounts.Any(a => a.Id == t.AccountId)).ToList();
                    return new ProviderStatsReportDto
                    {
                        Name = p.Name,
                        Accounts = providerAccounts.Count,
                        Categories = categories.Count(c => c.ProviderId == p.Id),
                        CreditCards = providerCreditCards.Count,
                        Invoices = new InvoiceStatsReportDto
                        {
                            FirstDate = providerInvoices.Min(i => i.Date),
                            LastDate = providerInvoices.Max(i => i.Date),
                            Count = providerInvoices.Count
                        },
                        Transactions = new TransactionStatsReportDto
                        {
                            FirstDate = providerTransactions.Min(t => t.Date),
                            LastDate = providerTransactions.Max(t => t.Date),
                            Count = providerTransactions.Count
                        }
                    };
                })
            };
        }
    }
}
