using System.Security.Cryptography;
using HamrahKolie.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>سرویس OTP مبتنی بر Cache حافظه‌ای با انقضای کوتاه.</summary>
public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(3);
    private readonly IMemoryCache _cache;
    private readonly IOtpSender _sender;

    public OtpService(IMemoryCache cache, IOtpSender sender)
    {
        _cache = cache;
        _sender = sender;
    }

    public async Task<OtpRequestResult> RequestAsync(string purpose, string key, string mobile, CancellationToken ct = default)
    {
        var code = RandomNumberGenerator.GetInt32(10000, 99999).ToString();
        _cache.Set(CacheKey(purpose, key), code, Ttl);
        await _sender.SendAsync(mobile, code, purpose, ct);
        return new OtpRequestResult(true, _sender.RevealsCode ? code : null);
    }

    public Task<bool> VerifyAsync(string purpose, string key, string code, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey(purpose, key), out string? expected) &&
            !string.IsNullOrEmpty(expected) &&
            string.Equals(expected, code?.Trim(), StringComparison.Ordinal))
        {
            _cache.Remove(CacheKey(purpose, key)); // یک‌بارمصرف
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private static string CacheKey(string purpose, string key) => $"otp:{purpose}:{key}";
}

/// <summary>
/// ارسال‌کننده OTP نسخه توسعه: کد را لاگ می‌کند و برای نمایش برمی‌گرداند.
/// در Production باید با یک Provider واقعی پیامک جایگزین شود.
/// </summary>
public sealed class DevOtpSender : IOtpSender
{
    private readonly ILogger<DevOtpSender> _logger;
    public DevOtpSender(ILogger<DevOtpSender> logger) => _logger = logger;

    public bool RevealsCode => true;

    public Task SendAsync(string mobile, string code, string purpose, CancellationToken ct = default)
    {
        _logger.LogInformation("OTP [{Purpose}] برای {Mobile}: {Code} (نسخه توسعه — پیامک واقعی ارسال نشد)",
            purpose, MaskMobile(mobile), code);
        return Task.CompletedTask;
    }

    private static string MaskMobile(string mobile)
        => mobile.Length >= 11 ? $"{mobile[..4]}***{mobile[^2..]}" : "***";
}
