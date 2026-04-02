using Joura.Application.Models;

namespace Joura.Application.Abstractions;

public interface IWorkTrackingService
{
    Task<WorkspaceOverviewDto> GetWorkspaceOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectReferenceDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserReferenceDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatusReferenceDto>> GetStatusesAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueSummaryDto>> SearchIssuesAsync(IssueFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KanbanColumnDto>> GetBoardAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IssueDetailDto?> GetIssueAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task<Guid> CreateProjectAsync(CreateProjectCommand command, CancellationToken cancellationToken = default);
    Task<Guid> CreateIssueAsync(CreateIssueCommand command, CancellationToken cancellationToken = default);
    Task UpdateIssueAsync(UpdateIssueCommand command, CancellationToken cancellationToken = default);
    Task MoveIssueAsync(MoveIssueCommand command, CancellationToken cancellationToken = default);
    Task AddCommentAsync(AddCommentCommand command, CancellationToken cancellationToken = default);
    Task SeedSampleDataAsync(CancellationToken cancellationToken = default);
}
