using Joura.Domain.Common;
using Joura.Domain.Enums;

namespace Joura.Domain.Entities;

public sealed class AppUser : Entity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public ProjectRole Role { get; set; } = ProjectRole.Member;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
