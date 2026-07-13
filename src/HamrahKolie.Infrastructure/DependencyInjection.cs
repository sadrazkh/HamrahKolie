using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Identity;
using HamrahKolie.Infrastructure.Identity;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Infrastructure.Persistence.Interceptors;
using HamrahKolie.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HamrahKolie.Infrastructure;

/// <summary>ثبت سرویس‌های لایه Infrastructure (پایگاه داده، Identity، Providerها).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var providerName = config["Database:Provider"] ?? "PostgreSql";
        var provider = Enum.TryParse<DatabaseProvider>(providerName, ignoreCase: true, out var p)
            ? p : DatabaseProvider.PostgreSql;

        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("رشته اتصال «Default» تنظیم نشده است.");

        services.AddMemoryCache();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            switch (provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString, sql =>
                        sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                           .EnableRetryOnFailure());
                    break;
                default:
                    options.UseNpgsql(connectionString, npg =>
                        npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                           .EnableRetryOnFailure());
                    break;
            }

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        // Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<PermissionClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        // سرویس‌های زیرساخت
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IAuditService, AuditService>();

        // سرویس‌های CMS
        services.AddSingleton<ISlugService, SlugService>();
        services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();
        services.AddSingleton<IStorageService, LocalStorageService>();
        services.AddScoped<HamrahKolie.Application.Cms.IContentService, ContentService>();
        services.AddScoped<HamrahKolie.Application.PageBuilder.IPageBuilderService, PageBuilderService>();

        // کمپین و کمک مالی
        services.AddScoped<HamrahKolie.Application.Campaigns.ICampaignService, CampaignService>();
        services.AddScoped<HamrahKolie.Application.Donations.IDonationService, DonationService>();

        // درخواست حمایت + OTP
        services.AddScoped<HamrahKolie.Application.SupportRequests.ISupportRequestService, SupportRequestService>();
        services.AddScoped<HamrahKolie.Application.Common.Interfaces.IOtpService, OtpService>();
        services.AddScoped<HamrahKolie.Application.Common.Interfaces.IOtpSender, DevOtpSender>();

        // درگاه پرداخت (قابل تعویض). پیش‌فرض: درگاه آزمایشی.
        var paymentProvider = config["Payment:Provider"] ?? "Fake";
        switch (paymentProvider.ToLowerInvariant())
        {
            // case "zarinpal": services.AddScoped<IPaymentGateway, ZarinpalGateway>(); break;
            default:
                services.AddScoped<HamrahKolie.Application.Payments.IPaymentGateway,
                    Payments.FakePaymentGateway>();
                break;
        }

        return services;
    }
}
