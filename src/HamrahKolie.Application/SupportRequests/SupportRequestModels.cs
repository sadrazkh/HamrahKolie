using System.ComponentModel.DataAnnotations;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.SupportRequests;

/// <summary>ورودی ثبت اولیه درخواست حمایت (فرم عمومی).</summary>
public class SupportRequestInput
{
    [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید.")]
    [StringLength(150)]
    public string ApplicantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید با ۰۹ شروع شود و ۱۱ رقم باشد.")]
    public string Mobile { get; set; } = string.Empty;

    [StringLength(10, MinimumLength = 10, ErrorMessage = "کد ملی باید ۱۰ رقم باشد.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "کد ملی باید ۱۰ رقم باشد.")]
    public string? NationalId { get; set; }

    public string? Province { get; set; }
    public string? City { get; set; }
    public string? Village { get; set; }

    public string? TreatmentCenter { get; set; }
    public string? ReferredBy { get; set; }
    public DialysisType DialysisType { get; set; } = DialysisType.Unknown;
    public int? SessionsPerWeek { get; set; }
    public string? NeedType { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? InsuranceStatus { get; set; }
    public int? HouseholdSize { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "برای ثبت درخواست، رضایت پردازش اطلاعات الزامی است.")]
    public bool DataProcessingConsent { get; set; }

    public bool PublicDisclosureConsent { get; set; }

    /// <summary>در صورت ثبت از پورتال بیمارستان، شناسه مرکز معرفی‌کننده.</summary>
    public long? ReferringCenterId { get; set; }
}
