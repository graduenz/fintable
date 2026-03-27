namespace Fintable.Features.Providers
{
    public class ProviderDto
    {
        public string? Id { get; set; }
        public required string Type { get; set; }
        public required string Name { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
