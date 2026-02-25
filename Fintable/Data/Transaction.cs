using Fintable.Models;

namespace Fintable.Data;

public class Transaction
{
    public required string Id { get; set; }
    public required string Description { get; set; }
    public DateTime Date { get; set; }
    public bool Paid { get; set; }
    public int Value { get; set; }
    public int TotalInstallments { get; set; }
    public int Installment { get; set; }
    public bool Recurring { get; set; }
    public required string AccountId { get; set; }
    public TransactionAccountType AccountType { get; set; }
    public required string CategoryId { get; set; }
    public required string ExternalId { get; set; }

    public Account? Account { get; set; }
    public CreditCard? CreditCard { get; set; }
    public Category? Category { get; set; }
}
