using Bogus;
using Fintable.Features.Providers;
using Fintable.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Fintable.Tests.Features.Providers;

public class ProvidersControllerTests : BaseControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Faker _faker = new();

    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmptyList()
    {
        // Arrange (empty DB)

        // Act
        var response = await Client.GetAsync("/v1/providers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var providers = await response.Content.ReadFromJsonAsync<List<ProviderDto>>(JsonOptions);
        Assert.NotNull(providers);
        Assert.Empty(providers);
    }

    [Fact]
    public async Task GetAll_WithProviders_ReturnsAll()
    {
        // Arrange
        var provider1 = new Provider { Id = Id.New(), Type = ProviderType.Organizze, Name = _faker.Company.CompanyName() };
        var provider2 = new Provider { Id = Id.New(), Type = ProviderType.Organizze, Name = _faker.Company.CompanyName() };
        Db.Providers.AddRange(provider1, provider2);
        await Db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/v1/providers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var providers = await response.Content.ReadFromJsonAsync<List<ProviderDto>>(JsonOptions);
        Assert.NotNull(providers);
        Assert.Equal(2, providers.Count);
    }

    [Fact]
    public async Task Get_ExistingProvider_ReturnsProvider()
    {
        // Arrange
        var provider = new Provider { Id = Id.New(), Type = ProviderType.Organizze, Name = _faker.Company.CompanyName() };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/v1/providers/{provider.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ProviderDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(provider.Id, dto.Id);
        Assert.Equal(provider.Name, dto.Name);
        Assert.Equal(provider.Type, dto.Type);
    }

    [Fact]
    public async Task Get_NonExistentProvider_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Id.New();

        // Act
        var response = await Client.GetAsync($"/v1/providers/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidProvider_ReturnsCreated()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Id = string.Empty,
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
            Metadata = new Dictionary<string, string>
            {
                ["email"] = _faker.Internet.Email(),
                ["apiKey"] = _faker.Random.AlphaNumeric(32),
            },
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/providers", dto, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ProviderDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.NotEqual(string.Empty, created.Id);
        Assert.Equal(dto.Name, created.Name);
        Assert.Equal(ProviderType.Organizze, created.Type);

        var getResponse = await Client.GetAsync($"/v1/providers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProviderDto>(JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(dto.Name, fetched.Name);
        Assert.Equal(ProviderType.Organizze, fetched.Type);
    }

    [Fact]
    public async Task Create_UnknownType_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ProviderDto
        {
            Id = string.Empty,
            Type = "unknown",
            Name = _faker.Company.CompanyName(),
        };

        // Act
        var response = await Client.PostAsJsonAsync("/v1/providers", dto, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingProvider_ReturnsUpdated()
    {
        // Arrange
        var provider = new Provider { Id = Id.New(), Type = ProviderType.Organizze, Name = _faker.Company.CompanyName() };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync();

        var updatedName = _faker.Company.CompanyName();
        var updateDto = new ProviderDto
        {
            Id = provider.Id,
            Type = ProviderType.Organizze,
            Name = updatedName,
            Metadata = new Dictionary<string, string> { ["email"] = _faker.Internet.Email(), ["apiKey"] = _faker.Random.AlphaNumeric(32) },
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/v1/providers/{provider.Id}", updateDto, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProviderDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(updatedName, updated.Name);

        var getResponse = await Client.GetAsync($"/v1/providers/{provider.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProviderDto>(JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(provider.Id, fetched.Id);
        Assert.Equal(updatedName, fetched.Name);
        Assert.Equal(ProviderType.Organizze, fetched.Type);
    }

    [Fact]
    public async Task Update_NonExistentProvider_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Id.New();
        var dto = new ProviderDto
        {
            Id = nonExistentId,
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/providers/{nonExistentId}", dto, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownType_ReturnsBadRequest()
    {
        // Arrange
        var provider = new Provider { Id = Id.New(), Type = ProviderType.Organizze, Name = _faker.Company.CompanyName() };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync();

        var dto = new ProviderDto
        {
            Id = provider.Id,
            Type = "unknown",
            Name = _faker.Company.CompanyName(),
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/providers/{provider.Id}", dto, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingProvider_ReturnsNoContent()
    {
        // Arrange
        var provider = new Provider { Id = Id.New(), Type = ProviderType.Organizze, Name = _faker.Company.CompanyName() };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/providers/{provider.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Db.ChangeTracker.Clear();
        var deleted = await Db.Providers.FindAsync(provider.Id);
        Assert.Null(deleted);

        var getResponse = await Client.GetAsync($"/providers/{provider.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentProvider_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Id.New();

        // Act
        var response = await Client.DeleteAsync($"/providers/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Validate_UnknownType_ReturnsNotFound()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/providers/unknown/validate");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Validate_FullySetUpProvider_ReturnsIsFullySetUpTrue()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
            Metadata = new Dictionary<string, string>
            {
                ["email"] = _faker.Internet.Email(),
                ["apiKey"] = _faker.Random.AlphaNumeric(32),
            },
        };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/providers/organizze/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProviderValidateResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsFullySetUp);
        Assert.Contains(provider.Id, result.Providers.Keys);
        Assert.True(result.Providers[provider.Id].IsFullySetUp);
        Assert.Empty(result.Providers[provider.Id].MissingKeys);
    }

    [Fact]
    public async Task Validate_MissingMetadata_ReturnsIsFullySetUpFalse()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Id.New(),
            Type = ProviderType.Organizze,
            Name = _faker.Company.CompanyName(),
            Metadata = new Dictionary<string, string>(),
        };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/providers/organizze/validate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProviderValidateResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.False(result.IsFullySetUp);
        Assert.Contains(provider.Id, result.Providers.Keys);
        Assert.False(result.Providers[provider.Id].IsFullySetUp);
        Assert.Contains("email", result.Providers[provider.Id].MissingKeys);
        Assert.Contains("apiKey", result.Providers[provider.Id].MissingKeys);
    }
}
