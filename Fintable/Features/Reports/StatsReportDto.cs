namespace Fintable.Features.Reports
{
    public class StatsReportDto
    {
        public Dictionary<string, ProviderStatsReportDto>? Providers { get; set; }
        public int TotalProviders { get; set; }
        public int TotalAccounts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalCreditCards { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalTransactions { get; set; }

        public override string ToString()
        {
            var providersCount = Providers?.Count ?? 0;
            return $"[Providers: {providersCount}] [TotalProviders: {TotalProviders}] [Accounts: {TotalAccounts}] [Categories: {TotalCategories}] [CreditCards: {TotalCreditCards}] [Invoices: {TotalInvoices}] [Transactions: {TotalTransactions}]";
        }
    }

    public class ProviderStatsReportDto
    {
        public required string Name { get; set; }
        public int Accounts { get; set; }
        public int Categories { get; set; }
        public int CreditCards { get; set; }
        public InvoiceStatsReportDto? Invoices { get; set; }
        public TransactionStatsReportDto? Transactions { get; set; }

        public override string ToString()
        {
            var invoicesCount = Invoices?.Count ?? 0;
            var transactionsCount = Transactions?.Count ?? 0;
            return $"[{Name}] [Accounts: {Accounts}] [Categories: {Categories}] [CreditCards: {CreditCards}] [Invoices: {invoicesCount}] [Transactions: {transactionsCount}]";
        }
    }

    public class InvoiceStatsReportDto
    {
        public DateTime? FirstDate { get; set; }
        public DateTime? LastDate { get; set; }
        public int Count { get; set; }

        public override string ToString()
        {
            return $"[FirstDate: {FirstDate:O}] [LastDate: {LastDate:O}] [Count: {Count}]";
        }
    }

    public class TransactionStatsReportDto
    {
        public DateTime? FirstDate { get; set; }
        public DateTime? LastDate { get; set; }
        public int Count { get; set; }

        public override string ToString()
        {
            return $"[FirstDate: {FirstDate:O}] [LastDate: {LastDate:O}] [Count: {Count}]";
        }
    }
}
