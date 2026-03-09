using Fintable;

namespace Fintable.Features.Providers;

public static class ProviderMetadataSchemaRegistry
{
    public static IReadOnlyList<string>? GetRequiredKeys(string providerType)
    {
        var normalized = providerType?.Trim().ToLowerInvariant() ?? "";
        return normalized switch
        {
            _ when normalized == ProviderType.Organizze => ["email", "apiKey"],
            _ => null
        };
    }
}
