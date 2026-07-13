namespace HamrahKolie.Domain.Enums;

/// <summary>کانال اطلاع‌رسانی.</summary>
public enum NotificationChannel
{
    InApp = 0,   // پیام داخل سیستم
    Email = 1,
    Sms = 2,
}

/// <summary>وضعیت تحویل پیام.</summary>
public enum NotificationDeliveryStatus
{
    Queued = 0,
    Sent = 1,
    Failed = 2,
    Skipped = 3, // Provider تنظیم نشده
}

/// <summary>نوع فیلد فرم در فرم‌ساز.</summary>
public enum FormFieldType
{
    Text = 0,
    Textarea = 1,
    Number = 2,
    Mobile = 3,
    Email = 4,
    Date = 5,
    Select = 6,
    Radio = 7,
    Checkbox = 8,
    Consent = 9,   // چک‌باکس پذیرش قوانین (الزامی)
}
