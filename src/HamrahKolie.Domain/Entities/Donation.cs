using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>یک حامی (برای گزارش حامیان جدید/تکرارشونده). با شماره موبایل شناسایی می‌شود.</summary>
public class Donor : BaseEntity
{
    public string FullName { get; set; } = default!;
    public string Mobile { get; set; } = default!;
    public string? Email { get; set; }

    public decimal TotalDonated { get; set; }
    public int DonationCount { get; set; }
    public DateTime? FirstDonationAt { get; set; }
    public DateTime? LastDonationAt { get; set; }
}

/// <summary>
/// یک کمک مالی (تصمیم منطقی به کمک). پرداخت آنلاین/آفلاین جداگانه نگه‌داری می‌شود.
/// اطلاعات بانکی حساس ذخیره نمی‌شود.
/// </summary>
public class Donation : BaseEntity
{
    /// <summary>کد پیگیری یکتا (برای کاربر).</summary>
    public string TrackingCode { get; set; } = default!;

    public decimal Amount { get; set; }
    public DonationType Type { get; set; } = DonationType.General;
    public PaymentMethod Method { get; set; } = PaymentMethod.Online;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // اطلاعات حامی (ممکن است ناشناس باشد)
    public string DonorName { get; set; } = default!;
    public string DonorMobile { get; set; } = default!;
    public string? DonorEmail { get; set; }
    public bool IsAnonymous { get; set; }
    public bool ShowNamePublicly { get; set; }
    public string? Note { get; set; }

    public long? DonorId { get; set; }
    public Donor? Donor { get; set; }

    public long? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Payment? Payment { get; set; }
    public OfflinePayment? OfflinePayment { get; set; }
}

/// <summary>پرداخت آنلاین مرتبط با یک کمک (از طریق درگاه).</summary>
public class Payment : BaseEntity
{
    public long DonationId { get; set; }
    public Donation Donation { get; set; } = default!;

    public string Provider { get; set; } = default!;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>شناسه/توکن تراکنش نزد درگاه (Authority).</summary>
    public string? Authority { get; set; }

    /// <summary>کد پیگیری بانک پس از موفقیت (RefId).</summary>
    public string? ReferenceId { get; set; }

    /// <summary>کلید یکتا برای جلوگیری از ثبت/تأیید تکراری (Idempotency).</summary>
    public string IdempotencyKey { get; set; } = default!;

    public DateTime? PaidAt { get; set; }

    /// <summary>پاسخ خام درگاه (بدون اطلاعات حساس) برای Reconciliation.</summary>
    public string? RawResponse { get; set; }
}

/// <summary>پرداخت آفلاین (ثبت فیش) مرتبط با یک کمک.</summary>
public class OfflinePayment : BaseEntity
{
    public long DonationId { get; set; }
    public Donation Donation { get; set; } = default!;

    public long? ReceiptImageId { get; set; }
    public MediaFile? ReceiptImage { get; set; }

    public string? ReferenceNumber { get; set; }
    public string? PaidToAccount { get; set; }

    public OfflineReviewStatus ReviewStatus { get; set; } = OfflineReviewStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
