using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.SupportRequests;

/// <summary>گردش‌کار درخواست حمایت (عمومی و مدیریت).</summary>
public interface ISupportRequestService
{
    // ── عمومی ────────────────────────────────────────────────────
    /// <summary>ثبت اولیه درخواست؛ شناسه و کد پیگیری بازگردانده می‌شود.</summary>
    Task<(long Id, string TrackingCode)> SubmitAsync(SupportRequestInput input, string consentVersion, CancellationToken ct = default);

    /// <summary>یافتن درخواست با کد پیگیری و موبایل (پس از تأیید OTP).</summary>
    Task<SupportRequest?> GetForApplicantAsync(string trackingCode, string mobile, CancellationToken ct = default);

    /// <summary>ثبت پاسخ متقاضی (پیام) روی درخواست.</summary>
    Task<bool> AddApplicantMessageAsync(long requestId, string mobile, string body, CancellationToken ct = default);

    /// <summary>افزودن مدرک به درخواست (فایل قبلاً در رسانه ذخیره شده و شناسه آن داده می‌شود).</summary>
    Task<bool> AddDocumentAsync(long requestId, long mediaFileId, string title, bool uploadedByApplicant, CancellationToken ct = default);

    /// <summary>افزودن مدرک توسط متقاضی با بررسی مالکیت (موبایل).</summary>
    Task<bool> AddApplicantDocumentAsync(long requestId, string mobile, long mediaFileId, string title, CancellationToken ct = default);

    // ── پورتال مرکز درمانی ───────────────────────────────────────
    Task<PagedResult<SupportRequest>> GetForCenterAsync(long centerId, int page, int pageSize, CancellationToken ct = default);
    Task<SupportRequest?> GetForCenterDetailAsync(long id, long centerId, CancellationToken ct = default);

    /// <summary>آمار بیماران معرفی‌شده توسط یک مرکز (برای داشبورد پورتال).</summary>
    Task<CenterPatientStats> GetCenterStatsAsync(long centerId, CancellationToken ct = default);

    /// <summary>همهٔ بیماران یک مرکز (بدون صفحه‌بندی) برای خروجی گرفتن.</summary>
    Task<IReadOnlyList<SupportRequest>> GetAllForCenterAsync(long centerId, CancellationToken ct = default);

    /// <summary>ثبت پیام مرکز درمانی روی بیمار (نمایان برای کارشناسان خیریه).</summary>
    Task<bool> AddCenterMessageAsync(long requestId, long centerId, string authorName, string body, CancellationToken ct = default);

    /// <summary>ویرایش اطلاعات بیمار توسط مرکز معرفی‌کننده.</summary>
    Task<bool> UpdateForCenterAsync(long id, long centerId, SupportRequestInput input, CancellationToken ct = default);

    /// <summary>تعداد بیماران ثبت‌شدهٔ مرکز در ماه جاری (برای بررسی سقف).</summary>
    Task<int> CountCenterThisMonthAsync(long centerId, CancellationToken ct = default);

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
