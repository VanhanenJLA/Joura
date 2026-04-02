using Joura.Domain.Common;

namespace Joura.Domain.Entities;

public sealed class Comment : Entity
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;
    public required string Body { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
