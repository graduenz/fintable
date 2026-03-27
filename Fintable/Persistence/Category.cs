using Fintable.Models;

namespace Fintable.Persistence;

public class Category
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public CategoryKind Kind { get; set; }
    public required string ProviderId { get; set; }
    public required string ExternalId { get; set; }

    public string? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];

    public Provider? Provider { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
}
