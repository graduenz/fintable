namespace Fintable.Data;

public class CreditCard
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ProviderId { get; set; }
    public required string ExternalId { get; set; }

    public Provider? Provider { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}
