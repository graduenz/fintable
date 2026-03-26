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
    }

    public class ProviderStatsReportDto
    {
        public required string Name { get; set; }
        public int Accounts { get; set; }
        public int Categories { get; set; }
        public int CreditCards { get; set; }
        public InvoiceStatsReportDto? Invoices { get; set; }
        public TransactionStatsReportDto? Transactions { get; set; }
    }

    public class InvoiceStatsReportDto
    {
        public DateTime? FirstDate { get; set; }
        public DateTime? LastDate { get; set; }
        public int Count { get; set; }
    }

    public class TransactionStatsReportDto
    {
        public DateTime? FirstDate { get; set; }
        public DateTime? LastDate { get; set; }
        public int Count { get; set; }
    }
}
