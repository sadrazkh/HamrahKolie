using System.Globalization;
using HamrahKolie.Application;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Infrastructure;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Infrastructure.Seed;
using HamrahKolie.Web.Infrastructure.Authorization;
using HamrahKolie.Web.Infrastructure.Hangfire;
using HamrahKolie.Web.Infrastructure.Identity;
using HamrahKolie.Web.Infrastructure.Vite;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, provider, cfg) => cfg
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(provider)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/hamrahkolie-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

    var services = builder.Services;
    var config = builder.Configuration;
    var presentationMode = builder.Environment.IsDevelopment()
        && config.GetValue<bool>("PresentationMode:Enabled");
    var skipDatabase = presentationMode
        && config.GetValue<bool>("PresentationMode:SkipDatabase");

    // ── لایه‌های اپلیکیشن ────────────────────────────────────────────
    services.AddApplication();
    services.AddInfrastructure(config);

    // کاربر جاری و دسترسی‌ها
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUser, CurrentUser>();
    services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
    services.AddSingleton<ViteManifestService>();
    services.AddScoped<HamrahKolie.Application.Common.Interfaces.IOutputCacheInvalidator,
        HamrahKolie.Web.Infrastructure.OutputCacheInvalidator>();
    services.AddScoped<HamrahKolie.Web.Services.IFileUploadService, HamrahKolie.Web.Services.FileUploadService>();

    // کوکی ورود
    services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    // MVC + Localization
    services.AddControllersWithViews()
        .AddViewLocalization()
        .AddDataAnnotationsLocalization();
    services.AddLocalization(o => o.ResourcesPath = "Resources");

    services.Configure<RequestLocalizationOptions>(options =>
    {
        var supported = new[] { new CultureInfo("fa-IR"), new CultureInfo("en-US") };
        options.DefaultRequestCulture = new RequestCulture("fa-IR");
        options.SupportedCultures = supported;
        options.SupportedUICultures = supported;
    });

    // پشتیبانی از هدرهای Forwarded هنگام اجرا پشت Reverse Proxy (Nginx/Docker)
    services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
            Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // نشست (برای ناحیه پیگیری درخواست حمایت پس از تأیید OTP)
    services.AddSession(o =>
    {
        o.IdleTimeout = TimeSpan.FromMinutes(20);
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
    });

    // کارایی
    services.AddOutputCache(options =>
    {
        // سیاست کش صفحات عمومی با تگ «content» تا پس از انتشار محتوا بتوان آن را باطل کرد.
        options.AddPolicy("PublicContent", b => b
            .Expire(TimeSpan.FromMinutes(5))
            .Tag("content")
            .SetVaryByQuery("page", "province", "category", "tag", "search"));
    });
    services.AddResponseCompression();

    // محدودسازی نرخ (روی فرم‌های عمومی حساس اعمال می‌شود)
    services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("public-forms", o =>
        {
            o.PermitLimit = 10;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueLimit = 0;
        });
    });

    // Health Checks
    var healthChecks = services.AddHealthChecks();
    if (!skipDatabase)
    {
        healthChecks.AddDbContextCheck<ApplicationDbContext>("database");
    }

    // بررسی دسترس‌پذیری دیتابیس؛ اگر در دسترس نباشد، سرویس‌های وابسته (Hangfire) رد می‌شوند
    // تا اپلیکیشن به‌جای Crash، بالا بیاید و بخش‌های عمومی سایت کار کنند.
    var dbReachable = !skipDatabase && IsDatabaseReachable(
        config["Database:Provider"] ?? "PostgreSql",
        config.GetConnectionString("Default"));

    if (skipDatabase)
    {
        Log.Warning("Presentation mode is enabled. Database checks, migrations, seed and Hangfire are disabled.");
    }
    else if (!dbReachable)
    {
        Log.Warning("اتصال به پایگاه داده برقرار نشد. اپلیکیشن بالا می‌آید اما بخش‌های وابسته به دیتابیس " +
                    "(ورود، پنل، Hangfire) تا زمان تنظیم صحیح رشته اتصال فعال نخواهند بود.");
    }

    // Hangfire (Background Jobs) — فقط در صورت دسترس‌پذیری دیتابیس
    if (dbReachable)
    {
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(config.GetConnectionString("Default"))));
        services.AddHangfireServer();
    }

    var app = builder.Build();

    // ── Pipeline ────────────────────────────────────────────────────
    app.UseForwardedHeaders();

    // هدرهای امنیتی
    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-XSS-Protection"] = "0";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' data: https:; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "font-src 'self' https://cdn.jsdelivr.net; " +
            "script-src 'self' 'unsafe-inline'; " +
            "connect-src 'self'; frame-ancestors 'self'; base-uri 'self'; form-action 'self'";
        await next();
    });

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");
    app.UseHttpsRedirection();
    app.UseResponseCompression();
    app.UseStaticFiles();

    app.UseRequestLocalization();
    app.UseRouting();

    app.UseSession();
    app.UseRateLimiter();
    app.UseOutputCache();

    app.UseAuthentication();
    app.UseAuthorization();

    // داشبورد Hangfire فقط برای دارندگان دسترسی فنی (و فقط اگر Hangfire فعال شده باشد)
    if (dbReachable)
    {
        app.UseHangfireDashboard("/jobs", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
        });
    }

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapHealthChecks("/health");

    // ── Migration و Seed در راه‌اندازی ───────────────────────────────
    if (dbReachable)
    {
        await MigrateAndSeedAsync(app);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "برنامه به‌صورت غیرمنتظره متوقف شد.");
}
finally
{
    Log.CloseAndFlush();
}

static async Task MigrateAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(sp);
        logger.LogInformation("پایگاه داده به‌روزرسانی و داده اولیه بارگذاری شد.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطا در Migration/Seed پایگاه داده.");
    }
}

static bool IsDatabaseReachable(string provider, string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString)) return false;
    try
    {
        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 3
            };
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(sb.ConnectionString);
            conn.Open();
            return true;
        }
        else
        {
            var sb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
            {
                Timeout = 3,
                CommandTimeout = 3
            };
            using var conn = new Npgsql.NpgsqlConnection(sb.ConnectionString);
            conn.Open();
            return true;
        }
    }
    catch
    {
        return false;
    }
}

// برای دسترسی تست‌های Integration به کلاس Program.
public partial class Program { }
