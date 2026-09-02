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

## Repository Relationships

- [Frontend](https://github.com/ordreflow/ordreflow-frontend)
- [Shared documentation](https://github.com/ordreflow/ordreflow-docs)
- [GitHub Project](https://github.com/orgs/ordreflow/projects)

## Planned Architecture

The backend should keep these responsibilities separate:

- API layer: HTTP endpoints and request handling
- Application layer: use cases and orchestration
- Domain layer: entities and business rules
- Infrastructure layer: EF Core, PostgreSQL, and external services

Frontend components should not access `DbContext` directly. The API should expose DTOs rather than database entities.

## Current Status

This repository is currently in the initial setup phase. The project structure, local run instructions, database configuration, migration commands, and test commands will be added when the API is scaffolded.

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
