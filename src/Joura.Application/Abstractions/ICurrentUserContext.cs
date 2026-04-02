using System.Security.Claims;

namespace Joura.Application.Abstractions;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? UserName { get; }
    string? Email { get; }
    ClaimsPrincipal Principal { get; }
}
