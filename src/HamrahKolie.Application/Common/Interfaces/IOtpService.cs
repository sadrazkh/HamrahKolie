namespace HamrahKolie.Application.Common.Interfaces;

/// <summary>نتیجه درخواست کد یک‌بارمصرف.</summary>
/// <param name="Sent">آیا کد ارسال شد؟</param>
/// <param name="DevCode">فقط در محیط توسعه پر می‌شود (چون پیامک واقعی نیست).</param>
public record OtpRequestResult(bool Sent, string? DevCode);

/// <summary>سرویس کد یک‌بارمصرف (OTP) برای احراز موبایل.</summary>
public interface IOtpService
{
    Task<OtpRequestResult> RequestAsync(string purpose, string key, string mobile, CancellationToken ct = default);
    Task<bool> VerifyAsync(string purpose, string key, string code, CancellationToken ct = default);
}

/// <summary>ارسال‌کننده کد (پیامک/…). قابل تعویض؛ در نبود Provider واقعی، نسخه توسعه لاگ می‌کند.</summary>
public interface IOtpSender
{
    /// <summary>آیا این ارسال‌کننده کد را برای نمایش برمی‌گرداند؟ (فقط توسعه)</summary>
    bool RevealsCode { get; }
    Task SendAsync(string mobile, string code, string purpose, CancellationToken ct = default);
}
