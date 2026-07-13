using HamrahKolie.Application.Campaigns;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class CampaignService : ICampaignService
{
    private readonly ApplicationDbContext _db;

    public CampaignService(ApplicationDbContext db) => _db = db;

    private static readonly CampaignStatus[] VisibleStatuses =
        { CampaignStatus.Active, CampaignStatus.Successful, CampaignStatus.Completed };

    public async Task<PagedResult<Campaign>> GetPublishedListAsync(int page, int pageSize, bool urgentFirst = true, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.Campaigns.AsNoTracking()
            .Include(c => c.FeaturedImage)
            .Where(c => VisibleStatuses.Contains(c.Status));

        var total = await q.CountAsync(ct);

        q = urgentFirst
            ? q.OrderByDescending(c => c.IsUrgent).ThenByDescending(c => c.CreatedAt)
            : q.OrderByDescending(c => c.CreatedAt);

        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Campaign> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<Campaign?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Campaigns.AsNoTracking()
            .Include(c => c.FeaturedImage)
            .Include(c => c.Updates.OrderByDescending(u => u.PublishedAt))
            .Where(c => VisibleStatuses.Contains(c.Status))
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<IReadOnlyList<Campaign>> GetActiveForHomeAsync(int count, CancellationToken ct = default)
        => await _db.Campaigns.AsNoTracking()
            .Include(c => c.FeaturedImage)
            .Where(c => c.Status == CampaignStatus.Active)
            .OrderByDescending(c => c.IsUrgent).ThenByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
}
