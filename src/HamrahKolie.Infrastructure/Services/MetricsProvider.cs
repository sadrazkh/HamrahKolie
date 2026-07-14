using System.Globalization;
using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>محاسبه شاخص‌های زنده سایت از پایگاه داده (با Cache کوتاه).</summary>
public sealed class MetricsProvider : IMetricsProvider
{
    private const string CacheKey = "site:metrics";
    private static readonly CultureInfo Fa = new("fa-IR");
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public MetricsProvider(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<SiteMetric>> GetAllAsync(CancellationToken ct = default)
    {
        return (await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await ComputeAsync(ct);
        }))!;
    }

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

        SiteMetric Num(string key, string label, decimal value) => new(key, label, value, value.ToString("N0", Fa));

        return new List<SiteMetric>
        {
            Num("total_raised", "مجموع کمک‌های جذب‌شده (تومان)", totalRaised),
            Num("total_donations", "تعداد کمک‌ها", totalDonations),
            Num("donors", "تعداد حامیان", donors),
            Num("patients_supported", "بیماران تحت حمایت", patients),
            Num("support_requests", "کل درخواست‌های حمایت", requests),
            Num("active_campaigns", "کمپین‌های فعال", activeCampaigns),
            Num("volunteers", "داوطلبان همراه", volunteers),
            Num("centers", "مراکز دیالیز ثبت‌شده", centers),
            Num("articles", "مقالات منتشرشده", articles),
            Num("news", "اخبار منتشرشده", news),
        };
    }
}
