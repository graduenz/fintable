using Fintable.Features.Providers;
using Fintable.Organizze;

namespace Fintable.Tests.Features.Providers;

public class ProviderMetadataSchemaRegistryTests
{
    [Fact]
    public void GetRequiredKeys_OrganizzeType_ReturnsOrganizzeKeys()
    {
        // Arrange
        var type = "organizze";

        // Act
        var keys = ProviderMetadataSchemaRegistry.GetRequiredKeys(type);

        // Assert
        Assert.NotNull(keys);
        Assert.Equal(OrganizzeMetadata.RequiredKeys, keys);
    }

    [Theory]
    [InlineData(" organizze ")]
    [InlineData("ORGANIZZE")]
    [InlineData(" Organizze ")]
    public void GetRequiredKeys_OrganizzeTypeVariants_ReturnsOrganizzeKeys(string type)
    {
        // Arrange & Act
        var keys = ProviderMetadataSchemaRegistry.GetRequiredKeys(type);

        // Assert
        Assert.NotNull(keys);
        Assert.Equal(OrganizzeMetadata.RequiredKeys, keys);
    }

    [Fact]
    public void GetRequiredKeys_UnknownType_ReturnsNull()
    {
        // Arrange
        var type = "unknown";

        // Act
        var keys = ProviderMetadataSchemaRegistry.GetRequiredKeys(type);

        // Assert
        Assert.Null(keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRequiredKeys_NullOrEmptyType_ReturnsNull(string? type)
    {
        // Arrange & Act
        var keys = ProviderMetadataSchemaRegistry.GetRequiredKeys(type!);

        // Assert
        Assert.Null(keys);
    }
}
