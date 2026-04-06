# Authentication And Multitenancy

Joura uses a simple cookie-based login flow intended for local development and demo usage.

## Login Flow

The login UI is the page at `/login`.

- The page loads users directly from the database
- Users are grouped by tenant
- Submitting a login form posts the selected email to `/auth/login`

The login endpoint lives in `src/Joura.Web/Program.cs`.

It:

1. looks up the selected user by email
2. loads that user's tenant
3. creates a cookie principal with user and tenant claims
4. redirects back to the requested page or `/`

There is no password validation in the current implementation.

## Claims Issued At Login

The cookie principal contains:

- `ClaimTypes.NameIdentifier`
- `ClaimTypes.Name`
- `ClaimTypes.Email`
- `ClaimTypes.Role`
- `joura:tenant_id`
- `joura:tenant_name`
- `joura:tenant_key`

Custom claim names are defined in `src/Joura.Application/Abstractions/AuthClaimTypes.cs`.

## How Current User Resolution Works

`HttpCurrentUserContext` in `src/Joura.Infrastructure/Services/HttpCurrentUserContext.cs` resolves the current principal from:

1. `IHttpContextAccessor` when available
2. `AuthenticationStateProvider` as fallback

It exposes:

- `UserId`
- `TenantId`
- `UserName`
- `Email`
- `Principal`

## Tenant Isolation Model

Tenant isolation is enforced mainly in the service layer.

`WorkTrackingService` reads the current tenant from claims and applies tenant filters to queries and mutations. Typical patterns:

- projects are filtered by `TenantId`
- issues are filtered through `Issue.Project.TenantId`
- statuses are filtered through `Status.Project.TenantId`
- worklogs are filtered through `WorklogEntry.Issue.Project.TenantId`

This is explicit application-level isolation, not database-level row security.

## What To Preserve When Adding Features

When you add a new query or command:

1. resolve the current tenant from `ICurrentUserContext`
2. filter data by tenant before returning or mutating it
3. validate related entities belong to the same tenant
4. avoid exposing cross-tenant identifiers in UI or APIs

If a new feature touches projects, issues, statuses, users, or worklogs and does not enforce tenant scope, treat that as a bug.

## Known Limitations

- Demo login has no passwords
- Authorization is broad once authenticated
- Tenant isolation depends on service-layer discipline
- There is no external identity provider integration yet
