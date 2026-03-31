using Fintable.Features.Providers;

namespace Fintable.Tests.Features.Providers;

public class ProviderDtosTests
{
    [Fact]
    public void ToString_ProviderDtoWithNullIdAndMetadata_UsesNullAndEmptyMetadataKeys()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Type = "organizze",
            Name = "Personal",
            Id = null,
            Metadata = null,
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[organizze] [Personal] [Id: null] [Metadata: ]", text);
    }

    [Fact]
    public void ToString_ProviderDtoWithMetadata_ListsMetadataKeys()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Type = "organizze",
            Name = "Personal",
            Id = "provider-1",
            Metadata = new Dictionary<string, string>
            {
                ["email"] = "test@example.com",
                ["apiKey"] = "secret",
            },
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Contains("[Id: provider-1]", text);
        Assert.Contains("email", text);
        Assert.Contains("apiKey", text);
    }

    [Fact]
    public void ToString_ProviderValidateEntryDtoWithMissingKeys_IncludesAllFields()
    {
        // Arrange
        var dto = new ProviderValidateEntryDto
        {
            Id = "provider-1",
            Type = "organizze",
            Name = "Main",
            IsFullySetUp = false,
            MissingKeys = ["email", "apiKey"],
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[organizze] [Main] [Id: provider-1] [FullySetUp: False] [MissingKeys: email, apiKey]", text);
    }

    [Fact]
    public void ToString_ProviderValidateResultDto_ListsRequiredAndProviderKeys()
    {
        // Arrange
        var dto = new ProviderValidateResultDto
        {
            IsFullySetUp = true,
            RequiredKeys = ["email", "apiKey"],
            Providers = new Dictionary<string, ProviderValidateEntryDto>
            {
                ["provider-1"] = new()
                {
                    Id = "provider-1",
                    Type = "organizze",
                    Name = "Main",
                    IsFullySetUp = true,
                    MissingKeys = [],
                },
            },
        };

        // Act
        var text = dto.ToString();

        // Assert
        Assert.Equal("[FullySetUp: True] [RequiredKeys: email, apiKey] [Providers: provider-1]", text);
    }
}
