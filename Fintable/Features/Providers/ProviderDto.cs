namespace Fintable.Features.Providers
{
    public class ProviderDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
