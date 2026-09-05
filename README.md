# Joura

[![CI/CD](https://github.com/VanhanenJLA/Joura/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/VanhanenJLA/Joura/actions/workflows/ci-cd.yml)

Joura is a small Jira-style issue tracking practice project built as a modular monolith with Blazor, ASP.NET Core, EF Core, and PostgreSQL by default.

The project is intentionally simple:

- one web app
- one database
- one main application service
- tenant-scoped data access
- enough seeded demo data to make the product usable on first boot

## What The App Does

Joura currently supports:

- tenant-scoped login using seeded demo users
- a home dashboard
- a kanban board with drag-and-drop status changes
- issue search and bulk updates
- issue detail pages with comments, worklogs, and audit history
- weekly worklog entry and rescheduling
- worklog reporting with PDF export
- a due-date calendar

## Solution Structure

- `src/Joura.Domain`
  Domain entities and enums.
- `src/Joura.Application`
  DTOs, commands, and application-facing abstractions.
- `src/Joura.Infrastructure`
  EF Core persistence, migrations, current-user resolution, and service implementations.
- `src/Joura.Web`
  Blazor UI and the ASP.NET Core composition root.
- `docs`
  Focused developer documentation.
- `ops`
  Azure deployment manifests and notes.

Start with these files if you are new:

1. `src/Joura.Web/Program.cs`
2. `src/Joura.Infrastructure/Services/WorkTrackingService.cs`
3. `src/Joura.Infrastructure/Persistence/JouraDbContext.cs`
4. `src/Joura.Web/Components/Pages`

## Local Development

1. Start PostgreSQL:

```bash
docker compose up -d
```

2. Apply the database schema:

```bash
dotnet ef database update --project src/Joura.Infrastructure --startup-project src/Joura.Web
```

3. Run the web app:

```bash
dotnet run --project src/Joura.Web
```

Default local configuration lives in `src/Joura.Web/appsettings.Development.json`.

## Demo Login Users

The app seeds two tenants with demo users on startup.

### Demo Company

- Avery Architect: `avery@joura.local`
- Priya PM: `priya@joura.local`
- Devon Developer: `devon@joura.local`
- Quinn QA: `quinn@joura.local`

### Itsu Company

- Nina Ninja: `nina@joura.local`
- Paul Product: `paul@joura.local`
- Mika Maker: `mika@joura.local`

The login page lets you sign in as any seeded user without a password.

## Architecture In One Minute

The app is a modular monolith.

- Blazor pages call `IWorkTrackingService`
- `WorkTrackingService` performs tenant-scoped reads and writes
- tenant and user identity come from auth claims
- EF Core persists the model through `JouraDbContext`
- issue changes usually write both current state and an audit event

This matters because most business logic is not in the pages. It is concentrated in `WorkTrackingService`.

## Database Providers

Two providers are supported in configuration:

- `PostgreSql`
- `SqlServer`

Current behavior:

- PostgreSQL uses EF Core migrations on startup
- SQL Server uses `EnsureCreated()` on startup

That means PostgreSQL is the primary development path today.

## Documentation Map

- `docs/architecture.md`: current codebase structure and decisions
- `docs/local-development.md`: local setup, database workflow, and troubleshooting
- `docs/auth-and-multitenancy.md`: login flow, claims, and tenant isolation model
- `docs/request-flows.md`: how key user actions move through the system
- `docs/data-model.md`: entities, relationships, and persistence rules
- `docs/contributing.md`: where to place changes and what conventions matter
- `ops/README.md`: Azure deployment notes
- `.github/workflows/ci-cd.yml`: build, test, publish, and Azure deployment pipeline

## UI Tests

The repo now includes a minimal Playwright-based UI suite in `tests/Joura.UiTests`.

Current coverage is intentionally small:

- login through the real `/login` page
- create an issue from the board and add a comment
- select worklog rows and download the PDF report

Before running the suite:

1. start PostgreSQL with `docker compose up -d`
2. install Playwright browsers

Example commands:

```bash
dotnet restore tests/Joura.UiTests/Joura.UiTests.csproj
pwsh tests/Joura.UiTests/bin/Debug/net8.0/playwright.ps1 install --with-deps
dotnet test tests/Joura.UiTests/Joura.UiTests.csproj
```

The test fixture creates an isolated PostgreSQL database per test run, starts the real web app, and drives it through Playwright.
