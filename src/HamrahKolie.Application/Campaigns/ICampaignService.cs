using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Application.Campaigns;

/// <summary>خواندن کمپین‌ها برای بخش عمومی.</summary>
public interface ICampaignService
{
    Task<PagedResult<Campaign>> GetPublishedListAsync(int page, int pageSize, bool urgentFirst = true, CancellationToken ct = default);

    Task<Campaign?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<Campaign>> GetActiveForHomeAsync(int count, CancellationToken ct = default);
}
