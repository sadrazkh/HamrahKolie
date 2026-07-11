using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HamrahKolie.Infrastructure.Services;

public sealed class PageBuilderService : IPageBuilderService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public PageBuilderService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PageSection>> GetVisibleAsync(string pageKey, CancellationToken ct = default)
    {
        return (await _cache.GetOrCreateAsync(CacheKey(pageKey), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _db.PageSections
                .AsNoTracking()
                .Include(s => s.Image)
                .Where(s => s.PageKey == pageKey && s.IsEnabled && s.IsPublished)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);
        }))!;
    }

    public async Task<IReadOnlyList<PageSection>> GetEnabledForPreviewAsync(string pageKey, CancellationToken ct = default)
    {
        return await _db.PageSections
            .AsNoTracking()
            .Include(s => s.Image)
            .Where(s => s.PageKey == pageKey && s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
    }

    public void InvalidateCache(string pageKey) => _cache.Remove(CacheKey(pageKey));

    private static string CacheKey(string pageKey) => $"pagebuilder:{pageKey}";
}
