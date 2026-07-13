using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Application.Donations;
using HamrahKolie.Application.Payments;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class DonationService : IDonationService
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IDateTimeProvider _clock;

    public DonationService(ApplicationDbContext db, IPaymentGateway gateway, IDateTimeProvider clock)
    {
        _db = db;
        _gateway = gateway;
        _clock = clock;
    }

    public async Task<CreateOnlineResult> CreateOnlineAsync(DonationInput input, string callbackUrl, CancellationToken ct = default)
    {
        var campaign = await ValidateCampaignAsync(input.CampaignId, input.Amount, ct);
        if (input.CampaignId is not null && campaign is null)
            return CreateOnlineResult.Fail("کمپین انتخاب‌شده در دسترس نیست.");

        var donation = new Donation
        {
            TrackingCode = await GenerateTrackingAsync(ct),
            Amount = decimal.Truncate(input.Amount),
            Type = input.CampaignId is not null ? DonationType.Campaign : input.Type,
            Method = PaymentMethod.Online,
            Status = PaymentStatus.Pending,
            DonorName = input.DonorName.Trim(),
            DonorMobile = input.DonorMobile.Trim(),
            DonorEmail = input.DonorEmail?.Trim(),
            IsAnonymous = input.IsAnonymous,
            ShowNamePublicly = input.ShowNamePublicly && !input.IsAnonymous,
            Note = input.Note?.Trim(),
            CampaignId = campaign?.Id,
        };

        var payment = new Payment
        {
            Donation = donation,
            Provider = _gateway.Name,
            Amount = donation.Amount,
            Status = PaymentStatus.Pending,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
        };

        _db.Donations.Add(donation);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        var initiation = new PaymentInitiation(
            payment.Id, payment.Amount,
            $"کمک به همراه کلیه — {donation.TrackingCode}",
            callbackUrl, donation.DonorMobile, donation.DonorEmail);

        var result = await _gateway.RequestAsync(initiation, ct);
        if (!result.Success || result.RedirectUrl is null)
        {
            payment.Status = PaymentStatus.Failed;
            donation.Status = PaymentStatus.Failed;
            await _db.SaveChangesAsync(ct);
            return CreateOnlineResult.Fail(result.Error ?? "خطا در اتصال به درگاه پرداخت.");
        }

        payment.Authority = result.Authority;
        await _db.SaveChangesAsync(ct);

        return CreateOnlineResult.Ok(donation.TrackingCode, result.RedirectUrl);
    }

    public async Task<CallbackResult> HandleCallbackAsync(
        string authority, IReadOnlyDictionary<string, string> callbackParams, CancellationToken ct = default)
    {
        var payment = await _db.Payments
            .Include(p => p.Donation).ThenInclude(d => d.Campaign)
            .FirstOrDefaultAsync(p => p.Authority == authority, ct);

        if (payment is null) return new CallbackResult(false, null, "تراکنش یافت نشد.");
        var donation = payment.Donation;

        // Idempotency: اگر قبلاً تأیید شده، همان نتیجه موفق بازگردد.
        if (payment.Status == PaymentStatus.Succeeded)
            return new CallbackResult(true, donation.TrackingCode, null);

        if (payment.Status != PaymentStatus.Pending)
            return new CallbackResult(false, donation.TrackingCode, "این تراکنش قبلاً پردازش شده است.");

        var verify = await _gateway.VerifyAsync(new PaymentVerification(authority, payment.Amount, callbackParams), ct);

        if (verify.Success)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.ReferenceId = verify.ReferenceId;
            payment.PaidAt = _clock.UtcNow;
            payment.RawResponse = verify.RawResponse;

            donation.Status = PaymentStatus.Succeeded;
            donation.CompletedAt = _clock.UtcNow;

            await ApplySuccessSideEffectsAsync(donation, ct);
            await _db.SaveChangesAsync(ct);
            return new CallbackResult(true, donation.TrackingCode, null);
        }

        payment.Status = PaymentStatus.Failed;
        payment.RawResponse = verify.RawResponse;
        donation.Status = PaymentStatus.Failed;
        await _db.SaveChangesAsync(ct);
        return new CallbackResult(false, donation.TrackingCode, verify.Error);
    }

    public async Task<string> SubmitOfflineAsync(OfflineDonationInput input, CancellationToken ct = default)
    {
        var campaign = await ValidateCampaignAsync(input.CampaignId, input.Amount, ct);

        var donation = new Donation
        {
            TrackingCode = await GenerateTrackingAsync(ct),
            Amount = decimal.Truncate(input.Amount),
            Type = input.CampaignId is not null ? DonationType.Campaign : input.Type,
            Method = PaymentMethod.Offline,
            Status = PaymentStatus.Pending,
            DonorName = input.DonorName.Trim(),
            DonorMobile = input.DonorMobile.Trim(),
            DonorEmail = input.DonorEmail?.Trim(),
            IsAnonymous = input.IsAnonymous,
            ShowNamePublicly = input.ShowNamePublicly && !input.IsAnonymous,
            Note = input.Note?.Trim(),
            CampaignId = campaign?.Id,
            OfflinePayment = new OfflinePayment
            {
                ReceiptImageId = input.ReceiptImageId,
                ReferenceNumber = input.ReferenceNumber?.Trim(),
                ReviewStatus = OfflineReviewStatus.Pending,
            }
        };

        _db.Donations.Add(donation);
        await _db.SaveChangesAsync(ct);
        return donation.TrackingCode;
    }

    public async Task<Donation?> GetByTrackingAsync(string trackingCode, string mobile, CancellationToken ct = default)
    {
        var code = trackingCode.Trim().ToUpperInvariant();
        var m = mobile.Trim();
        return await _db.Donations
            .AsNoTracking()
            .Include(d => d.Campaign)
            .Include(d => d.Payment)
            .Include(d => d.OfflinePayment)
            .FirstOrDefaultAsync(d => d.TrackingCode == code && d.DonorMobile == m, ct);
    }

    public Task<Donation?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct = default)
    {
        var code = trackingCode.Trim().ToUpperInvariant();
        return _db.Donations.AsNoTracking()
            .Include(d => d.Campaign).Include(d => d.Payment).Include(d => d.OfflinePayment)
            .FirstOrDefaultAsync(d => d.TrackingCode == code, ct);
    }

    // ── مدیریت ───────────────────────────────────────────────────
    public async Task<PagedResult<Donation>> GetAdminListAsync(
        PaymentStatus? status, PaymentMethod? method, long? campaignId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.Donations.AsNoTracking().Include(d => d.Campaign).AsQueryable();
        if (status is not null) q = q.Where(d => d.Status == status);
        if (method is not null) q = q.Where(d => d.Method == method);
        if (campaignId is not null) q = q.Where(d => d.CampaignId == campaignId);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Donation> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<Donation?> GetAdminDetailAsync(long id, CancellationToken ct = default)
        => _db.Donations.AsNoTracking()
            .Include(d => d.Campaign).Include(d => d.Payment)
            .Include(d => d.OfflinePayment).ThenInclude(o => o!.ReceiptImage)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<int> GetPendingOfflineCountAsync(CancellationToken ct = default)
        => _db.OfflinePayments.CountAsync(o => o.ReviewStatus == OfflineReviewStatus.Pending, ct);

    public async Task<bool> ApproveOfflineAsync(long donationId, string? reviewer, string? note, CancellationToken ct = default)
    {
        var donation = await _db.Donations
            .Include(d => d.OfflinePayment).Include(d => d.Campaign)
            .FirstOrDefaultAsync(d => d.Id == donationId, ct);
        if (donation?.OfflinePayment is null || donation.OfflinePayment.ReviewStatus != OfflineReviewStatus.Pending)
            return false;

        donation.OfflinePayment.ReviewStatus = OfflineReviewStatus.Approved;
        donation.OfflinePayment.ReviewedBy = reviewer;
        donation.OfflinePayment.ReviewedAt = _clock.UtcNow;
        donation.OfflinePayment.ReviewNote = note;
        donation.Status = PaymentStatus.Succeeded;
        donation.CompletedAt = _clock.UtcNow;

        await ApplySuccessSideEffectsAsync(donation, ct);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RejectOfflineAsync(long donationId, string? reviewer, string? note, CancellationToken ct = default)
    {
        var donation = await _db.Donations.Include(d => d.OfflinePayment)
            .FirstOrDefaultAsync(d => d.Id == donationId, ct);
        if (donation?.OfflinePayment is null || donation.OfflinePayment.ReviewStatus != OfflineReviewStatus.Pending)
            return false;

        donation.OfflinePayment.ReviewStatus = OfflineReviewStatus.Rejected;
        donation.OfflinePayment.ReviewedBy = reviewer;
        donation.OfflinePayment.ReviewedAt = _clock.UtcNow;
        donation.OfflinePayment.ReviewNote = note;
        donation.Status = PaymentStatus.Failed;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RefundAsync(long donationId, string? by, CancellationToken ct = default)
    {
        var donation = await _db.Donations
            .Include(d => d.Campaign).Include(d => d.Payment)
            .FirstOrDefaultAsync(d => d.Id == donationId, ct);
        if (donation is null || donation.Status != PaymentStatus.Succeeded) return false;

        donation.Status = PaymentStatus.Refunded;
        if (donation.Payment is not null) donation.Payment.Status = PaymentStatus.Refunded;

        // بازگردانی اثر روی کمپین و حامی.
        if (donation.Campaign is not null)
        {
            donation.Campaign.CollectedAmount = Math.Max(0, donation.Campaign.CollectedAmount - donation.Amount);
            donation.Campaign.SupporterCount = Math.Max(0, donation.Campaign.SupporterCount - 1);
        }
        var donor = donation.DonorId is null ? null
            : await _db.Donors.FirstOrDefaultAsync(d => d.Id == donation.DonorId, ct);
        if (donor is not null)
        {
            donor.TotalDonated = Math.Max(0, donor.TotalDonated - donation.Amount);
            donor.DonationCount = Math.Max(0, donor.DonationCount - 1);
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── کمکی ─────────────────────────────────────────────────────
    private async Task ApplySuccessSideEffectsAsync(Donation donation, CancellationToken ct)
    {
        // Upsert حامی بر اساس موبایل.
        var donor = await _db.Donors.FirstOrDefaultAsync(d => d.Mobile == donation.DonorMobile, ct);
        if (donor is null)
        {
            donor = new Donor
            {
                FullName = donation.DonorName,
                Mobile = donation.DonorMobile,
                Email = donation.DonorEmail,
                FirstDonationAt = _clock.UtcNow,
            };
            _db.Donors.Add(donor);
        }
        donor.TotalDonated += donation.Amount;
        donor.DonationCount += 1;
        donor.LastDonationAt = _clock.UtcNow;
        donation.Donor = donor;

        // به‌روزرسانی مبلغ و تعداد حامیان کمپین.
        if (donation.Campaign is not null)
        {
            donation.Campaign.CollectedAmount += donation.Amount;
            donation.Campaign.SupporterCount += 1;
        }
    }

    private async Task<Campaign?> ValidateCampaignAsync(long? campaignId, decimal amount, CancellationToken ct)
    {
        if (campaignId is null) return null;
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null || campaign.Status != CampaignStatus.Active) return null;
        return campaign;
    }

    private async Task<string> GenerateTrackingAsync(CancellationToken ct)
    {
        while (true)
        {
            var code = "HK" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await _db.Donations.AnyAsync(d => d.TrackingCode == code, ct))
                return code;
        }
    }
}
