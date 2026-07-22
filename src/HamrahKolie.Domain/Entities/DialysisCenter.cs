using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>یک مرکز دیالیز در دایرکتوری مراکز.</summary>
public class DialysisCenter : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public CenterType Type { get; set; }

    public string? Province { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Phone { get; set; }
    public string? WorkingHours { get; set; }
    public string? Services { get; set; }
    public string? Facilities { get; set; }
    public string? DialysisTypes { get; set; }
    public string? AccessibilityNotes { get; set; }

    public string? Website { get; set; }

    /// <summary>آیا توسط ادمین تأیید شده و برای عموم نمایش داده می‌شود؟</summary>
    public bool IsApproved { get; set; }

    /// <summary>آیا توسط کاربر عمومی پیشنهاد شده است؟</summary>
    public bool SubmittedByPublic { get; set; }

    public DateTime? LastReviewedAt { get; set; }

    /// <summary>امکانات فعال پورتال این مرکز (bitmask). توسط مدیر سامانه تعیین می‌شود.</summary>
    public HospitalFeature Features { get; set; } = HospitalFeature.Default;

    /// <summary>سقف ثبت بیمار در ماه برای این مرکز (خالی = بدون محدودیت).</summary>
    public int? MonthlyPatientQuota { get; set; }

    /// <summary>آیا امکان مشخص فعال است؟</summary>
    public bool Has(HospitalFeature feature) => (Features & feature) == feature;
}
