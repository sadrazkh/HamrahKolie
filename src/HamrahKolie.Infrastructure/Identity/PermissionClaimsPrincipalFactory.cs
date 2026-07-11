using System.Security.Claims;
using HamrahKolie.Application.Authorization;
using HamrahKolie.Domain.Identity;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HamrahKolie.Infrastructure.Identity;

/// <summary>نوع Claim مورد استفاده برای دسترسی‌ها.</summary>
public static class AppClaimTypes
{
    public const string Permission = "permission";
}

/// <summary>
/// هنگام ساخت ClaimsPrincipal کاربر، دسترسی‌های ناشی از نقش‌های او را به‌صورت Claim اضافه می‌کند.
/// نقش SuperAdmin یک Claim ویژه دریافت می‌کند که همه دسترسی‌ها را پوشش می‌دهد.
/// </summary>
public sealed class PermissionClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly ApplicationDbContext _db;

    public PermissionClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options,
        ApplicationDbContext db)
        : base(userManager, roleManager, options)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var roleNames = await UserManager.GetRolesAsync(user);

        // اگر کاربر SuperAdmin است، Claim فراگیر اضافه کن.
        if (roleNames.Contains(Roles.SuperAdmin))
        {
            identity.AddClaim(new Claim(AppClaimTypes.Permission, "*"));
            return identity;
        }

        var permissionKeys = await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => _db.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .Select(r => r.Id)
                .Contains(rp.RoleId))
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToListAsync();

        foreach (var key in permissionKeys)
            identity.AddClaim(new Claim(AppClaimTypes.Permission, key));

        return identity;
    }
}
