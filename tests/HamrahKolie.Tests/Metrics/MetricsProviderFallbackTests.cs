using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace HamrahKolie.Tests.Metrics;

/// <summary>
/// شاخص‌های صفحه اصلی وقتی دیتابیس در دسترس نیست.
///
/// کل اپ عمداً بدون دیتابیس هم بالا می‌آید: HomeController هر فراخوانی دیتابیس را در try/catch
/// گذاشته و NavMenu فهرست پیش‌فرض دارد. ولی Views/Home/Index.cshtml خودش
/// <c>Metrics.GetMapAsync()</c> را صدا می‌زند — داخل View، جایی که هیچ catch‌ای نیست — و
/// MetricsProvider هیچ مقدار پیش‌فرضی نداشت. نتیجه: صفحه اصلی ۵۰۰ می‌داد، health check هاربورا
/// که همان «/» را می‌زند رد می‌شد، و کل دیپلوی Failed می‌خورد؛ در حالی که تنها چیزِ ازکارافتاده
/// ده عدد روی صفحه بود.
/// </summary>
public class MetricsProviderFallbackTests
{
    /// <summary>
    /// یک Context که هر کوئری روی آن می‌افتد. Dispose شده یعنی خطا قطعی و فوری است — بدون
    /// نیاز به دیتابیس واقعی و بدون انتظار برای timeout شبکه.
    /// </summary>
    private static ApplicationDbContext BrokenContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=nope;Username=u;Password=p")
            .Options;
        var db = new ApplicationDbContext(options);
        db.Dispose();
        return db;
    }

    private static MetricsProvider Provider() =>
        new(BrokenContext(), new MemoryCache(new MemoryCacheOptions()), NullLogger<MetricsProvider>.Instance);

    [Fact]
    public async Task وقتی_دیتابیس_نیست_به_جای_استثنا_مقدار_پیشفرض_برمیگردد()
    {
        var metrics = await Provider().GetAllAsync();

        Assert.NotEmpty(metrics);
        Assert.All(metrics, m => Assert.Equal(0m, m.Value));
    }

    [Fact]
    public async Task همه_کلیدهایی_که_قالب_به_آنها_bind_میکند_موجودند()
    {
        // Index.cshtml این نام‌ها را داخل {{ }} می‌نویسد. کلید غایب یعنی متن خام {{ … }} روی
        // صفحه، که از عدد صفر بدتر است.
        var map = await Provider().GetMapAsync();

        Assert.Equal(
            [
                "active_campaigns", "articles", "centers", "donors", "news",
                "patients_supported", "support_requests", "total_donations",
                "total_raised", "volunteers"
            ],
            map.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task هر_شاخص_متن_آمادهٔ_نمایش_دارد()
    {
        // قالب Formatted را مستقیم چاپ می‌کند؛ رشته تهی یعنی یک جای خالی روی صفحه اصلی.
        foreach (var metric in await Provider().GetAllAsync())
        {
            Assert.False(string.IsNullOrWhiteSpace(metric.Formatted));
            Assert.False(string.IsNullOrWhiteSpace(metric.Label));
        }
    }

    [Fact]
    public async Task شکست_کش_نمیشود()
    {
        // اگر مقدار پیش‌فرض پنج دقیقه کش می‌شد، اپی که درست بعد از دیتابیس بالا می‌آید تا پنج
        // دقیقه صفر نشان می‌داد — و کسی که تازه اتصال را درست کرده فکر می‌کرد هنوز خراب است.
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new MetricsProvider(BrokenContext(), cache, NullLogger<MetricsProvider>.Instance);

        await provider.GetAllAsync();

        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task لغو_درخواست_قورت_داده_نمیشود()
    {
        // یک catch فراگیر، انصراف کاربر را هم به «دیتابیس خراب است» ترجمه می‌کند و لاگ را پر
        // می‌کند از خطایی که اتفاق نیفتاده.
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Provider().GetAllAsync(cancelled.Token));
    }
}
