namespace Fintable.Persistence;

public class CreditCard
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ProviderId { get; set; }
    public required string ExternalId { get; set; }

    public Provider? Provider { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = [];
}
