using Joura.Domain.Common;
using Joura.Domain.Enums;

namespace Joura.Domain.Entities;

public sealed class Issue : Entity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid StatusId { get; set; }
    public IssueStatus Status { get; set; } = null!;
    public Guid ReporterId { get; set; }
    public AppUser Reporter { get; set; } = null!;
    public Guid? AssigneeId { get; set; }
    public AppUser? Assignee { get; set; }
    public required string Key { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public IssueType Type { get; set; } = IssueType.Task;
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public uint RowVersion { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
    public ICollection<AttachmentMetadata> Attachments { get; set; } = new List<AttachmentMetadata>();
}
