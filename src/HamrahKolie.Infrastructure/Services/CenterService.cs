using HamrahKolie.Application.Centers;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class CenterService : ICenterService
{
    private readonly ApplicationDbContext _db;
    private readonly ISlugService _slug;
    private readonly IOutputCacheInvalidator _cache;

    public CenterService(ApplicationDbContext db, ISlugService slug, IOutputCacheInvalidator cache)
    {
        _db = db;
        _slug = slug;
        _cache = cache;
    }

    // ── عمومی ────────────────────────────────────────────────────
    public async Task<PagedResult<DialysisCenter>> GetApprovedListAsync(
        string? province, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.DialysisCenters.AsNoTracking().Where(c => c.IsApproved);
        if (!string.IsNullOrWhiteSpace(province)) q = q.Where(c => c.Province == province);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search) || (c.City != null && c.City.Contains(search)));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(c => c.Province).ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<DialysisCenter> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<DialysisCenter?> GetApprovedBySlugAsync(string slug, CancellationToken ct = default)
        => _db.DialysisCenters.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug && c.IsApproved, ct);

    public async Task<IReadOnlyList<string>> GetProvincesAsync(CancellationToken ct = default)
        => await _db.DialysisCenters.AsNoTracking()
            .Where(c => c.IsApproved && c.Province != null)
            .Select(c => c.Province!).Distinct().OrderBy(p => p).ToListAsync(ct);

    public async Task<long> SubmitPublicAsync(CenterInput input, CancellationToken ct = default)
    {
        var c = new DialysisCenter { SubmittedByPublic = true, IsApproved = false };
        await MapAsync(c, input, isNew: true, ct);
        _db.DialysisCenters.Add(c);
        await _db.SaveChangesAsync(ct);
        return c.Id;
    }

    // ── مدیریت ───────────────────────────────────────────────────
    public async Task<PagedResult<DialysisCenter>> GetAdminListAsync(
        bool? approved, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.DialysisCenters.AsNoTracking().AsQueryable();
        if (approved is not null) q = q.Where(c => c.IsApproved == approved);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<DialysisCenter> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<DialysisCenter?> GetAsync(long id, CancellationToken ct = default)
        => _db.DialysisCenters.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
        => _db.DialysisCenters.CountAsync(c => !c.IsApproved, ct);

    public async Task<long> CreateAsync(CenterInput input, bool approved, CancellationToken ct = default)
    {
        var c = new DialysisCenter { IsApproved = approved, LastReviewedAt = approved ? DateTime.UtcNow : null };
        await MapAsync(c, input, isNew: true, ct);
        _db.DialysisCenters.Add(c);
        await _db.SaveChangesAsync(ct);
        return c.Id;
    }

    public async Task<bool> UpdateAsync(CenterInput input, CancellationToken ct = default)
    {
        var c = await _db.DialysisCenters.FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (c is null) return false;
        await MapAsync(c, input, isNew: false, ct);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateAsync("content", ct);
        return true;
    }

    public async Task<bool> SetApprovalAsync(long id, bool approved, CancellationToken ct = default)
    {
        var c = await _db.DialysisCenters.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return false;
        c.IsApproved = approved;
        c.LastReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateAsync("content", ct);
        return true;
    }

    public async Task<bool> SetFeaturesAsync(long id, Domain.Enums.HospitalFeature features, int? monthlyQuota, CancellationToken ct = default)
    {
        var c = await _db.DialysisCenters.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return false;
        c.Features = features;
        c.MonthlyPatientQuota = monthlyQuota is > 0 ? monthlyQuota : null;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var c = await _db.DialysisCenters.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return false;
        _db.DialysisCenters.Remove(c);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task MapAsync(DialysisCenter c, CenterInput input, bool isNew, CancellationToken ct)
    {
        c.Name = input.Name.Trim();
        c.Type = input.Type;
        c.Province = input.Province?.Trim();
        c.City = input.City?.Trim();
        c.Address = input.Address?.Trim();
        c.Latitude = input.Latitude;
        c.Longitude = input.Longitude;
        c.Phone = input.Phone?.Trim();
        c.WorkingHours = input.WorkingHours?.Trim();
        c.Services = input.Services?.Trim();
        c.Facilities = input.Facilities?.Trim();
        c.DialysisTypes = input.DialysisTypes?.Trim();
        c.AccessibilityNotes = input.AccessibilityNotes?.Trim();
        c.Website = input.Website?.Trim();
        c.Features = input.Features;
        c.MonthlyPatientQuota = input.MonthlyPatientQuota is > 0 ? input.MonthlyPatientQuota : null;

        var baseSlug = _slug.Generate(input.Name);
        if (isNew || string.IsNullOrEmpty(c.Slug))
        {
            c.Slug = await _slug.GenerateUniqueAsync(baseSlug,
                candidate => _db.DialysisCenters.AnyAsync(x => x.Slug == candidate && x.Id != c.Id, ct), ct);
        }
    }
}
