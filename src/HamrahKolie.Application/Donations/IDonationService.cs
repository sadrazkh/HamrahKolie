using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Donations;

/// <summary>منطق کمک مالی: ایجاد، تأیید (Idempotent)، پرداخت آفلاین، پیگیری و مدیریت.</summary>
public interface IDonationService
{
    /// <summary>ایجاد کمک آنلاین و شروع پرداخت. آدرس بازگشت (callback) از بیرون داده می‌شود.</summary>
    Task<CreateOnlineResult> CreateOnlineAsync(DonationInput input, string callbackUrl, CancellationToken ct = default);

    /// <summary>پردازش بازگشت از درگاه؛ تأیید سمت سرور و به‌روزرسانی وضعیت به‌صورت Idempotent.</summary>
    Task<CallbackResult> HandleCallbackAsync(string authority, IReadOnlyDictionary<string, string> callbackParams, CancellationToken ct = default);

    /// <summary>ثبت کمک آفلاین (فیش) در وضعیت در انتظار بررسی.</summary>
    Task<string> SubmitOfflineAsync(OfflineDonationInput input, CancellationToken ct = default);

    /// <summary>پیگیری کمک با کد پیگیری و موبایل.</summary>
    Task<Donation?> GetByTrackingAsync(string trackingCode, string mobile, CancellationToken ct = default);

    /// <summary>یافتن کمک با کد پیگیری (برای نمایش رسید بلافاصله پس از پرداخت).</summary>
    Task<Donation?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct = default);

    // ── مدیریت ───────────────────────────────────────────────────
    Task<PagedResult<Donation>> GetAdminListAsync(
        PaymentStatus? status, PaymentMethod? method, long? campaignId, int page, int pageSize, CancellationToken ct = default);

    Task<Donation?> GetAdminDetailAsync(long id, CancellationToken ct = default);

    Task<int> GetPendingOfflineCountAsync(CancellationToken ct = default);

    Task<bool> ApproveOfflineAsync(long donationId, string? reviewer, string? note, CancellationToken ct = default);

    Task<bool> RejectOfflineAsync(long donationId, string? reviewer, string? note, CancellationToken ct = default);

    Task<bool> RefundAsync(long donationId, string? by, CancellationToken ct = default);
}
