using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>
/// درخواست حمایت یک بیمار. اطلاعات حساس (کد ملی، مدارک) با کنترل دسترسی سطح‌فیلد
/// نمایش داده می‌شوند. ثبت اولیه بدون نیاز به حساب کاربری انجام می‌شود.
/// </summary>
public class SupportRequest : BaseEntity
{
    /// <summary>کد پیگیری یکتا.</summary>
    public string TrackingCode { get; set; } = default!;

    public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Submitted;
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;

    // ── هویت و تماس (بخشی حساس) ─────────────────────────────────
    public string ApplicantName { get; set; } = default!;
    public string Mobile { get; set; } = default!;
    /// <summary>کد ملی — اطلاعات حساس.</summary>
    public string? NationalId { get; set; }

    // ── مکان ────────────────────────────────────────────────────
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? Village { get; set; }

    // ── درمان ───────────────────────────────────────────────────
    public string? TreatmentCenter { get; set; }
    public string? ReferredBy { get; set; }
    public DialysisType DialysisType { get; set; } = DialysisType.Unknown;
    public int? SessionsPerWeek { get; set; }
    public string? NeedType { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? InsuranceStatus { get; set; }
    public int? HouseholdSize { get; set; }
    public string? Description { get; set; }

    // ── رضایت‌نامه‌ها ────────────────────────────────────────────
    /// <summary>رضایت پردازش اطلاعات (الزامی).</summary>
    public bool DataProcessingConsent { get; set; }
    public DateTime? ConsentedAt { get; set; }
    public string? ConsentVersion { get; set; }
    /// <summary>رضایت انتشار عمومی (اختیاری و جداگانه).</summary>
    public bool PublicDisclosureConsent { get; set; }

    // ── مدیریت ───────────────────────────────────────────────────
    public string? AssignedToUserId { get; set; }
    public string? Tags { get; set; }

    public ICollection<SupportRequestDocument> Documents { get; set; } = new List<SupportRequestDocument>();
    public ICollection<SupportRequestStatusHistory> History { get; set; } = new List<SupportRequestStatusHistory>();
    public ICollection<SupportRequestMessage> Messages { get; set; } = new List<SupportRequestMessage>();
}

/// <summary>مدرک پیوست‌شده به درخواست.</summary>
public class SupportRequestDocument : BaseEntity
{
    public long SupportRequestId { get; set; }
    public SupportRequest SupportRequest { get; set; } = default!;

    public string Title { get; set; } = default!;
    public long? MediaFileId { get; set; }
    public MediaFile? MediaFile { get; set; }

    /// <summary>آیا توسط متقاضی بارگذاری شده؟ (در برابر کارشناس)</summary>
    public bool UploadedByApplicant { get; set; }
}

/// <summary>تاریخچه تغییر وضعیت درخواست.</summary>
public class SupportRequestStatusHistory : BaseEntity
{
    public long SupportRequestId { get; set; }
    public SupportRequest SupportRequest { get; set; } = default!;

    public SupportRequestStatus? FromStatus { get; set; }
    public SupportRequestStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public string? ChangedByUserId { get; set; }
    public string? ChangedByName { get; set; }
}

/// <summary>پیام یا یادداشت مرتبط با درخواست (داخلی یا برای متقاضی).</summary>
public class SupportRequestMessage : BaseEntity
{
    public long SupportRequestId { get; set; }
    public SupportRequest SupportRequest { get; set; } = default!;

    public MessageVisibility Visibility { get; set; }
    public string Body { get; set; } = default!;

    /// <summary>آیا از سوی متقاضی ارسال شده؟ (در پاسخ به کارشناس)</summary>
    public bool IsFromApplicant { get; set; }

    public string? AuthorUserId { get; set; }
    public string? AuthorName { get; set; }
}
