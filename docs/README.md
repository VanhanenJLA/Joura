# Joura

Joura is a small Jira-style issue tracking practice project built as a modular monolith with Blazor, ASP.NET Core, EF Core, PostgreSQL, and Azure-oriented deployment targets.

## Current baseline

- `src/Joura.Web`: Blazor web host and UI for dashboard, backlog, board, and issue details
- `src/Joura.Application`: application contracts and DTOs
- `src/Joura.Domain`: core entities and enums
- `src/Joura.Infrastructure`: EF Core persistence, PostgreSQL wiring, and work-tracking service implementation

The app applies EF Core migrations and seeds a sample tenant and project on first run so the board and backlog are usable immediately after the database is available.

## Run locally

1. Start PostgreSQL:

```bash
docker compose up -d
```

2. Create or apply the database schema:

```bash
dotnet ef database update --project src/Joura.Infrastructure --startup-project src/Joura.Web
```

3. Run the web app:

```bash
dotnet run --project src/Joura.Web
```

The default connection string is in [appsettings.Development.json](/Users/jouni/repos/VanhanenJLA/Joura/Joura/src/Joura.Web/appsettings.Development.json).

## Database providers

The app supports two configured providers:

- `PostgreSql` for local/default development
- `SqlServer` for Azure SQL-style deployments

Set `DatabaseProvider` and the matching connection string in configuration.

- PostgreSQL uses EF Core migrations on startup.
- SQL Server currently bootstraps schema with `EnsureCreated()` on startup and is intended for deployment environments until a dedicated SQL Server migration set is added.

## Notes

- The app now uses EF Core migrations as the source of truth for schema management.
- If you previously started the app before migrations existed, reset the local database once so migrations can take over cleanly:

```bash
docker-compose down -v
docker-compose up -d
dotnet ef database update --project src/Joura.Infrastructure --startup-project src/Joura.Web
```

- The app now includes cookie-based authentication and tenant-scoped data access; Entra ID integration, Blob attachments, App Insights, and Key Vault remain next-stage concerns and are documented in [architecture.md](/Users/jouni/repos/VanhanenJLA/Joura/Joura/docs/architecture.md).
