using Bogus;
using Fintable.Features.Sync;
using Fintable.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Fintable.Tests.Features.Sync;

public class SyncControllerTests : BaseControllerTests
{
    private readonly Faker _faker = new();
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    [Fact]
    public async Task SyncAll_EmptyProviders_ReturnsOk()
    {
        // Arrange (empty DB)
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await Client.PostAsync("/v1/sync", null, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<SyncExecutionResultDto>(JsonOptions, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Empty(payload.SyncedProviders);
        Assert.Single(payload.WarningGroups);
        Assert.Equal(SyncWarningCodes.NoProvidersToSync, payload.WarningGroups[0].Code);
        Assert.Equal(1, payload.WarningGroups[0].Count);
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
        var payload = await response.Content.ReadFromJsonAsync<SyncExecutionResultDto>(JsonOptions, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Single(payload.SyncedProviders);
        Assert.Equal(provider.Id, payload.SyncedProviders[0].Id);
        Assert.Equal(SyncProviderOutcome.Skipped, payload.SyncedProviders[0].Outcome);
        Assert.Single(payload.WarningGroups);
        Assert.Equal(SyncWarningCodes.ProviderTypeNotSupportedSkipped, payload.WarningGroups[0].Code);
        Assert.Equal(1, payload.WarningGroups[0].Count);
    }

    [Fact]
    public async Task SyncProvider_ExistingNonOrganizzeProvider_SerializesOutcomeAsEnumText()
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
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = JsonNode.Parse(rawResponse);
        var outcomeText = json?["syncedProviders"]?[0]?["outcome"]?.GetValue<string>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Skipped", outcomeText);
    }
}
