using Joura.Domain.Enums;

namespace Joura.Application.Models;

public sealed record CreateProjectCommand(string Name, string Key, string Description);

public sealed record CreateIssueCommand(
    Guid ProjectId,
    string Title,
    string Description,
    IssueType Type,
    IssuePriority Priority,
    Guid ReporterId,
    Guid? AssigneeId,
    DateOnly? DueDate,
    IReadOnlyList<string> Labels);

public sealed record UpdateIssueCommand(
    Guid IssueId,
    string Title,
    string Description,
    IssueType Type,
    IssuePriority Priority,
    Guid? AssigneeId,
    DateOnly? DueDate,
    IReadOnlyList<string> Labels);

public sealed record MoveIssueCommand(Guid IssueId, Guid TargetStatusId, Guid ActorId);

public sealed record AddCommentCommand(Guid IssueId, Guid AuthorId, string Body);

public sealed record AddWorklogCommand(Guid IssueId, DateTime StartAt, DateTime EndAt, string? Note);

public sealed record UpdateWorklogCommand(Guid WorklogEntryId, DateTime StartAt, DateTime EndAt, string? Note);
