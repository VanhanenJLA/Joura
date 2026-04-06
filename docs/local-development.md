# Local Development

This guide is for running the current codebase locally with the default PostgreSQL setup.

## Prerequisites

- .NET 8 SDK
- Docker

## Start The Database

```bash
docker compose up -d
```

This starts PostgreSQL 16 using the configuration in `compose.yaml`.

Default local connection string:

```text
Host=localhost;Port=5432;Database=joura;Username=joura;Password=joura
```

## Apply The Schema

```bash
dotnet ef database update --project src/Joura.Infrastructure --startup-project src/Joura.Web
```

PostgreSQL is the main migration-backed provider in this repo.

## Run The App

```bash
dotnet run --project src/Joura.Web
```

The web host will:

- start the Blazor app
- apply migrations automatically for PostgreSQL
- seed demo tenants, users, projects, issues, comments, audit events, and worklogs

## First Login

Open `/login` and choose any seeded user.

There are no passwords in the current demo authentication flow.

## Reset The Local Database

If your local schema is out of sync or you want a clean seed:

```bash
docker compose down -v
docker compose up -d
dotnet ef database update --project src/Joura.Infrastructure --startup-project src/Joura.Web
```

## Provider Notes

The app supports both `PostgreSql` and `SqlServer` through configuration.

Current differences:

- PostgreSQL uses migrations
- SQL Server uses `EnsureCreated()`

If you are developing features locally, use PostgreSQL unless you have a specific reason not to.

## Troubleshooting

### The app starts but login shows no users

Check that:

- the database is reachable
- startup completed seeding successfully
- you are using the same database configured in `src/Joura.Web/appsettings.Development.json`

### Migrations fail against an old local database

Reset the database volume and reapply migrations using the reset steps above.

### I changed the domain model

For PostgreSQL development:

1. Add a migration under `src/Joura.Infrastructure/Persistence/Migrations`
2. Apply it locally
3. Verify the app still starts and seeds correctly
