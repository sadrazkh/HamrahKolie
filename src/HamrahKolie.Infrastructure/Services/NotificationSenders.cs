using HamrahKolie.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>ارسال‌کننده ایمیل نسخه توسعه: فقط لاگ می‌کند (بدون Provider واقعی).</summary>
public sealed class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;
    public DevEmailSender(ILogger<DevEmailSender> logger) => _logger = logger;

    public bool IsConfigured => false;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL (dev) → {To} | {Subject}", to, subject);
        return Task.CompletedTask;
    }
}

/// <summary>ارسال‌کننده پیامک نسخه توسعه: فقط لاگ می‌کند.</summary>
public sealed class DevSmsSender : ISmsSender
{
    private readonly ILogger<DevSmsSender> _logger;
    public DevSmsSender(ILogger<DevSmsSender> logger) => _logger = logger;

    public bool IsConfigured => false;

    public Task SendAsync(string to, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("SMS (dev) → {To} | {Message}", to, message);
        return Task.CompletedTask;
    }
}
