using System.Security.Claims;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace HamrahKolie.Web.Infrastructure.Identity;

/// <summary>پیاده‌سازی <see cref="ICurrentUser"/> بر پایه HttpContext.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => Principal?.Identity?.Name;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var ua = _accessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrEmpty(ua) ? null : ua;
        }
    }

    public bool HasPermission(string permissionKey)
    {
        var principal = Principal;
        if (principal is null) return false;

        // Claim فراگیر «*» به معنای دسترسی کامل (Super Admin) است.
        return principal.HasClaim(AppClaimTypes.Permission, "*")
            || principal.HasClaim(AppClaimTypes.Permission, permissionKey);
    }
}
