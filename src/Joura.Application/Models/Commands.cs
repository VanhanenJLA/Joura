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
    IReadOnlyList<string> Labels);

public sealed record UpdateIssueCommand(
    Guid IssueId,
    string Title,
    string Description,
    IssuePriority Priority,
    Guid? AssigneeId,
    IReadOnlyList<string> Labels);

public sealed record MoveIssueCommand(Guid IssueId, Guid TargetStatusId, Guid ActorId);

public sealed record AddCommentCommand(Guid IssueId, Guid AuthorId, string Body);
