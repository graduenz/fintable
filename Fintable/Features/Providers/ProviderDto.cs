namespace Fintable.Features.Providers
{
    public class ProviderDto
    {
        public string? Id { get; set; }
        public required string Type { get; set; }
        public required string Name { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }

        public override string ToString()
        {
            var metadataKeys = Metadata is null ? string.Empty : string.Join(", ", Metadata.Keys);
            return $"[{Type}] [{Name}] [Id: {Id ?? "null"}] [Metadata: {metadataKeys}]";
        }
    }
}
