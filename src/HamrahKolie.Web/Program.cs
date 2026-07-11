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

    // ── لایه‌های اپلیکیشن ────────────────────────────────────────────
    services.AddApplication();
    services.AddInfrastructure(config);

    // کاربر جاری و دسترسی‌ها
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUser, CurrentUser>();
    services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
    services.AddSingleton<ViteManifestService>();

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
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // کارایی
    services.AddOutputCache();
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
    services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");

    // Hangfire (Background Jobs)
    services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(config.GetConnectionString("Default"))));
    services.AddHangfireServer();

    var app = builder.Build();

    // ── Pipeline ────────────────────────────────────────────────────
    app.UseForwardedHeaders();
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

    app.UseRateLimiter();
    app.UseOutputCache();

    app.UseAuthentication();
    app.UseAuthorization();

    // داشبورد Hangfire فقط برای دارندگان دسترسی فنی
    app.UseHangfireDashboard("/jobs", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
    });

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapHealthChecks("/health");

    // ── Migration و Seed در راه‌اندازی ───────────────────────────────
    await MigrateAndSeedAsync(app);

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

// برای دسترسی تست‌های Integration به کلاس Program.
public partial class Program { }
