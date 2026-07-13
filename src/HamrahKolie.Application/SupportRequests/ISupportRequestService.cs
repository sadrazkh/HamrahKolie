using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.SupportRequests;

/// <summary>گردش‌کار درخواست حمایت (عمومی و مدیریت).</summary>
public interface ISupportRequestService
{
    // ── عمومی ────────────────────────────────────────────────────
    /// <summary>ثبت اولیه درخواست؛ کد پیگیری بازگردانده می‌شود.</summary>
    Task<string> SubmitAsync(SupportRequestInput input, string consentVersion, CancellationToken ct = default);

    /// <summary>یافتن درخواست با کد پیگیری و موبایل (پس از تأیید OTP).</summary>
    Task<SupportRequest?> GetForApplicantAsync(string trackingCode, string mobile, CancellationToken ct = default);

    /// <summary>ثبت پاسخ متقاضی (پیام) روی درخواست.</summary>
    Task<bool> AddApplicantMessageAsync(long requestId, string mobile, string body, CancellationToken ct = default);

    // ── مدیریت ───────────────────────────────────────────────────
    Task<PagedResult<SupportRequest>> GetAdminListAsync(
        SupportRequestStatus? status, RequestPriority? priority, string? assignedToUserId, string? search,
        int page, int pageSize, CancellationToken ct = default);

    Task<SupportRequest?> GetAdminDetailAsync(long id, CancellationToken ct = default);

    Task<int> GetOpenCountAsync(CancellationToken ct = default);

    /// <summary>تغییر وضعیت با ثبت در تاریخچه.</summary>
    Task<bool> ChangeStatusAsync(long id, SupportRequestStatus newStatus, string? note, CancellationToken ct = default);

    Task<bool> AssignAsync(long id, string? userId, CancellationToken ct = default);

    Task<bool> SetPriorityAsync(long id, RequestPriority priority, CancellationToken ct = default);

    Task<bool> AddNoteAsync(long id, string body, MessageVisibility visibility, CancellationToken ct = default);

    /// <summary>یافتن درخواست‌های مشکوک به تکراری بودن (بر اساس موبایل/کد ملی).</summary>
    Task<IReadOnlyList<SupportRequest>> FindPossibleDuplicatesAsync(long id, CancellationToken ct = default);
}
