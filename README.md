# fintable

Fintable is a lightweight .NET service that syncs personal finance data from one or more sources, normalizes and stores it locally, and exposes a simple REST API for querying and reporting.

> [!NOTE]
> This project is in active development. Features and APIs may change.

## Architecture

```
Providers → Sync Orchestrator → SQLite (EF Core) → REST API
```

Data is fetched from external providers (currently [Organizze](https://www.organizze.com.br)), stored locally in SQLite, and served through a versioned REST API. The database is not the source of truth — it's a local cache that can be fully rebuilt by re-syncing.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Getting Started

```bash
# Build
dotnet build

# Run
dotnet run --project Fintable/Fintable.csproj

# Test
dotnet test
```

The API starts on `http://localhost:5027` (or `https://localhost:7246`). In development, the interactive Scalar UI is available at `http://localhost:5027/scalar`.

## Configuration

All settings live in `Fintable/appsettings.json` and can be overridden via environment variables using `__` as the separator.

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:Fintable` | `Data Source=fintable.db;` | SQLite connection string |
| `SyncWindow:YearsBack` | `1` | How many years back to sync |
| `SyncWindow:YearsForward` | `5` | How many years forward to sync |

**Environment variable examples:**
```bash
ConnectionStrings__Fintable="Data Source=/data/fintable.db;"
SyncWindow__YearsBack=2
```

## Database

SQLite is used for local storage. The database file is created automatically on first run — no migrations needed. If the file is lost, all data can be restored by re-syncing from providers.

## Provider Setup

### Organizze

1. Obtain your API key from your Organizze account settings.
2. Register the provider:

```http
POST /v1/providers
Content-Type: application/json

{
  "name": "My Organizze",
  "type": "organizze",
  "metadata": {
    "email": "you@example.com",
    "apiKey": "your-api-key"
  }
}
```

3. Verify configuration is valid:

```http
GET /v1/providers/organizze/validate
```

4. Trigger a sync:

```http
POST /v1/sync
```

> Metadata values (secrets) are masked as `**********` in GET responses.

## CI / CD

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push to `main` and on pull requests. It builds, runs tests with code coverage, and reports results to [SonarCloud](https://sonarcloud.io/project/overview?id=graduenz_fintable).

**Required secret:** add a `SONAR_TOKEN` secret in your GitHub repository settings (Settings > Secrets and variables > Actions). Generate the token from SonarCloud under My Account > Security.

## API Reference

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/v1/providers` | List all providers |
| `GET` | `/v1/providers/{id}` | Get provider by ID |
| `POST` | `/v1/providers` | Create a provider |
| `PUT` | `/v1/providers/{id}` | Update a provider |
| `DELETE` | `/v1/providers/{id}` | Delete a provider |
| `GET` | `/v1/providers/{type}/validate` | Validate required metadata for a provider type |
| `POST` | `/v1/sync` | Sync all providers |
| `POST` | `/v1/sync/{providerId}` | Sync a specific provider |
| `GET` | `/v1/reports/stats` | Get stats report |

Full interactive docs available at `/scalar` when running in development.
