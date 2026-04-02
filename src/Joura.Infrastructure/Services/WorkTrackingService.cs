using Joura.Application.Abstractions;
using Joura.Application.Models;
using Joura.Domain.Entities;
using Joura.Domain.Enums;
using Joura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Joura.Infrastructure.Services;

public sealed class WorkTrackingService(
    JouraDbContext dbContext,
    ICurrentUserContext currentUserContext) : IWorkTrackingService
{
    public async Task<TenantOverviewDto> GetTenantOverviewAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Include(x => x.Projects)
            .ThenInclude(x => x.Issues)
            .ThenInclude(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return new TenantOverviewDto("No tenant", [], [], 0, 0);
        }

        var recentIssues = (await LoadIssueSummariesAsync(cancellationToken))
            .OrderByDescending(x => x.UpdatedUtc)
            .Take(6)
            .ToList();

        var issueCounts = tenant.Projects.SelectMany(x => x.Issues).ToList();

        var projects = tenant.Projects
            .OrderBy(x => x.Name)
            .Select(project => new ProjectSummaryDto(
                project.Id,
                project.Key,
                project.Name,
                project.Issues.Count,
                project.Issues.Count(issue => issue.Status.Category == IssueStatusCategory.Done)))
            .ToList();

        return new TenantOverviewDto(
            tenant.Name,
            projects,
            recentIssues,
            issueCounts.Count(x => x.Status.Category != IssueStatusCategory.Done),
            issueCounts.Count(x => x.Status.Category == IssueStatusCategory.Done));
    }

    public async Task<IReadOnlyList<ProjectReferenceDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        return await dbContext.Projects
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => new ProjectReferenceDto(x.Id, x.Key, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserReferenceDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.DisplayName)
            .Select(x => new UserReferenceDto(x.Id, x.DisplayName, x.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StatusReferenceDto>> GetStatusesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        return await dbContext.IssueStatuses
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.Project.TenantId == tenantId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new StatusReferenceDto(x.Id, x.Name, x.Category, x.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IssueSummaryDto>> SearchIssuesAsync(IssueFilter filter, CancellationToken cancellationToken = default)
    {
        var query = (await LoadIssueSummariesAsync(cancellationToken)).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim().ToLowerInvariant();
            query = query.Where(x => x.Key.ToLower().Contains(term) || x.Title.ToLower().Contains(term));
        }

        if (filter.ProjectId.HasValue)
        {
            var projectId = filter.ProjectId.Value;
            query = query.Where(x => x.ProjectId == projectId);
        }

        if (filter.AssigneeId.HasValue)
        {
            var assigneeId = filter.AssigneeId.Value;
            query = query.Where(x => x.AssigneeId == assigneeId);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(x => x.StatusCategory == filter.Category.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(x => x.Priority == filter.Priority.Value);
        }

        return query
            .OrderByDescending(x => x.UpdatedUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<KanbanColumnDto>> GetBoardAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var statuses = await dbContext.IssueStatuses
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.Project.TenantId == tenantId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var issues = (await LoadIssueSummariesAsync(cancellationToken))
            .Where(x => x.ProjectId == projectId)
            .ToList();

        return statuses
            .Select(status => new KanbanColumnDto(
                status.Id,
                status.Name,
                status.Category,
                issues.Where(issue => issue.StatusId == status.Id).OrderByDescending(issue => issue.Priority).ToList()))
            .ToList();
    }

    public async Task<IssueDetailDto?> GetIssueAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var issue = await dbContext.Issues
            .AsNoTracking()
            .Include(x => x.Project)
            .Include(x => x.Status)
            .Include(x => x.Assignee)
            .Include(x => x.Reporter)
            .Include(x => x.IssueLabels)
            .ThenInclude(x => x.Label)
            .Include(x => x.Comments.OrderByDescending(comment => comment.CreatedUtc))
            .ThenInclude(x => x.Author)
            .Include(x => x.AuditEvents.OrderByDescending(audit => audit.CreatedUtc))
            .ThenInclude(x => x.Actor)
            .FirstOrDefaultAsync(x => x.Id == issueId && x.Project.TenantId == tenantId, cancellationToken);

        return issue is null
            ? null
            : new IssueDetailDto(
                issue.Id,
                issue.Key,
                issue.Title,
                issue.Description,
                issue.Project.Name,
                issue.Status.Name,
                issue.Status.Category,
                issue.Assignee?.DisplayName,
                issue.Reporter.DisplayName,
                issue.Priority,
                issue.Type,
                issue.IssueLabels.Select(x => new LabelDto(x.LabelId, x.Label.Name, x.Label.Color)).ToList(),
                issue.Comments.Select(x => new CommentDto(x.Author.DisplayName, x.Body, x.CreatedUtc)).ToList(),
                issue.AuditEvents.Select(x => new AuditEventDto(x.Actor.DisplayName, x.EventType, x.Description, x.CreatedUtc)).ToList());
    }

    public async Task<Guid> CreateProjectAsync(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();

        var project = new Project
        {
            TenantId = tenantId,
            Name = command.Name.Trim(),
            Key = command.Key.Trim().ToUpperInvariant(),
            Description = command.Description.Trim()
        };

        dbContext.Projects.Add(project);

        dbContext.IssueStatuses.AddRange(
            new IssueStatus { Project = project, Name = "To Do", Category = IssueStatusCategory.ToDo, SortOrder = 1, IsDefault = true },
            new IssueStatus { Project = project, Name = "In Progress", Category = IssueStatusCategory.InProgress, SortOrder = 2 },
            new IssueStatus { Project = project, Name = "Done", Category = IssueStatusCategory.Done, SortOrder = 3 });

        await dbContext.SaveChangesAsync(cancellationToken);
        return project.Id;
    }

    public async Task<Guid> CreateIssueAsync(CreateIssueCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var actorId = GetUserIdOrThrow();

        var project = await dbContext.Projects
            .Where(x => x.TenantId == tenantId)
            .Include(x => x.Statuses)
            .Include(x => x.Issues)
            .FirstOrDefaultAsync(x => x.Id == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");

        var defaultStatus = project.Statuses.OrderBy(x => x.SortOrder).FirstOrDefault(x => x.IsDefault)
            ?? project.Statuses.OrderBy(x => x.SortOrder).First();

        var assigneeId = command.AssigneeId;
        if (assigneeId.HasValue)
        {
            var assigneeBelongsToTenant = await dbContext.Users
                .AnyAsync(x => x.Id == assigneeId.Value && x.TenantId == tenantId, cancellationToken);
            if (!assigneeBelongsToTenant)
            {
                assigneeId = null;
            }
        }

        var issueNumber = project.Issues.Count + 1;
        var issue = new Issue
        {
            ProjectId = project.Id,
            StatusId = defaultStatus.Id,
            ReporterId = actorId,
            AssigneeId = assigneeId,
            Key = $"{project.Key}-{issueNumber}",
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            Type = command.Type,
            Priority = command.Priority
        };

        foreach (var labelName in command.Labels.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var label = await GetOrCreateLabelAsync(labelName, cancellationToken);
            issue.IssueLabels.Add(new IssueLabel { Issue = issue, Label = label });
        }

        dbContext.Issues.Add(issue);
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Issue = issue,
            ActorId = actorId,
            EventType = "IssueCreated",
            Description = $"Created issue {issue.Key}."
        });

        if (assigneeId.HasValue)
        {
            dbContext.Notifications.Add(new Notification
            {
                Issue = issue,
                UserId = assigneeId.Value,
                Type = NotificationType.IssueAssigned,
                Message = $"You were assigned to {issue.Key}."
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return issue.Id;
    }

    public async Task UpdateIssueAsync(UpdateIssueCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var actorId = GetUserIdOrThrow();

        var issue = await dbContext.Issues
            .Where(x => x.Project.TenantId == tenantId)
            .Include(x => x.IssueLabels)
            .ThenInclude(x => x.Label)
            .FirstOrDefaultAsync(x => x.Id == command.IssueId, cancellationToken)
            ?? throw new InvalidOperationException("Issue not found.");

        var assigneeId = command.AssigneeId;
        if (assigneeId.HasValue)
        {
            var assigneeBelongsToTenant = await dbContext.Users
                .AnyAsync(x => x.Id == assigneeId.Value && x.TenantId == tenantId, cancellationToken);
            if (!assigneeBelongsToTenant)
            {
                assigneeId = null;
            }
        }

        issue.Title = command.Title.Trim();
        issue.Description = command.Description.Trim();
        issue.Priority = command.Priority;
        issue.AssigneeId = assigneeId;
        issue.UpdatedUtc = DateTimeOffset.UtcNow;

        dbContext.IssueLabels.RemoveRange(issue.IssueLabels);
        issue.IssueLabels.Clear();
        foreach (var labelName in command.Labels.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var label = await GetOrCreateLabelAsync(labelName, cancellationToken);
            issue.IssueLabels.Add(new IssueLabel { IssueId = issue.Id, LabelId = label.Id, Label = label });
        }

        dbContext.AuditEvents.Add(new AuditEvent
        {
            IssueId = issue.Id,
            ActorId = actorId,
            EventType = "IssueUpdated",
            Description = $"Updated title, description, assignee, priority, or labels on {issue.Key}."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveIssueAsync(MoveIssueCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var actorId = GetUserIdOrThrow();

        var issue = await dbContext.Issues
            .Where(x => x.Project.TenantId == tenantId)
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.Id == command.IssueId, cancellationToken)
            ?? throw new InvalidOperationException("Issue not found.");

        var targetStatus = await dbContext.IssueStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.TargetStatusId && x.ProjectId == issue.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Target status not found.");

        if (issue.StatusId == targetStatus.Id)
        {
            return;
        }

        var fromStatus = issue.Status.Name;
        issue.StatusId = targetStatus.Id;
        issue.UpdatedUtc = DateTimeOffset.UtcNow;

        dbContext.AuditEvents.Add(new AuditEvent
        {
            IssueId = issue.Id,
            ActorId = actorId,
            EventType = "StatusChanged",
            Description = $"{issue.Key} moved from {fromStatus} to {targetStatus.Name}."
        });

        if (issue.AssigneeId.HasValue)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = issue.AssigneeId.Value,
                IssueId = issue.Id,
                Type = NotificationType.StatusChanged,
                Message = $"{issue.Key} moved to {targetStatus.Name}."
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCommentAsync(AddCommentCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var actorId = GetUserIdOrThrow();
        var issue = await dbContext.Issues
            .FirstOrDefaultAsync(x => x.Id == command.IssueId && x.Project.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Issue not found.");

        dbContext.Comments.Add(new Comment
        {
            IssueId = issue.Id,
            AuthorId = actorId,
            Body = command.Body.Trim()
        });

        dbContext.AuditEvents.Add(new AuditEvent
        {
            IssueId = issue.Id,
            ActorId = actorId,
            EventType = "CommentAdded",
            Description = $"Added a comment to {issue.Key}."
        });

        issue.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SeedSampleDataAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var tenant = new Tenant { Name = "Demo Company", Key = "DEMO" };

        var admin = new AppUser { Tenant = tenant, DisplayName = "Avery Architect", Email = "avery@joura.local", Role = ProjectRole.Admin };
        var pm = new AppUser { Tenant = tenant, DisplayName = "Priya PM", Email = "priya@joura.local", Role = ProjectRole.ProjectManager };
        var developer = new AppUser { Tenant = tenant, DisplayName = "Devon Developer", Email = "devon@joura.local", Role = ProjectRole.Member };

        var project = new Project
        {
            Tenant = tenant,
            Name = "Joura Platform",
            Key = "JOU",
            Description = "Homemade issue tracking for when enterprise software feels like overkill."
        };

        var todo = new IssueStatus { Project = project, Name = "To Do", Category = IssueStatusCategory.ToDo, SortOrder = 1, IsDefault = true };
        var inProgress = new IssueStatus { Project = project, Name = "In Progress", Category = IssueStatusCategory.InProgress, SortOrder = 2 };
        var done = new IssueStatus { Project = project, Name = "Done", Category = IssueStatusCategory.Done, SortOrder = 3 };

        var labels = new[]
        {
            new Label { Name = "architecture", Color = "#244b40" },
            new Label { Name = "azure", Color = "#1f4566" },
            new Label { Name = "backend", Color = "#6a3f22" }
        };

        var issue1 = new Issue
        {
            Project = project,
            Status = todo,
            Reporter = admin,
            Assignee = pm,
            Key = "JOU-1",
            Title = "Define modular monolith boundaries",
            Description = "Split the solution into domain, application, infrastructure, and web projects.",
            Type = IssueType.Story,
            Priority = IssuePriority.High
        };
        issue1.IssueLabels.Add(new IssueLabel { Issue = issue1, Label = labels[0] });

        var issue2 = new Issue
        {
            Project = project,
            Status = inProgress,
            Reporter = pm,
            Assignee = developer,
            Key = "JOU-2",
            Title = "Wire PostgreSQL persistence",
            Description = "Add EF Core, Npgsql, and the initial issue tracking schema.",
            Type = IssueType.Task,
            Priority = IssuePriority.High
        };
        issue2.IssueLabels.Add(new IssueLabel { Issue = issue2, Label = labels[1] });
        issue2.IssueLabels.Add(new IssueLabel { Issue = issue2, Label = labels[2] });

        var issue3 = new Issue
        {
            Project = project,
            Status = done,
            Reporter = admin,
            Assignee = developer,
            Key = "JOU-3",
            Title = "Draft Azure target architecture",
            Description = "Document App Service, PostgreSQL, Blob Storage, Key Vault, and Application Insights.",
            Type = IssueType.Improvement,
            Priority = IssuePriority.Medium
        };
        issue3.IssueLabels.Add(new IssueLabel { Issue = issue3, Label = labels[1] });

        dbContext.AddRange(tenant, admin, pm, developer, project, todo, inProgress, done);
        dbContext.Labels.AddRange(labels);
        dbContext.Issues.AddRange(issue1, issue2, issue3);
        dbContext.Comments.AddRange(
            new Comment { Issue = issue2, Author = developer, Body = "The DbContext is in place; migrations are next." },
            new Comment { Issue = issue1, Author = pm, Body = "Keep search and notifications behind clear application contracts." });
        dbContext.AuditEvents.AddRange(
            new AuditEvent { Issue = issue1, Actor = admin, EventType = "IssueCreated", Description = "Created issue JOU-1." },
            new AuditEvent { Issue = issue2, Actor = pm, EventType = "IssueCreated", Description = "Created issue JOU-2." },
            new AuditEvent { Issue = issue2, Actor = developer, EventType = "StatusChanged", Description = "JOU-2 moved from To Do to In Progress." },
            new AuditEvent { Issue = issue3, Actor = admin, EventType = "IssueCreated", Description = "Created issue JOU-3." });
        dbContext.Notifications.AddRange(
            new Notification { User = pm, Issue = issue1, Type = NotificationType.IssueAssigned, Message = "You were assigned to JOU-1." },
            new Notification { User = developer, Issue = issue2, Type = NotificationType.IssueAssigned, Message = "You were assigned to JOU-2." });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<IssueSummaryDto>> LoadIssueSummariesAsync(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantIdOrThrow();
        var issues = await dbContext.Issues
            .AsNoTracking()
            .Where(x => x.Project.TenantId == tenantId)
            .Include(x => x.Project)
            .Include(x => x.Status)
            .Include(x => x.Assignee)
            .Include(x => x.Reporter)
            .Include(x => x.IssueLabels)
            .ThenInclude(x => x.Label)
            .ToListAsync(cancellationToken);

        return issues
            .Select(issue => new IssueSummaryDto(
                issue.Id,
                issue.Key,
                issue.Title,
                issue.ProjectId,
                issue.Project.Key,
                issue.StatusId,
                issue.Status.Name,
                issue.Status.Category,
                issue.AssigneeId,
                issue.Assignee != null ? issue.Assignee.DisplayName : null,
                issue.Reporter.DisplayName,
                issue.Priority,
                issue.Type,
                issue.IssueLabels.Select(label => new LabelDto(label.LabelId, label.Label.Name, label.Label.Color)).ToList(),
                issue.UpdatedUtc))
            .ToList();
    }

    private async Task<Label> GetOrCreateLabelAsync(string labelName, CancellationToken cancellationToken)
    {
        var normalized = labelName.Trim().ToLowerInvariant();
        var existing = await dbContext.Labels.FirstOrDefaultAsync(x => x.Name.ToLower() == normalized, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var label = new Label
        {
            Name = normalized,
            Color = normalized switch
            {
                "azure" => "#1f4566",
                "backend" => "#6a3f22",
                "architecture" => "#244b40",
                _ => "#555f2a"
            }
        };

        dbContext.Labels.Add(label);
        return label;
    }

    private Guid GetTenantIdOrThrow()
    {
        return currentUserContext.TenantId
            ?? throw new InvalidOperationException("Authenticated tenant is missing.");
    }

    private Guid GetUserIdOrThrow()
    {
        return currentUserContext.UserId
            ?? throw new InvalidOperationException("Authenticated user is missing.");
    }
}
