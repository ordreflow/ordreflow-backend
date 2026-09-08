# OrdreFlow Backend

The backend provides the HTTP/REST API for OrdreFlow, an internal time-registration and order-management system.

## Responsibilities

The backend is responsible for:

- Exposing the API used by the frontend
- Validating incoming requests
- Enforcing authorization and access rules
- Applying business rules
- Managing domain entities
- Persisting data through Entity Framework Core
- Managing PostgreSQL database migrations
- Returning stable request and response DTOs
- Providing API documentation through OpenAPI/Swagger

The backend is not responsible for:

- Rendering the user interface
- Storing frontend state
- Trusting employee or role values supplied directly by the frontend

## Planned Technology

- C# and .NET
- ASP.NET Core Web API
- Entity Framework Core
- Npgsql PostgreSQL provider
- PostgreSQL
- HTTP/REST and JSON
- OpenAPI/Swagger

The complete stack is documented in the shared [technology stack documentation](https://github.com/ordreflow/ordreflow-docs/blob/main/docs/technology-stack.md).
The shared [development environment documentation](https://github.com/ordreflow/ordreflow-docs/blob/main/docs/development-environment.md)
describes the Flox and WSL conventions.

## Repository Relationships

- [Frontend](https://github.com/ordreflow/ordreflow-frontend)
- [Shared documentation](https://github.com/ordreflow/ordreflow-docs)
- [GitHub Project](https://github.com/orgs/ordreflow/projects)

## Project Structure

- `Backend/Backend.sln` — backend solution
- `Backend/WebApi/` — ASP.NET Core API
- `Backend/Persistence/` — EF Core context, repositories, and migrations
- `.flox/env/manifest.toml` — Flox environment definition
- `.flox/env/manifest.lock` — locked Flox package resolution
- `.config/dotnet-tools.json` — repository-local .NET tools
- `global.json` — required .NET SDK version

## Planned Architecture

The backend should keep these responsibilities separate:

- API layer: HTTP endpoints and request handling
- Application layer: use cases and orchestration
- Domain layer: entities and business rules
- Infrastructure layer: EF Core, PostgreSQL, and external services

Frontend components should not access `DbContext` directly. The API should expose DTOs rather than database entities.

## Current Status

The API project, initial persistence setup, and first EF Core migration are
present. Database configuration, local service startup, and test commands are
still being completed.

## Running Locally

Install [Flox](https://flox.dev/docs/install-flox/install/) before setting up
the repository. The committed Flox environment provides .NET SDK `8.0.130`.
The root `global.json` keeps the .NET CLI on that version.

From the repository root, activate the environment:

```bash
flox activate
```

Run the remaining commands inside the activated shell:

```bash
dotnet --version
dotnet tool restore
dotnet restore Backend/Backend.sln
dotnet build Backend/Backend.sln
dotnet run --project Backend/WebApi
```

The version check should print `8.0.130`. The API's HTTP and HTTPS URLs are
listed in `Backend/WebApi/Properties/launchSettings.json`; Swagger is available
at `/swagger` when the API is running in Development.

The repository-local `dotnet-ef` tool is pinned to `8.0.13` to match the EF Core
design package. After database environment variables and a local PostgreSQL
instance are configured, migrations can be inspected or applied with commands
such as:

```bash
dotnet ef migrations list --project Backend/Persistence --startup-project Backend/WebApi
dotnet ef database update --project Backend/Persistence --startup-project Backend/WebApi
```

The backend currently expects `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USERNAME`,
and `DB_PASSWORD`. A Compose file and a documented local database setup will be
added separately.

## Updating the Development Environment

Use Flox from the repository root when changing environment packages:

```bash
flox search <package>
flox install <package>
```

Commit changes to `.flox/env/manifest.toml` and `.flox/env/manifest.lock`
together. Restore repository-local .NET tools with `dotnet tool restore` inside
the activated Flox environment. Flox runtime, cache, log, and telemetry files
are local-only.

## Planned POC Endpoints

The first proof of concept is expected to use endpoints similar to:

```text
GET  /api/orders
POST /api/time-entries
GET  /api/time-entries
```

The final request and response formats will be defined in the API contract documentation and represented by DTOs in the backend.

## Database Notes

- PostgreSQL is the database system.
- EF Core migrations must be committed with the backend code.
- Connection strings and credentials must be supplied through environment-specific configuration.
- Secrets must not be committed to the repository.
- The POC should use real PostgreSQL persistence rather than only an in-memory test database.
