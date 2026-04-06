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

    public async Task<IReadOnlyList<WorklogEntryDto>> GetIssueWorklogAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        return await dbContext.WorklogEntries
            .AsNoTracking()
            .Where(x => x.IssueId == issueId && x.Issue.Project.TenantId == tenantId)
            .Include(x => x.Issue)
            .Include(x => x.User)
            .OrderByDescending(x => x.StartAt)
            .ThenByDescending(x => x.CreatedUtc)
            .Select(x => new WorklogEntryDto(
                x.Id,
                x.Issue.ProjectId,
                x.Issue.Project.Key,
                x.Issue.Project.Name,
                x.IssueId,
                x.Issue.Key,
                x.Issue.Title,
                x.UserId,
                x.User.DisplayName,
                x.StartAt,
                x.EndAt,
                x.Note,
                x.CreatedUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorklogEntryDto>> GetWorklogEntriesAsync(WorklogFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var query = dbContext.WorklogEntries
            .AsNoTracking()
            .Where(x => x.Issue.Project.TenantId == tenantId)
            .Include(x => x.Issue)
            .ThenInclude(x => x.Status)
            .Include(x => x.User)
            .AsQueryable();

        if (filter.ProjectId.HasValue)
        {
            var projectId = filter.ProjectId.Value;
            query = query.Where(x => x.Issue.ProjectId == projectId);
        }

        if (filter.LoggerId.HasValue)
        {
            var loggerId = filter.LoggerId.Value;
            query = query.Where(x => x.UserId == loggerId);
        }

        if (filter.StartAt.HasValue)
        {
            var startAt = filter.StartAt.Value;
            query = query.Where(x => x.StartAt >= startAt);
        }

        if (filter.EndAt.HasValue)
        {
            var endAt = filter.EndAt.Value;
            query = query.Where(x => x.EndAt <= endAt);
        }

        return await query
            .OrderByDescending(x => x.StartAt)
            .ThenByDescending(x => x.CreatedUtc)
            .Select(x => new WorklogEntryDto(
                x.Id,
                x.Issue.ProjectId,
                x.Issue.Project.Key,
                x.Issue.Project.Name,
                x.IssueId,
                x.Issue.Key,
                x.Issue.Title,
                x.UserId,
                x.User.DisplayName,
                x.StartAt,
                x.EndAt,
                x.Note,
                x.CreatedUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddWorklogAsync(AddWorklogCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var actorId = GetUserIdOrThrow();
        var minutes = ValidateWorklogInterval(command.StartAt, command.EndAt);

        var issue = await dbContext.Issues
            .FirstOrDefaultAsync(x => x.Id == command.IssueId && x.Project.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Issue not found.");

        var entry = new WorklogEntry
        {
            IssueId = issue.Id,
            UserId = actorId,
            StartAt = command.StartAt,
            EndAt = command.EndAt,
            Note = command.Note?.Trim() ?? string.Empty
        };

        dbContext.WorklogEntries.Add(entry);
        dbContext.AuditEvents.Add(new AuditEvent
        {
            IssueId = issue.Id,
            ActorId = actorId,
            EventType = "WorklogAdded",
            Description = $"Logged {FormatDuration(minutes)} on {command.StartAt:dd MMM yyyy} from {command.StartAt:HH:mm} to {command.EndAt:HH:mm}."
        });

        issue.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }

    public async Task UpdateWorklogAsync(UpdateWorklogCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var actorId = GetUserIdOrThrow();
        var minutes = ValidateWorklogInterval(command.StartAt, command.EndAt);

        var entry = await dbContext.WorklogEntries
            .Include(x => x.Issue)
            .FirstOrDefaultAsync(x => x.Id == command.WorklogEntryId && x.Issue.Project.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Worklog entry not found.");

        entry.StartAt = command.StartAt;
        entry.EndAt = command.EndAt;
        entry.Note = command.Note?.Trim() ?? string.Empty;
        entry.UpdatedUtc = DateTimeOffset.UtcNow;
        entry.Issue.UpdatedUtc = DateTimeOffset.UtcNow;

        dbContext.AuditEvents.Add(new AuditEvent
        {
            IssueId = entry.IssueId,
            ActorId = actorId,
            EventType = "WorklogUpdated",
            Description = $"Updated worklog to {FormatDuration(minutes)} on {command.StartAt:dd MMM yyyy} from {command.StartAt:HH:mm} to {command.EndAt:HH:mm}."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteWorklogAsync(Guid worklogEntryId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var actorId = GetUserIdOrThrow();

        var entry = await dbContext.WorklogEntries
            .Include(x => x.Issue)
            .FirstOrDefaultAsync(x => x.Id == worklogEntryId && x.Issue.Project.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Worklog entry not found.");

        var duration = FormatDuration((int)(entry.EndAt - entry.StartAt).TotalMinutes);
        var startAt = entry.StartAt;
        var endAt = entry.EndAt;
        var issueId = entry.IssueId;

        entry.Issue.UpdatedUtc = DateTimeOffset.UtcNow;
        dbContext.WorklogEntries.Remove(entry);
        dbContext.AuditEvents.Add(new AuditEvent
        {
            IssueId = issueId,
            ActorId = actorId,
            EventType = "WorklogDeleted",
            Description = $"Deleted worklog {duration} from {startAt:dd MMM yyyy} {startAt:HH:mm}-{endAt:HH:mm}."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SeedSampleDataAsync(CancellationToken cancellationToken = default)
    {
        await SeedTenantAsync(
            tenantKey: "DEMO",
            tenantName: "Demo Company",
            projectKey: "JOU",
            projectName: "Joura Platform",
            projectDescription: "Self-referential demo project tracking the features Joura has grown so far.",
            users:
            [
                new SeedUser("Avery Architect", "avery@joura.local", ProjectRole.Admin),
                new SeedUser("Priya PM", "priya@joura.local", ProjectRole.ProjectManager),
                new SeedUser("Devon Developer", "devon@joura.local", ProjectRole.Member),
                new SeedUser("Quinn QA", "quinn@joura.local", ProjectRole.Member)
            ],
            issueBlueprints: BuildDemoIssueBlueprints(),
            cancellationToken);

        await SeedTenantAsync(
            tenantKey: "ITSU",
            tenantName: "Itsu Company",
            projectKey: "ITSU",
            projectName: "Itsu Customer Portal",
            projectDescription: "Second tenant to validate isolation across authentication and data access.",
            users:
            [
                new SeedUser("Nina Ninja", "nina@joura.local", ProjectRole.Admin),
                new SeedUser("Paul Product", "paul@joura.local", ProjectRole.ProjectManager),
                new SeedUser("Mika Maker", "mika@joura.local", ProjectRole.Member)
            ],
            issueBlueprints: BuildItsuIssueBlueprints(),
            cancellationToken);
    }

    private async Task SeedTenantAsync(
        string tenantKey,
        string tenantName,
        string projectKey,
        string projectName,
        string projectDescription,
        IReadOnlyList<SeedUser> users,
        IReadOnlyList<SeedIssueBlueprint> issueBlueprints,
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
        var comments = new List<Comment>();
        var auditEvents = new List<AuditEvent>();
        var worklogEntries = new List<WorklogEntry>();
        foreach (var blueprint in issueBlueprints)
        {
            var createdUtc = CreateSeedTimestamp(blueprint.CreatedDaysAgo, 9, 0);
            var issue = new Issue
            {
                Project = project,
                Status = statusByName[blueprint.StatusName],
                Reporter = userByName[blueprint.ReporterName],
                Assignee = blueprint.AssigneeName is null ? null : userByName[blueprint.AssigneeName],
                Key = blueprint.Key,
                Title = blueprint.Title,
                Description = blueprint.Description,
                Type = blueprint.Type,
                Priority = blueprint.Priority,
                DueDate = blueprint.DueDateOffsetDays.HasValue
                    ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(blueprint.DueDateOffsetDays.Value).Date)
                    : null,
                CreatedUtc = createdUtc,
                UpdatedUtc = createdUtc
            };

            foreach (var labelName in blueprint.Labels)
            {
                issue.IssueLabels.Add(new IssueLabel { Issue = issue, Label = labels[labelName] });
            }

            issues.Add(issue);
            auditEvents.Add(new AuditEvent
            {
                Issue = issue,
                Actor = issue.Reporter,
                EventType = "IssueCreated",
                Description = $"Created issue {issue.Key}.",
                CreatedUtc = createdUtc
            });

            foreach (var commentBlueprint in blueprint.Comments)
            {
                var commentCreatedUtc = CreateSeedTimestamp(commentBlueprint.DaysAgo, commentBlueprint.Hour, commentBlueprint.Minute);
                var author = userByName[commentBlueprint.AuthorName];
                comments.Add(new Comment
                {
                    Issue = issue,
                    Author = author,
                    Body = commentBlueprint.Body,
                    CreatedUtc = commentCreatedUtc
                });

                auditEvents.Add(new AuditEvent
                {
                    Issue = issue,
                    Actor = author,
                    EventType = "CommentAdded",
                    Description = $"Added a comment to {issue.Key}.",
                    CreatedUtc = commentCreatedUtc
                });

                if (commentCreatedUtc > issue.UpdatedUtc)
                {
                    issue.UpdatedUtc = commentCreatedUtc;
                }
            }

            foreach (var worklogBlueprint in blueprint.Worklogs)
            {
                var startAt = CreateSeedDateTime(worklogBlueprint.DaysAgo, worklogBlueprint.StartHour, worklogBlueprint.StartMinute);
                var endAt = startAt.AddMinutes(worklogBlueprint.DurationMinutes);
                var createdWorklogUtc = new DateTimeOffset(endAt, TimeSpan.Zero);
                var user = userByName[worklogBlueprint.UserName];

                worklogEntries.Add(new WorklogEntry
                {
                    Issue = issue,
                    User = user,
                    StartAt = startAt,
                    EndAt = endAt,
                    Note = worklogBlueprint.Note,
                    CreatedUtc = createdWorklogUtc,
                    UpdatedUtc = createdWorklogUtc
                });

                auditEvents.Add(new AuditEvent
                {
                    Issue = issue,
                    Actor = user,
                    EventType = "WorklogAdded",
                    Description = $"Logged {FormatDuration(worklogBlueprint.DurationMinutes)} on {startAt:dd MMM yyyy} from {startAt:HH:mm} to {endAt:HH:mm}.",
                    CreatedUtc = createdWorklogUtc
                });

                if (createdWorklogUtc > issue.UpdatedUtc)
                {
                    issue.UpdatedUtc = createdWorklogUtc;
                }
            }

            foreach (var auditBlueprint in blueprint.Audits)
            {
                var auditCreatedUtc = CreateSeedTimestamp(auditBlueprint.DaysAgo, auditBlueprint.Hour, auditBlueprint.Minute);
                var actor = userByName[auditBlueprint.ActorName];
                auditEvents.Add(new AuditEvent
                {
                    Issue = issue,
                    Actor = actor,
                    EventType = auditBlueprint.EventType,
                    Description = auditBlueprint.Description,
                    CreatedUtc = auditCreatedUtc
                });

                if (auditCreatedUtc > issue.UpdatedUtc)
                {
                    issue.UpdatedUtc = auditCreatedUtc;
                }
            }
        }

        dbContext.AddRange(tenant, project, todo, inProgress, done);
        dbContext.Users.AddRange(tenantUsers);
        dbContext.Issues.AddRange(issues);
        dbContext.Comments.AddRange(comments);
        dbContext.WorklogEntries.AddRange(worklogEntries);
        dbContext.AuditEvents.AddRange(auditEvents);
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
                "frontend" => "#375a7f",
                "auth" => "#7a2338",
                "multitenancy" => "#5b4b7a",
                "worklog" => "#226b63",
                "reports" => "#695d24",
                "due-dates" => "#7a4f22",
                "demo-data" => "#466027",
                "ux" => "#7a2f5f",
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

    private static int ValidateWorklogInterval(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new InvalidOperationException("Worklog end time must be after start time.");
        }

        var minutes = (int)(endAt - startAt).TotalMinutes;
        if (minutes <= 0 || minutes > 24 * 60)
        {
            throw new InvalidOperationException("Worklog duration must be between 1 and 1440 minutes.");
        }

        return minutes;
    }

    private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var remainder = minutes % 60;
        return remainder == 0 ? $"{hours}h" : $"{hours}h {remainder}m";
    }

    private static DateTime CreateSeedDateTime(int daysAgo, int hour, int minute)
    {
        return DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-daysAgo).AddHours(hour).AddMinutes(minute), DateTimeKind.Unspecified);
    }

    private static DateTimeOffset CreateSeedTimestamp(int daysAgo, int hour, int minute)
    {
        return new DateTimeOffset(CreateSeedDateTime(daysAgo, hour, minute), TimeSpan.Zero);
    }

    private static IReadOnlyList<SeedIssueBlueprint> BuildDemoIssueBlueprints()
    {
        return
        [
            new SeedIssueBlueprint(
                "JOU-1",
                "Shape the self-referential Demo Company seed",
                "Broaden the default dataset so the Demo Company project explains Joura through its own backlog, labels, due dates, comments, and worklogs.",
                IssueType.Story,
                IssuePriority.High,
                5,
                3,
                "Done",
                "Priya PM",
                "Avery Architect",
                ["demo-data", "architecture"],
                [
                    new SeedCommentBlueprint("Avery Architect", "The seed should be strong enough to demo the board, issue detail, reports, and due dates without manual prep.", 4, 10, 15),
                    new SeedCommentBlueprint("Priya PM", "Agreed. Make the project describe Joura itself so the sample feels intentional instead of random.", 3, 14, 20)
                ],
                [
                    new SeedWorklogBlueprint("Avery Architect", 4, 9, 0, 90, "Outlined the richer demo backlog."),
                    new SeedWorklogBlueprint("Priya PM", 3, 13, 0, 60, "Reviewed the seeded issue narrative and priorities.")
                ],
                [
                    new SeedAuditBlueprint("Avery Architect", "StatusChanged", "JOU-1 moved from To Do to In Progress.", 4, 11, 0),
                    new SeedAuditBlueprint("Priya PM", "StatusChanged", "JOU-1 moved from In Progress to Done.", 3, 16, 0)
                ]),
            new SeedIssueBlueprint(
                "JOU-2",
                "Expand issue details with useful history",
                "Ensure the issue detail page has enough seeded comments and audit events to demonstrate collaboration and traceability.",
                IssueType.Task,
                IssuePriority.High,
                6,
                5,
                "Done",
                "Avery Architect",
                "Devon Developer",
                ["backend", "demo-data"],
                [
                    new SeedCommentBlueprint("Devon Developer", "I added seed comments so the discussion timeline is not empty on first boot.", 5, 11, 10),
                    new SeedCommentBlueprint("Quinn QA", "Please keep the wording realistic enough to feel like an actual project thread.", 4, 15, 35)
                ],
                [
                    new SeedWorklogBlueprint("Devon Developer", 5, 10, 0, 120, "Seeded comments and matching audit history."),
                    new SeedWorklogBlueprint("Quinn QA", 4, 15, 0, 45, "Reviewed issue detail timelines for realism.")
                ],
                [
                    new SeedAuditBlueprint("Devon Developer", "StatusChanged", "JOU-2 moved from To Do to In Progress.", 5, 12, 30),
                    new SeedAuditBlueprint("Avery Architect", "StatusChanged", "JOU-2 moved from In Progress to Done.", 4, 17, 0)
                ]),
            new SeedIssueBlueprint(
                "JOU-3",
                "Build weekly worklog planning and rescheduling",
                "Support dragging across planner slots to create worklogs and dragging existing entries to reschedule them in the week view.",
                IssueType.Story,
                IssuePriority.High,
                4,
                2,
                "In Progress",
                "Priya PM",
                "Devon Developer",
                ["frontend", "worklog"],
                [
                    new SeedCommentBlueprint("Priya PM", "This should make the product feel more like a daily operating tool and less like a static tracker.", 3, 9, 45),
                    new SeedCommentBlueprint("Devon Developer", "Planner selection and drag state are working, but the demo needs more seeded entries to show the pattern.", 2, 16, 15)
                ],
                [
                    new SeedWorklogBlueprint("Devon Developer", 3, 9, 0, 90, "Implemented planner range selection."),
                    new SeedWorklogBlueprint("Devon Developer", 2, 13, 0, 120, "Added drag-to-reschedule behaviour."),
                    new SeedWorklogBlueprint("Quinn QA", 1, 15, 0, 30, "Checked overlaps and drag interactions.")
                ],
                [
                    new SeedAuditBlueprint("Priya PM", "StatusChanged", "JOU-3 moved from To Do to In Progress.", 3, 10, 30)
                ]),
            new SeedIssueBlueprint(
                "JOU-4",
                "Refactor Worklog Search into Reports",
                "Rename the worklog search surface to Reports, align the hero copy, and split filters from result tables for a clearer page structure.",
                IssueType.Task,
                IssuePriority.Medium,
                3,
                4,
                "To Do",
                "Priya PM",
                "Devon Developer",
                ["reports", "frontend"],
                [
                    new SeedCommentBlueprint("Priya PM", "Reports reads better than Worklog Search now that the page is turning into a reusable reporting surface.", 1, 11, 20)
                ],
                [
                    new SeedWorklogBlueprint("Priya PM", 1, 10, 0, 30, "Captured the rename and layout goals.")
                ],
                []),
            new SeedIssueBlueprint(
                "JOU-5",
                "Add due dates calendar for delivery planning",
                "Expose issue due dates on a monthly calendar so teams can spot clustering and navigate deadlines quickly.",
                IssueType.Story,
                IssuePriority.Medium,
                7,
                6,
                "Done",
                "Avery Architect",
                "Priya PM",
                ["due-dates", "frontend"],
                [
                    new SeedCommentBlueprint("Priya PM", "The month view already helps explain why due dates matter in the seed project.", 6, 14, 0)
                ],
                [
                    new SeedWorklogBlueprint("Priya PM", 6, 13, 0, 75, "Built and styled the due dates calendar."),
                    new SeedWorklogBlueprint("Quinn QA", 5, 10, 30, 45, "Verified issue links and month navigation.")
                ],
                [
                    new SeedAuditBlueprint("Priya PM", "StatusChanged", "JOU-5 moved from To Do to In Progress.", 6, 15, 0),
                    new SeedAuditBlueprint("Avery Architect", "StatusChanged", "JOU-5 moved from In Progress to Done.", 5, 12, 0)
                ]),
            new SeedIssueBlueprint(
                "JOU-6",
                "Tighten tenant-aware authentication flows",
                "Make login and current-user context feel explicit so Demo Company and Itsu Company clearly demonstrate multitenant isolation.",
                IssueType.Task,
                IssuePriority.High,
                8,
                1,
                "Done",
                "Avery Architect",
                "Quinn QA",
                ["auth", "multitenancy", "backend"],
                [
                    new SeedCommentBlueprint("Quinn QA", "Tenant switching is much easier to validate when the sample users belong to distinct companies.", 7, 9, 15),
                    new SeedCommentBlueprint("Avery Architect", "Keep the login choices simple enough that the seed still feels approachable.", 6, 11, 0)
                ],
                [
                    new SeedWorklogBlueprint("Quinn QA", 7, 9, 0, 60, "Validated tenant boundaries in auth flows."),
                    new SeedWorklogBlueprint("Avery Architect", 6, 10, 0, 90, "Cleaned up current-user context and seed users.")
                ],
                [
                    new SeedAuditBlueprint("Avery Architect", "StatusChanged", "JOU-6 moved from To Do to In Progress.", 7, 11, 0),
                    new SeedAuditBlueprint("Quinn QA", "StatusChanged", "JOU-6 moved from In Progress to Done.", 6, 13, 0)
                ]),
            new SeedIssueBlueprint(
                "JOU-7",
                "Polish board interactions for day-to-day use",
                "Keep drag-and-drop board interactions fast and clear so seeded issues can be moved without leaving the page.",
                IssueType.Bug,
                IssuePriority.Medium,
                2,
                7,
                "In Progress",
                "Priya PM",
                "Quinn QA",
                ["frontend", "ux"],
                [
                    new SeedCommentBlueprint("Quinn QA", "The board works, but the drop affordance could be more obvious on longer columns.", 1, 9, 40),
                    new SeedCommentBlueprint("Devon Developer", "I can tune the active column treatment after the reports cleanup lands.", 0, 8, 50)
                ],
                [
                    new SeedWorklogBlueprint("Quinn QA", 1, 9, 0, 50, "Tested drag/drop feedback on the board."),
                    new SeedWorklogBlueprint("Devon Developer", 0, 8, 0, 40, "Tweaked board column states and ticket drag behaviour.")
                ],
                [
                    new SeedAuditBlueprint("Priya PM", "StatusChanged", "JOU-7 moved from To Do to In Progress.", 1, 10, 30)
                ])
        ];
    }

    private static IReadOnlyList<SeedIssueBlueprint> BuildItsuIssueBlueprints()
    {
        return
        [
            new SeedIssueBlueprint(
                "ITSU-1",
                "Create customer onboarding flow",
                "Build signup and onboarding screens for the portal.",
                IssueType.Story,
                IssuePriority.High,
                4,
                5,
                "To Do",
                "Nina Ninja",
                "Paul Product",
                ["frontend"],
                [],
                [new SeedWorklogBlueprint("Paul Product", 1, 10, 0, 45, "Outlined the onboarding flow.")],
                []),
            new SeedIssueBlueprint(
                "ITSU-2",
                "Implement account settings API",
                "Add backend endpoints for profile and preference updates.",
                IssueType.Task,
                IssuePriority.Medium,
                5,
                9,
                "In Progress",
                "Paul Product",
                "Mika Maker",
                ["backend"],
                [new SeedCommentBlueprint("Mika Maker", "Settings payload shape looks stable enough to continue the API work.", 2, 13, 0)],
                [new SeedWorklogBlueprint("Mika Maker", 2, 13, 0, 90, "Built the first account settings endpoints.")],
                [new SeedAuditBlueprint("Paul Product", "StatusChanged", "ITSU-2 moved from To Do to In Progress.", 2, 15, 0)]),
            new SeedIssueBlueprint(
                "ITSU-3",
                "Enable audit export",
                "Support CSV export for tenant-level audit trail.",
                IssueType.Task,
                IssuePriority.Low,
                6,
                15,
                "Done",
                "Nina Ninja",
                "Mika Maker",
                ["architecture"],
                [],
                [new SeedWorklogBlueprint("Mika Maker", 4, 9, 30, 60, "Added export formatting for audit records.")],
                [new SeedAuditBlueprint("Nina Ninja", "StatusChanged", "ITSU-3 moved from In Progress to Done.", 3, 16, 0)])
        ];
    }

    private sealed record SeedUser(string DisplayName, string Email, ProjectRole Role);

    private sealed record SeedIssueBlueprint(
        string Key,
        string Title,
        string Description,
        IssueType Type,
        IssuePriority Priority,
        int CreatedDaysAgo,
        int? DueDateOffsetDays,
        string StatusName,
        string ReporterName,
        string? AssigneeName,
        IReadOnlyList<string> Labels,
        IReadOnlyList<SeedCommentBlueprint> Comments,
        IReadOnlyList<SeedWorklogBlueprint> Worklogs,
        IReadOnlyList<SeedAuditBlueprint> Audits);

    private sealed record SeedCommentBlueprint(string AuthorName, string Body, int DaysAgo, int Hour, int Minute);

    private sealed record SeedWorklogBlueprint(string UserName, int DaysAgo, int StartHour, int StartMinute, int DurationMinutes, string Note);

    private sealed record SeedAuditBlueprint(string ActorName, string EventType, string Description, int DaysAgo, int Hour, int Minute);
}
