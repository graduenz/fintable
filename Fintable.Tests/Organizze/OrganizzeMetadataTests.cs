using Bogus;
using Fintable.Organizze;
using System.Text.Json;

namespace Fintable.Tests.Organizze;

public class OrganizzeMetadataTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void FromJson_ValidJson_ReturnsCorrectProperties()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var apiKey = _faker.Random.AlphaNumeric(32);
        var json = JsonSerializer.Serialize(new { Email = email, ApiKey = apiKey });

        // Act
        var metadata = OrganizzeMetadata.FromJson(json);

        // Assert
        Assert.Equal(email, metadata.Email);
        Assert.Equal(apiKey, metadata.ApiKey);
    }

    [Fact]
    public void FromJson_EmptyJson_ThrowsException()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => OrganizzeMetadata.FromJson(json));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "not-valid-json";

        // Act & Assert
        Assert.Throws<JsonException>(() => OrganizzeMetadata.FromJson(json));
    }

    [Fact]
    public void FromJson_NullJson_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsAny<Exception>(() => OrganizzeMetadata.FromJson(null!));
    }

    [Fact]
    public void ToCredentials_ReturnsCredentialsWithMatchingValues()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var apiKey = _faker.Random.AlphaNumeric(32);
        var metadata = new OrganizzeMetadata { Email = email, ApiKey = apiKey };

        // Act
        var credentials = metadata.ToCredentials();

        // Assert
        Assert.Equal(email, credentials.Email);
        Assert.Equal(apiKey, credentials.ApiKey);
    }

    [Fact]
    public void RequiredKeys_ContainsExpectedKeys()
    {
        // Arrange & Act
        var keys = OrganizzeMetadata.RequiredKeys;

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("email", keys);
        Assert.Contains("apiKey", keys);
    }
}
