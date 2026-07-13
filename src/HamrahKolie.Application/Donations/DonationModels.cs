using System.ComponentModel.DataAnnotations;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Donations;

/// <summary>ورودی فرم کمک مالی (آنلاین و آفلاین).</summary>
public class DonationInput
{
    [Range(1000, 1_000_000_000, ErrorMessage = "مبلغ کمک نامعتبر است.")]
    public decimal Amount { get; set; }

    public DonationType Type { get; set; } = DonationType.General;

    public long? CampaignId { get; set; }

    [Required(ErrorMessage = "نام را وارد کنید.")]
    [StringLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید با ۰۹ شروع شود و ۱۱ رقم باشد.")]
    public string DonorMobile { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل نامعتبر است.")]
    public string? DonorEmail { get; set; }

    public bool IsAnonymous { get; set; }
    public bool ShowNamePublicly { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "پذیرش قوانین الزامی است.")]
    public bool AcceptTerms { get; set; }
}

/// <summary>ورودی ثبت پرداخت آفلاین (فیش).</summary>
public class OfflineDonationInput : DonationInput
{
    public long? ReceiptImageId { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }
}

/// <summary>نتیجه ایجاد کمک آنلاین.</summary>
public record CreateOnlineResult(bool Success, string? TrackingCode, string? RedirectUrl, string? Error)
{
    public static CreateOnlineResult Ok(string trackingCode, string redirectUrl) => new(true, trackingCode, redirectUrl, null);
    public static CreateOnlineResult Fail(string error) => new(false, null, null, error);
}

/// <summary>نتیجه پردازش بازگشت از درگاه.</summary>
public record CallbackResult(bool Success, string? TrackingCode, string? Error);
