using Joura.Domain.Common;
using Joura.Domain.Enums;

namespace Joura.Domain.Entities;

public sealed class AppUser : Entity
{
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public ProjectRole Role { get; set; } = ProjectRole.Member;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
