using System.Security.Claims;
using Joura.Application.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Joura.Infrastructure.Services;

public sealed class HttpCurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider) : ICurrentUserContext
{
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public Guid? UserId => TryParseGuidClaim(ClaimTypes.NameIdentifier);

    public Guid? TenantId => TryParseGuidClaim(AuthClaimTypes.TenantId);

    public string? UserName => Principal.Identity?.Name;

    public string? Email => Principal.FindFirstValue(ClaimTypes.Email);

    public ClaimsPrincipal Principal => ResolvePrincipal();

    private Guid? TryParseGuidClaim(string claimType)
    {
        var value = Principal.FindFirstValue(claimType);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private ClaimsPrincipal ResolvePrincipal()
    {
        var httpPrincipal = httpContextAccessor.HttpContext?.User;
        if (httpPrincipal?.Identity?.IsAuthenticated == true)
        {
            return httpPrincipal;
        }

        var authenticationState = authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
        if (authenticationState.User.Identity?.IsAuthenticated == true)
        {
            return authenticationState.User;
        }

        return httpPrincipal ?? authenticationState.User;
    }
}
