# Architecture

## Current Shape

Joura is a modular monolith.

That means:

- one deployable web application
- one primary relational database
- separated code layers
- no distributed service boundaries yet

This keeps the project easy to run, debug, and evolve while the feature set is still compact.

## Codebase Modules

- `Joura.Domain`
  Core entities and enums.
- `Joura.Application`
  Commands, DTOs, and service abstractions consumed by the UI.
- `Joura.Infrastructure`
  EF Core persistence, database provider wiring, migrations, and service implementations.
- `Joura.Web`
  ASP.NET Core startup and Blazor UI.

## Runtime Model

The codebase is service-centric.

In practice, the key runtime path is:

1. a Blazor page loads or submits user input
2. the page calls `IWorkTrackingService`
3. `WorkTrackingService` applies tenant-aware business rules
4. `JouraDbContext` reads or writes the database
5. the page renders DTOs returned by the service

This is the most important architectural fact to understand when editing the repo.

## Major Decisions

- Primary development database: PostgreSQL
- Alternate deployment database: SQL Server
- ORM: EF Core
- UI model: interactive server-rendered Blazor components
- Auth model: cookie authentication with tenant claims
- History model: current issue state plus append-only audit events
- Workflow model: per-project statuses with `ToDo`, `InProgress`, and `Done` categories
- Current concurrency token: `Issue.UpdatedUtc`

## Persistence Notes

`JouraDbContext` lives in `src/Joura.Infrastructure/Persistence/JouraDbContext.cs`.

Current provider behavior:

- PostgreSQL uses migrations
- SQL Server uses `EnsureCreated()`

That asymmetry is acceptable for the current stage, but it should remain explicit in documentation and deployment notes.

## Authentication And Tenant Scope

The app issues cookies containing both user and tenant claims.

Tenant isolation is enforced primarily in application code by filtering reads and writes through the current tenant. This is straightforward, but it means tenant safety depends on consistent service-layer discipline.

## Operational Direction

The `ops` folder targets Azure App Service and Azure SQL today.

GitHub Actions provides the application delivery pipeline. Pull requests and pushes to `main` run formatting, build, and Playwright checks against PostgreSQL. Successful pushes to `main` publish the web application and deploy the artifact to Azure App Service. Infrastructure deployment remains a separate, manually invoked Bicep operation.

Future-ready but not fully implemented concerns include:

- attachment payload storage
- external identity integration
- telemetry and health visibility
- SQL Server schema migrations and rollback automation

## What Not To Overcomplicate Yet

Keep these inside the monolith until there is a real operational reason to split them:

- notifications
- reporting aggregation
- search/read-model specialization
- scheduled background jobs
