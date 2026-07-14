using HamrahKolie.Application.Cms;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class ContentService : IContentService
{
    private readonly ApplicationDbContext _db;
    private readonly ISlugService _slug;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly IDateTimeProvider _clock;
    private readonly IOutputCacheInvalidator _cache;

    public ContentService(
        ApplicationDbContext db, ISlugService slug, IHtmlSanitizerService sanitizer, IDateTimeProvider clock,
        IOutputCacheInvalidator cache)
    {
        _db = db;
        _slug = slug;
        _sanitizer = sanitizer;
        _clock = clock;
        _cache = cache;
    }

    public async Task<PagedResult<ContentListItemDto>> GetAdminListAsync(
        ContentType? type, string? search, ContentStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var query = _db.Contents.AsNoTracking().AsQueryable();

        if (type is not null) query = query.Where(c => c.Type == type);
        if (status is not null) query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search) || c.Slug.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContentListItemDto(
                c.Id, c.Type, c.Title, c.Slug, c.Status,
                c.Category != null ? c.Category.Name : null,
                c.PublishedAt, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ContentListItemDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<ContentEditDto?> GetForEditAsync(long id, CancellationToken ct = default)
    {
        var c = await _db.Contents
            .Include(x => x.FeaturedImage)
            .Include(x => x.ContentTags).ThenInclude(ct2 => ct2.Tag)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return null;

        return new ContentEditDto
        {
            Id = c.Id,
            Type = c.Type,
            Title = c.Title,
            Slug = c.Slug,
            Summary = c.Summary,
            Body = c.Body,
            Status = c.Status,
            PublishedAt = c.PublishedAt,
            CategoryId = c.CategoryId,
            FeaturedImageId = c.FeaturedImageId,
            FeaturedImageUrl = c.FeaturedImage?.Url,
            Language = c.Language,
            Tags = string.Join("، ", c.ContentTags.Select(t => t.Tag.Name)),
            MedicalReviewer = c.MedicalReviewer,
            SeoTitle = c.Seo.SeoTitle,
            MetaDescription = c.Seo.MetaDescription,
            CanonicalUrl = c.Seo.CanonicalUrl,
            OgImageUrl = c.Seo.OgImageUrl,
            NoIndex = c.Seo.NoIndex,
        };
    }

    public async Task<long> CreateAsync(ContentEditInput input, string? authorId, CancellationToken ct = default)
    {
        var content = new Content { Type = input.Type, AuthorId = authorId };
        await MapAsync(content, input, isNew: true, ct);
        _db.Contents.Add(content);
        await _db.SaveChangesAsync(ct);
        await SyncTagsAsync(content, input.Tags, ct);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateAsync("content", ct);
        return content.Id;
    }

    public async Task<bool> UpdateAsync(long id, ContentEditInput input, CancellationToken ct = default)
    {
        var content = await _db.Contents
            .Include(c => c.ContentTags)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (content is null) return false;

        await MapAsync(content, input, isNew: false, ct);
        await SyncTagsAsync(content, input.Tags, ct);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateAsync("content", ct);
        return true;
    }

    public async Task<bool> SetStatusAsync(long id, ContentStatus status, CancellationToken ct = default)
    {
        var content = await _db.Contents.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (content is null) return false;

        content.Status = status;
        if (status == ContentStatus.Published && content.PublishedAt is null)
            content.PublishedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateAsync("content", ct);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default)
    {
        var content = await _db.Contents.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (content is null) return false;
        _db.Contents.Remove(content); // به‌واسطه Interceptor به حذف نرم تبدیل می‌شود
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateAsync("content", ct);
        return true;
    }

    // ── عمومی ────────────────────────────────────────────────────
    public async Task<Content?> GetPublishedBySlugAsync(ContentType type, string slug, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        return await _db.Contents
            .AsNoTracking()
            .Include(c => c.FeaturedImage)
            .Include(c => c.Category)
            .Include(c => c.ContentTags).ThenInclude(t => t.Tag)
            .Where(c => c.Type == type && c.Slug == slug
                        && c.Status == ContentStatus.Published
                        && (c.PublishedAt == null || c.PublishedAt <= now))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Content>> GetLatestPublishedAsync(ContentType type, int count, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        return await _db.Contents
            .AsNoTracking()
            .Include(c => c.FeaturedImage)
            .Where(c => c.Type == type && c.Status == ContentStatus.Published
                        && (c.PublishedAt == null || c.PublishedAt <= now))
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Content>> GetPublishedListAsync(
        ContentType type, string? categorySlug, string? tagSlug, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var now = _clock.UtcNow;
        var query = _db.Contents.AsNoTracking()
            .Include(c => c.FeaturedImage)
            .Include(c => c.Category)
            .Where(c => c.Type == type && c.Status == ContentStatus.Published
                        && (c.PublishedAt == null || c.PublishedAt <= now));

        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(c => c.Category != null && c.Category.Slug == categorySlug);
        if (!string.IsNullOrWhiteSpace(tagSlug))
            query = query.Where(c => c.ContentTags.Any(t => t.Tag.Slug == tagSlug));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Content> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<IReadOnlyList<Content>> GetAllPublishedForSitemapAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        return await _db.Contents
            .AsNoTracking()
            .Where(c => c.Status == ContentStatus.Published
                        && !c.Seo.NoIndex
                        && (c.PublishedAt == null || c.PublishedAt <= now))
            .Select(c => new Content
            {
                Id = c.Id, Type = c.Type, Slug = c.Slug,
                UpdatedAt = c.UpdatedAt, CreatedAt = c.CreatedAt, PublishedAt = c.PublishedAt
            })
            .ToListAsync(ct);
    }

    // ── کمکی ─────────────────────────────────────────────────────
    private async Task MapAsync(Content content, ContentEditInput input, bool isNew, CancellationToken ct)
    {
        content.Title = input.Title.Trim();
        content.Summary = input.Summary?.Trim();
        content.Body = _sanitizer.Sanitize(input.Body);
        content.Status = input.Status;
        content.CategoryId = input.CategoryId;
        content.FeaturedImageId = input.FeaturedImageId;
        content.Language = string.IsNullOrWhiteSpace(input.Language) ? "fa" : input.Language;
        content.MedicalReviewer = input.MedicalReviewer?.Trim();

        content.Seo.SeoTitle = input.SeoTitle?.Trim();
        content.Seo.MetaDescription = input.MetaDescription?.Trim();
        content.Seo.CanonicalUrl = input.CanonicalUrl?.Trim();
        content.Seo.OgImageUrl = input.OgImageUrl?.Trim();
        content.Seo.NoIndex = input.NoIndex;

        // نامک: اگر خالی بود از عنوان ساخته می‌شود؛ یکتا بودن در نوع محتوا تضمین می‌شود.
        var desiredSlug = string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug;
        var baseSlug = _slug.Generate(desiredSlug);
        if (isNew || !string.Equals(baseSlug, content.Slug, StringComparison.Ordinal))
        {
            content.Slug = await _slug.GenerateUniqueAsync(baseSlug,
                candidate => _db.Contents.AnyAsync(
                    c => c.Type == content.Type && c.Slug == candidate && c.Id != content.Id, ct),
                ct);
        }

        // زمان انتشار
        if (input.Status == ContentStatus.Published && content.PublishedAt is null)
            content.PublishedAt = input.PublishedAt ?? _clock.UtcNow;
        else if (input.PublishedAt is not null)
            content.PublishedAt = input.PublishedAt;
    }

    private async Task SyncTagsAsync(Content content, string? tagsCsv, CancellationToken ct)
    {
        content.ContentTags.Clear();
        if (string.IsNullOrWhiteSpace(tagsCsv)) return;

        var names = tagsCsv
            .Split(new[] { ',', '،' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .Take(20);

        foreach (var name in names)
        {
            var slug = _slug.Generate(name);
            if (string.IsNullOrEmpty(slug)) continue;

            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Slug == slug, ct);
            if (tag is null)
            {
                tag = new Tag { Name = name, Slug = slug };
                _db.Tags.Add(tag);
                await _db.SaveChangesAsync(ct);
            }
            content.ContentTags.Add(new ContentTag { ContentId = content.Id, TagId = tag.Id });
        }
    }
}
