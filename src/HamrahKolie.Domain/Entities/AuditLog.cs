namespace HamrahKolie.Domain.Entities;

/// <summary>
/// ثبت رویدادهای حساس امنیتی و مدیریتی. این جدول توسط مدیران معمولی قابل حذف نیست
/// و فقط عملیات درج روی آن انجام می‌شود (Append-only).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>زمان رویداد (UTC).</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>نوع عملیات (مثل «Login»، «Setting.Update»، «Donation.Refund»).</summary>
    public string Action { get; set; } = default!;

    /// <summary>نوع موجودیت هدف (مثل «Setting»، «Campaign»).</summary>
    public string? EntityType { get; set; }

    /// <summary>شناسه موجودیت هدف.</summary>
    public string? EntityId { get; set; }

    /// <summary>شناسه کاربر انجام‌دهنده.</summary>
    public string? UserId { get; set; }

    /// <summary>نام کاربر انجام‌دهنده (برای نمایش سریع).</summary>
    public string? UserName { get; set; }

    /// <summary>آدرس IP (در صورت مجاز بودن ثبت).</summary>
    public string? IpAddress { get; set; }

    /// <summary>User Agent مرورگر.</summary>
    public string? UserAgent { get; set; }

    /// <summary>توضیح خوانا از رویداد (فارسی).</summary>
    public string? Description { get; set; }

    /// <summary>داده تکمیلی به‌صورت JSON (بدون اطلاعات حساس).</summary>
    public string? MetadataJson { get; set; }
}
