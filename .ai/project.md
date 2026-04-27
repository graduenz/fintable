# Fintable — Project Architecture & Rules

Fintable is a lightweight .NET service that syncs personal finance data from one or more sources, normalizes and stores it locally, and exposes a simple REST API for querying and reporting.

## Architecture

- The project intentionally favors simplicity and pragmatism.
- Main concept: Providers → Sync Orchestrator → [... Sync Services → SQLite (EF Core)] → REST API (reports).
- No DDD layers (no Domain/Application/Infrastructure separation).

## Backend

- Single .NET project for the application code.
- Authentication: no authentication at all.
- Dependency injection: .NET built-in dependency injection configured at `Program.cs`.
- Mapping between types should be done using the `Mapster` NuGet package.
- Dates in UTC by default: always prefer using `DateTime.UtcNow` over `DateTime.Now` to get the current date and time.
- API enum serialization must use enum names (string) instead of numeric values. Keep `System.Text.Json` configured with `JsonStringEnumConverter` in `Program.cs` for controller JSON responses.
- No repository pattern; operations on top of `FintableDb` are the way to go.
- DTOs should be received/returned by the APIs, and not the EF entity itself (such as `ProviderDto`).
- DTOs should include concise `ToString()` overrides for easier debugging and readable logs.
- Prefer `Dictionary<,>` over `IReadOnlyDictionary<,>` and `IDictionary<,>` to avoid code smells and increase performance.

## Database

- The database driver is SQLite for simplicity.
- The database is not the source of truth, only a local projection/cache.
- The actual finance data is fetched from different providers, such as Organizze.
- If the DB is lost, the system can re-sync everything from the providers.
- Database entities have a `string Id`, which is a ULID relying on the `Ulid` NuGet package, that should be created using `Id.New()`.
- Avoid unsupported EF translations in controller queries:
  - Do not use `string.Equals(..., StringComparison.OrdinalIgnoreCase)` inside `IQueryable` LINQ.
  - Use SQL-translatable alternatives (`ToLower()`/`ToUpper()` normalization) or evaluate in-memory after materialization.

## Providers

- Providers implement sync services, such as `OrganizzeSyncService`, that ensures the data is up to date.
- The Organizze provider leverages on the `NOrganizze` NuGet package. It's maintained by the same author of Fintable.
- Providers return DTOs that are mapped to EF entities in the sync service.
- Provider metadata is stored as `Dictionary<string,string>` serialized to JSON using an EF Core ValueConverter (stored as TEXT in SQLite).
- Provider metadata is used to store provider-specific configurations, such as user email and API key to make requests to the provider's APIs.

### Organizze and NOrganizze

In order to understand the NOrganizze documentation and types, you can use the following GitHub repositories:
- https://github.com/graduenz/norganizze → contains the NOrganizze source code and documentation.
- https://github.com/organizze/api-doc → contains the actual Organizze API documentation.

You can even ask to make changes in the NOrganizze project, since it's from the same author.

## Project Structure (under the Fintable project)

| Folder | Purpose |
|---|---|
| `/Features` | Vertical-sliced feature implementations |
| `/Models` | General models for the application, not exclusively persistence models |
| `/Persistence` | Entity Framework related artifacts: DB models and database context |
| `/Organizze` | Artifacts to work with the Organizze provider |

## Breaking Changes

For every breaking change that does not impact an API surface (e.g. changing the database schema or a behavior), there is no need for a migration plan. Simply delete the database and resync the data.
