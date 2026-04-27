# AI Instructions — Fintable

This is the central entrypoint for all AI agent context in the Fintable project. Every agent operating on this codebase must read and follow all documents listed in the Context Map below before taking any action.

---

## Context Map

| File | Purpose |
|---|---|
| [`project.md`](./project.md) | Fintable project overview: architecture, backend rules, database design, providers (Organizze/NOrganizze), project structure, and breaking-change policy |
| [`dotnet.md`](./dotnet.md) | .NET/C# coding standards: naming conventions, code style, API design, EF Core usage, performance, and tooling |
| [`testing.md`](./testing.md) | Testing conventions: test naming, AAA pattern, integration-test assertion layers, coverage workflow, and build verification |

---

## Core Mandate

Fintable is a **lightweight .NET service** that syncs personal finance data from one or more external providers, normalizes it, persists it locally in SQLite via Entity Framework Core, and exposes a simple REST API for querying and reporting.

### Guiding principles

- **Simplicity over complexity** — no DDD layers, no repository pattern, no authentication.
- **Providers as the source of truth** — the local SQLite database is a cache/projection; it can always be rebuilt from the provider.
- **Pragmatic .NET** — idiomatic C# 10+, ASP.NET Core conventions, EF Core directly, Mapster for mapping, `System.Text.Json` with string enum conversion.
- **Test correctness end-to-end** — integration tests must verify HTTP status, response payload, and database state together.
- **UTC everywhere** — always use `DateTime.UtcNow`; never `DateTime.Now`.
- **IDs are ULIDs** — every entity `Id` is a `string` ULID created with `Id.New()`.

### Tech stack snapshot

| Layer | Technology |
|---|---|
| Runtime | .NET (C# 10+) |
| Web framework | ASP.NET Core |
| ORM | Entity Framework Core |
| Database | SQLite |
| Mapping | Mapster |
| Serialization | System.Text.Json (`JsonStringEnumConverter`) |
| Testing | xUnit v3 · Moq · Bogus |
| API docs | Scalar / OpenAPI (`Scalar.AspNetCore`) |
| Finance provider | Organizze via `NOrganizze` NuGet package |

---

## Documentation Protocol

The `.ai/` directory is a **living specification**. It must stay in sync with the actual codebase at all times. Agents are responsible for maintaining and evolving these documents as the project grows.

### When to update

| Trigger | Action |
|---|---|
| New feature added | Update `project.md` (structure, providers, or rules) and/or `testing.md` if new test patterns are introduced |
| New tech/package adopted | Update the relevant file(s) and the tech stack table in `instructions.md` |
| Coding convention changed | Update `dotnet.md` |
| New provider integrated | Add a section to `project.md` under **Providers** |
| Test strategy or tooling changed | Update `testing.md` |
| Large refactor or architectural shift | Review and update all affected `.ai/` files; update the Context Map if new files are added |

### Rules for agents

1. **Never let the codebase and `.ai/` fall out of sync.** If you make a change that contradicts or extends something documented here, update the relevant `.ai/` file in the same work unit.
2. **Add, don't replace, unless the old content is wrong.** Prefer appending new sections or rows over deleting existing guidance.
3. **New document, new Context Map entry.** If you create a new `.ai/*.md` file, add it to the Context Map table in this file.
4. **Keep language concise and actionable.** These documents are instructions for agents, not prose documentation for humans.
5. **Cross-reference when related.** If a rule in `dotnet.md` is tightly coupled to a rule in `project.md`, add a brief note in both files pointing to the other.
