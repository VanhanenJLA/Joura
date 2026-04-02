# Architecture Notes

## Style

The solution starts as a modular monolith. This keeps deployment, debugging, and transactional consistency simple while preserving domain boundaries that can be extracted later if the project earns that complexity.

## Logical modules

- `Joura.Domain`: issue tracking model, workflow status model, comments, notifications, and audit trail
- `Joura.Application`: use-case contracts and DTOs consumed by the web layer
- `Joura.Infrastructure`: PostgreSQL persistence, EF Core mapping, and service implementations
- `Joura.Web`: Blazor UI and composition root

## Main decisions

- Primary relational store: PostgreSQL
- ORM: EF Core with Npgsql
- UI: Blazor Server-style interactive components
- Audit model: current issue state plus append-only audit events
- Workflow: per-project status definitions with simple categories (`ToDo`, `InProgress`, `Done`)
- Concurrency: row-version field on `Issue`

## Azure target

- Azure App Service: web app hosting
- Azure Database for PostgreSQL: primary relational data
- Azure Blob Storage: attachment payloads
- Azure Key Vault: secret storage
- Application Insights: tracing, exceptions, and performance telemetry
- Optional Azure Functions later: notification delivery or scheduled jobs

## Delivery path

### Phase 1

- Solution structure
- Core issue/project domain
- PostgreSQL persistence
- Dashboard, backlog, board, issue detail UI

### Phase 2

- Cookie-based authentication with tenant-aware access
- Issue editing flow
- Notification delivery channel
- Better filtering and saved searches
- EF Core migrations instead of `EnsureCreated`

### Phase 3

- Azure deployment manifests or IaC
- App Insights and health checks
- Key Vault integration
- Blob-backed attachments
- CI/CD pipeline

## Extraction candidates

Keep these inside the monolith until load, ownership, or operational concerns justify separation:

- Notifications
- Search/read-model optimization
- Reporting/dashboard aggregation
