using System.Text.Json;

namespace Fintable.Features.Providers.Organizze;

public class OrganizzeMetadata
{
    public required string Email { get; set;}
    public required string ApiKey { get; set; }

    public static OrganizzeMetadata FromJson(string json) =>
        JsonSerializer.Deserialize<OrganizzeMetadata>(json, (JsonSerializerOptions?)null)
            ?? throw new InvalidOperationException("Failed to deserialize Organizze metadata.");

    public NOrganizze.Credentials ToCredentials() => new(Email, ApiKey);
}

