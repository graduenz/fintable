using Bogus;
using Fintable.Features.Sync;
using Fintable.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fintable.Tests.Features.Sync;

public class SyncOrchestratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FintableDb _db;
    private readonly SyncOrchestrator _orchestrator;

    public SyncOrchestratorTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FintableDb>()
            .UseSqlite(_connection)
            .Options;

        _db = new FintableDb(options);
        _db.Database.EnsureCreated();

        var syncOptions = Options.Create(new SyncWindowOptions());
        _orchestrator = new SyncOrchestrator(
            _db,
            syncOptions,
            NullLogger<SyncOrchestrator>.Instance,
            NullLogger<Fintable.Organizze.OrganizzeSyncService>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_NoProviders_CompletesSuccessfully()
    {
        // Arrange (empty DB)
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _orchestrator.ExecuteAsync(cancellationToken);

        // Assert
        Assert.Empty(result.SyncedProviders);
        Assert.Single(result.WarningGroups);
        Assert.Equal(SyncWarningCodes.NoProvidersToSync, result.WarningGroups[0].Code);
    }

    [Fact]
    public async Task ExecuteAsync_NonOrganizzeProvider_SkipsSync()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var faker = new Faker();
        var provider = new Provider
        {
            Id = Id.New(),
            Type = "unsupported",
            Name = faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _orchestrator.ExecuteAsync(cancellationToken);

        // Assert
        Assert.Single(result.SyncedProviders);
        Assert.Equal(provider.Id, result.SyncedProviders[0].Id);
        Assert.Equal(SyncProviderOutcome.Skipped, result.SyncedProviders[0].Outcome);
        Assert.Single(result.WarningGroups);
        Assert.Equal(SyncWarningCodes.ProviderTypeNotSupportedSkipped, result.WarningGroups[0].Code);
        Assert.Equal(1, result.WarningGroups[0].Count);
        Assert.Empty(await _db.Accounts.ToListAsync(cancellationToken));
    }

    [Fact]
    public async Task ExecuteForProviderAsync_NonExistentProvider_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Id.New();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _orchestrator.ExecuteForProviderAsync(nonExistentId, cancellationToken);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
