using Fintable.Features.Reports;
using Fintable.Features.Sync;
using Fintable.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    // Application services
    .AddScoped<IReportsService, ReportsService>()
    // Sync module
    .AddScoped<ISyncOrchestrator, SyncOrchestrator>();

builder.Services.Configure<SyncWindowOptions>(builder.Configuration.GetSection("SyncWindow"));

builder.Services.AddDbContext<FintableDb>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Fintable")));

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
