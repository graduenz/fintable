namespace Fintable.Features.Providers.Organizze;

public class OrganizzeAccountDto
{
    public required string ExternalId { get; set; }
    public required string Name { get; set; }
}

public class OrganizzeCategoryDto
{
    public required string ExternalId { get; set; }
    public required string Name { get; set; }
}

public class OrganizzeCreditCardDto
{
    public required string ExternalId { get; set; }
    public required string Name { get; set; }
}

public class OrganizzeInvoiceDto
{
    public required string ExternalId { get; set; }
    public DateTime Date { get; set; }
    public int AmountCents { get; set; }
    public bool Paid { get; set; }
    public required string CreditCardExternalId { get; set; }
}

public class OrganizzeTransactionDto
{
    public required string ExternalId { get; set; }
    public required string AccountExternalId { get; set; }
    public string? CategoryExternalId { get; set; }
    public required string Description { get; set; }
    public DateTime Date { get; set; }
    public bool Paid { get; set; }
    public int AmountCents { get; set; }
    public int TotalInstallments { get; set; }
    public int Installment { get; set; }
    public bool Recurring { get; set; }
}

