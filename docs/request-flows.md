# Request Flows

This document explains how the main user actions move through the current codebase.

## Mental Model

Most Blazor pages are thin.

The usual flow is:

1. a page gathers user input
2. the page calls `IWorkTrackingService`
3. `WorkTrackingService` resolves the current tenant and user
4. the service reads or mutates `JouraDbContext`
5. the service returns DTOs to the page

For write operations, the service often also creates:

- an `AuditEvent`
- a `Notification` when relevant

## Example: Open The Board

Files involved:

- `src/Joura.Web/Components/Pages/Board.razor`
- `src/Joura.Infrastructure/Services/WorkTrackingService.cs`

Flow:

1. `Board.razor` loads projects and users
2. it chooses a project from the query string or first available project
3. it calls `GetBoardAsync(projectId)`
4. the service loads statuses for that project within the current tenant
5. the service loads issue summaries for the tenant
6. the service groups issues into kanban columns
7. the page renders draggable columns and tickets

## Example: Move An Issue On The Board

Files involved:

- `src/Joura.Web/Components/Pages/Board.razor`
- `src/Joura.Infrastructure/Services/WorkTrackingService.cs`

Flow:

1. the user drags a ticket into another column
2. the page calls `MoveIssueAsync`
3. the service verifies the issue belongs to the current tenant
4. the service verifies the target status belongs to the same project
5. the issue status is updated
6. `UpdatedUtc` is changed
7. an `AuditEvent` is written
8. a `Notification` is written for the assignee when present

## Example: Create Or Edit An Issue

Files involved:

- `src/Joura.Web/Components/Pages/Board.razor`
- `src/Joura.Web/Components/Pages/IssueDetails.razor`
- `src/Joura.Application/Models/Commands.cs`
- `src/Joura.Infrastructure/Services/WorkTrackingService.cs`

Flow:

1. the page builds a command record
2. the service validates project and assignee tenant membership
3. issue fields are written to the database
4. labels are normalized and created on demand when missing
5. label links are created or replaced
6. an audit event is recorded

On create, the service also:

- assigns the default project status
- generates the issue key
- may create an assignment notification

## Example: Log Time

Files involved:

- `src/Joura.Web/Components/Pages/Worklogs.razor`
- `src/Joura.Web/Components/Pages/IssueDetails.razor`
- `src/Joura.Infrastructure/Services/WorkTrackingService.cs`

Flow:

1. a page collects start and end times
2. the service validates the interval
3. the worklog entry is inserted
4. the parent issue `UpdatedUtc` is changed
5. an audit event is recorded

Update and delete follow the same pattern.

## Example: Download A Worklog PDF

Files involved:

- `src/Joura.Web/Components/Pages/Reports.razor`
- `src/Joura.Web/Program.cs`
- `src/Joura.Infrastructure/Services/WorkTrackingService.cs`
- `src/Joura.Web/Services/WorklogReportPdfService.cs`

Flow:

1. the report page lets the user select worklog rows
2. the page navigates to `/reports/worklogs.pdf?ids=...`
3. the endpoint loads a grouped report from `IWorkTrackingService`
4. `IWorklogReportPdfService` renders the PDF
5. the response returns a downloadable file
