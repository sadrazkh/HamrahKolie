using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Notifications;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly IDateTimeProvider _clock;

    public NotificationService(ApplicationDbContext db, IEmailSender email, ISmsSender sms, IDateTimeProvider clock)
    {
        _db = db;
        _email = email;
        _sms = sms;
        _clock = clock;
    }

    public async Task NotifyStaffAsync(string title, string? message = null, string? url = null, CancellationToken ct = default)
    {
        // کارکنان = کاربرانی که حداقل یک نقش دارند.
        var staffIds = await _db.UserRoles.Select(ur => ur.UserId).Distinct().ToListAsync(ct);
        if (staffIds.Count == 0) return;

        foreach (var uid in staffIds)
        {
            _db.Notifications.Add(new Notification
            {
                RecipientUserId = uid,
                Title = title,
                Message = message,
                Url = url,
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        => _db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct);

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int take, CancellationToken ct = default)
        => await _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> GetAllAsync(string userId, int take, CancellationToken ct = default)
        => await _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync(ct);

    public async Task MarkReadAsync(long id, string userId, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId, ct);
        if (n is null || n.IsRead) return;
        n.IsRead = true;
        n.ReadAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in unread) { n.IsRead = true; n.ReadAt = _clock.UtcNow; }
        await _db.SaveChangesAsync(ct);
    }

    public async Task SendTemplatedAsync(NotificationChannel channel, string recipient, string templateKey,
        IReadOnlyDictionary<string, string> tokens, CancellationToken ct = default)
    {
        var template = await _db.NotificationTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == templateKey && t.Channel == channel && t.IsEnabled, ct);

        var log = new NotificationLog
        {
            Channel = channel,
            Recipient = recipient,
            TemplateKey = templateKey,
        };

        if (template is null)
        {
            log.Status = NotificationDeliveryStatus.Skipped;
            log.Error = "قالب فعالی یافت نشد.";
            _db.NotificationLogs.Add(log);
            await _db.SaveChangesAsync(ct);
            return;
        }

        var subject = Render(template.Subject, tokens);
        var body = Render(template.Body, tokens);
        log.Subject = subject;

        try
        {
            switch (channel)
            {
                case NotificationChannel.Email when _email.IsConfigured:
                    await _email.SendAsync(recipient, subject ?? "", body, ct);
                    log.Status = NotificationDeliveryStatus.Sent;
                    log.SentAt = _clock.UtcNow;
                    break;
                case NotificationChannel.Sms when _sms.IsConfigured:
                    await _sms.SendAsync(recipient, body, ct);
                    log.Status = NotificationDeliveryStatus.Sent;
                    log.SentAt = _clock.UtcNow;
                    break;
                default:
                    // Provider واقعی تنظیم نشده؛ در نسخه توسعه فقط لاگ می‌شود.
                    if (channel == NotificationChannel.Email) await _email.SendAsync(recipient, subject ?? "", body, ct);
                    else if (channel == NotificationChannel.Sms) await _sms.SendAsync(recipient, body, ct);
                    log.Status = NotificationDeliveryStatus.Skipped;
                    log.Error = "Provider واقعی تنظیم نشده است (نسخه توسعه).";
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Status = NotificationDeliveryStatus.Failed;
            log.Error = ex.Message;
        }

        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    private static string? Render(string? template, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrEmpty(template)) return template;
        var result = template;
        foreach (var (k, v) in tokens)
            result = result.Replace("{{" + k + "}}", v);
        return result;
    }
}
