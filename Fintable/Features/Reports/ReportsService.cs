using Fintable.Models;
using Fintable.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintable.Features.Reports
{
    public class ReportsService(FintableDb db) : IReportsService
    {
        private const string KindIncome = "Income";
        private const string KindExpense = "Expense";
        private const string KindCreditCard = "CreditCard";
        private const string KindUnknown = "Unknown";

        internal static decimal CentsToBrl(int cents) => cents / 100m;

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
                            FirstDate = providerInvoices.Count > 0 ? providerInvoices.Min(i => i.Date) : null,
                            LastDate = providerInvoices.Count > 0 ? providerInvoices.Max(i => i.Date) : null,
                            Count = providerInvoices.Count
                        },
                        Transactions = new TransactionStatsReportDto
                        {
                            FirstDate = providerTransactions.Count > 0 ? providerTransactions.Min(t => t.Date) : null,
                            LastDate = providerTransactions.Count > 0 ? providerTransactions.Max(t => t.Date) : null,
                            Count = providerTransactions.Count
                        }
                    };
                })
            };
        }

        public async Task<FintableReportDto> GetFintableReportAsync(int year)
        {
            var categories = await db.Categories
                .Include(c => c.Parent)
                .ToListAsync();

            var transactions = await db.Transactions
                .Where(t => t.AccountType == TransactionAccountType.Account)
                .Where(t => t.InvoiceId == null)
                .Where(t => t.Date.Year == year)
                .ToListAsync();

            var creditCards = await db.CreditCards.ToListAsync();

            var invoices = await db.Invoices
                .Where(i => i.Date.Year == year)
                .ToListAsync();

            var categoriesById = categories.ToDictionary(c => c.Id);
            var rows = new List<FintableReportRowDto>();

            rows.AddRange(BuildCategoryRows(transactions, categoriesById));
            rows.AddRange(BuildUncategorizedRows(transactions));
            rows.AddRange(BuildCreditCardRows(creditCards, invoices));

            SortRows(rows);

            return new FintableReportDto { Year = year, Rows = rows };
        }

        internal static List<FintableReportRowDto> BuildCategoryRows(
            List<Transaction> transactions,
            Dictionary<string, Category> categoriesById)
        {
            var categorized = transactions.Where(t => t.CategoryId != null);
            var grouped = categorized.GroupBy(t => t.CategoryId!);

            var rows = new List<FintableReportRowDto>();
            foreach (var group in grouped)
            {
                if (!categoriesById.TryGetValue(group.Key, out var category))
                    continue;

                var displayName = GetFlattenedCategoryName(category);
                var kind = MapCategoryKind(category.Kind);

                rows.Add(new FintableReportRowDto
                {
                    Category = displayName,
                    Kind = kind,
                    Months = BuildMonthCells(group.ToList()),
                });
            }

            return rows;
        }

        internal static List<FintableReportRowDto> BuildUncategorizedRows(List<Transaction> transactions)
        {
            var uncategorized = transactions.Where(t => t.CategoryId == null).ToList();
            if (uncategorized.Count == 0)
                return [];

            var rows = new List<FintableReportRowDto>();

            var incomeTransactions = uncategorized.Where(t => t.Value > 0).ToList();
            if (incomeTransactions.Count > 0)
            {
                rows.Add(new FintableReportRowDto
                {
                    Category = "Uncategorized",
                    Kind = KindIncome,
                    Months = BuildMonthCells(incomeTransactions),
                });
            }

            var expenseTransactions = uncategorized.Where(t => t.Value < 0).ToList();
            if (expenseTransactions.Count > 0)
            {
                rows.Add(new FintableReportRowDto
                {
                    Category = "Uncategorized",
                    Kind = KindExpense,
                    Months = BuildMonthCells(expenseTransactions),
                });
            }

            return rows;
        }

        internal static List<FintableReportRowDto> BuildCreditCardRows(
            List<CreditCard> creditCards,
            List<Invoice> invoices)
        {
            var invoicesByCard = invoices.GroupBy(i => i.CreditCardId);
            var creditCardsById = creditCards.ToDictionary(c => c.Id);

            var rows = new List<FintableReportRowDto>();
            foreach (var group in invoicesByCard)
            {
                if (!creditCardsById.TryGetValue(group.Key, out var card))
                    continue;

                var monthCells = new List<FintableReportCellDto>();
                var invoicesByMonth = group.GroupBy(i => i.Date.Month);

                foreach (var monthGroup in invoicesByMonth)
                {
                    var totalValue = monthGroup.Sum(i => i.Value);
                    var allPaid = monthGroup.All(i => i.Paid);

                    monthCells.Add(new FintableReportCellDto
                    {
                        Month = monthGroup.Key,
                        Value = CentsToBrl(Math.Abs(totalValue)),
                        Paid = allPaid,
                    });
                }

                var row = new FintableReportRowDto
                {
                    Category = card.Name,
                    Kind = KindCreditCard,
                    Months = FillAllMonths(monthCells),
                };
                rows.Add(row);
            }

            return rows;
        }

        internal static List<FintableReportCellDto> BuildMonthCells(List<Transaction> transactions)
        {
            var byMonth = transactions.GroupBy(t => t.Date.Month);
            var cells = new List<FintableReportCellDto>();

            foreach (var group in byMonth)
            {
                var sum = group.Sum(t => t.Value);
                var allPaid = group.All(t => t.Paid);

                cells.Add(new FintableReportCellDto
                {
                    Month = group.Key,
                    Value = CentsToBrl(Math.Abs(sum)),
                    Paid = allPaid,
                });
            }

            return FillAllMonths(cells);
        }

        internal static List<FintableReportCellDto> FillAllMonths(List<FintableReportCellDto> existingCells)
        {
            var byMonth = existingCells.ToDictionary(c => c.Month);
            var result = new List<FintableReportCellDto>();

            for (var month = 1; month <= 12; month++)
            {
                result.Add(byMonth.TryGetValue(month, out var cell)
                    ? cell
                    : new FintableReportCellDto { Month = month, Value = 0m, Paid = null });
            }

            return result;
        }

        internal static string GetFlattenedCategoryName(Category category)
        {
            return category.Parent != null
                ? $"{category.Parent.Name} - {category.Name}"
                : category.Name;
        }

        internal static string MapCategoryKind(CategoryKind kind) => kind switch
        {
            CategoryKind.Income => KindIncome,
            CategoryKind.Expense => KindExpense,
            _ => KindUnknown,
        };

        internal static void SortRows(List<FintableReportRowDto> rows)
        {
            var kindOrder = new Dictionary<string, int>
            {
                [KindIncome] = 0,
                [KindCreditCard] = 1,
                [KindExpense] = 2,
                [KindUnknown] = 3,
            };

            rows.Sort((a, b) =>
            {
                var orderA = kindOrder.GetValueOrDefault(a.Kind, 99);
                var orderB = kindOrder.GetValueOrDefault(b.Kind, 99);

                if (orderA != orderB)
                    return orderA.CompareTo(orderB);

                return string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
