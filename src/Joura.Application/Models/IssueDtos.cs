using Joura.Domain.Enums;

namespace Joura.Application.Models;

public sealed record TenantOverviewDto(
    string TenantName,
    IReadOnlyList<ProjectSummaryDto> Projects,
    IReadOnlyList<IssueSummaryDto> RecentIssues,
    int OpenIssueCount,
    int CompletedIssueCount);

public sealed record ProjectSummaryDto(Guid Id, string Key, string Name, int IssueCount, int DoneIssueCount);

public sealed record ProjectReferenceDto(Guid Id, string Key, string Name);

public sealed record UserReferenceDto(Guid Id, string DisplayName, string Email);

public sealed record IssueSummaryDto(
    Guid Id,
    string Key,
    string Title,
    Guid ProjectId,
    string ProjectKey,
    Guid StatusId,
    string StatusName,
    IssueStatusCategory StatusCategory,
    Guid? AssigneeId,
    string? AssigneeName,
    string ReporterName,
    IssuePriority Priority,
    IssueType Type,
    DateOnly? DueDate,
    IReadOnlyList<LabelDto> Labels,
    DateTimeOffset UpdatedUtc);

public sealed record IssueDetailDto(
    Guid Id,
    string Key,
    string Title,
    string Description,
    string ProjectName,
    string StatusName,
    IssueStatusCategory StatusCategory,
    string? AssigneeName,
    string ReporterName,
    IssuePriority Priority,
    IssueType Type,
    DateOnly? DueDate,
    IReadOnlyList<LabelDto> Labels,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<AuditEventDto> AuditTrail);

public sealed record CommentDto(string AuthorName, string Body, DateTimeOffset CreatedUtc);

public sealed record AuditEventDto(string ActorName, string EventType, string Description, DateTimeOffset CreatedUtc);

public sealed record LabelDto(Guid Id, string Name, string Color);

public sealed record WorklogEntryDto(
    Guid Id,
    Guid IssueId,
    string IssueKey,
    string IssueTitle,
    Guid UserId,
    string UserName,
    DateTime StartAt,
    DateTime EndAt,
    string Note,
    DateTimeOffset CreatedUtc)
{
    public DateOnly LoggedDate => DateOnly.FromDateTime(StartAt);
    public int Minutes => (int)(EndAt - StartAt).TotalMinutes;
}

public sealed record WorklogFilter(Guid? ProjectId, Guid? LoggerId, DateTime? StartAt, DateTime? EndAt);

public sealed record KanbanColumnDto(Guid StatusId, string StatusName, IssueStatusCategory Category, IReadOnlyList<IssueSummaryDto> Issues);

public sealed record StatusReferenceDto(Guid Id, string Name, IssueStatusCategory Category, int SortOrder);

public sealed record IssueFilter(string? SearchText, Guid? ProjectId, Guid? AssigneeId, IssueStatusCategory? Category, IssuePriority? Priority);
