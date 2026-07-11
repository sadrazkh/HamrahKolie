using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Cms;

/// <summary>منطق کسب‌وکار محتوا (CMS) برای پنل و بخش عمومی.</summary>
public interface IContentService
{
    // ── پنل مدیریت ───────────────────────────────────────────────
    Task<PagedResult<ContentListItemDto>> GetAdminListAsync(
        ContentType? type, string? search, ContentStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task<ContentEditDto?> GetForEditAsync(long id, CancellationToken ct = default);

    Task<long> CreateAsync(ContentEditInput input, string? authorId, CancellationToken ct = default);

    Task<bool> UpdateAsync(long id, ContentEditInput input, CancellationToken ct = default);

    Task<bool> SetStatusAsync(long id, ContentStatus status, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default);

    // ── بخش عمومی ────────────────────────────────────────────────
    Task<Content?> GetPublishedBySlugAsync(ContentType type, string slug, CancellationToken ct = default);

    Task<IReadOnlyList<Content>> GetLatestPublishedAsync(ContentType type, int count, CancellationToken ct = default);

    Task<PagedResult<Content>> GetPublishedListAsync(
        ContentType type, string? categorySlug, string? tagSlug, int page, int pageSize, CancellationToken ct = default);

    /// <summary>همه محتوای منتشرشده برای Sitemap.</summary>
    Task<IReadOnlyList<Content>> GetAllPublishedForSitemapAsync(CancellationToken ct = default);
}
