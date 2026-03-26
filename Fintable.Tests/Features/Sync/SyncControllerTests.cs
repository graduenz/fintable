using Bogus;
using Fintable.Persistence;
using System.Net;

namespace Fintable.Tests.Features.Sync;

public class SyncControllerTests : BaseControllerTests
{
    private readonly Faker _faker = new();

    [Fact]
    public async Task SyncAll_EmptyProviders_ReturnsOk()
    {
        // Arrange (empty DB)

        // Act
        var response = await Client.PostAsync("/v1/sync", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SyncProvider_NonExistentProvider_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Id.New();

        // Act
        var response = await Client.PostAsync($"/v1/sync/{nonExistentId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SyncProvider_ExistingNonOrganizzeProvider_ReturnsOk()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Id.New(),
            Type = "unsupported",
            Name = _faker.Company.CompanyName(),
        };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync($"/v1/sync/{provider.Id}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
