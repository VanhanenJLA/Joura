# Data Model

This is a practical summary of the current persistence model. For exact mapping rules, see `src/Joura.Infrastructure/Persistence/JouraDbContext.cs`.

## Core Entities

### Tenant

Top-level isolation boundary.

- has many users
- has many projects

### AppUser

A tenant member who can report, be assigned, comment, and log work.

- belongs to one tenant
- has a role
- email is unique within a tenant

### Project

Container for issues and workflow statuses.

- belongs to one tenant
- has many issues
- has many statuses
- key is unique within a tenant

### IssueStatus

Project-specific workflow column.

- belongs to one project
- categories are `ToDo`, `InProgress`, or `Done`
- name is unique within a project

### Issue

The main tracked work item.

- belongs to one project
- has one status
- has one reporter
- may have one assignee
- may have many labels, comments, worklogs, audit events, and attachments

Important fields:

- `Key`
- `Title`
- `Description`
- `Type`
- `Priority`
- `DueDate`
- `CreatedUtc`
- `UpdatedUtc`

Current concurrency behavior uses `UpdatedUtc` as the EF Core concurrency token.

### Label And IssueLabel

Labels are many-to-many through `IssueLabel`.

- labels are stored as reusable rows
- label names are globally unique in the current model

### Comment

Freeform text attached to an issue.

- belongs to one issue
- belongs to one author

### WorklogEntry

Time logged by a user against an issue.

- belongs to one issue
- belongs to one user
- stores `StartAt` and `EndAt`
- has a check constraint requiring `EndAt > StartAt`

### AuditEvent

Append-only history describing changes around an issue.

- belongs to one issue
- belongs to one actor
- stores an event type and readable description

### Notification

Simple notification row tied to a user and optionally to an issue.

### AttachmentMetadata

Metadata placeholder for attachment storage.

The current codebase defines it in the model, but attachment payload handling is not yet built out.

## Important Relationship Rules

- deleting an issue cascades to worklog entries
- many user relationships use `DeleteBehavior.Restrict`
- status, assignee, and reporter links on issues are restricted deletes

These choices reduce accidental data loss in core history paths.

## Provider Notes

The model currently supports:

- PostgreSQL via Npgsql
- SQL Server via Microsoft SQL Server provider

There are provider-specific type mappings for worklog date-time columns and check constraints.

## Seed Data

Startup seeds:

- two tenants
- multiple users per tenant
- multiple projects
- default workflow statuses
- demo issues
- comments
- worklogs
- audit events

This seed data is important to the developer experience. Do not treat it as throwaway.
