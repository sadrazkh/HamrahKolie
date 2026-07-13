using HamrahKolie.Application.Payments;

namespace HamrahKolie.Infrastructure.Payments;

/// <summary>
/// درگاه پرداخت آزمایشی برای محیط توسعه. کاربر را به یک صفحه شبیه‌سازی داخلی هدایت می‌کند
/// که در آن می‌تواند «پرداخت موفق» یا «ناموفق» را انتخاب کند. هیچ پول واقعی جابه‌جا نمی‌شود.
/// ساختار این کلاس با درگاه‌های واقعی سازگار است: Request → Redirect → Callback → Verify.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public string Name => "Fake";

    public Task<PaymentInitiationResult> RequestAsync(PaymentInitiation initiation, CancellationToken ct = default)
    {
        // در درگاه واقعی، Authority از سرویس بانک گرفته می‌شود. اینجا محلی تولید می‌شود.
        var authority = "FAKE-" + Guid.NewGuid().ToString("N");
        var redirectUrl = $"/payment/simulate?authority={Uri.EscapeDataString(authority)}";
        return Task.FromResult(PaymentInitiationResult.Ok(authority, redirectUrl));
    }

    public Task<PaymentVerificationResult> VerifyAsync(PaymentVerification verification, CancellationToken ct = default)
    {
        // موفقیت زمانی است که صفحه شبیه‌سازی وضعیت OK بازگردانده باشد.
        var ok = verification.CallbackParams.TryGetValue("status", out var status)
                 && string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase);

        if (ok)
        {
            var refId = "REF-" + DateTime.UtcNow.Ticks.ToString()[^10..];
            return Task.FromResult(PaymentVerificationResult.Ok(refId, "{\"result\":\"ok\",\"gateway\":\"fake\"}"));
        }

        return Task.FromResult(PaymentVerificationResult.Fail("پرداخت توسط کاربر لغو یا ناموفق شد.", "{\"result\":\"nok\"}"));
    }
}
