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
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.PostAsync("/v1/sync", null, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SyncProvider_NonExistentProvider_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Id.New();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.PostAsync($"/v1/sync/{nonExistentId}", null, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SyncProvider_ExistingNonOrganizzeProvider_ReturnsOk()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = new Provider
        {
            Id = Id.New(),
            Type = "unsupported",
            Name = _faker.Company.CompanyName(),
        };
        Db.Providers.Add(provider);
        await Db.SaveChangesAsync(cancellationToken);

        // Act
        var response = await Client.PostAsync($"/v1/sync/{provider.Id}", null, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
