using Joura.Domain.Common;
using Joura.Domain.Enums;

namespace Joura.Domain.Entities;

public sealed class Notification : Entity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public NotificationType Type { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
