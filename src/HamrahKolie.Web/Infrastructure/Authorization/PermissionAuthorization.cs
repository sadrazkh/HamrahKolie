using HamrahKolie.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace HamrahKolie.Web.Infrastructure.Authorization;

/// <summary>پیشوند سیاست‌های دسترسی پویا.</summary>
public static class PermissionPolicy
{
    public const string Prefix = "perm:";
    public static string For(string permissionKey) => Prefix + permissionKey;
}

/// <summary>الزام وجود یک دسترسی مشخص.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionKey { get; }
    public PermissionRequirement(string permissionKey) => PermissionKey = permissionKey;
}

/// <summary>
/// بررسی می‌کند کاربر Claim دسترسی موردنظر یا Claim فراگیر «*» (Super Admin) را دارد.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(AppClaimTypes.Permission, "*")
            || context.User.HasClaim(AppClaimTypes.Permission, requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// سیاست‌های دسترسی را به‌صورت پویا از روی نام سیاست («perm:xxx») می‌سازد،
/// بدون نیاز به ثبت دستی هر سیاست.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicy.Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var key = policyName[PermissionPolicy.Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(key))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// نسخه راحت‌تر [Authorize] برای دسترسی‌ها: [HasPermission(Permissions.ContentEdit)].
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionKey) : base(PermissionPolicy.For(permissionKey)) { }
}
