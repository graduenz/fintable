using Fintable.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fintable.Tests;

public abstract class BaseControllerTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;

    protected HttpClient Client { get; }
    protected FintableDb Db { get; }

    protected BaseControllerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<FintableDb>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<FintableDb>(opt => opt.UseSqlite(_connection));
            });
        });

        Client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<FintableDb>();
    }

    public async ValueTask InitializeAsync()
    {
        await Db.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose()
    {
        _scope.Dispose();
        Client.Dispose();
        _factory.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
