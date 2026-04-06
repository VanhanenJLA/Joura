# Contributing

This codebase is small, but there are a few structural rules worth preserving.

## Where Changes Should Go

### Domain

Put pure business model concepts in `src/Joura.Domain`.

Examples:

- entities
- enums
- simple model-level invariants

### Application

Put application-facing contracts in `src/Joura.Application`.

Examples:

- command records
- DTOs
- service interfaces

### Infrastructure

Put persistence and runtime implementations in `src/Joura.Infrastructure`.

Examples:

- `JouraDbContext`
- EF Core mappings
- migrations
- `WorkTrackingService`
- current-user resolution

### Web

Put UI, routing, and startup composition in `src/Joura.Web`.

Pages should stay relatively thin. If you are adding business rules, they usually belong in a service, not in a Razor component.

## Tenant Safety Rule

Any feature that reads or mutates tenant-owned data must be tenant-scoped.

Before merging a change, check:

- does it resolve the current tenant?
- does it filter or validate by tenant?
- could it accidentally expose cross-tenant data?

## Schema Changes

If you change the EF model for the PostgreSQL path:

1. create a migration
2. apply it locally
3. verify startup still works
4. verify seed data still succeeds

Remember that SQL Server currently does not use the PostgreSQL migration set.

## Adding New Features

A good default path is:

1. add or update DTOs and commands in `Joura.Application`
2. implement the behavior in `WorkTrackingService`
3. update the relevant page in `Joura.Web`
4. add or adjust migrations if the schema changed
5. test the feature with seeded tenants and users

## Documentation Expectations

Update docs when you change:

- local setup steps
- authentication behavior
- tenant isolation assumptions
- entity relationships
- request flow for a major feature

Prefer docs that describe the current codebase as it exists today.
