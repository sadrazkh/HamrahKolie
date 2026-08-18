using System.Globalization;
using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>محاسبه شاخص‌های زنده سایت از پایگاه داده (با Cache کوتاه).</summary>
public sealed class MetricsProvider : IMetricsProvider
{
    private const string CacheKey = "site:metrics";
    private static readonly CultureInfo Fa = new("fa-IR");
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MetricsProvider> _logger;

    /// <summary>
    /// کلید و برچسب هر شاخص، یک‌جا. مقدار پیش‌فرض و مقدار محاسبه‌شده هر دو از همین فهرست ساخته
    /// می‌شوند تا نتوانند از هم فاصله بگیرند؛ کلیدی که در قالب bind شده باشد و اینجا نباشد،
    /// روی صفحه اصلی به شکل متن خام {{ … }} ظاهر می‌شود.
    /// </summary>
    private static readonly (string Key, string Label)[] Definitions =
    [
        ("total_raised", "مجموع کمک‌های جذب‌شده (تومان)"),
        ("total_donations", "تعداد کمک‌ها"),
        ("donors", "تعداد حامیان"),
        ("patients_supported", "بیماران تحت حمایت"),
        ("support_requests", "کل درخواست‌های حمایت"),
        ("active_campaigns", "کمپین‌های فعال"),
        ("volunteers", "داوطلبان همراه"),
        ("centers", "مراکز دیالیز ثبت‌شده"),
        ("articles", "مقالات منتشرشده"),
        ("news", "اخبار منتشرشده"),
    ];

    public MetricsProvider(ApplicationDbContext db, IMemoryCache cache, ILogger<MetricsProvider> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// شاخص‌ها، و اگر دیتابیس در دسترس نباشد صفرها.
    ///
    /// این متد از داخل Views/Home/Index.cshtml صدا زده می‌شود، جایی که هیچ try/catch بالادستی
    /// وجود ندارد؛ استثنا از اینجا یعنی صفحه اصلی ۵۰۰ می‌دهد و health check میزبان رد می‌شود —
    /// یعنی کل سایت به‌خاطر ده عدد پایین می‌آید. صفر نمایش دادن بدتر از آن نیست.
    ///
    /// مقدار پیش‌فرض عمداً کش نمی‌شود: وگرنه اپی که درست بعد از دیتابیس بالا می‌آید تا پنج دقیقه
    /// صفر نشان می‌داد و کسی که تازه اتصال را درست کرده فکر می‌کرد هنوز خراب است.
    /// </summary>
    public async Task<IReadOnlyList<SiteMetric>> GetAllAsync(CancellationToken ct = default)
    {
        // پیش از هر کاری: اگر درخواست لغو شده، نه کوئری بزن نه آن را «خرابی دیتابیس» بشمار.
        ct.ThrowIfCancellationRequested();

        try
        {
            return (await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await ComputeAsync(ct);
            }))!;
        }
        catch (OperationCanceledException)
        {
            // انصراف خود درخواست است، نه خرابی دیتابیس.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "شاخص‌های سایت از پایگاه داده خوانده نشد؛ مقدار پیش‌فرض (صفر) نمایش داده می‌شود.");
            return Defaults();
        }
    }

    private static List<SiteMetric> Defaults() =>
        Definitions.Select(d => Metric(d.Key, d.Label, 0)).ToList();

    public async Task<IReadOnlyDictionary<string, SiteMetric>> GetMapAsync(CancellationToken ct = default)
        => (await GetAllAsync(ct)).ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

    private async Task<List<SiteMetric>> ComputeAsync(CancellationToken ct)
    {
        var succeeded = _db.Donations.AsNoTracking().Where(d => d.Status == PaymentStatus.Succeeded);
        var supportedStatuses = new[]
        {
            SupportRequestStatus.FinalApproved, SupportRequestStatus.SupportAssigned,
            SupportRequestStatus.InProgress, SupportRequestStatus.Completed
        };

        decimal totalRaised = await succeeded.SumAsync(d => (decimal?)d.Amount, ct) ?? 0;
        int totalDonations = await succeeded.CountAsync(ct);
        int donors = await _db.Donors.CountAsync(ct);
        int patients = await _db.SupportRequests.CountAsync(r => supportedStatuses.Contains(r.Status), ct);
        int requests = await _db.SupportRequests.CountAsync(ct);
        int activeCampaigns = await _db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Active, ct);
        int volunteers = await _db.Volunteers.CountAsync(
            v => v.Status == VolunteerStatus.Approved || v.Status == VolunteerStatus.Active, ct);
        int centers = await _db.DialysisCenters.CountAsync(c => c.IsApproved, ct);
        int articles = await _db.Contents.CountAsync(c => c.Type == ContentType.Article && c.Status == ContentStatus.Published, ct);
        int news = await _db.Contents.CountAsync(c => c.Type == ContentType.News && c.Status == ContentStatus.Published, ct);

        var values = new Dictionary<string, decimal>
        {
            ["total_raised"] = totalRaised,
            ["total_donations"] = totalDonations,
            ["donors"] = donors,
            ["patients_supported"] = patients,
            ["support_requests"] = requests,
            ["active_campaigns"] = activeCampaigns,
            ["volunteers"] = volunteers,
            ["centers"] = centers,
            ["articles"] = articles,
            ["news"] = news,
        };

        // از روی همان فهرستی که مقدار پیش‌فرض می‌سازد، تا کلیدها نتوانند از هم فاصله بگیرند.
        return Definitions.Select(d => Metric(d.Key, d.Label, values[d.Key])).ToList();
    }

    private static SiteMetric Metric(string key, string label, decimal value) =>
        new(key, label, value, value.ToString("N0", Fa));
}
