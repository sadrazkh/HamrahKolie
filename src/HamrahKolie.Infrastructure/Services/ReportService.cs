using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Reports;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ReportService(ApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ManagementReport> GetManagementReportAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var today = now.Date;

        var succeeded = _db.Donations.AsNoTracking().Where(d => d.Status == PaymentStatus.Succeeded);

        async Task<MoneyStat> Sum(IQueryable<Domain.Entities.Donation> q)
        {
            var r = await q.GroupBy(_ => 1)
                .Select(g => new { Amount = g.Sum(x => (decimal?)x.Amount) ?? 0, Count = g.Count() })
                .FirstOrDefaultAsync(ct);
            return new MoneyStat(r?.Amount ?? 0, r?.Count ?? 0);
        }

        var since7 = today.AddDays(-7);
        var since30 = today.AddDays(-30);
        var since365 = today.AddDays(-365);

        var report = new ManagementReport
        {
            Today = await Sum(succeeded.Where(d => d.CompletedAt >= today)),
            ThisWeek = await Sum(succeeded.Where(d => d.CompletedAt >= since7)),
            ThisMonth = await Sum(succeeded.Where(d => d.CompletedAt >= since30)),
            ThisYear = await Sum(succeeded.Where(d => d.CompletedAt >= since365)),
            AllTime = await Sum(succeeded),
            Online = await Sum(succeeded.Where(d => d.Method == PaymentMethod.Online)),
            Offline = await Sum(succeeded.Where(d => d.Method == PaymentMethod.Offline)),
            NewDonors = await _db.Donors.CountAsync(d => d.DonationCount == 1, ct),
            RepeatDonors = await _db.Donors.CountAsync(d => d.DonationCount > 1, ct),
        };
        report.AverageDonation = report.AllTime.Count == 0 ? 0
            : Math.Round(report.AllTime.Amount / report.AllTime.Count);

        report.Campaigns = await _db.Campaigns.AsNoTracking()
            .OrderByDescending(c => c.CollectedAmount).Take(10)
            .Select(c => new CampaignProgress(c.Title, c.Slug, c.GoalAmount, c.CollectedAmount, c.SupporterCount,
                c.GoalAmount <= 0 ? 0 : (int)Math.Min(100, Math.Round(c.CollectedAmount / c.GoalAmount * 100))))
            .ToListAsync(ct);

        var srByStatus = await _db.SupportRequests.AsNoTracking()
            .GroupBy(r => r.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        report.SupportRequestsByStatus = srByStatus
            .Select(x => new LabeledCount(StatusName(x.Key), x.Count)).ToList();

        var srByProvince = await _db.SupportRequests.AsNoTracking()
            .Where(r => r.Province != null)
            .GroupBy(r => r.Province!).Select(g => new { Province = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(10).ToListAsync(ct);
        report.SupportRequestsByProvince = srByProvince.Select(x => new LabeledCount(x.Province, x.Count)).ToList();

        var volByStatus = await _db.Volunteers.AsNoTracking()
            .GroupBy(v => v.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        report.VolunteersByStatus = volByStatus.Select(x => new LabeledCount(VolunteerStatusName(x.Key), x.Count)).ToList();

        return report;
    }

    public async Task<TransparencyReport> GetTransparencyReportAsync(CancellationToken ct = default)
    {
        var succeeded = _db.Donations.AsNoTracking().Where(d => d.Status == PaymentStatus.Succeeded);
        var supportedStatuses = new[]
        {
            SupportRequestStatus.FinalApproved, SupportRequestStatus.SupportAssigned,
            SupportRequestStatus.InProgress, SupportRequestStatus.Completed
        };

        return new TransparencyReport
        {
            TotalRaised = await succeeded.SumAsync(d => (decimal?)d.Amount, ct) ?? 0,
            TotalDonations = await succeeded.CountAsync(ct),
            ActiveCampaigns = await _db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Active, ct),
            PatientsSupported = await _db.SupportRequests.CountAsync(r => supportedStatuses.Contains(r.Status), ct),
            RegisteredVolunteers = await _db.Volunteers.CountAsync(
                v => v.Status == VolunteerStatus.Approved || v.Status == VolunteerStatus.Active, ct),
            Campaigns = await _db.Campaigns.AsNoTracking()
                .Where(c => c.Status == CampaignStatus.Active || c.Status == CampaignStatus.Successful)
                .OrderByDescending(c => c.CollectedAmount).Take(6)
                .Select(c => new CampaignProgress(c.Title, c.Slug, c.GoalAmount, c.CollectedAmount, c.SupporterCount,
                    c.GoalAmount <= 0 ? 0 : (int)Math.Min(100, Math.Round(c.CollectedAmount / c.GoalAmount * 100))))
                .ToListAsync(ct),
        };
    }

    private static string StatusName(SupportRequestStatus s) => s switch
    {
        SupportRequestStatus.Submitted => "ثبت اولیه",
        SupportRequestStatus.PendingReview => "در انتظار بررسی",
        SupportRequestStatus.NeedsDocuments => "نیازمند مدارک",
        SupportRequestStatus.SocialWorkerReview => "بررسی مددکار",
        SupportRequestStatus.MedicalReview => "بررسی پزشکی",
        SupportRequestStatus.PreliminaryApproved => "تأیید اولیه",
        SupportRequestStatus.Rejected => "ردشده",
        SupportRequestStatus.FinalApproved => "تأیید نهایی",
        SupportRequestStatus.SupportAssigned => "تخصیص حمایت",
        SupportRequestStatus.InProgress => "در حال اجرا",
        SupportRequestStatus.Completed => "تکمیل‌شده",
        SupportRequestStatus.Archived => "بایگانی",
        _ => s.ToString()
    };

    private static string VolunteerStatusName(VolunteerStatus s) => s switch
    {
        VolunteerStatus.Pending => "در انتظار",
        VolunteerStatus.Approved => "تأییدشده",
        VolunteerStatus.Active => "فعال",
        VolunteerStatus.Inactive => "غیرفعال",
        VolunteerStatus.Blacklisted => "لیست سیاه",
        _ => s.ToString()
    };
}
