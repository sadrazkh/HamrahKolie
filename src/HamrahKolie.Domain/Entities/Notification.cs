using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>پیام داخل‌سیستمی برای یک کاربر کارمند.</summary>
public class Notification : BaseEntity
{
    /// <summary>گیرنده (کاربر کارمند).</summary>
    public string RecipientUserId { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Message { get; set; }
    public string? Url { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>قالب پیام قابل مدیریت (برای کانال‌های مختلف و متغیرهای جایگزین).</summary>
public class NotificationTemplate : BaseEntity
{
    /// <summary>کلید یکتای قالب (مثل «donation.success»).</summary>
    public string Key { get; set; } = default!;
    public NotificationChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = default!;
    public string Language { get; set; } = "fa";
    public bool IsEnabled { get; set; } = true;

    /// <summary>توضیح متغیرهای مجاز (برای راهنمای ادمین).</summary>
    public string? AvailableTokens { get; set; }
}

/// <summary>ثبت لاگ ارسال پیام (برای Retry و Reconciliation).</summary>
public class NotificationLog : BaseEntity
{
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = default!;
    public string? TemplateKey { get; set; }
    public string? Subject { get; set; }
    public NotificationDeliveryStatus Status { get; set; }
    public string? Error { get; set; }
    public DateTime? SentAt { get; set; }
}
