namespace Fintable.Persistence;

public class Provider
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required string Name { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }

    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<CreditCard> CreditCards { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
}
