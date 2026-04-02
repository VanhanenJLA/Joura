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
                issue.DueDate,
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
            Priority = command.Priority,
            DueDate = command.DueDate
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
        issue.Type = command.Type;
        issue.Priority = command.Priority;
        issue.DueDate = command.DueDate;
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
            Description = $"Updated title, description, type, due date, assignee, priority, or labels on {issue.Key}."
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
        await SeedTenantAsync(
            tenantKey: "DEMO",
            tenantName: "Demo Company",
            projectKey: "JOU",
            projectName: "Joura Platform",
            projectDescription: "Homemade issue tracking built on vibes.",
            users:
            [
                ("Avery Architect", "avery@joura.local", ProjectRole.Admin),
                ("Priya PM", "priya@joura.local", ProjectRole.ProjectManager),
                ("Devon Developer", "devon@joura.local", ProjectRole.Member)
            ],
            issueBlueprints:
            [
                ("JOU-1", "Define modular monolith boundaries", "Split the solution into domain, application, infrastructure, and web projects.", IssueType.Story, IssuePriority.High, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3).Date), "To Do", "Avery Architect", "Priya PM", new[] { "architecture" }),
                ("JOU-2", "Wire PostgreSQL persistence", "Add EF Core, Npgsql, and the initial issue tracking schema.", IssueType.Task, IssuePriority.High, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7).Date), "In Progress", "Priya PM", "Devon Developer", new[] { "azure", "backend" }),
                ("JOU-3", "Draft Azure target architecture", "Document App Service, PostgreSQL, Blob Storage, Key Vault, and Application Insights.", IssueType.Task, IssuePriority.Medium, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12).Date), "Done", "Avery Architect", "Devon Developer", new[] { "azure" })
            ],
            cancellationToken);

        await SeedTenantAsync(
            tenantKey: "ITSU",
            tenantName: "Itsu Company",
            projectKey: "ITSU",
            projectName: "Itsu Customer Portal",
            projectDescription: "Second tenant to validate isolation across authentication and data access.",
            users:
            [
                ("Nina Ninja", "nina@joura.local", ProjectRole.Admin),
                ("Paul Product", "paul@joura.local", ProjectRole.ProjectManager),
                ("Mika Maker", "mika@joura.local", ProjectRole.Member)
            ],
            issueBlueprints:
            [
                ("ITSU-1", "Create customer onboarding flow", "Build signup and onboarding screens for the portal.", IssueType.Story, IssuePriority.High, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5).Date), "To Do", "Nina Ninja", "Paul Product", new[] { "frontend" }),
                ("ITSU-2", "Implement account settings API", "Add backend endpoints for profile and preference updates.", IssueType.Task, IssuePriority.Medium, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9).Date), "In Progress", "Paul Product", "Mika Maker", new[] { "backend" }),
                ("ITSU-3", "Enable audit export", "Support CSV export for tenant-level audit trail.", IssueType.Task, IssuePriority.Low, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15).Date), "Done", "Nina Ninja", "Mika Maker", new[] { "architecture" })
            ],
            cancellationToken);
    }

    private async Task SeedTenantAsync(
        string tenantKey,
        string tenantName,
        string projectKey,
        string projectName,
        string projectDescription,
        IReadOnlyList<(string DisplayName, string Email, ProjectRole Role)> users,
        IReadOnlyList<(string Key, string Title, string Description, IssueType Type, IssuePriority Priority, DateOnly? DueDate, string StatusName, string ReporterName, string AssigneeName, IReadOnlyList<string> Labels)> issueBlueprints,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Tenants.AnyAsync(x => x.Key == tenantKey, cancellationToken))
        {
            return;
        }

        var tenant = new Tenant { Name = tenantName, Key = tenantKey };
        var tenantUsers = users
            .Select(x => new AppUser { Tenant = tenant, DisplayName = x.DisplayName, Email = x.Email, Role = x.Role })
            .ToList();

        var project = new Project
        {
            Tenant = tenant,
            Name = projectName,
            Key = projectKey,
            Description = projectDescription
        };

        var todo = new IssueStatus { Project = project, Name = "To Do", Category = IssueStatusCategory.ToDo, SortOrder = 1, IsDefault = true };
        var inProgress = new IssueStatus { Project = project, Name = "In Progress", Category = IssueStatusCategory.InProgress, SortOrder = 2 };
        var done = new IssueStatus { Project = project, Name = "Done", Category = IssueStatusCategory.Done, SortOrder = 3 };
        var statusByName = new Dictionary<string, IssueStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["To Do"] = todo,
            ["In Progress"] = inProgress,
            ["Done"] = done
        };

        var labels = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);
        foreach (var labelName in issueBlueprints.SelectMany(x => x.Labels).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            labels[labelName] = await GetOrCreateLabelAsync(labelName, cancellationToken);
        }

        var userByName = tenantUsers.ToDictionary(x => x.DisplayName, StringComparer.OrdinalIgnoreCase);
        var issues = new List<Issue>();
        var issueCreatedAuditEvents = new List<AuditEvent>();
        foreach (var blueprint in issueBlueprints)
        {
            var issue = new Issue
            {
                Project = project,
                Status = statusByName[blueprint.StatusName],
                Reporter = userByName[blueprint.ReporterName],
                Assignee = userByName[blueprint.AssigneeName],
                Key = blueprint.Key,
                Title = blueprint.Title,
                Description = blueprint.Description,
                Type = blueprint.Type,
                Priority = blueprint.Priority,
                DueDate = blueprint.DueDate
            };

            foreach (var labelName in blueprint.Labels)
            {
                issue.IssueLabels.Add(new IssueLabel { Issue = issue, Label = labels[labelName] });
            }

            issues.Add(issue);
            issueCreatedAuditEvents.Add(new AuditEvent
            {
                Issue = issue,
                Actor = issue.Reporter,
                EventType = "IssueCreated",
                Description = $"Created issue {issue.Key}.",
                CreatedUtc = issue.CreatedUtc
            });
        }

        dbContext.AddRange(tenant, project, todo, inProgress, done);
        dbContext.Users.AddRange(tenantUsers);
        dbContext.Issues.AddRange(issues);
        dbContext.AuditEvents.AddRange(issueCreatedAuditEvents);
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
                issue.DueDate,
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
