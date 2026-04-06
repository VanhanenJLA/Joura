namespace Joura.Application.Models;

public sealed record WorklogReportDto(
    IReadOnlyList<WorklogReportProjectDto> Projects,
    int TotalMinutes,
    int TotalEntryCount);

public sealed record WorklogReportProjectDto(
    Guid ProjectId,
    string ProjectKey,
    string ProjectName,
    IReadOnlyList<WorklogReportIssueDto> Issues,
    int TotalMinutes,
    int TotalEntryCount);

public sealed record WorklogReportIssueDto(
    Guid IssueId,
    string IssueKey,
    string IssueTitle,
    IReadOnlyList<WorklogReportRowDto> Rows,
    int TotalMinutes,
    int TotalEntryCount);

public sealed record WorklogReportRowDto(
    Guid WorklogEntryId,
    string LoggerName,
    string Note,
    int Minutes,
    DateTime StartAt,
    DateTime EndAt);
