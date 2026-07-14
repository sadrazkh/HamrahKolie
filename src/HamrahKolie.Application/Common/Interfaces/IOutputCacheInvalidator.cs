namespace HamrahKolie.Application.Common.Interfaces;

/// <summary>باطل‌کردن کش خروجی صفحات عمومی بر اساس تگ (پس از انتشار/تغییر محتوا).</summary>
public interface IOutputCacheInvalidator
{
    Task InvalidateAsync(string tag, CancellationToken ct = default);
}
