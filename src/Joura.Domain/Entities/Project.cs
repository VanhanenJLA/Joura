using Joura.Domain.Common;

namespace Joura.Domain.Entities;

public sealed class Project : Entity
{
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public required string Name { get; set; }
    public required string Key { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<IssueStatus> Statuses { get; set; } = new List<IssueStatus>();
}
