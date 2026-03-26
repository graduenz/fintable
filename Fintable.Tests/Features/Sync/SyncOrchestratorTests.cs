using Bogus;
using Fintable.Features.Sync;
using Fintable.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
        _orchestrator = new SyncOrchestrator(_db, syncOptions);
    }

    [Fact]
    public async Task ExecuteAsync_NoProviders_CompletesSuccessfully()
    {
        // Arrange (empty DB)

        // Act
        var exception = await Record.ExceptionAsync(() => _orchestrator.ExecuteAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ExecuteAsync_NonOrganizzeProvider_SkipsSync()
    {
        // Arrange
        var faker = new Faker();
        var provider = new Provider
        {
            Id = Id.New(),
            Type = "unsupported",
            Name = faker.Company.CompanyName(),
        };
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => _orchestrator.ExecuteAsync());

        // Assert
        Assert.Null(exception);
        Assert.Empty(await _db.Accounts.ToListAsync());
    }

    [Fact]
    public async Task ExecuteForProviderAsync_NonExistentProvider_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Id.New();

        // Act
        var found = await _orchestrator.ExecuteForProviderAsync(nonExistentId);

        // Assert
        Assert.False(found);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
