namespace HamrahKolie.Application.Payments;

/// <summary>درخواست شروع پرداخت به درگاه.</summary>
public record PaymentInitiation(
    long PaymentId,
    decimal Amount,
    string Description,
    string CallbackUrl,
    string? Mobile,
    string? Email);

/// <summary>نتیجه شروع پرداخت.</summary>
public record PaymentInitiationResult(bool Success, string? Authority, string? RedirectUrl, string? Error)
{
    public static PaymentInitiationResult Ok(string authority, string redirectUrl) => new(true, authority, redirectUrl, null);
    public static PaymentInitiationResult Fail(string error) => new(false, null, null, error);
}

/// <summary>درخواست تأیید پرداخت پس از بازگشت از درگاه.</summary>
public record PaymentVerification(
    string Authority,
    decimal Amount,
    IReadOnlyDictionary<string, string> CallbackParams);

/// <summary>نتیجه تأیید پرداخت.</summary>
public record PaymentVerificationResult(bool Success, string? ReferenceId, string? RawResponse, string? Error)
{
    public static PaymentVerificationResult Ok(string referenceId, string? raw = null) => new(true, referenceId, raw, null);
    public static PaymentVerificationResult Fail(string error, string? raw = null) => new(false, null, raw, error);
}

/// <summary>
/// درگاه پرداخت قابل تعویض. پیاده‌سازی آزمایشی برای توسعه؛ ساختار Adapter برای درگاه‌های واقعی ایرانی.
/// تأیید همیشه سمت سرور انجام می‌شود.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>نام درگاه (برای ثبت در Payment.Provider).</summary>
    string Name { get; }

    Task<PaymentInitiationResult> RequestAsync(PaymentInitiation initiation, CancellationToken ct = default);

    Task<PaymentVerificationResult> VerifyAsync(PaymentVerification verification, CancellationToken ct = default);
}
