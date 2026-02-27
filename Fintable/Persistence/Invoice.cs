namespace Fintable.Persistence;

public class Invoice
{
    public required string Id { get; set; }
    public DateTime Date { get; set; }
    public int Value { get; set; }
    public bool Paid { get; set; }
    public required string CreditCardId { get; set; }
    public required string ExternalId { get; set; }

    public CreditCard? CreditCard { get; set; }
}
