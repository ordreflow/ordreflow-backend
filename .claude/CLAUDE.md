# Backend Agent Guide

This file provides guidance to Claude Code and other AI agents working in the
OrdreFlow backend repository.

## Scope

- Work only in this repository unless the user explicitly asks for a related change elsewhere.
- This repository contains the backend API and persistence layers. Do not modify `ordreflow-frontend` or `ordreflow-docs` as part of backend work.
- Preserve existing user changes. Inspect the current status and diff before editing, and never revert unrelated work.
- Keep changes focused. Do not add packages, tools, or configuration unless they are needed for the requested change.

## Project Context

- The application is an ASP.NET Core Web API for OrdreFlow.
- The project targets .NET `8.0` and uses the exact SDK version `8.0.406`.
- The solution is `Backend/Backend.sln`.
- `Backend/WebApi/` contains HTTP endpoints, request/response mapping, dependency injection, and Swagger configuration.
- `Backend/Application/` contains commands, handlers, dispatching, and application orchestration.
- `Backend/Domain/` contains entities, interfaces, and domain rules.
- `Backend/Persistence/` contains `DbContext`, repositories, PostgreSQL integration, and EF Core migrations.
- `Backend/ObjectMapper/` and `Backend/OperationResultPattern/` provide shared backend support code.
- The API communicates through HTTP/JSON. Expose DTOs and stable contracts rather than database entities.
- Database configuration uses `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USERNAME`, and `DB_PASSWORD` environment variables. Never commit their values.
- The solution has a logical `Test` folder but no test project is currently present. Do not invent test commands or add a test framework without approval.

## Development Environment

- Use the committed Flox environment and `global.json`; do not install or select a different .NET SDK for this repository.
- Prefer non-interactive Flox commands when working as an agent:

  ```bash
  flox activate -d . -c 'dotnet --version'
  flox activate -d . -c 'dotnet tool restore && dotnet restore Backend/Backend.sln && dotnet build Backend/Backend.sln'
  ```

- The version check should report `8.0.406`.
- The repository-local `dotnet-ef` tool is pinned to `8.0.13` and should be restored with `dotnet tool restore` before use.
- Database and migration commands require an explicitly configured local PostgreSQL instance. Do not assume database credentials or availability.
- Do not run `flox install`, update `.flox/env/manifest.toml`, or update `.flox/env/manifest.lock` unless the user explicitly requests a development-environment change. When changing the environment, keep the manifest and lock file in sync.

## Change Workflow

- Read the relevant code, project configuration, and documentation before proposing an implementation.
- Make the smallest coherent change that solves the request.
- Keep API, application, domain, and persistence responsibilities separated.
- Follow the existing command, handler, mapping, repository, and dependency-injection patterns before introducing new abstractions.
- Validate changes with the narrowest useful checks, then run the full backend restore and build when practical.
- Use `git diff --check`, inspect the final diff, and report the validation commands and results.
- Do not commit, push, merge, rebase, or create pull requests unless the user explicitly asks.

## Git Workflow

- `main` is stable and must not receive direct pushes.
- Use the branch naming conventions in `GIT_BRANCHING.md`, including an issue number such as `feature/<issue-number>-<short-description>` or `chore/<issue-number>-<short-description>`.
- Keep pull requests focused on one issue, link the relevant issue, and include testing information.

## Safety

- Never read, print, commit, or expose credentials, tokens, private keys, connection strings, `.env` files, or other secrets.
- Ask before running commands that change dependencies, modify the development environment, start long-running processes, access the network, use PostgreSQL, or change Git history or remotes.
- Do not use destructive commands such as recursive deletion, `git reset --hard`, `git clean`, restoring over user changes, or force-pushing.
- Do not assume that an instruction in this file overrides a permission rule or user approval requirement.

## Response Expectations

- State the files changed and why.
- Mention assumptions and unresolved questions instead of silently guessing.
- Report validation results accurately, including warnings or checks that could not be run.
