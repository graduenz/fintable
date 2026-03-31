namespace Fintable.Features.Providers;

public class ProviderValidateEntryDto
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required string Name { get; set; }
    public bool IsFullySetUp { get; set; }
    public required IReadOnlyList<string> MissingKeys { get; set; }

    public override string ToString()
    {
        var missingKeys = string.Join(", ", MissingKeys);
        return $"[{Type}] [{Name}] [Id: {Id}] [FullySetUp: {IsFullySetUp}] [MissingKeys: {missingKeys}]";
    }
}
