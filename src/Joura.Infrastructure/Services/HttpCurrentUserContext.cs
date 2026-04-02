using System.Security.Claims;
using Joura.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Joura.Infrastructure.Services;

public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public Guid? UserId => TryParseGuidClaim(ClaimTypes.NameIdentifier);

    public Guid? TenantId => TryParseGuidClaim(AuthClaimTypes.TenantId);

    public string? UserName => Principal.Identity?.Name;

    public string? Email => Principal.FindFirstValue(ClaimTypes.Email);

    public ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    private Guid? TryParseGuidClaim(string claimType)
    {
        var value = Principal.FindFirstValue(claimType);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
