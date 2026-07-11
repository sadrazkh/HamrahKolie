using HamrahKolie.Application.Authorization;
using HamrahKolie.Domain.Identity;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HamrahKolie.Infrastructure.Seed;

/// <summary>
/// داده اولیه سامانه را می‌سازد: دسترسی‌ها، نقش‌ها، اتصال نقش-دسترسی، تنظیمات پایه
/// و حساب Super Admin (از Environment Variable، نه رمز ثابت در کد).
/// این متد Idempotent است و اجرای مکرر آن مشکلی ایجاد نمی‌کند.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await SeedPermissionsAsync(db, ct);
        await SeedRolesAndPermissionsAsync(db, roleManager, ct);
        await SeedSettingsAsync(db, ct);
        await SeedSuperAdminAsync(userManager, config, logger, ct);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Key).ToListAsync(ct);
        var toAdd = Permissions.All
            .Where(p => !existing.Contains(p.Key))
            .Select(p => new Permission { Key = p.Key, DisplayName = p.DisplayName, Group = p.Group });

        db.Permissions.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRolesAndPermissionsAsync(
        ApplicationDbContext db, RoleManager<ApplicationRole> roleManager, CancellationToken ct)
    {
        var allPermissions = await db.Permissions.ToDictionaryAsync(p => p.Key, ct);

        foreach (var def in Roles.All)
        {
            var role = await roleManager.FindByNameAsync(def.Name);
            if (role is null)
            {
                role = new ApplicationRole(def.Name)
                {
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    IsSystemRole = true,
                };
                await roleManager.CreateAsync(role);
            }

            // Super Admin دسترسی‌ها را از طریق Claim فراگیر می‌گیرد، نیازی به ثبت تک‌تک نیست.
            if (def.Permissions.Length == 1 && def.Permissions[0] == "*")
                continue;

            var currentPermIds = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(ct);

            foreach (var permKey in def.Permissions)
            {
                if (!allPermissions.TryGetValue(permKey, out var perm)) continue;
                if (currentPermIds.Contains(perm.Id)) continue;

                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var defaults = new (string Key, string? Value, string Group)[]
        {
            ("organization.name", "همراه کلیه", "organization"),
            ("organization.slogan", "هیچ مسیر سختی با همراهی شما دور نخواهد بود", "organization"),
            ("organization.message", "از روستا تا زندگی", "organization"),
            ("organization.email", "", "organization"),
            ("organization.phone", "", "organization"),
            ("site.default_language", "fa", "general"),
            ("site.timezone", "Asia/Tehran", "general"),
            ("site.maintenance_mode", "false", "general"),
            ("seo.default_title", "همراه کلیه — سامانه حمایت از بیماران دیالیزی", "seo"),
            ("seo.default_description", "مؤسسه خیریه حمایت از بیماران کلیوی و دیالیزی مناطق محروم و روستایی", "seo"),
        };

        var existing = await db.Settings.Select(s => s.Key).ToListAsync(ct);
        foreach (var (key, value, group) in defaults)
        {
            if (existing.Contains(key)) continue;
            db.Settings.Add(new Domain.Entities.Setting { Key = key, Value = value, Group = group });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSuperAdminAsync(
        UserManager<ApplicationUser> userManager, IConfiguration config, ILogger logger, CancellationToken ct)
    {
        // اطلاعات از Environment Variable / appsettings خوانده می‌شود؛ رمز ثابت در کد وجود ندارد.
        var email = config["SuperAdmin:Email"] ?? Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL");
        var password = config["SuperAdmin:Password"] ?? Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "حساب Super Admin ساخته نشد: متغیرهای SUPERADMIN_EMAIL و SUPERADMIN_PASSWORD تنظیم نشده‌اند. " +
                "می‌توانید از طریق Setup Wizard حساب را بسازید.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "مدیر",
            LastName = "ارشد",
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
            logger.LogInformation("حساب Super Admin با ایمیل {Email} ساخته شد.", email);
        }
        else
        {
            logger.LogError("ساخت Super Admin ناموفق بود: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
