using HamrahKolie.Application.Authorization;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
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
        await SeedCmsAsync(db, ct);
    }

    /// <summary>
    /// فهرست سکشن‌های پیش‌فرض صفحه اصلی. این متد به‌صورت خودکار در Seed اجرا نمی‌شود؛
    /// از پنل (صفحه‌ساز → «ایجاد سکشن‌های پیش‌فرض») فراخوانی می‌شود تا طرح سفارشی فعلی
    /// صفحه اصلی حفظ شود و مدیر فقط در صورت تمایل از صفحه‌ساز استفاده کند.
    /// </summary>
    public static List<PageSection> BuildDefaultHomeSections()
    {
        return new List<PageSection>
        {
            new()
            {
                PageKey = "home", Type = SectionType.Hero, SortOrder = 1, IsEnabled = true, IsPublished = true,
                Subtitle = "با حمایت شما، مسیر سخت درمان را برای بیماران دیالیزی مناطق محروم هموار می‌کنیم؛ تا آن‌ها به زندگی و خانواده‌ی خود بازگردند.",
                Title = "از روستا تا زندگی",
                ButtonText = "❤ حمایت مالی", ButtonUrl = "/Home/Donate",
                SecondaryButtonText = "آشنایی با فعالیت‌ها", SecondaryButtonUrl = "/Home/Services",
            },
            new()
            {
                PageKey = "home", Type = SectionType.FeatureCards, SortOrder = 2, IsEnabled = true, IsPublished = true,
                Title = "چرا بیماران روستایی به همراهی نیاز دارند؟",
                Subtitle = "درمان دیالیز پرهزینه و مداوم است؛ برای خانواده‌های روستایی، فاصله و هزینه به بحرانی روزمره تبدیل می‌شود.",
                SettingsJson = """{"cards":[{"title":"فاصله و مسیر سخت","text":"رفت‌وآمد مکرر به مراکز دیالیز شهری، توان و زمان خانواده را می‌فرساید."},{"title":"هزینه‌های درمان","text":"دارو، جلسات دیالیز و ایاب‌وذهاب، فشار مالی سنگینی بر دوش خانوار می‌گذارد."},{"title":"خستگی و نگرانی","text":"بیمار و خانواده‌اش به حمایت روانی و اجتماعی در کنار درمان نیاز دارند."}]}""",
            },
            new()
            {
                PageKey = "home", Type = SectionType.Stats, SortOrder = 3, IsEnabled = true, IsPublished = true,
                Title = "مسیر ما در یک نگاه", Background = SectionBackground.Surface,
                SettingsJson = """{"stats":[{"value":"۱۲۰","label":"بیمار تحت حمایت"},{"value":"۴۵","label":"روستای تحت پوشش"},{"value":"۱۸٬۰۰۰","label":"جلسه دیالیز حمایت‌شده"},{"value":"۳۵۰","label":"خیر همراه"}]}""",
            },
            new()
            {
                PageKey = "home", Type = SectionType.Steps, SortOrder = 4, IsEnabled = true, IsPublished = true,
                Title = "چطور کمک می‌کنیم؟",
                SettingsJson = """{"cards":[{"title":"شناسایی بیمار","text":"معرفی از سوی مراکز درمانی و مددکاران."},{"title":"بررسی و تأیید","text":"ارزیابی مددکاری و پزشکی با رعایت حریم خصوصی."},{"title":"جذب حمایت","text":"همراهی خیرین برای تأمین هزینه‌ها."},{"title":"ادامه زندگی","text":"تداوم درمان و بازگشت بیمار به زندگی."}]}""",
            },
            new()
            {
                PageKey = "home", Type = SectionType.LatestContent, SortOrder = 5, IsEnabled = true, IsPublished = true,
                Title = "آخرین اخبار و مطالب", Background = SectionBackground.Surface,
                SettingsJson = """{"count":3}""",
            },
            new()
            {
                PageKey = "home", Type = SectionType.CallToAction, SortOrder = 6, IsEnabled = true, IsPublished = true,
                Title = "همراه ما شوید",
                Subtitle = "هر همراهی، یک قدم به بازگشت بیمار به زندگی نزدیک‌تر است.",
                ButtonText = "حمایت مالی", ButtonUrl = "/Home/Donate", Background = SectionBackground.Tint,
            },
        };
    }

    private static async Task SeedCmsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Contents.AnyAsync(ct)) return; // فقط یک‌بار

        // دسته‌بندی‌ها
        var catEducation = new Category { Name = "آموزش بیماران", Slug = "amoozesh-bimaran", Description = "مطالب آموزشی برای بیماران دیالیزی و خانواده‌ها" };
        var catNews = new Category { Name = "اخبار مؤسسه", Slug = "akhbar-moassese" };
        var catHealth = new Category { Name = "سلامت کلیه", Slug = "salamat-kolie" };
        db.Categories.AddRange(catEducation, catNews, catHealth);
        await db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;

        Content Make(ContentType type, string title, string slug, string summary, string body, Category? cat, int daysAgo) => new()
        {
            Type = type,
            Title = title,
            Slug = slug,
            Summary = summary,
            Body = body,
            Status = ContentStatus.Published,
            PublishedAt = now.AddDays(-daysAgo),
            CategoryId = cat?.Id,
            Language = "fa",
            Seo = new Domain.Entities.SeoMetadata { MetaDescription = summary }
        };

        db.Contents.AddRange(
            Make(ContentType.Article, "آشنایی با دیالیز و مراحل آن",
                "ashnaei-ba-dializ",
                "دیالیز چیست، چه زمانی لازم می‌شود و بیمار چگونه برای جلسات آماده می‌شود.",
                "<h2>دیالیز چیست؟</h2><p>دیالیز فرایندی است که وظیفه پالایش خون را در نبود عملکرد کافی کلیه بر عهده می‌گیرد. این مطلب صرفاً جنبه آموزشی دارد و جایگزین توصیه پزشک نیست.</p><h3>آمادگی برای جلسه</h3><ul><li>رعایت رژیم غذایی توصیه‌شده</li><li>مصرف به‌موقع داروها</li><li>حضور به‌موقع در مرکز</li></ul>",
                catEducation, 2),
            Make(ContentType.Article, "تغذیه مناسب بیماران دیالیزی",
                "taghzie-bimaran-dializi",
                "اصول کلی تغذیه و نکات مهم درباره مصرف مایعات، پتاسیم و فسفر.",
                "<p>تغذیه نقش مهمی در کیفیت زندگی بیماران دیالیزی دارد. توصیه‌های دقیق باید توسط پزشک و کارشناس تغذیه ارائه شود.</p><blockquote>این محتوا جایگزین مشاوره تخصصی نیست.</blockquote>",
                catHealth, 5),
            Make(ContentType.News, "آغاز کمپین حمایت از بیماران روستایی",
                "aghaz-campaign-roostaei",
                "کمپین جدید مؤسسه برای تأمین هزینه رفت‌وآمد بیماران دیالیزی مناطق دور آغاز شد.",
                "<p>مؤسسه همراه کلیه کمپین تازه‌ای را برای کاهش بار ایاب‌وذهاب بیماران روستایی آغاز کرد. همراهی شما این مسیر را هموارتر می‌کند.</p>",
                catNews, 1),
            Make(ContentType.News, "گزارش عملکرد فصل بهار",
                "gozaresh-bahar",
                "خلاصه‌ای از فعالیت‌ها و حمایت‌های انجام‌شده در فصل بهار.",
                "<p>در فصل بهار، با همراهی خیرین، تعداد قابل‌توجهی جلسه دیالیز حمایت شد. جزئیات در گزارش شفافیت ارائه می‌شود.</p>",
                catNews, 12),
            Make(ContentType.PatientStory, "روایت امید: بازگشت به زندگی",
                "revayat-omid",
                "داستان یک بیمار روستایی که با همراهی خیرین درمان خود را ادامه داد. (با رعایت حریم خصوصی)",
                "<p>این روایت با رضایت و با رعایت کامل حریم خصوصی منتشر شده و اطلاعات هویتی در آن وجود ندارد.</p>",
                catEducation, 8)
        );
        await db.SaveChangesAsync(ct);

        // منوها
        var header = new Menu { Name = "منوی اصلی", Location = MenuLocation.Header };
        var footer = new Menu { Name = "منوی پاورقی", Location = MenuLocation.Footer };
        db.Menus.AddRange(header, footer);
        await db.SaveChangesAsync(ct);

        db.MenuItems.AddRange(
            new MenuItem { MenuId = header.Id, Title = "صفحه اصلی", Url = "/", SortOrder = 1 },
            new MenuItem { MenuId = header.Id, Title = "درباره ما", Url = "/Home/About", SortOrder = 2 },
            new MenuItem { MenuId = header.Id, Title = "خدمات حمایتی", Url = "/Home/Services", SortOrder = 3 },
            new MenuItem { MenuId = header.Id, Title = "اخبار", Url = "/news", SortOrder = 4 },
            new MenuItem { MenuId = header.Id, Title = "مقالات", Url = "/articles", SortOrder = 5 },
            new MenuItem { MenuId = header.Id, Title = "تماس با ما", Url = "/Home/Contact", SortOrder = 6 },
            new MenuItem { MenuId = footer.Id, Title = "درباره مؤسسه", Url = "/Home/About", SortOrder = 1 },
            new MenuItem { MenuId = footer.Id, Title = "اخبار", Url = "/news", SortOrder = 2 },
            new MenuItem { MenuId = footer.Id, Title = "مقالات آموزشی", Url = "/articles", SortOrder = 3 },
            new MenuItem { MenuId = footer.Id, Title = "حمایت مالی", Url = "/Home/Donate", SortOrder = 4 }
        );
        await db.SaveChangesAsync(ct);
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
