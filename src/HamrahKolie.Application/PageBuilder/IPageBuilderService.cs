using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Application.PageBuilder;

/// <summary>خواندن سکشن‌های صفحه‌ساز برای نمایش عمومی و پیش‌نمایش مدیریت.</summary>
public interface IPageBuilderService
{
    /// <summary>سکشن‌های قابل نمایش عمومی (فعال و منتشرشده)، مرتب و Cache‌شده.</summary>
    Task<IReadOnlyList<PageSection>> GetVisibleAsync(string pageKey, CancellationToken ct = default);

    /// <summary>همه سکشن‌های فعال برای پیش‌نمایش مدیریت (شامل پیش‌نویس‌ها).</summary>
    Task<IReadOnlyList<PageSection>> GetEnabledForPreviewAsync(string pageKey, CancellationToken ct = default);

    /// <summary>آیا برای این صفحه در صفحه‌ساز داده‌ای وجود دارد؛ حتی اگر همه بخش‌ها غیرفعال باشند.</summary>
    Task<bool> HasSectionsAsync(string pageKey, CancellationToken ct = default);

    void InvalidateCache(string pageKey);
}
