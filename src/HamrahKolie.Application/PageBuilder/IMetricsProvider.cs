namespace HamrahKolie.Application.PageBuilder;

/// <summary>یک شاخص داده زنده که می‌تواند در سکشن‌ها به‌صورت {{key}} استفاده شود.</summary>
/// <param name="Key">کلید یکتا (مثل «total_raised»).</param>
/// <param name="Label">برچسب فارسی برای نمایش در انتخابگر.</param>
/// <param name="Value">مقدار عددی (برای انیمیشن شمارنده).</param>
/// <param name="Formatted">مقدار قالب‌بندی‌شده فارسی برای نمایش.</param>
public record SiteMetric(string Key, string Label, decimal Value, string Formatted);

/// <summary>
/// تأمین‌کننده داده‌های زنده سایت برای صفحه‌ساز؛ اجازه می‌دهد اعداد و ارقام واقعی
/// (مبلغ کمک‌ها، تعداد بیماران، داوطلبان، ...) مستقیماً در سکشن‌ها استفاده شوند.
/// </summary>
public interface IMetricsProvider
{
    /// <summary>فهرست همه شاخص‌ها (برای انتخابگر ویرایشگر).</summary>
    Task<IReadOnlyList<SiteMetric>> GetAllAsync(CancellationToken ct = default);

    /// <summary>نگاشت کلید → شاخص (برای جایگزینی توکن‌ها هنگام رندر).</summary>
    Task<IReadOnlyDictionary<string, SiteMetric>> GetMapAsync(CancellationToken ct = default);
}
