using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Notifications;

/// <summary>ارسال‌کننده ایمیل (قابل تعویض؛ در نبود Provider، نسخه توسعه لاگ می‌کند).</summary>
public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>ارسال‌کننده پیامک (قابل تعویض).</summary>
public interface ISmsSender
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string message, CancellationToken ct = default);
}

/// <summary>سرویس یکپارچه اطلاع‌رسانی: پیام داخل‌سیستمی + ایمیل/پیامک بر پایه قالب.</summary>
public interface INotificationService
{
    /// <summary>ارسال پیام داخل‌سیستمی به همه کارکنان.</summary>
    Task NotifyStaffAsync(string title, string? message = null, string? url = null, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetAllAsync(string userId, int take, CancellationToken ct = default);
    Task MarkReadAsync(long id, string userId, CancellationToken ct = default);
    Task MarkAllReadAsync(string userId, CancellationToken ct = default);

    /// <summary>ارسال پیام بر پایه قالب از طریق کانال مشخص (ایمیل/پیامک) + ثبت لاگ.</summary>
    Task SendTemplatedAsync(NotificationChannel channel, string recipient, string templateKey,
        IReadOnlyDictionary<string, string> tokens, CancellationToken ct = default);
}
