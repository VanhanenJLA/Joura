using Joura.Domain.Common;

namespace Joura.Domain.Entities;

public sealed class AuditEvent : Entity
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public Guid ActorId { get; set; }
    public AppUser Actor { get; set; } = null!;
    public required string EventType { get; set; }
    public required string Description { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
