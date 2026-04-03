using Joura.Domain.Common;

namespace Joura.Domain.Entities;

public sealed class WorklogEntry : Entity
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
